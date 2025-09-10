using System;
using System.Collections.Generic;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public enum CellStatus
    {
        Empty,
        Occupied,
    }
    
    public class  OccupancyMap : IOccupancyMap
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public Vector3Int MapOrigin { get; set; }
        public CellStatus[,] Grid { get; set; }

        private int[,] _mark;
        private int _generation; 
        private Queue<Vector2Int> _queue = new();
        
        private static readonly Vector2Int[] Directions = {
            new Vector2Int(-1,  0),  // left
            new Vector2Int( 1,  0),  // right
            new Vector2Int( 0, -1),  // down
            new Vector2Int( 0,  1)   // up
        };

        public OccupancyMap(PlacementSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("Missing placement settings on the Map");
                return;
            }
            
            MapWidth = settings.mapWidth;
            MapHeight = settings.mapHeight;
            MapOrigin = settings.mapOrigin;
            
            Grid = new CellStatus[MapWidth, MapHeight];
            _mark = new int[MapWidth, MapHeight];
        }
        
        public bool TryPlaceOccupant(int x, int z, int width, int height)
        {
            if(!VacantSpace(x, z, width, height)) return false;

            //Debug.Log($"Registered occupant at {x} | {z}");
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    Grid[i, j] = CellStatus.Occupied;
                }
            }
            
            return true; 
        }

        public void RemoveOccupant(int x, int z, int width, int height)
        {
            // Debug.Log($"Deregister occupant at {x} | {z}");
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    Grid[i, j] = CellStatus.Empty;
                }
            }
        }
        
        public bool VacantSpace(int x, int z, int width, int height)
        {
            if(!InBounds(x, z)) return false;
            if(!InBounds(x + width - 1, z + height - 1)) return false;
            
            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    if (Grid[i, j] == CellStatus.Occupied) 
                        return false; 
                }
            }

            return true; 
        }
        
        public bool VacantCell(int x, int z)
        {
            if (!InBounds(x, z)) return false;
            return Grid[x, z] == CellStatus.Empty; 
        }
        
        public Vector2Int NearestVacantCell(Vector2Int start)
        {
            if (!InBounds(start.x, start.y)) return new Vector2Int(-1, -1);
            if (Grid[start.x, start.y] == CellStatus.Empty) return start;
        
            _generation++; 
            _mark[start.x, start.y] = _generation;
            
            _queue.Clear();
            _queue.Enqueue(start);
        
            while (_queue.Count > 0)
            {
                Vector2Int current = _queue.Dequeue();
        
                // Use Grid.GetNeighbours instead of a manual directions array
                Vector3Int currentV3 = new Vector3Int(current.x, 0, current.y);
                foreach (Vector3Int neighborV3 in Utilities.Grid.GetNeighbours(currentV3, 1, MapWidth, MapHeight))
                {
                    int nx = neighborV3.x;
                    int ny = neighborV3.z;
                    
                    if (_mark[nx, ny] == _generation) 
                        continue;
                    
                    _mark[nx, ny] = _generation;
                        
                    if (Grid[nx, ny] == CellStatus.Empty)
                        return new Vector2Int(nx, ny);
        
                    _queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
            
            // No vacant cell found
            return new Vector2Int(-1, -1); 
        }
        
        public bool InBounds(int x, int z)
        {
            return x >= 0 && x < Grid.GetLength(0) &&
                   z >= 0 && z < Grid.GetLength(1); 
        }
    }
}