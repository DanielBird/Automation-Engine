using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public class NodeMap : INodeMap
    {
        private Node[,] _nodeGrid;
        private HashSet<Node> _nodeHashSet = new();
        public IMap Map { get; private set; }

        public NodeMap(IMap map)
        {
            Map = map;
            _nodeGrid = new Node[map.MapWidth, map.MapHeight];
        }
        
        public HashSet<Node> GetNodes() => _nodeHashSet;
        
        public bool RegisterNode(Node node)
        {
            if (!_nodeHashSet.Add(node))
                return false;
            
            int x = node.GridCoord.x;
            int z = node.GridCoord.z;
            int width = node.GridWidth;
            int height = node.GridHeight; 
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    _nodeGrid[i, j] = node; 
                }
            }
            
            return true;
        }

        public void DeregisterNode(Node node)
        {
            if (!_nodeHashSet.Remove(node))
                return;
            
            int x = node.GridCoord.x;
            int z = node.GridCoord.z;

            if (_nodeGrid[x, z] != node)
            {
                Debug.Log("Attempted to deregister a node that doesn't belong at its recorded location on the grid!");
                return;
            }
            
            int width = node.GridWidth;
            int height = node.GridHeight; 
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    if (_nodeGrid[i, j] == node)
                    {
                        _nodeGrid[i, j] = null;
                    }
                }
            }
            
            EventBus<NodeRemoved>.Raise(new NodeRemoved(node));
        }
        
        public bool TryGetNode(int x, int z, out Node node)
        {
            if (_nodeGrid[x, z] == null)
            {
                node = null; 
                return false;
            }
            
            node = _nodeGrid[x, z];
            return true; 
        }

        public bool HasNode(int x, int z)
        {
            if(!InBounds(x, z)) return false; 
            return _nodeGrid[x, z] != null;
        }

        [Button]
        public void TestGetNeighbour(int x, int z, Direction direction)
        {
            if(GetNeighbour(x, z, direction, out Node node)) Debug.Log($"{node} found");
            else Debug.Log("No node found");
        }
        
        public bool GetNeighbour(int x, int z, Direction direction, out Node node)
        {
            Vector2Int neighbour = direction switch
            {
                Direction.North => new Vector2Int(x, z + 1),
                Direction.East => new Vector2Int(x + 1, z),
                Direction.South => new Vector2Int(x, z - 1),
                Direction.West => new Vector2Int(x - 1, z),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };

            bool neighbourFound = GetNeighbourAt(neighbour, out Node foundNode);
            node = foundNode; 
            return neighbourFound; 
        }

        public bool GetNeighbourAt(Vector2Int position, out Node node)
        {
            if (InBounds(position.x, position.y) && _nodeGrid[position.x, position.y] is {} foundNode)
            {
                node = foundNode;
                return true;
            }
    
            node = null;
            return false;
        }
        
        public bool InBounds(int x, int z)
        {
            return x >= 0 && x < _nodeGrid.GetLength(0) &&
                   z >= 0 && z < _nodeGrid.GetLength(1); 
        }
    }
}