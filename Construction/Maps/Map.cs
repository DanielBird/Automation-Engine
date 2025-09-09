using System;
using System.Collections.Generic;
using Engine.Construction.Events;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public enum CellStatus
    {
        Empty,
        Occupied,
    }
    
    public class  Map : IMap
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public Vector3Int MapOrigin { get; set; }
        public CellStatus[,] Grid { get; set; }

        private int[,] _mark;
        private int _generation; 
        private Queue<Vector2Int> _queue = new();

        private GridParams gridParams; 
        
        private static readonly Vector2Int[] Directions = {
            new Vector2Int(-1,  0),  // left
            new Vector2Int( 1,  0),  // right
            new Vector2Int( 0, -1),  // down
            new Vector2Int( 0,  1)   // up
        };

        private EventBinding<RegisterOccupantEvent> _onRequestOccupation; 

        public Map(PlacementSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("Missing placement settings on the Map");
                return;
            }
            
            MapWidth = settings.mapWidth;
            MapHeight = settings.mapHeight;
            MapOrigin = settings.mapOrigin;
            
            gridParams = new GridParams(MapOrigin, MapWidth, MapHeight, settings.cellSize);
            
            Grid = new CellStatus[MapWidth, MapHeight];
            _mark = new int[MapWidth, MapHeight];

            _onRequestOccupation = new EventBinding<RegisterOccupantEvent>(OnRequestOccupation); 
            EventBus<RegisterOccupantEvent>.Register(_onRequestOccupation);
        }

        public void Disable()
        {
            EventBus<RegisterOccupantEvent>.Deregister(_onRequestOccupation);
        }

        private void OnRequestOccupation(RegisterOccupantEvent ev)
        {
            Vector3Int gridCoord = Utilities.Grid.WorldToGridCoordinate(ev.WorldPosition, gridParams);

            if (!RegisterOccupant(gridCoord.x, gridCoord.z, ev.GridWidth, ev.GridHeight))
            {
                Debug.LogWarning($"Failed to register occupant: {ev.Occupant.name}");
            }
        }
        
        public bool RegisterOccupant(int x, int z, int width, int height)
        {
            if(!VacantSpace(x, z, width, height)) return false;

            for (int i = x; i < x + width; i++)
            {
                for (int j = z; j < z + height; j++)
                {
                    Grid[i, j] = CellStatus.Occupied;
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
                    Grid[i, j] = CellStatus.Empty;
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

        public Vector2Int NearestVacantCell_Legacy(Vector2Int start)
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

                for (int i = 0; i < Directions.Length; i++)
                {
                    int nx = current.x + Directions[i].x;
                    int ny = current.y + Directions[i].y;
                    if (nx < 0 || nx >= MapWidth || ny < 0 || ny >= MapHeight)
                        continue;

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

        private bool InBounds(int x, int z)
        {
            return x >= 0 && x < Grid.GetLength(0) &&
                   z >= 0 && z < Grid.GetLength(1); 
        }
    }
}