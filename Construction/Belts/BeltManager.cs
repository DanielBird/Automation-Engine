using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Events;
using Engine.Construction.Maps;
using Engine.Construction.Widgets;
using Engine.GameState;
using Engine.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using ZLinq;

namespace Engine.Construction.Belts
{
    [RequireComponent(typeof(INodeMap))]
    public class BeltManager : MonoBehaviour
    {
        [Header("Setup")]
        private INodeMap _nodeMap;
        [Tooltip("Time between attempts to push all belts forwards")]
        public float tickForwardFrequency = 2f;
        public bool runOnStart = true; 
        [field:SerializeField] public bool Active { get; private set; }
        [SerializeField] private int timeOfLastTick; 

        private HashSet<Belt> _belts = new HashSet<Belt>(); 
        private Dictionary<Belt, List<Belt>> _graph = new();

        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        private EventBinding<NodeTargetEvent> _onNodeTargetChange;

        private CancellationTokenSource _tickTokenSource;
        private Coroutine _tickForwardRoutine; 
        
        private void Awake()
        { 
            _nodeMap = GetComponent<INodeMap>();
            
            _onNodePlaced = new EventBinding<NodePlaced>(OnNodePlaced);
            _onNodeRemoved = new EventBinding<NodeRemoved>(OnNodeRemoved);
            _onNodeTargetChange = new EventBinding<NodeTargetEvent>(OnNodeTargetChange);
            
            EventBus<NodePlaced>.Register(_onNodePlaced);
            EventBus<NodeRemoved>.Register(_onNodeRemoved);
            EventBus<NodeTargetEvent>.Register(_onNodeTargetChange);
        }

        private void OnDisable()
        {
            EventBus<NodePlaced>.Deregister(_onNodePlaced);
            EventBus<NodeRemoved>.Deregister(_onNodeRemoved);
            EventBus<NodeTargetEvent>.Deregister(_onNodeTargetChange);

            ClearToken();
            
            if(_tickForwardRoutine != null)
                StopCoroutine(_tickForwardRoutine);
        }

        private void ClearToken()
        {
            if (_tickTokenSource == null) return;
            _tickTokenSource.Cancel();
            _tickTokenSource.Dispose();
            _tickTokenSource = null;
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

        private void Start()
        {
            if(runOnStart)
                Run();
        }

        [Button]
        public void Run()
        {
            Active = true;
            
            #if UNITASK
            _tickTokenSource = new CancellationTokenSource();
            TickForward().Forget();

            
            #else
            _tickForwardRoutine = StartCoroutine(TickForwardCoroutine()); 

            #endif
        }

        [Button]
        public void Stop()
        {
            Active = false;
            ClearToken();
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
                timeOfLastTick = Mathf.FloorToInt(Time.time);
                await UniTask.WaitForSeconds(tickForwardFrequency, cancellationToken: _tickTokenSource.Token);
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
                timeOfLastTick = Mathf.FloorToInt(Time.time);
                yield return new WaitForSeconds(tickForwardFrequency);
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
                if (!belt.ReadyToShip(out Belt target, out Widget widget)) continue;
                Move move = new Move { Source = belt, Target = target, Widget = widget };

                if (!selectedMoves.TryGetValue(target, out Move best) || move.Source.TimeOfReceipt < best.Source.TimeOfReceipt)
                    selectedMoves[target] = move;
            }

            foreach (Move move in selectedMoves.Values)
            {
                move.Source.Ship(move.Target, move.Widget);
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