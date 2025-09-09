using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Engine.Construction.Events;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Placement;
using Engine.Construction.Visuals;
using Engine.Utilities;
using Engine.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Nodes
{
    /// <summary>
    /// Abstract parent class for everything that is either a connection mechanism (e.g., belt, road)
    /// Or that can be connected to a connection mechanism (e.g., buildings that connect to belts) 
    /// </summary>
    
    [RequireComponent(typeof(NodeVisuals))]
    [Serializable]
    public abstract class Node : MonoBehaviour, IPlaceable, IRotatable, IClickable
    {
        [Header("Setup")] 
        public NodeTypeSo nodeTypeSo;
        [field: SerializeField] public int GridWidth { get; set; }
        [field: SerializeField] public int GridHeight { get; set; }
        [field: SerializeField] public bool Draggable { get; set; }
        [field: SerializeField] public NodeType NodeType { get; private set; }
        [field: SerializeField] public bool ReplaceOnPlacement { get; private set; }
        [field: SerializeField] public int PathId { get; private set; } = -1;
        
        [Header("Position & Rotation")] 
        [SerializeField] private Direction myDirection; 
        public Direction startingDirection; 
        public float rotationTime = 0.2f;
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease rotationEasing = EasingFunctions.Ease.EaseOutSine;
        [field: SerializeField] public Vector3Int GridCoord { get; set; }
        [field: SerializeField] public Vector3Int WorldPosition { get; private set; }
        public Direction Direction
        {
            get => myDirection;
            set => myDirection = value;
        }
        [field: SerializeField] public List<Node> TargetNodes { get; private set; } 
        [field: SerializeField] public NodeVisuals Visuals { get; private set; }
        [field: SerializeField] public bool Initialised { get; private set; }    
        public INodeMap NodeMap { get; protected set; }
        protected NodeRotation NodeRotation;
        public NodeConnections NodeConnections;
        private StringBuilder _nameBuilder = new(64);
        
        // Status
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public bool isRemovable = true; 
        
        [Header("Parents/Children")]
        // Certain GameObjects like Splitters and Combiners are made up of two nodes (a parent and child).
        // The removal manager refers to the parent node to organize the despawning of both nodes. 
        // That way, if the player requests destruction of the child node, both the parent and child are removed. 
        public Node ParentNode { get; protected set; }
        
        // Name
        private static readonly  Regex TailSuffix = new Regex(@"(?: \(\d+\))$|(?:_-?\d+_-?\d+_?$)", RegexOptions.Compiled);

        private void Awake()
        {
            if (nodeTypeSo == null)
            {
                Debug.LogWarning("NodeTypeSo is null");
                nodeTypeSo = ScriptableObject.CreateInstance<NodeTypeSo>();
                nodeTypeSo.width = 1; 
                nodeTypeSo.height = 1;
            }
            
            GridWidth = nodeTypeSo.width;
            GridHeight = nodeTypeSo.height;
            Draggable = nodeTypeSo.draggable;
            _ease = EasingFunctions.GetEasingFunction(rotationEasing);
            if(Visuals == null) Visuals = GetComponent<NodeVisuals>();
            TargetNodes = new List<Node>();
            
            NodeRotation = new NodeRotation(this, rotationTime, _ease);
            NodeConnections = new NodeConnections(this);
        }
        
        public virtual void Place(Vector3Int gridCoord, INodeMap map)
        {
            NodeMap = map; 
            GridCoord = gridCoord;
        }
        
        // Initialise is called when the player releases lmb during a drag
        // Or when the player clicks lmb during a single placement operation
        
        // Note - nodes register themselves with the Node Map
        // But they do not register themselves with Map
        // This must be done at the place of instantiating the node
        
        public virtual void Initialise(NodeConfiguration config)
        {
            WorldPosition = Vector3Int.RoundToInt(transform.position);
            if (config.UpdateDirection) Direction = config.Direction;
            if (config.UpdateRotation) RotateInstant(config.Direction);
            
            NodeMap = config.NodeMap; 
            NodeMap.RegisterNode(this);
            NodeConnections.UpdateMap(NodeMap);

            Initialised = true;
            IsEnabled = true;
            
            SetGameObjectName(GridCoord);
        }
        
        private void SetGameObjectName(Vector3Int coord)
        {
            /*string baseName = gameObject.name;
            int index = baseName.IndexOf('(');
            if (index >= 0) 
                baseName = baseName.Substring(0, index);*/

            string baseName = TailSuffix.Replace(gameObject.name, "");
            baseName = baseName.TrimEnd('_', ' ');
            
            _nameBuilder.Clear();
            _nameBuilder.Append(baseName);
            _nameBuilder.Append('_');
            _nameBuilder.Append(coord.x);
            _nameBuilder.Append('_');
            _nameBuilder.Append(coord.z);
            _nameBuilder.Append('_');
            gameObject.name = _nameBuilder.ToString();
        }

        public virtual void FailedPlacement(Vector3Int gridCoord)
        {
            Debug.Log($"Failed placement for {name} at {gridCoord}");
        }

        public void Reset() => Direction = startingDirection;
        
        public void SetDirection(Direction direction) => Direction = direction;

        public bool HasPathId() => PathId > 0;
        public void SetPathId(int pathId) => PathId = pathId;
        
        [Button]
        public void Rotate()
        {
            NodeRotation.Rotate();
        }
        
        public void Rotate(Direction direction)
        {
            NodeRotation.Rotate(direction);
        }

        public void RotateInstant(Direction direction)
        {
            NodeRotation.RotateInstant(direction);
        }

        public virtual void OnRemoval()
        {
            IsEnabled = false;
            Visuals.EnableRenderer();
            TargetNodes.Clear();

            if (Initialised == false)
            {
                // Trigger the event as we won't reach DeregisterNode() on the Node Map
                EventBus<NodeRemoved>.Raise(new NodeRemoved(this));
                return;
            }
            if (NodeMap == null)
            {
                // Trigger the event as we won't reach DeregisterNode() on the Node Map
                EventBus<NodeRemoved>.Raise(new NodeRemoved(this));
                return;
            }
            NodeMap.DeregisterNode(this);
        }
        
        public Vector2Int GetSize() => Direction switch
        {
            Direction.North => new Vector2Int(GridWidth, GridHeight),
            Direction.East => new Vector2Int(GridHeight, GridWidth),
            Direction.South => new Vector2Int(GridWidth, GridHeight),
            Direction.West => new Vector2Int(GridHeight, GridWidth),
            _ => new Vector2Int(GridWidth, GridHeight),
        };
        
        public bool HasForwardNode(out Node forwardNode)
        {
            if (TargetNodes.Count > 0)
            {
                forwardNode = TargetNodes[0];
                return true;
            }
            
            if (TryGetForwardNode(out forwardNode))
            {
                AddTargetNode(forwardNode);
                return true; 
            }
            
            forwardNode = null;
            return false;
        }
        
        // TO DO: Review how the list of Target Nodes should be managed
        // What should happen here when Target Nodes Count > 0? 
        public bool AddTargetNode(Node node)
        {
            if (TargetNodes.Contains(node))
                return false;
            
            if (LoopDetected(node))
            {
                Debug.Log($"Failed to add target node: {node.name} to the node: {name} due to loop detected.");
                return false;
            }
            
            TargetNodes.Add(node);
            EventBus<NodeTargetEvent>.Raise(new NodeTargetEvent(this, node));
            return true;
        }

        public void RemoveTargetNode(Node node)
        {
            if(!TargetNodes.Contains(node)) return;
            TargetNodes.Remove(node);
        }
        
        public bool IsConnected() => NodeConnections.IsConnected();

        public bool TryGetForwardNode(out Node forwardNode) => NodeConnections.TryGetForwardNode(out forwardNode);
        
        public bool TryGetBackwardNode(out Node backwardNode) => NodeConnections.TryGetBackwardNode(out backwardNode);

        public bool HasNeighbour(Direction direction) => NodeConnections.HasNeighbour(direction);

        public bool TryGetNeighbour(Direction direction, out Node neighbour) => NodeConnections.TryGetNeighbour(direction, out neighbour); 

        // The orientation that a neighbouring input node must have to correctly input to this node
        public Direction InputDirection() => NodeConnections.InputDirection(); 

        // The direction of the cell that should be checked for a connecting input node
        public Direction InputPosition() => NodeConnections.InputPosition();

        public virtual void OnPlayerSelect()
        {
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
        }

        public virtual void OnPlayerDeselect()
        {
            if(!IsSelected) return;
            IsSelected = false;
        }

        public bool LoopDetected(HashSet<Vector3Int> newPath)
        {
            HashSet<Node> visited = new();

            bool Dfs(Node start)
            {
                if (!visited.Add(start)) return true;
                
                Vector3Int target = PositionByDirection.GetForwardPositionV3(start.GridCoord, start.Direction, start.GridWidth);
                if (newPath.Contains(target)) return true;

                foreach (Node node in start.TargetNodes)
                {
                    if(Dfs(node)) return true;
                }

                return false;
            }
            
            return Dfs(this);
        }
        
        private bool LoopDetected(Node start)
        {
            HashSet<Node> visited = new();
            HashSet<Node> recursionStack = new();

            bool Dfs(Node n)
            {
                if (recursionStack.Contains(n)) return true;  // Back edge - cycle found
                if (visited.Contains(n)) return false;        // Already processed this subtree
                
                visited.Add(n);
                recursionStack.Add(n);
                
                if (n == this) 
                {
                    recursionStack.Remove(n);
                    return true;  // Direct loop to self
                }
                
                foreach (Node next in n.TargetNodes)
                {
                    if (Dfs(next)) 
                    {
                        recursionStack.Remove(n);
                        return true;
                    }
                }
                
                recursionStack.Remove(n);
                return false;
            }

            return Dfs(start);
        }
    }
}