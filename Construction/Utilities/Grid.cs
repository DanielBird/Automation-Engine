using System;
using System.Collections.Generic;
using Construction.Maps;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public static class Grid
    {
        /// <summary>
        /// Converts a world position to a grid-aligned world position, centered on the nearest tile.
        /// </summary>
        public static Vector3Int GridAlignedWorldPosition(Vector3 position, GridParams gridParams)
        {
            Vector3Int gridPos = WorldToGridCoordinate(position, gridParams);
            Vector3Int worldPosition = GridToWorldPosition(gridPos, gridParams.Origin, gridParams.TileSize); 
            return worldPosition;
        }
        
        /// <summary>
        /// Converts a world position to grid coordinate, within the map bounds
        /// </summary>
        public static Vector3Int WorldToGridCoordinate(Vector3 position, GridParams gp)
        {
            Vector3 relative = position - gp.Origin;

            int gridX = Mathf.FloorToInt(relative.x / gp.TileSize);
            int gridZ = Mathf.FloorToInt(relative.z / gp.TileSize);

            gridX = Mathf.Clamp(gridX, 0, gp.Width - 1);
            gridZ = Mathf.Clamp(gridZ, 0, gp.Height - 1);

            return new Vector3Int(gridX, 0, gridZ);
        }
        
        /// <summary>
        /// Converts a grid coordinate to a world position, aligned to the grid.
        /// </summary>
        public static Vector3Int GridToWorldPosition(Vector3Int gridCoord, Vector3Int gridOrigin, float tileSize)
        {
            int worldX = Mathf.FloorToInt(gridOrigin.x + (gridCoord.x * tileSize) + (tileSize * 0.5f));
            int worldZ = Mathf.FloorToInt(gridOrigin.z + (gridCoord.z * tileSize) + (tileSize * 0.5f));
            return new Vector3Int(worldX, 0, worldZ);
        }

        /// <summary>
        /// Converts a list of grid coordinates to world positions.
        /// </summary>
        public static List<Vector3Int> GridToWorldPositions(List<Vector3Int> gridPositions, Vector3Int gridOrigin, float tileSize)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>(gridPositions.Count);
            float halfTileSize = tileSize * 0.5f;
            float originX = gridOrigin.x;
            float originZ = gridOrigin.z;

            foreach (var gridPos in gridPositions)
            {
                int worldX = Mathf.FloorToInt(originX + (gridPos.x * tileSize) + halfTileSize);
                int worldZ = Mathf.FloorToInt(originZ + (gridPos.z * tileSize) + halfTileSize);
                worldPositions.Add(new Vector3Int(worldX, 0, worldZ));
            }

            return worldPositions;
        }
        
        public static CellSelection SelectCells(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, CellSelectionParams selectionParams)
        {
            CellSelection selection = new CellSelection();

            if (!TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, selectionParams.Map, selectionParams.Settings, out Vector3Int endGridCoord))
            {
                return selection;
            }
            
            DetermineSelectionAxis(start, endGridCoord, selection);

            switch (selectionParams.Settings.cellSelectionAlgorithm)
            {
                case CellSelectionAlgorithm.StraightLinesOnly:
                    SelectCellsInRange(start, endGridCoord, selection, selectionParams);
                    break;
                case CellSelectionAlgorithm.LShapedPaths:
                    SelectCellsAsLShapedPath(start, endGridCoord, selection, selectionParams);
                    break;
                case CellSelectionAlgorithm.FindShortestPath:
                    SelectCellsByShortestPath(start, endGridCoord, selection, selectionParams);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return selection;
        }

        public static CellSelection SelectCellArea(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings, out Vector3Int end, int stepSize = 1)
        {
            CellSelection selection = new();
            
            if (!TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, map, settings, out end))
                return selection;
            
            IEnumerable<Vector3Int> cells = GetCellArea(start, end, stepSize);
            selection.AddRangeToDictionary(cells, Direction.North, settings);
            
            return selection;
        }

        private static IEnumerable<Vector3Int> GetCellArea(Vector3Int start, Vector3Int end, int stepSize)
        {
            int xMin = Mathf.Min(start.x, end.x);
            int xMax = Mathf.Max(start.x, end.x);
            int zMin = Mathf.Min(start.z, end.z);
            int zMax = Mathf.Max(start.z, end.z);
            
            for (int x = xMin; x <= xMax; x += stepSize)
            for (int z = zMin; z <= zMax; z += stepSize)
                yield return new Vector3Int(x, 0, z);
        }
        
        /// <summary>
        /// Attempts to get the current grid position where a raycast intersects with the floor.
        /// </summary>
        private static bool TryGetCurrentGridCoord(Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings, out Vector3Int gridCoordinate)
        {
            gridCoordinate = Vector3Int.zero;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(ray, cellHits, 300f, floorLayer); 
            if (hits <= 0) return false;
            
            gridCoordinate = WorldToGridCoordinate(cellHits[0].point, new GridParams(settings.mapOrigin, map.MapWidth, map.MapHeight, settings.cellSize));
            return true;
        }

        /// <summary>
        /// Determines whether the user is selecting cells horizontally or vertically based on mouse movement.
        /// 
        /// # How it works
        /// 1. Calculates the absolute differences in X and Z coordinates between start and current position
        /// 2. If no movement is detected (both deltas are 0), returns without changes
        /// 3. Compares the movement in X and Z directions:
        ///    - If movement along X ≤ movement along Z: Sets axis to XAxis (vertical selection)
        ///    - If movement along X > movement along Z: Sets axis to ZAxis (horizontal selection)
        /// 
        /// </summary>

        private static void DetermineSelectionAxis(Vector3Int start, Vector3Int currentPosition, CellSelection selection)
        {
            int deltaX = Mathf.Abs(currentPosition.x - start.x);
            int deltaZ = Mathf.Abs(currentPosition.z - start.z);
            if (deltaX == 0 && deltaZ == 0) return;

            // If movement along x is less than or equal to movement along z, assume a vertical selection (x remains fixed).
            // Otherwise, assume a horizontal selection (z remains fixed).
            selection.SetAxis(deltaX <= deltaZ ? Axis.XAxis : Axis.ZAxis);
        }
        
        private static void SelectCellsInRange(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
        {
            int min, max, fixedCoord;
            bool ascending;
            Direction direction;
    
            if (selection.Axis == Axis.XAxis)
            {
                min = Mathf.Min(start.z, end.z);
                max = Mathf.Max(start.z, end.z);
                fixedCoord = start.x;
                ascending = start.z <= end.z;
                direction = ascending ? Direction.North : Direction.South;
            }
            else
            {
                min = Mathf.Min(start.x, end.x);
                max = Mathf.Max(start.x, end.x);
                fixedCoord = start.z;
                ascending = start.x <= end.x;
                direction = ascending ? Direction.East : Direction.West;
            }
    
            selection.SetDirection(direction);
            AddCellsInRange(fixedCoord, ascending ? min : max, ascending ? max : min, ascending, selection, selectionParams);
        }

        private static void AddCellsInRange(int fixedCoord, int start, int end, bool ascending, CellSelection selection, CellSelectionParams selectionParams)
        {
            int stepSize = selectionParams.StepSize; 
            if(stepSize == 0) stepSize = 1;
            
            for (int i = start; ascending ? i <= end : i >= end; i += (ascending ? stepSize : -stepSize))
            {
                if (!IsValidCell(fixedCoord, i, selection.Axis, selectionParams.Map)) return;
                Vector3Int position = CreateCellPosition(fixedCoord, i, selection.Axis);
                selection.AddCell(position, selectionParams.Settings);
            }
        }
        
        private static void SelectCellsAsLShapedPath(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams, Direction defaultDirection = Direction.North, bool horizontalFirst = true)
        {
            if (start == end)
            {
                AddSingleCell(selection, selectionParams, start, defaultDirection);
                return;
            }

            if (IsStraightLine(start, end))
            {
                AddStraightLine(start, end, selection, selectionParams);
                return;
            }
            
            AddLShapedPath(start, end, selection, selectionParams, horizontalFirst);
        }
                
        private static void AddSingleCell(CellSelection selection, CellSelectionParams selectionParams, Vector3Int cell, Direction direction, NodeType type = NodeType.Straight)
        { 
            int x = cell.x;
            int z = cell.z;
            
            if (!selectionParams.Map.VacantCell(x, z))
            {
                if (selectionParams.NodeMap.HasNode(x, z)) type = NodeType.Intersection; 
                else return;
            }
            selection.AddCell(cell, direction, type, selectionParams.Settings);
        }
        
        private static bool IsStraightLine(Vector3Int a, Vector3Int b)
        {
            return a.x == b.x || a.z == b.z;
        }
        
        private static void AddStraightLine(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
        {
            if (start.x == end.x)
                AddLineAlongAxis(start, end, Axis.ZAxis, selection, selectionParams);
            else
                AddLineAlongAxis(start, end, Axis.XAxis, selection, selectionParams);
            
            selection.Corner = Corner.None; 
        }
        
        private static void AddLShapedPath(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams, bool horizontalFirst)
        {
            Vector3Int corner;
            Vector3Int adjustedEnd = SnapToStepGrid(start, end, selectionParams.StepSize);

            if (horizontalFirst)
            {
                corner = new Vector3Int(adjustedEnd.x, 0, start.z);
                NodeType cornerType =  IsLeftTurn(start, corner, adjustedEnd) ? NodeType.LeftCorner : NodeType.RightCorner;
                AddLineAlongAxis(start, corner, Axis.XAxis, selection, selectionParams, true);
                AddLineAlongAxis(corner, adjustedEnd, Axis.ZAxis, selection, selectionParams, false,true, cornerType);
            }
            else
            {
                corner = new Vector3Int(start.x, 0, adjustedEnd.z);
                NodeType cornerType =  IsLeftTurn(start, corner, adjustedEnd) ? NodeType.LeftCorner : NodeType.RightCorner;
                AddLineAlongAxis(start, corner, Axis.ZAxis, selection, selectionParams, true);
                AddLineAlongAxis(corner, adjustedEnd, Axis.XAxis, selection, selectionParams, false, true, cornerType);
            }
            
            selection.Corner = IsLeftTurn(start, corner, adjustedEnd) ? Corner.Left : Corner.Right; 
            selection.SetCornerGridCoord(corner);
        }
        
        private static void AddLineAlongAxis(
            Vector3Int from, Vector3Int to, Axis axis, 
            CellSelection selection, CellSelectionParams selectionParams,
            bool excludeEnd = false, 
            bool startIsCorner = false, NodeType cornerType = NodeType.LeftCorner)
        {
            bool ascending = axis == Axis.XAxis 
                ? from.x < to.x 
                : from.z < to.z;
            
            int step = ascending ? selectionParams.StepSize : -selectionParams.StepSize;
            Direction direction = GetDirectionFromAxis(ascending, axis);

            HashSet<Cell> cells = new(); 
            
            // Add the main cells 
            if (axis == Axis.XAxis)
            {
                int xStart = from.x + step;
                for (int x = xStart; ascending ? x < to.x : x >  to.x; x += step)
                {
                    AddCell(selectionParams, x, from.z, cells, direction);
                }
            }
            else
            {
                int zStart = from.z + step;
                for (int z = zStart; ascending ? z < to.z : z > to.z; z += step)
                {
                    AddCell(selectionParams, from.x, z, cells, direction);
                }
            }

            // Add the 'from' cell to the HashSet
            if (startIsCorner) cells.Add(new Cell(from, direction, cornerType, selectionParams.Settings));
            else AddStartEndCell(cells, from, direction, selectionParams, false);
            
            // Add the 'to' cell to the HashSet
            if (!excludeEnd)
                AddStartEndCell(cells, to, direction, selectionParams, true);
            
            // Add the HashSet of cells to the cell selection class
            selection.AddCells(cells);
        }

        private static void AddCell(CellSelectionParams selectionParams, int x, int z, HashSet<Cell> cells, Direction direction, NodeType nodeType = NodeType.Straight)
        {
            Vector3Int gridCoord = new (x, 0, z); 
            if (selectionParams.Intersections.Contains(gridCoord)) nodeType = NodeType.Intersection;
            cells.Add(new Cell(gridCoord, direction, nodeType, selectionParams.Settings));
        }

        private static void AddStartEndCell(HashSet<Cell> cells, Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, bool end)
        {
            if (selectionParams.Intersections.Contains(gridCoord))
            {
                if (end) cells.Add(new Cell(gridCoord, direction, NodeType.Intersection, selectionParams.Settings));
                return;
            }
            
            // Check if the left or right neighbouring cells are occupied by another node

            int stepSize = selectionParams.StepSize;
            stepSize = Mathf.Abs(stepSize); // step should always be positive for this method
            
            Direction rightDirection = DirectionUtils.Increment(direction);
            Direction leftDirection = DirectionUtils.Decrement(direction);
            
            Vector2Int rightPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, rightDirection, stepSize);
            Vector2Int leftPos = PositionByDirection.Get(gridCoord.x, gridCoord.z, leftDirection, stepSize);

            bool rightFound = selectionParams.NodeMap.GetNeighbourAt(rightPos, out Node rightN);
            bool leftFound  = selectionParams.NodeMap.GetNeighbourAt(leftPos, out Node leftN);
            
            bool right = rightFound && rightN.Direction == (end ? rightDirection : leftDirection);
            bool left = leftFound && leftN.Direction == (end ? leftDirection : rightDirection);
            
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

            Direction finalDirection = (left, right) switch
            {
                (false,  false) => direction,
                (true,  false) => end ? leftDirection : direction,
                (false, true)  => end ? rightDirection : direction,
                _              => direction
            };

            cells.Add(new Cell(gridCoord, finalDirection, nodeType, selectionParams.Settings));
        }
        
        private static Vector3Int SnapToStepGrid(Vector3Int start, Vector3Int end, int stepSize)
        {
            if (stepSize <= 1) return end;
    
            // Calculate the number of steps from start to end for each axis
            int deltaX = end.x - start.x;
            int deltaZ = end.z - start.z;
    
            // Snap to the nearest step-aligned position
            int stepsX = Mathf.RoundToInt((float)deltaX / stepSize);
            int stepsZ = Mathf.RoundToInt((float)deltaZ / stepSize);
    
            return new Vector3Int(
                start.x + stepsX * stepSize,
                end.y,
                start.z + stepsZ * stepSize
            );
        }
        
        private static bool IsLeftTurn(Vector3Int start, Vector3Int corner, Vector3Int end)
        {
            // Calculate direction vectors
            Vector3Int firstLeg = corner - start;
            Vector3Int secondLeg = end - corner;
    
            // Calculate cross-product in 2D (ignoring y-axis)
            int crossProduct = (firstLeg.x * secondLeg.z) - (firstLeg.z * secondLeg.x);
    
            // If cross-product is positive, it's a left turn, else right turn
            return crossProduct > 0;
        }
        
        private static Direction GetDirectionFromAxis(bool ascending, Axis axis)
        {
            switch (axis)
            {
                case Axis.XAxis:
                    if(ascending) return Direction.East;
                    return Direction.West;
                case Axis.ZAxis:
                    if (ascending) return Direction.North;
                    return Direction.South;
            }
            return Direction.North;
        }
        
        private static bool IsValidCell(int fixedCoord, int variableCoord, Axis axis, IMap map)
        {
            return axis == Axis.XAxis 
                ? map.VacantCell(fixedCoord, variableCoord)
                : map.VacantCell(variableCoord, fixedCoord);
        }

        private static Vector3Int CreateCellPosition(int fixedCoord, int variableCoord, Axis axis)
        {
            return axis == Axis.XAxis
                ? new Vector3Int(fixedCoord, 0, variableCoord)
                : new Vector3Int(variableCoord, 0, fixedCoord);
        }
        
        private static void SelectCellsByShortestPath(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
        {
            // Snap end to step grid so path expands in multiples of StepSize
            Vector3Int adjustedEnd = SnapToStepGrid(start, end, selectionParams.StepSize);

            // Single cell selection
            if (start == adjustedEnd)
            {
                AddSingleCell(selection, selectionParams, start, Direction.North);
                selection.Corner = Corner.None;
                return;
            }

            List<Vector3Int> path = FindShortestPath(start, adjustedEnd, selectionParams);
            if (path == null || path.Count == 0)
                return;

            AddPathCells(path, selection, selectionParams);
            selectionParams.FilterIntersections(path);
            selection.Corner = Corner.None; // multi-corner paths: don't use single-corner UI
        }
        
        private static List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, CellSelectionParams p)
        {
            int w = p.Map.MapWidth;
            int h = p.Map.MapHeight;
            int step = Mathf.Max(1, p.StepSize);

            // Breadth-first search
            Queue<Vector3Int> q = new();
            HashSet<Vector3Int> visited = new();
            Dictionary<Vector3Int, Vector3Int> parent = new();

            q.Enqueue(start);
            visited.Add(start);

            while (q.Count > 0)
            {
                Vector3Int cur = q.Dequeue();
                if (cur == goal)
                {
                    List<Vector3Int> path = new();
                    Vector3Int t = goal;
                    while (true)
                    {
                        path.Add(t);
                        if (t == start) break;
                        t = parent[t];
                    }
                    path.Reverse();
                    return path;
                }

                foreach (Vector3Int nb in GetNeighbours(cur, step, w, h))
                {
                    if (visited.Contains(nb)) continue;
                    if (!IsPassable(nb.x, nb.z, p)) continue;

                    visited.Add(nb);
                    parent[nb] = cur;
                    q.Enqueue(nb);
                }
            }

            // No path
            return null;
        }
        
        private static IEnumerable<Vector3Int> GetNeighbours(Vector3Int c, int step, int width, int height)
        {
            // 4-connected moves by step
            int nx;

            nx = c.x + step; if (nx >= 0 && nx < width) yield return new Vector3Int(nx, 0, c.z);
            nx = c.x - step; if (nx >= 0 && nx < width) yield return new Vector3Int(nx, 0, c.z);

            int nz;
            nz = c.z + step; if (nz >= 0 && nz < height) yield return new Vector3Int(c.x, 0, nz);
            nz = c.z - step; if (nz >= 0 && nz < height) yield return new Vector3Int(c.x, 0, nz);
        }

        private static bool IsPassable(int x, int z, CellSelectionParams p)
        {
            // Allow moving through vacant cells; 
            if (p.Map.VacantCell(x, z)) return true;
            
            // Allow intersections
            if (ViableAsIntersection(x, z, p))
            {
                p.Intersections.Add(new Vector3Int(x, 0, z));
                return true;
            }
            
            return false;
        }

        private static bool ViableAsIntersection(int x, int z, CellSelectionParams p)
        {
            // if there is a node, and that node is a straight node, and it is connected...
            // then this is viable as an intersection
            if (!p.NodeMap.TryGetNode(x, z, out Node node)) return false;
            if (node.NodeType != NodeType.Straight) return false;
            return node.IsConnected();
        }
        
        private static void AddPathCells(List<Vector3Int> path, CellSelection selection, CellSelectionParams selectionParams)
        {
            int n = path.Count;
            int step = Mathf.Max(1, selectionParams.StepSize);
            HashSet<Cell> cells = new();

            if (n == 1)
            {
                // Single cell
                AddSingleCell(selection, selectionParams, path[0], Direction.North);
                selection.AddCells(cells);
                return;
            }

            // First cell (start)
            Direction firstDir = DirectionBetween(path[0], path[1]);
            AddStartEndCell(cells, path[0], firstDir, selectionParams, false);

            // Middle cells
            for (int i = 1; i < n - 1; i++)
            {
                Direction prevDir = DirectionBetween(path[i - 1], path[i]);
                Direction nextDir = DirectionBetween(path[i], path[i + 1]);

                if (prevDir == nextDir)
                {
                    // Straight
                    AddCell(selectionParams, path[i].x, path[i].z, cells, nextDir);
                }
                else
                {
                    // Corner at path[i]
                    bool leftTurn = IsLeftTurn(path[i - 1], path[i], path[i + 1]);
                    NodeType cornerType = leftTurn ? NodeType.LeftCorner : NodeType.RightCorner;
                    AddCell(selectionParams, path[i].x, path[i].z, cells, nextDir, cornerType);
                }
            }

            // Last cell (end)
            Direction lastDir = DirectionBetween(path[n - 2], path[n - 1]);
            AddStartEndCell(cells, path[n - 1], lastDir, selectionParams, true);

            selection.AddCells(cells);
        }
        
        private static Direction DirectionBetween(Vector3Int a, Vector3Int b)
        {
            if (b.x > a.x) return Direction.East;
            if (b.x < a.x) return Direction.West;
            if (b.z > a.z) return Direction.North;
            return Direction.South;
        }
    }
}