using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Construction.Events;
using Construction.Maps;
using Construction.Widgets;
using Cysharp.Threading.Tasks;
using GameState;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities.Events;
using ZLinq;

namespace Construction.Belts
{
    public struct Move
    {
        public Belt Source;
        public Belt Target;
        public Widget Widget; 
    }
    
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
        private Dictionary<Belt, Belt> _graph = new();

        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        private EventBinding<NodeTargetEvent> _onNodeTargetChange;

        private CancellationTokenSource _tickTokenSource; 
        
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

            if (b.TargetNodes.Count == 0) return;
            if (b.TargetNodes[0] is Belt next)
            {
                _graph[b] = next;
                if (DetectsLoop(b))
                    Debug.Log($"Loop detected between {b.name} and {next.name}");
            }
        }
        
        private void RegisterBelt(Belt belt)
        {
            if(!_belts.Add(belt))
                return;

            if (belt.TargetNodes.FirstOrDefault() is Belt next)
            {
                _graph[belt] = next;
                if (DetectsLoop(belt))
                    Debug.LogError($"Loop detected between {belt.name} and {next.name}");
            }
        }
        
        private bool DetectsLoop(Belt start)
        {
            HashSet<Belt> seen = new HashSet<Belt>();
            Belt current = start;
            while (_graph.TryGetValue(current, out Belt next))
            {
                if (next == start) return true;
                if (!seen.Add(next)) return false;  // hit another cycle or end
                current = next;
            }
            return false;
        }

        private void UnregisterBelt(Belt belt)
        {
            if(!_belts.Remove(belt))
                return;
            
            _graph.Remove(belt);

            var connections = _graph
                .AsValueEnumerable()
                .Where(kv => kv.Value == belt)
                .ToList(); 

            foreach (var kvp in connections)
            {
                _graph.Remove(kvp.Key);
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
            _tickTokenSource = new CancellationTokenSource();
            TickForward().Forget();
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
                if (CoreGameState.paused || UiState.uiOpen)
                {
                    await UniTask.NextFrame(cancellationToken: _tickTokenSource.Token);
                }
                
                UpdateTick();
                timeOfLastTick = Mathf.FloorToInt(Time.time);
                await UniTask.WaitForSeconds(tickForwardFrequency, cancellationToken: _tickTokenSource.Token);
            }
        }
        
        private void UpdateTick()
        {
            List<Belt> orderedBelts = _belts
                .AsValueEnumerable()
                .OrderByDescending(GetPathLength)
                .ToList();

            List<Move> moves = new List<Move>(); 
            
            foreach (var belt in orderedBelts)
            {
                if (belt.ReadyToShip(out Belt target, out Widget widget))
                {
                    moves.Add(new Move { Source = belt, Target = target, Widget = widget});
                }
            }

            foreach (Move move in moves)
            {
                move.Source.Ship(move.Target, move.Widget);
            }
        }

        private int GetPathLength(Belt start)
        {
            int length = 0;
            Belt current = start; 
            while (_graph.TryGetValue(current, out var nxt))
            {
                length++;
                current = nxt;
            }
            return length;
        }
    }
}