﻿using System.Collections.Generic;
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

        private int[,] _mark;
        private int _generation; 
        private Queue<Vector2Int> _queue;
        
        private static readonly Vector2Int[] Directions = {
            new Vector2Int(-1,  0),  // left
            new Vector2Int( 1,  0),  // right
            new Vector2Int( 0, -1),  // down
            new Vector2Int( 0,  1)   // up
        };

        private void Awake()
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            _grid = new CellStatus[MapWidth, MapHeight];
            _mark = new int[MapWidth, MapHeight];
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
                        
                    if (_grid[nx, ny] == CellStatus.Empty)
                        return new Vector2Int(nx, ny);

                    _queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
            
            // No vacant cell found
            return new Vector2Int(-1, -1); 
        }

        private bool InBounds(int x, int z)
        {
            return x >= 0 && x < _grid.GetLength(0) &&
                   z >= 0 && z < _grid.GetLength(1); 
        }
    }
}