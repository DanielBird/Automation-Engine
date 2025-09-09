using Engine.Construction.Drag.Selection;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Placement
{
    [System.Serializable]
    public class PlacementState
    {
        public bool IsRunning { get; private set; }
        public GameObject CurrentObject { get; private set; }
        public Vector3Int TargetGridCoordinate { get; set; }
        public Vector3Int WorldAlignedPosition { get; set; }
        public Direction CurrentDirection { get; private set; }
        public Axis CurrentAxis { get; private set; }
        public bool PlaceableFound { get; private set; }
        public IPlaceable MainPlaceable { get; private set; }
        public bool PlaceableIsNode { get;  private set; }
        public Node Node { get; private set; }
        public bool PlaceableReplacesNodes { get;  private set; }
        public bool RotatableFound { get; private set; }
        public IRotatable MainRotatable { get; private set; }
        public int PathId { get; private set; }

        public void SetGameObject(GameObject gameObject)
        {
            CurrentObject = gameObject;
            IsRunning = true; 
            
            if (CurrentObject.TryGetComponent(out IPlaceable placeable))
                SetPlaceable(placeable); 
            
            if (CurrentObject.TryGetComponent(out IRotatable rotatable))
                SetRotatable(rotatable);
        }
        
        public void StopRunning() => IsRunning = false;
        
        private void SetPlaceable(IPlaceable placeable)
        {
            MainPlaceable = placeable;
            PlaceableFound = true;

            if (MainPlaceable is Node node)
            {
                PlaceableIsNode = true;
                Node = node;
                PlaceableReplacesNodes = node.ReplaceOnPlacement; 
            }
            else
            {
                PlaceableIsNode = false;
                PlaceableReplacesNodes = false;
            }
        }
        
        private void SetRotatable(IRotatable rotatable)
        {
            MainRotatable = rotatable;
            RotatableFound = true;
        }
        
        public void SetDirection(Direction direction) => CurrentDirection = direction;

        public bool UpdateState(IMap map, INodeMap nodeMap, PlacementSettings settings,out NodeType newNodeType, out Direction newDirection)
        {
            newDirection = Node.Direction;
            newNodeType = Node.NodeType;
            
            if(!PlaceableIsNode) return false;
            if(!Node.nodeTypeSo.draggable) return false;
            
            CellSelectionParams selectionParams = new CellSelectionParams(map, nodeMap, settings, Node.GridWidth);
            newNodeType = CellDefinition.DefineCell(TargetGridCoordinate, Node.Direction, selectionParams, out newDirection);
            
            return newNodeType != Node.NodeType;
        }
        
        public void SetAxis(Axis axis) => CurrentAxis = axis;
        public void SetPathId(int id) => PathId = id;
        public void ClearPathId() => PathId = -1;
    }
} 