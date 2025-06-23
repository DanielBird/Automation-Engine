using System;
using Construction.Nodes;
using Construction.Placement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Construction.Maps
{
    [RequireComponent(typeof(Map))]
    public class NodeMap : MonoBehaviour, INodeMap
    {
        [ShowInInspector] private Node[,] _nodeGrid;
        public Map map;

        private void Awake()
        {
            if (map == null) map = GetComponent<Map>();
            _nodeGrid = new Node[map.mapWidth, map.mapHeight];
        }

        public void RegisterNode(Node node)
        {
            int x = node.Position.x;
            int z = node.Position.z;
            int width = node.Width;
            int height = node.Height; 
            
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
            int x = node.Position.x;
            int z = node.Position.z;
            int width = node.Width;
            int height = node.Height; 
            
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
            if (GetNode(x, z, out Node node))
            {
                Debug.Log(node.name + " was found");
            }
            else
            {
                Debug.Log("No node found at " + x + " " + z);
            }
        }

        public bool GetNode(int x, int z, out Node node)
        {
            if (_nodeGrid[x, z] == null)
            {
                node = null; 
                return false;
            }
            
            node = _nodeGrid[x, z];
            return true; 
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
            node = null; 
            
            if (!InBounds(position.x, position.y)) return false;
            if (_nodeGrid[position.x, position.y] == null) return false;
            
            node = _nodeGrid[position.x, position.y];
            return true; 
        }
        
        private bool InBounds(int x, int z)
        {
            return x >= 0 && x < _nodeGrid.GetLength(0) &&
                   z >= 0 && z < _nodeGrid.GetLength(1); 
        }
    }
}