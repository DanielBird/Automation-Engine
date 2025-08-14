using System;
using System.Collections.Generic;
using Construction.Events;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Placement;
using Construction.Visuals;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities;
using Utilities.Events;

namespace Construction.Nodes
{
    public enum NodeType
    {
        Straight,
        LeftCorner,
        RightCorner,
        Intersection,
        Producer
    }
    
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
        
        [Header("Position & Rotation")] 
        protected Vector3Int _position; 
        private Direction _direction; 
        
        public Direction startingDirection; 
        public float rotationTime = 0.25f;
        
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease rotationEasing = EasingFunctions.Ease.EaseOutSine;
        [field: SerializeField] public Vector3Int GridCoord { get; set; }
        [field: SerializeField] public Vector3Int WorldPosition { get; private set; }

        [ShowInInspector]
        public Direction Direction
        {
            get => _direction;
            set => _direction = value;
        }
        [field: SerializeField] public List<Node> TargetNodes { get; private set; } 
        [field: SerializeField] public NodeVisuals Visuals { get; private set; }
        [field: SerializeField] public bool Initialised { get; private set; }    
        public INodeMap NodeMap { get; protected set; }
        private NodeRotation _nodeRotation;
        private NodeConnections _nodeConnections;
        
        // Player Selection
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        
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
            
            _nodeRotation = new NodeRotation(this, rotationTime, _ease);
            _nodeConnections = new NodeConnections(this);
        }
        
        public virtual void Place(Vector3Int gridCoord, INodeMap map)
        {
            NodeMap = map; 
            GridCoord = gridCoord;
            WorldPosition = Vector3Int.RoundToInt(transform.position);
        }
        
        // Note - nodes register themselves with the Node Map
        // But they do not register themselves with Map
        // This must be done at the place of creating / instantiating the node
        
        public virtual void Initialise(NodeConfiguration config)
        {
            NodeMap = config.NodeMap; 
            NodeMap.RegisterNode(this);
            _nodeConnections.UpdateMap(NodeMap);
            
            if (config.UpdateRotation)
            {
                Direction = config.Direction;
                RotateInstant(config.Direction, false);
            }
            NodeType = config.NodeType;
            UpdateTargetNode();
            Initialised = true;
            
            EventBus<NodePlaced>.Raise(new NodePlaced(this));
            IsEnabled = true;
        }

        public virtual void FailedPlacement()
        {
            Debug.Log("Failed placement on " + name);
        }

        public void Reset() => Direction = startingDirection;
        
        public void SetDirection(Direction direction) => Direction = direction; 
        
        [Button]
        public void Rotate(bool updateTarget = true)
        {
            _nodeRotation.Rotate();
            if(updateTarget) UpdateTargetNode();
        }
        
        public void Rotate(Direction direction, bool updateTarget = true)
        {
            _nodeRotation.Rotate(direction);
            if(updateTarget) UpdateTargetNode();
        }

        public void RotateInstant(Direction direction, bool updateTarget = true)
        {
            _nodeRotation.RotateInstant(direction);
            if(updateTarget) UpdateTargetNode();
        }
        
        public Vector2Int GetSize() => Direction switch
        {
            Direction.North => new Vector2Int(GridWidth, GridHeight),
            Direction.East => new Vector2Int(GridHeight, GridWidth),
            Direction.South => new Vector2Int(GridWidth, GridHeight),
            Direction.West => new Vector2Int(GridHeight, GridWidth),
            _ => new Vector2Int(GridWidth, GridHeight),
        };

        public void UpdateTargetNode()
        {
            // DURING A DRAG
            // Trying to get forward node will fail, as the node hasn't been placed yet
            // Setting up target nodes during a drag relies on the node in front updating this node 
            
            if (TryGetForwardNode(out Node forwardNode))
                AddTargetNode(forwardNode);
            
            if (TryGetBackwardNode(out Node backwardNode))
                backwardNode.AddTargetNode(this);
        }

        public bool HasForwardNode(out Node forwardNode)
        {
            if (TryGetForwardNode(out forwardNode))
            {
                AddTargetNode(forwardNode);
                return true; 
            }
            forwardNode = null;
            return false;
        }

        
        // To Do: Review how the list of Target Nodes should be managed
        // What should happen here when Target Nodes Count > 0? 
        public void AddTargetNode(Node node)
        {
            if (LoopDetected(node)) return; 
            
            TargetNodes.Add(node);
            
            EventBus<NodeTargetEvent>.Raise(new NodeTargetEvent(this, node));
        }
        
        public bool IsConnected() => _nodeConnections.IsConnected();

        public bool TryGetForwardNode(out Node forwardNode) => _nodeConnections.TryGetForwardNode(out forwardNode);
        
        public bool TryGetBackwardNode(out Node backwardNode) => _nodeConnections.TryGetBackwardNode(out backwardNode);

        public bool HasNeighbour(Direction direction) => _nodeConnections.HasNeighbour(direction);

        public bool TryGetNeighbour(Direction direction, out Node neighbour) => _nodeConnections.TryGetNeighbour(direction, out neighbour); 

        // The orientation that a neighbouring input node must have to correctly input to this node
        public Direction InputDirection() => _nodeConnections.InputDirection(); 

        // The direction of the cell that should be checked for a connecting input node
        public Direction InputPosition() => _nodeConnections.InputPosition();

        public virtual void OnPlayerSelect()
        {
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
        }

        public virtual void OnPlayerDeselect()
        {
            if(!IsSelected || !IsEnabled) return;
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

            bool Dfs(Node n)
            {
                if (!visited.Add(n)) return false;
                if (n == this) return true;           
                foreach (Node next in n.TargetNodes)
                {
                    if (Dfs(next)) return true;
                }
                return false;
            }

            return Dfs(start);
        }
    }
}