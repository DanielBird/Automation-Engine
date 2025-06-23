using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Construction.Maps
{
    public enum CellStatus
    {
        Empty,
        Occupied,
    }
    
    public class Map : MonoBehaviour, IMap
    {
        public int mapWidth = 50;
        public int mapHeight = 50;
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        
        [ShowInInspector] private CellStatus[,] _grid;

        private void Awake()
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            _grid = new CellStatus[MapWidth, MapHeight];
        }
        
        public bool RegisterOccupant(int x, int z, int width, int height)
        {
            if(!VacantSpace(x, z, width, height)) return false;

            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    _grid[i, j] = CellStatus.Occupied;
                }
            }

            return true; 
        }

        public void DeregisterOccupant(int x, int z, int width, int height)
        {
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    _grid[i, j] = CellStatus.Empty;
                }
            }
        }

        public bool VacantSpace(int x, int z, int width, int height)
        {
            if(!InBounds(x, z)) return false;
            if(!InBounds(x + width, z + height)) return false;
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    if (_grid[i, j] == CellStatus.Occupied) 
                        return false; 
                }
            }

            return true; 
        }
        
        public bool VacantCell(int x, int z)
        {
            if (!InBounds(x, z)) return false;
            return _grid[x, z] == CellStatus.Empty; 
        }

        public Vector2Int NearestVacantCell(Vector2Int start)
        {
            if (VacantCell(start.x, start.y)) return start;

            bool[,] visited = new bool[MapWidth, MapHeight];

            Queue<Vector2Int> queue = new Queue<Vector2Int>(); 
            
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (Vector2Int v in GetNeighbours(current))
                {
                    int nx = v.x;
                    int ny = v.y;
                    if (!visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        if (_grid[nx, ny] == CellStatus.Empty)
                            return v; 
                        
                        queue.Enqueue(v);
                    }
                }
            }

            return new Vector2Int(-1, -1); 
        }

        public IEnumerable<Vector2Int> GetNeighbours(Vector2Int start)
        {
            int x = start.x;
            int y = start.y;

            if (x > 0) yield return new Vector2Int(x - 1, y);
            if (x < mapWidth - 1) yield return new Vector2Int(x + 1, y);
            if (y > 0) yield return new Vector2Int(x, y - 1);
            if (y < mapHeight - 1) yield return new Vector2Int(x, y + 1); 
        }

        private bool InBounds(int x, int z)
        {
            return x >= 0 && x < _grid.GetLength(0) &&
                   z >= 0 && z < _grid.GetLength(1); 
        }
    }
}