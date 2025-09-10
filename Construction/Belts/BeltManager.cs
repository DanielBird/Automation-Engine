using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Engine.GameState;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;
using ZLinq;

namespace Engine.Construction.Belts
{
    public class BeltManager
    {
        private ConstructionEngine _engine; 
        
        [Header("Setup")]
        [Tooltip("Time between attempts to push all belts forwards")]
        private readonly float _tickForwardFrequency;
        private readonly bool _runOnStart;

        private bool _active; 
        private bool Active
        {
            get => _active;
            set
            {
                _engine.beltActive = value; 
                _active = value;
            } 
        }

        private HashSet<Belt> _belts = new HashSet<Belt>(); 
        private Dictionary<Belt, List<Belt>> _graph = new();

        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeGroupPlaced> _onNodeGroupPlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        private EventBinding<NodeTargetEvent> _onNodeTargetChange;
        private bool _eventsRegistered; 

        private CancellationTokenSource _tickTokenSource;
        private Coroutine _tickForwardRoutine;

        public BeltManager(ConstructionEngine engine, float tickForwardFrequency, bool runOnStart)
        {
            _engine = engine;
            _tickForwardFrequency = tickForwardFrequency;
            _runOnStart = runOnStart;
            
            RegisterEvents();
        }
        
        private void RegisterEvents()
        { 
            if (_eventsRegistered) return;
            _onNodePlaced = new EventBinding<NodePlaced>(OnNodePlaced);
            _onNodeGroupPlaced = new EventBinding<NodeGroupPlaced>(OnNodeGroupPlaced);
            _onNodeRemoved = new EventBinding<NodeRemoved>(OnNodeRemoved);
            _onNodeTargetChange = new EventBinding<NodeTargetEvent>(OnNodeTargetChange);
            
            EventBus<NodePlaced>.Register(_onNodePlaced);
            EventBus<NodeGroupPlaced>.Register(_onNodeGroupPlaced);
            EventBus<NodeRemoved>.Register(_onNodeRemoved);
            EventBus<NodeTargetEvent>.Register(_onNodeTargetChange);
            
            _eventsRegistered = true;
        }

        public void Disable()
        {
            if (_eventsRegistered)
            {
                EventBus<NodePlaced>.Deregister(_onNodePlaced);
                EventBus<NodeGroupPlaced>.Deregister(_onNodeGroupPlaced);
                EventBus<NodeRemoved>.Deregister(_onNodeRemoved);
                EventBus<NodeTargetEvent>.Deregister(_onNodeTargetChange);
                _eventsRegistered = false;
            }
            
            ClearToken();
            
            if(_tickForwardRoutine != null)
                _engine.StopCoroutine(_tickForwardRoutine);
        }

        private void ClearToken() => CtsCtrl.Clear(ref _tickTokenSource);

        private void OnNodeGroupPlaced(NodeGroupPlaced e)
        {
            foreach (Node node in e.NodeGroup)
            {
                if(node is Belt b)
                    RegisterBelt(b);
            }
        }
        
        private void OnNodePlaced(NodePlaced placedEvent)
        {
            if (placedEvent.Node is Belt b)
                RegisterBelt(b);
        }

        private void OnNodeRemoved(NodeRemoved removedEvent)
        {
            if (removedEvent.Node is Belt b)
                UnregisterBelt(b);
        }

        private void OnNodeTargetChange(NodeTargetEvent targetEvent)
        {
            if (targetEvent.Node is not Belt b)
                return;
            
            _graph.Remove(b);

            List<Belt> nextBelts = b.TargetNodes.OfType<Belt>().ToList();
            if (nextBelts.Count == 0) return; 
            
            _graph[b] = nextBelts;
            
# if UNITY_EDITOR
            if (DetectsLoop(b))
            {
                Debug.Log($"Loop detected on Node Target Change - between {b.name} and {nextBelts[0].name}");
            }
# endif
        }
        
        private void RegisterBelt(Belt belt)
        {
            if(!_belts.Add(belt))
                return;

            List<Belt> nextBelts = belt.TargetNodes.OfType<Belt>().ToList();
            _graph[belt] = nextBelts;

            if (DetectsLoop(belt))
            {
                # if UNITY_EDITOR
                Debug.Log($"Loop detected between {belt.name} and {nextBelts[0].name}");
                # endif
                
                _belts.Remove(belt);
                _graph.Remove(belt); 
            }
        }
        
