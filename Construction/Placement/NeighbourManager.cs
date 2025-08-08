using Construction.Drag;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Nodes;
using Construction.Utilities;
using UnityEngine;

namespace Construction.Placement
{
    /// <summary>
    /// Manages the relationships and connections between nodes in the construction system.
    /// Handles the creation and updating of corner nodes when nodes are connected.
    /// </summary>
    public class NeighbourManager
    {
        private IMap _map;
        private INodeMap _nodeMap;
        private CornerCreator _cornerCreator;
        private PlacementSettings _settings;

        public NeighbourManager(IMap map, INodeMap nodeMap, PlacementSettings settings, Transform transform)
        {
            _map = map;
            _nodeMap = nodeMap;
            _settings = settings;
            _cornerCreator = new CornerCreator(_nodeMap, settings, transform);
        }
        
        public bool UpdateToCorner(IPlaceable occupant, Vector3Int position, DragPos dragPosition)
        {
            if (occupant is not Node node) 
                return false;
            
            // Update due to Forward/Backward Neighbours
            // This updates the neighbouring node not this node so return FALSE
            if (UpdatedForwardBackwardNeighbours(position, dragPosition, node)) 
                return false;

            // Update due to Right Neighbour
            if (UpdateSideNeighbour(node, position, Target.Right, dragPosition)) 
                return true;
            
            // Update due to Left Neighbour
            if (UpdateSideNeighbour(node, position, Target.Left, dragPosition)) 
                return true;
            
            return false;
        }

        private bool UpdatedForwardBackwardNeighbours(Vector3Int position, DragPos dragPosition, Node node)
        {
            Vector2Int forwardPos = PositionByDirection.GetForwardPosition(position, node.Direction, node.GridWidth); 
            Vector2Int backwardPos = PositionByDirection.GetBackwardPosition(position, node.Direction, node.GridWidth);
            
            bool updatedForward = UpdateNeighbour(node, forwardPos, Target.Forward, dragPosition);
            bool updatedBackward = UpdateNeighbour(node, backwardPos, Target.Backward, dragPosition);

            // Allow both forward and backward neighbours to be updated
            // but don't update left or right neighbours if either of these are updated
            return updatedForward || updatedBackward;
        }
        
        private bool UpdateSideNeighbour(Node node, Vector3Int position, Target target, DragPos dragPosition)
        {
            Vector2Int sidePosition = target == Target.Right 
                ? PositionByDirection.GetRightPosition(position, node.Direction, node.GridWidth)
                : PositionByDirection.GetLeftPosition(position, node.Direction, node.GridWidth);

            return UpdateNeighbour(node, sidePosition, target, dragPosition);
        }

        private bool UpdateNeighbour(Node currentNode, Vector2Int neighbourPosition, Target target, DragPos dragPosition)
        {
            if (_map.VacantCell(neighbourPosition.x, neighbourPosition.y)) return false;
            if (!_nodeMap.GetNeighbourAt(neighbourPosition, out Node neighbourNode)) return false;
            if (neighbourNode.Connected()) return false;
            
            // Calculate the relative turn between the current node and neighbour
            // 0 = same direction, 1 = 90-degree clockwise turn, 2 = opposite direction, 3 = 90-degree counterclockwise turn 
            int turn = ((int)neighbourNode.Direction - (int)currentNode.Direction + 4) % 4;
            
            // For forward/backward neighbours, replace the neighbour
            if (target is Target.Forward or Target.Backward)
            {
                if (turn is 0 or 2) return false; 
                // Backward connections ignore looping issues - leave that concern to the end node of a drag
                if (target == Target.Forward && LoopDetected(currentNode, neighbourNode)) return false;
                _cornerCreator.ReplaceNeighbourWithCorner(neighbourNode, currentNode, target);
                return true;
            }
            
            // For left/right neighbours, replace this belt
            if (target is Target.Left or Target.Right)
            {
                if (turn is 0 or 2) return false; 
                if (LoopDetected(currentNode, neighbourNode)) return false; 
                _cornerCreator.ReplaceWithCorner(currentNode, neighbourNode, target, dragPosition);
                return true;
            }

            return false;
        }

        private bool LoopDetected(Node currentNode, Node neighbourNode)
        {
            return LoopDetection.WillLoop(currentNode, neighbourNode);
        }
    }

    /// <summary>
    /// Represents the relative position of a neighbour node
    /// </summary>
    public enum Target
    {
        Forward,
        Backward,
        Left,
        Right,
    }
}