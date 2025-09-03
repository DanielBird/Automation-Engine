using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;

namespace Engine.Construction.Drag.Selection
{
    public class CellDefinition
    {
        public static NodeType DefineUnknownCell(Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, out Direction finalDirection)
        {
            // Check if the neighbouring cells are occupied by another node
            int stepSize = selectionParams.StepSize;
            stepSize = Mathf.Abs(stepSize); // step should always be positive for this method
            
            Direction rightDirection = DirectionUtils.Increment(direction);
            Direction leftDirection = DirectionUtils.Decrement(direction);
            Direction oppositeDirection = DirectionUtils.Opposite(direction);
            
            Vector2Int forwardPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, direction, stepSize);
            Vector2Int rightPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, rightDirection, stepSize);
            Vector2Int leftPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, leftDirection, stepSize);
            Vector2Int backwardPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, oppositeDirection, stepSize);
            
            // Found a node 
            bool forwardFound = selectionParams.NodeMap.GetNeighbourAt(forwardPos, out Node forwardN);
            bool rightFound = selectionParams.NodeMap.GetNeighbourAt(rightPos, out Node rightN);
            bool leftFound  = selectionParams.NodeMap.GetNeighbourAt(leftPos, out Node leftN);
            bool backwardFound = selectionParams.NodeMap.GetNeighbourAt(backwardPos, out Node backwardN);

            // Debug.Log($"Forward is found is {forwardFound}. Right is found is {rightFound}. Left is found is {leftFound}. Backward is found is {backwardFound}.");
            // Debug.LogFormat($"Right direction: {rightDirection}. Left direction: {leftDirection}. Opposite direction: {oppositeDirection}. Current direction: {direction}");
   
            if (forwardFound && backwardFound)
            {
                finalDirection = direction; 
                return NodeType.Straight;
            }

            if (rightFound)
            {
                finalDirection = rightDirection; 
                return !backwardFound ? NodeType.Straight : NodeType.RightCorner;
            }

            if (leftFound)
            {
                finalDirection = leftDirection;
                return !backwardFound ? NodeType.Straight : NodeType.LeftCorner;
            }

            finalDirection = direction;
            return NodeType.Straight;
        }
        
        public static NodeType DefineCell(HashSet<Cell> cells, Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, bool end, out Direction finalDirection)
        {
            // Check if the left or right neighbouring cells are occupied by another node
            int stepSize = selectionParams.StepSize;
            stepSize = Mathf.Abs(stepSize); // step should always be positive for this method
            
            Direction rightDirection = DirectionUtils.Increment(direction);
            Direction leftDirection = DirectionUtils.Decrement(direction);
            
            Vector2Int rightPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, rightDirection, stepSize);
            Vector2Int leftPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, leftDirection, stepSize);

            // Found a node
            bool rightFound = selectionParams.NodeMap.GetNeighbourAt(rightPos, out Node rightN);
            bool leftFound  = selectionParams.NodeMap.GetNeighbourAt(leftPos, out Node leftN);
            
            // Found a node, and it is facing the right direction for this node to connect to it
            bool right = rightFound && DirectionIsGoodOnRight(end, rightN, rightDirection, leftDirection, direction);
            bool left = leftFound && DirectionIsGoodOnLeft(end, leftN, leftDirection, rightDirection, direction);

            // Avoid connecting if it leads to a loop
            if (right || left)
            {
                HashSet<Vector3Int> positions = cells.Select(c => c.GridCoordinate).ToHashSet();
                if (right && rightN.LoopDetected(positions)) right = false;
                if (left && leftN.LoopDetected(positions)) left = false;
            }
            
            Debug.Log($"Right direction is {rightDirection} and left direction is {leftDirection}");
            // Debug.Log($"Right Position is {rightPos} and Left is {leftPos}");
            Debug.Log($"Right is found is {rightFound} and it is facing the correct direction is {right}. Left is found is {leftFound} and it is facing the correct direction is {left}");
            
            NodeType nodeType = (left, right) switch
            {
                (true,  true)  => NodeType.Intersection,
                (true,  false) => NodeType.LeftCorner,
                (false, true)  => NodeType.RightCorner,
                _              => NodeType.Straight
            };

            finalDirection = (left, right) switch
            {
                (false,  false) => direction,
                (true,  false) => end ? leftDirection : direction,
                (false, true)  => end ? rightDirection : direction,
                _              => direction
            };
            return nodeType;
        }
        
        private static bool DirectionIsGoodOnRight(bool end, Node neighbour, Direction directionOne, Direction directionTwo, Direction current)
        {
            return DirectionIsGood(end, neighbour, directionOne, directionTwo, current, isRightSide: true);
        }
        
        private static bool DirectionIsGoodOnLeft(bool end, Node neighbour, Direction directionOne, Direction directionTwo, Direction current)
        {
            return DirectionIsGood(end, neighbour, directionOne, directionTwo, current, isRightSide: false);
        }
        
        private static bool DirectionIsGood(bool end, Node neighbour, Direction directionOne, Direction directionTwo, Direction current, bool isRightSide)
        {
            if (end)
            {
                // Ends don't connect to Producers
                if (neighbour.NodeType == NodeType.Producer) return false;

                // Define the mapping for corner connections
                (NodeType sameDirectionCorner, NodeType oppositeDirectionCorner) = isRightSide 
                    ? (NodeType.LeftCorner, NodeType.RightCorner)
                    : (NodeType.RightCorner, NodeType.LeftCorner);

                // Corners on the same side should face the same way
                // e.g. a corner to the right that is a left turn facing the current direction is good 
                if (neighbour.NodeType == sameDirectionCorner)
                {
                    return neighbour.Direction == current;
                }
                
                // Corners on the opposite side should face the opposite direction
                // e.g. a corner to the right that is a right turn facing the opposite direction is good
                if (neighbour.NodeType == oppositeDirectionCorner)
                {
                    return neighbour.Direction == DirectionUtils.Opposite(current);
                }
            }
            
            return neighbour.Direction == (end ? directionOne : directionTwo);
        }
    }
}