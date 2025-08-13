using Construction.Maps;
using Construction.Placement;
using Construction.Utilities;
using UnityEngine;

namespace Construction.Nodes
{
    public class NodeConnections
    {
        private readonly Node _node;
        private INodeMap _nodeMap;
        private bool _nodeMapSet; 
        
        public NodeConnections(Node node)  => _node = node;
        public void UpdateMap(INodeMap nodeMap)
        {
            _nodeMap = nodeMap;
            _nodeMapSet = true;
        } 
        
        public bool TryGetNeighbour(Direction direction, out Node neighbour)
        {
            neighbour = null;
            return _nodeMapSet && _nodeMap.GetNeighbour(_node.GridCoord.x, _node.GridCoord.z, direction, out neighbour);
        }
        
        public bool TryGetForwardNode(out Node forwardNode)
        {
            forwardNode = null; 
            Vector2Int forwardPosition = PositionByDirection.GetForwardPosition(_node.GridCoord, _node.Direction, _node.GridWidth);
            return _nodeMapSet && _nodeMap.GetNeighbourAt(forwardPosition, out forwardNode);
        }
        
        public bool TryGetBackwardNode(out Node backwardNode)
        {
            backwardNode = null;
            Vector2Int backwardPosition = PositionByDirection.GetForwardPosition(_node.GridCoord, InputPosition(), _node.GridWidth);
            if (_nodeMap == null) return false;
            return _nodeMap.GetNeighbourAt(backwardPosition, out backwardNode);
        }
        
        public bool HasNeighbour(Direction direction)
        {
            Vector3Int pos = _node.GridCoord; 
            Vector2Int position = PositionByDirection.Get(pos.x, pos.z, direction, _node.GridWidth);
            return _nodeMapSet && _nodeMap.GetNeighbourAt(position, out _);
        }
        
        public bool IsConnected()
        {
            // For corner belts, we need to check the direction they're receiving from
            // For standard belts the direction of the forward neighbour and backwards neighbour should be the same as the belt's direction
            // For corner belts, the forward direction should be the same but the backwards direction should be perpendicular
      
            if (!TryGetForwardNode(out Node forwardNode))
                return false;
            
            if (forwardNode.InputDirection() != _node.Direction)
                return false;

            if (!TryGetBackwardNode(out Node backwardNode))
                return false; 
            
            if (backwardNode.Direction != InputDirection())
                return false;

            return true;
        }
        
        // The orientation that a neighbouring input node must have to correctly input to this node
        public Direction InputDirection() => _node.NodeType switch
        {
            NodeType.Straight => _node.Direction,
            NodeType.LeftCorner => DirectionUtils.RotateClockwise(_node.Direction),
            NodeType.RightCorner => DirectionUtils.RotateCounterClockwise(_node.Direction),
            _ => _node.Direction,
        };
        
        // The direction of the cell that should be checked for a connecting input node
        public Direction InputPosition() => _node.NodeType switch
        {
            NodeType.Straight => DirectionUtils.Opposite(_node.Direction),
            NodeType.LeftCorner => DirectionUtils.RotateCounterClockwise(_node.Direction),
            NodeType.RightCorner => DirectionUtils.RotateClockwise(_node.Direction),
            _ => _node.Direction,
        };
    }
}