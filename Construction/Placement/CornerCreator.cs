﻿using Construction.Drag;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Nodes;
using UnityEngine;
using Utilities;

namespace Construction.Placement
{
    public class CornerCreator
    {
        private readonly INodeMap _nodeMap;
        private readonly PlacementSettings _settings;
        private readonly Transform _transform;
        
        private const int RightTurn = 1;
        private const int LeftTurn = 3;

        public CornerCreator(INodeMap nodeMap, PlacementSettings settings, Transform transform)
        {
            _nodeMap = nodeMap;
            _settings = settings;
            _transform = transform;
        }
        
        public void Replace(Node nodeToReplace, Node otherNode, Target target, DragPos? dragPosition = null)
        {
            // Consolidate ReplaceWithCorner and ReplaceNeighbourWithCorner into one method?
        }
        
        public void ReplaceWithCorner(Node nodeToReplace, Node otherNode, Target target, DragPos dragPosition)
        {
            _nodeMap.DeregisterNode(nodeToReplace);
            
            Vector3Int position = nodeToReplace.Position;
             
            Node oldNode = dragPosition == DragPos.Start ? otherNode : nodeToReplace;
            Node newNode = dragPosition == DragPos.Start ? nodeToReplace : otherNode; 
            
            Direction oldDirection = oldNode.Direction;
            Direction newDirection = newNode.Direction;
            
            SimplePool.Despawn(nodeToReplace.gameObject);
            
            GameObject prefab = GetCornerPrefab(oldDirection, newDirection, out NodeType nodeType);
            SpawnCorner(oldDirection, newDirection, target, prefab, position, nodeType);
        }

        public void ReplaceNeighbourWithCorner(Node nodeToReplace, Node otherNode, Target target)
        {
            _nodeMap.DeregisterNode(nodeToReplace);
            
            Vector3Int position = nodeToReplace.Position;
            
            Direction oldDirection = nodeToReplace.Direction;
            Direction newDirection = otherNode.Direction; 
            
            SimplePool.Despawn(nodeToReplace.gameObject);

            NodeType nodeType; 
            
            GameObject prefab = target == Target.Backward 
                ? GetCornerPrefab(oldDirection, newDirection, out nodeType)
                : GetCornerPrefab(newDirection, oldDirection, out nodeType);
            
            SpawnCorner(oldDirection, newDirection, target, prefab, position, nodeType);
        }
        
        private GameObject GetCornerPrefab(Direction oldDirection, Direction newDirection, out NodeType nodeType)
        {
            // Calculate the relative turn; adding 4 ensures a positive number before modulo.
            int turn = ((int)newDirection - (int)oldDirection + 4) % 4;

            if (turn == RightTurn)
            {
                nodeType = NodeType.RightCorner; 
                return _settings.rightBeltPrefab;
            }

            if (turn == LeftTurn)
            {
                nodeType = NodeType.LeftCorner; 
                return _settings.leftBeltPrefab;
            }
            
            Debug.Log($"Attempting to spawn a prefab based on invalid directions: {oldDirection} and {newDirection}");
            nodeType = NodeType.Straight;
            return _settings.standardBeltPrefab; 
        }
        
        private void SpawnCorner(Direction oldDirection, Direction newDirection, Target target, GameObject prefab, Vector3Int position, NodeType nodeType)
        {
            GameObject cornerNode = SimplePool.Spawn(prefab, position, Quaternion.identity, _transform);
            cornerNode.name = "Corner Belt_" + position.x + "_" + position.z;
            
            if (!cornerNode.TryGetComponent(out IPlaceable occupant))
            {
                Debug.LogError("Failed to get IOccupy component from corner prefab");
                return;
            }
            
            occupant.Place(position, _nodeMap);
            if (occupant is Node node)
            {
                Direction d = target != Target.Forward ? newDirection : oldDirection;
                
                NodeConfiguration config = NodeConfiguration.Create(_nodeMap, nodeType, d); 
                node.Initialise(config);
                node.Visuals.HideArrows();
            }
        }
    }
}