        private bool DetectsLoop(Belt start)
        {
            HashSet<Belt> visiting = new ();
            HashSet<Belt> visited  = new ();

            bool Dfs(Belt node)
            {
                if (!visiting.Add(node)) return true;   // back-edge
                if (visited.Contains(node)) { visiting.Remove(node); return false; }

                if (_graph.TryGetValue(node, out var outs))
                {
                    foreach (var nxt in outs)
                        if (Dfs(nxt)) return true;
                }

                visiting.Remove(node);
                visited.Add(node);
                return false;
            }

            return Dfs(start);
        }

        private void UnregisterBelt(Belt belt)
        {
            if(!_belts.Remove(belt))
                return;
            
            _graph.Remove(belt);

            // Remove any edges pointing to the removed belt
            foreach (Belt key in _graph.Keys.ToList())
            {
                List<Belt> connectedBelts = _graph[key];
                connectedBelts.RemoveAll(t => t == belt);
                if (connectedBelts.Count == 0) _graph.Remove(key);
            }
        }

        public void Start()
        {
            if(_runOnStart)
                Run();
        }
        
        public void Run()
        {
            Active = true;
            
            #if UNITASK
            _tickTokenSource = new CancellationTokenSource();
            TickForward().Forget();
            #else
            if (_tickForwardRoutine != null) _engine.StopCoroutine(_tickForwardRoutine);
            _tickForwardRoutine = _engine.StartCoroutine(TickForwardCoroutine()); 
            #endif
        }
        
        public void Stop()
        {
            Active = false; 
            ClearToken();
            if (_tickForwardRoutine != null) _engine.StopCoroutine(_tickForwardRoutine);
        }

        private async UniTaskVoid TickForward()
        {
            while (Active)
            {
                if (CoreGameState.Paused || UiState.uiOpen)
                {
                    await UniTask.NextFrame(cancellationToken: _tickTokenSource.Token);
                }
                
                UpdateTick();
                _engine.timeOfLastBeltTick = Mathf.FloorToInt(Time.time);
                await UniTask.WaitForSeconds(_tickForwardFrequency, cancellationToken: _tickTokenSource.Token);
            }
        }

        private IEnumerator TickForwardCoroutine()
        {
            while (Active)
            {
                if (CoreGameState.Paused || UiState.uiOpen)
                {
                    yield return null; 
                }
                
                UpdateTick();
                _engine.timeOfLastBeltTick = Mathf.FloorToInt(Time.time);
                yield return new WaitForSeconds(_tickForwardFrequency);
            }
        } 
        
        private void UpdateTick()
        {
            List<Belt> orderedBelts = _belts
                .AsValueEnumerable()
                .OrderByDescending(GetPathLength)
                .ToList();

            Dictionary<Belt, Move> selectedMoves = new ();

            foreach (Belt belt in orderedBelts)
            {
                if (!belt.ReadyToShip(out Belt target, out Resource widget)) continue;
                Move move = new Move { Source = belt, Target = target, Resource = widget };

                if (!selectedMoves.TryGetValue(target, out Move best) || move.Source.TimeOfReceipt < best.Source.TimeOfReceipt)
                    selectedMoves[target] = move;
            }

            foreach (Move move in selectedMoves.Values)
            {
                move.Source.Ship(move.Target, move.Resource);
            }
        }

        private int GetPathLength(Belt start)
        {
            Dictionary<Belt, int> memo = new();
            HashSet<Belt> visited = new();

            int Dfs(Belt b)
            {
                if (memo.TryGetValue(b, out int v)) return v;
                if (!visited.Add(b)) return 0; // loop detected

                if (!_graph.TryGetValue(b, out List<Belt> outs) || outs.Count == 0)
                {
                    visited.Remove(b);
                    return memo[b] = 0;
                }

                int best = 0;
                foreach (Belt nxt in outs)
                    best = Mathf.Max(best, 1 + Dfs(nxt));

                visited.Remove(b);
                return memo[b] = best;
            }

            return Dfs(start);
        }
    }
}