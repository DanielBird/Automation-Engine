using System;
using Engine.Construction.Maps;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;

namespace Engine.Construction.Nodes
{
    public class NodeConnections
    {
        private readonly Node _node;
        private IWorld _world;
        private bool _worldSet;
        
        // Cache
        private struct Key
        {
            public Vector3Int Coord;
            public Direction Dir;
            public NodeType Type;
        }

        private int _lastMapVersion = -1;
        private Key _lastKey;

        private bool _hasCachedOutput; 
        private Node _cachedOutput;
        
        private bool _hasCachedInput;
        private Node _cachedInput;
        
        public NodeConnections(Node node)  => _node = node;

        private bool StaleCache()
        {
            if (!_worldSet) return true;
            if (_world.Version() != _lastMapVersion) return true;
            Key k = CurrentKey(); 
            return k.Coord != _lastKey.Coord || k.Dir != _lastKey.Dir || k.Type != _lastKey.Type;
        }

        private Key CurrentKey() => new Key
        {
            Coord =  _node.GridCoord,
            Dir = _node.Direction,
            Type = _node.NodeType,
        };

        private void Stamp()
        {
            _lastMapVersion = _world.Version();
            _lastKey = CurrentKey();
        }
        
        public void UpdateMap(IWorld world)
        {
            _world = world;
            _worldSet = true;
        } 
        
        public bool TryGetNeighbour(Direction direction, out Node neighbour)
        {
            neighbour = null;
            return _worldSet && _world.GetNeighbour(_node.GridCoord.x, _node.GridCoord.z, direction, out neighbour);
        }
        
        // The node in front in the path
        public bool TryGetOutputNode(out Node outputNode)
        {
            outputNode = null;
            if (!_worldSet) return false;

            if (!_hasCachedOutput || StaleCache())
            {
                int width = _node.GridWidth; 
                int x = _node.GridCoord.x;
                int z = _node.GridCoord.z;
            
                Vector2Int outputPosition = _node.Direction switch
                {
                    Direction.North => new Vector2Int(x, z + width),
                    Direction.East => new Vector2Int(x + width, z),
                    Direction.South => new Vector2Int(x, z - width),
                    Direction.West => new Vector2Int(x - width, z),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (_world.GetNeighbourAt(outputPosition, out Node found))
                {
                    if (found.InputDirection() == _node.Direction || found.NodeType == NodeType.Intersection)
                    {                    
                        _cachedOutput = found;
                        _hasCachedOutput = true;
                    }
                }
                else
                {
                    _hasCachedOutput = false;
                }

                Stamp(); 
            }

            if (!_hasCachedOutput) return false;
            
            outputNode = _cachedOutput;
            return true;
        }
        
        // The node behind in the path
        public bool TryGetInputNode(out Node inputNode)
        {
            inputNode = null;
            if (!_worldSet) return false;

            if (!_hasCachedInput || StaleCache())
            {
                int width = _node.GridWidth; 
                int x = _node.GridCoord.x;
                int z = _node.GridCoord.z;
                Direction inputDirection = DirectionOfInputPosition();

                Vector2Int inputPosition = inputDirection switch
                {
                    Direction.North => new Vector2Int(x, z + width),
                    Direction.East => new Vector2Int(x + width, z),
                    Direction.South => new Vector2Int(x, z - width),
                    Direction.West => new Vector2Int(x - width, z),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (_world.GetNeighbourAt(inputPosition, out Node found))
                {
                    if (found.OutputGridCoord() == _node.GridCoord || found.NodeType == NodeType.Intersection)
                    {
                        _cachedInput = found;
                        _hasCachedInput = true;
                    }
                }
                else
                {
                    _hasCachedInput = false;
                }
                
                Stamp();
            }
            
            if (!_hasCachedInput) return false;
            inputNode = _cachedInput;
            return true;
        }

        public bool TryGetForwardLeftNode(int step, out Node forwardLeftNode)
        {
            forwardLeftNode = null;
            Vector2Int fPos = PositionByDirection.GetForwardPosition(_node.GridCoord, _node.Direction, step);
            Direction counterClockwiseTurn = DirectionUtils.RotateCounterClockwise(_node.Direction);
            Vector2Int forwardLeftPosition = PositionByDirection.GetForwardPosition(new Vector3Int(fPos.x, 0, fPos.y), counterClockwiseTurn, step);
            return _worldSet && _world.GetNeighbourAt(forwardLeftPosition, out forwardLeftNode); 
        }
        
        public bool HasNeighbour(Direction direction)
        {
            Vector3Int pos = _node.GridCoord; 
            Vector2Int position = PositionByDirection.Get(pos.x, pos.z, direction, _node.GridWidth);
            return _worldSet && _world.GetNeighbourAt(position, out _);
        }
        
        public bool IsConnected()
        {
            // For corner belts, we need to check the direction they're receiving from.
            // For standard belts, the direction of the forward neighbour and backwards neighbour should be the same as the belt's direction.
            // For corner belts, the forward direction should be the same but the backwards direction should be perpendicular.
      
            if (!TryGetOutputNode(out Node forwardNode))
                return false;
            
            if (forwardNode.InputDirection() != _node.Direction)
                return false;

            if (!TryGetInputNode(out Node backwardNode))
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
        public Direction DirectionOfInputPosition() => _node.NodeType switch
        {
            NodeType.Straight => DirectionUtils.Opposite(_node.Direction),
            NodeType.LeftCorner => DirectionUtils.RotateCounterClockwise(_node.Direction),
            NodeType.RightCorner => DirectionUtils.RotateClockwise(_node.Direction),
            _ => DirectionUtils.Opposite(_node.Direction),
        };
        
        public Vector3Int OutputGridCoord()
        { 
            int width = _node.GridWidth; 
            int x = _node.GridCoord.x;
            int z = _node.GridCoord.z;

            Vector2Int oP = _node.Direction switch
            {
                Direction.North => new Vector2Int(x, z + width),
                Direction.East => new Vector2Int(x + width, z),
                Direction.South => new Vector2Int(x, z - width),
                Direction.West => new Vector2Int(x - width, z),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            return new Vector3Int(oP.x, 0, oP.y);
        }
    }
}