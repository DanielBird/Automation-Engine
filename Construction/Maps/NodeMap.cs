using System;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Maps
{
    [RequireComponent(typeof(Map))]
    public class NodeMap : MonoBehaviour, INodeMap
    {
        private Node[,] _nodeGrid;
        public Map map;

        private void Awake()
        {
            if (map == null) map = GetComponent<Map>();
            _nodeGrid = new Node[map.MapWidth, map.MapHeight];
        }

        public void RegisterNode(Node node)
        {
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
        }
        
        [Button]
        public void DeregisterNode(Node node)
        {
            int x = node.GridCoord.x;
            int z = node.GridCoord.z;
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
        }

        [Button]
        public void CheckNode(int x, int z)
        {
            string s = TryGetNode(x, z, out Node node) ? $"{node.name} was found" : $"no node found at {x} _ {z}";
            Debug.Log(s);
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