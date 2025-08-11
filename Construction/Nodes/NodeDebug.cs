using Construction.Events;
using Construction.Placement;
using Construction.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Utilities.Events;

namespace Construction.Nodes
{
    public class NodeDebug : MonoBehaviour
    {
        [field: SerializeField] private Node node; 
        
        public bool showDirection;
        public bool showNeighbours = true;
        public bool showConnected;
        public bool logNewTargets; 
        
        private Vector3 _northGizmo = new Vector3(0, 1, 0.2f); 
        private Vector3 _eastGizmo = new Vector3(0.2f, 1,0); 
        private Vector3 _southGizmo = new Vector3(0, 1, -0.2f); 
        private Vector3 _westGizmo = new Vector3(-0.2f, 1,0);

        private EventBinding<NodeTargetEvent> _nodeTargetEventBinding;
        
        private void Awake()
        {
            if (node == null)
            {
                node = GetComponent<Node>(); 
            }

            _nodeTargetEventBinding = new EventBinding<NodeTargetEvent>(LogTargetNodeUpdate);
            EventBus<NodeTargetEvent>.Register(_nodeTargetEventBinding);
        }

        private void OnDisable()
        {
            EventBus<NodeTargetEvent>.Deregister(_nodeTargetEventBinding);
        }

        private void LogTargetNodeUpdate(NodeTargetEvent e)
        {
            // DURING A DRAG
            // Target nodes are set by the node in front updating the node behind it 
            
            if (!logNewTargets) return;
            if (e.Node != node) return;
            Debug.Log($"{node.name} target node updated to {e.Target.name}");
        }
        
        [Button]
        public void DebugConnection() =>
            Debug.Log(node.Connected() ? "Node is connected" : "Node is NOT connected");
        
        [Button]
        public void DebugLoops()
        {
            bool loops = LoopDetection.WillLoop(node, node.TargetNodes[0]); 
            Debug.Log(loops ? "Will loop" : "Will NOT loop");
        }
        
        [Button]
        public void DebugPotentialLoops(Vector2Int pos)
        {
            if (!node.NodeMap.GetNeighbourAt(pos, out Node target)) return;
            bool loops = LoopDetection.WillBecomeLoopDebug(node, target); 
            Debug.Log(loops ? $"{node.name} and {target.name} will loop" : $"{node.name} and {target.name} will NOT loop");
        }
        
        [Button]
        public void DebugNeighbour(Direction direction) =>  
            Debug.Log($"Has neighbour to the {direction} on {name} is {node.HasNeighbour(direction)}");

        [Button]
        public void WhoIsNeighbour(Direction direction)
        {
            if(!node.TryGetNeighbour(direction, out var neighbour)) Debug.Log("No neighbour found");
            else Debug.Log($"The neighbour to the {direction} is {neighbour.name}");
        }

        private void OnDrawGizmosSelected()
        {
            if (showDirection)
            {
                Vector3 position = transform.position + new Vector3(0, 1, 0); 
                Handles.Label(position, node.Direction.ToString());
            }

            if (showNeighbours)
            {
                Gizmos.color = node.HasNeighbour(Direction.North) ? Color.green : Color.red;
                Gizmos.DrawSphere(transform.position + _northGizmo, 0.05f);
                Gizmos.color = node.HasNeighbour(Direction.East) ? Color.green : Color.red;
                Gizmos.DrawSphere(transform.position + _eastGizmo, 0.05f);
                Gizmos.color = node.HasNeighbour(Direction.South) ? Color.green : Color.red;
                Gizmos.DrawSphere(transform.position + _southGizmo, 0.05f);
                Gizmos.color = node.HasNeighbour(Direction.West) ? Color.green : Color.red;
                Gizmos.DrawSphere(transform.position + _westGizmo, 0.05f);
            }

            if (showConnected)
            {
                Gizmos.color = node.Connected() ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
                Gizmos.DrawCube(transform.position, Vector3.one);
            }
        }
    }
}