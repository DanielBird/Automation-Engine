﻿using System;
using System.Collections.Generic;
using Construction.Events;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Placement;
using Construction.Visuals;
using Construction.Widgets;
using Events;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities;

namespace Construction.Nodes
{
    public enum NodeType
    {
        Straight,
        LeftCorner,
        RightCorner,
    }
    
    [RequireComponent(typeof(NodeVisuals))]
    [Serializable]
    public abstract class Node : MonoBehaviour, IPlaceable, IRotatable
    {
        [Header("Setup")] 
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private bool draggable;
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Draggable { get; set; }
        [field: SerializeField] public NodeType NodeType { get; private set; } 
        
        [Header("Position & Rotation")] 
        private Vector3Int _position; 
        private Direction _direction; 
        
        public Direction startingDirection; 
        public float rotationTime = 0.25f;
        
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease rotationEasing = EasingFunctions.Ease.EaseOutSine;
        [field: SerializeField] public Vector3Int Position { get; set; }

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
        
        private void Awake()
        {
            Width = width;
            Height = height;
            Draggable = draggable;
            _ease = EasingFunctions.GetEasingFunction(rotationEasing);
            if(Visuals == null) Visuals = GetComponent<NodeVisuals>();
            TargetNodes = new List<Node>();
            
            _nodeRotation = new NodeRotation(this, rotationTime, _ease);
            _nodeConnections = new NodeConnections(this);
        }
        
        public virtual void Place(Vector3Int position, INodeMap map)
        {
            NodeMap = map; 
            Position = position;
        }
        
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
        }

        public virtual void FailedPlacement()
        {
            //Debug.Log("Failed placement on " + name);
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
            Direction.North => new Vector2Int(Width, Height),
            Direction.East => new Vector2Int(Height, Width),
            Direction.South => new Vector2Int(Width, Height),
            Direction.West => new Vector2Int(Height, Width),
            _ => new Vector2Int(Width, Height),
        };

        public void UpdateTargetNode()
        {
            // DURING A DRAG
            // Trying to get forward node will fail, as the node hasn't been placed yet
            // Setting up target nodes during a drag relies on the node in front updating this node 
            
            if (TryGetForwardNode(out Node forwardNode))
                SetTargetNode(forwardNode);
            
            if (TryGetBackwardNode(out Node backwardNode))
                backwardNode.SetTargetNode(this);
        }

        public bool HasForwardNode(out Node forwardNode)
        {
            if (TryGetForwardNode(out forwardNode))
            {
                SetTargetNode(forwardNode);
                return true; 
            }
            forwardNode = null;
            return false;
        }

        public void SetTargetNode(Node node)
        {
            if (TargetNodes.Count == 0) TargetNodes.Add(node);
            else TargetNodes[0] = node;
            
            EventBus<NodeTargetEvent>.Raise(new NodeTargetEvent(this, node));
        }
        
        public bool Connected() => _nodeConnections.Connected();

        public bool TryGetForwardNode(out Node forwardNode) => _nodeConnections.TryGetForwardNode(out forwardNode);
        
        public bool TryGetBackwardNode(out Node backwardNode) => _nodeConnections.TryGetBackwardNode(out backwardNode);

        public bool HasNeighbour(Direction direction) => _nodeConnections.HasNeighbour(direction); 

        // The orientation that a neighbouring input node must have to correctly input to this node
        public Direction InputDirection() => _nodeConnections.InputDirection(); 

        // The direction of the cell that should be checked for a connecting input node
        public Direction InputPosition() => _nodeConnections.InputPosition();
    }
}