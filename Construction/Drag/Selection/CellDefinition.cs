using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;

namespace Engine.Construction.Drag.Selection
{
    public static class CellDefinition
    {
        public static NodeType DefineCell(Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, out Direction finalDirection)
        {
            int stepSize = selectionParams.StepSize;
            stepSize = Mathf.Abs(stepSize);
            INodeMap nodeMap = selectionParams.NodeMap;
            
            Vector2Int northPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, Direction.North, stepSize);
            Vector2Int eastPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, Direction.East, stepSize);
            Vector2Int southPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, Direction.South, stepSize);
            Vector2Int westPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, Direction.West, stepSize);
            
            bool northFound = nodeMap.GetNeighbourAt(northPos, out Node northNode);
            bool eastFound = nodeMap.GetNeighbourAt(eastPos, out Node eastNode);
            bool southFound = nodeMap.GetNeighbourAt(southPos, out Node southNode);
            bool westFound = nodeMap.GetNeighbourAt(westPos, out Node westNode);
            
            // For corner cases, analyze flow direction
            if (northFound && westFound && !eastFound && !southFound)
            {
                // North + West connection 
                return DetermineCornerType(northNode, westNode, Direction.North, Direction.West, out finalDirection);
            }
    
            if (northFound && eastFound && !westFound && !southFound)
            {
                // North + East connection
                return DetermineCornerType(northNode, eastNode, Direction.North, Direction.East, out finalDirection);
            }
    
            if (southFound && eastFound && !northFound && !westFound)
            {
                // South + East connection
                return DetermineCornerType(southNode, eastNode, Direction.South, Direction.East, out finalDirection);
            }
    
            if (southFound && westFound && !northFound && !eastFound)
            {
                // South + West connection
                return DetermineCornerType(southNode, westNode, Direction.South, Direction.West, out finalDirection);
            }

            if (northFound && eastFound && southFound && westFound)
            {
                finalDirection = direction; 
                return NodeType.Intersection; 
            }
    
            finalDirection = direction;
            return NodeType.Straight;

        }
        
        private static NodeType DetermineCornerType(Node node1, Node node2, Direction dir1, Direction dir2, out Direction finalDirection)
        {
            // Analyze which node is "feeding into" the placement position
            bool node1IsIncoming = IsNodePointingToward(node1, dir1);
            bool node2IsIncoming = IsNodePointingToward(node2, dir2);
    
            if (node1IsIncoming && node2IsIncoming)
            {
                finalDirection = dir1;
                return NodeType.Intersection;
            }
            
            if (node1IsIncoming)
            {
                return GetCornerTypeForConnections(dir1, dir2, out finalDirection);
            }
            
            if (node2IsIncoming)
            {
                return GetCornerTypeForConnections(dir2, dir1, out finalDirection);
            }
            
            // Neither are incoming 
            finalDirection = dir1;
            return NodeType.Straight;
        }

        private static bool IsNodePointingToward(Node node, Direction nodePosition) 
            => node.Direction == DirectionUtils.Opposite(nodePosition);
        
        private static NodeType GetCornerTypeForConnections(Direction incoming, Direction outgoing, out Direction bestFacing)
        {
            Direction leftConnection1 = DirectionUtils.Decrement(incoming);
            bool leftValid1 = (leftConnection1 == outgoing);
            
            Direction rightConnection1 = DirectionUtils.Increment(incoming);
            bool rightValid1 = (rightConnection1 == outgoing);
            
            bestFacing = outgoing;
            
            if (leftValid1) return NodeType.RightCorner;

            if (rightValid1) return NodeType.LeftCorner;
            
            // Fallback
            return NodeType.Straight;
        }
        
        
        // Used to determine the Node Type and direction of the start and end nodes in a path created during a drag
        public static NodeType DefinePathCell(HashSet<Cell> cells, Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, bool end, out Direction finalDirection)
        {
            int stepSize = selectionParams.StepSize;
            stepSize = Mathf.Abs(stepSize);
            INodeMap nodeMap = selectionParams.NodeMap;
            
            Direction rightDirection = DirectionUtils.Increment(direction);
            Direction leftDirection = DirectionUtils.Decrement(direction);
            
            Vector2Int rightPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, rightDirection, stepSize);
            Vector2Int leftPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, leftDirection, stepSize);

            // Found a node
            bool rightFound = nodeMap.GetNeighbourAt(rightPos, out Node rightN);
            bool leftFound  = nodeMap.GetNeighbourAt(leftPos, out Node leftN);
            
            // Found a node, and it is facing the right direction for this node to connect to it
            bool right = rightFound && DirectionIsGood(end, rightN, rightDirection, leftDirection, direction, isRightSide: true);
            bool left = leftFound && DirectionIsGood(end, leftN, leftDirection, rightDirection, direction, isRightSide: false);

            // Avoid connecting if it leads to a loop
            if (right || left)
            {
                HashSet<Vector3Int> positions = cells.Select(c => c.GridCoordinate).ToHashSet();
                if (right && rightN.LoopDetected(positions)) right = false;
                if (left && leftN.LoopDetected(positions)) left = false;
            }
            
            // Debug.Log($"Right direction is {rightDirection} and left direction is {leftDirection}");
            // Debug.Log($"Right Position is {rightPos} and Left is {leftPos}");
            // Debug.Log($"Right is found is {rightFound} and it is facing the correct direction is {right}. Left is found is {leftFound} and it is facing the correct direction is {left}");
            
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