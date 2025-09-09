using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Drag.Selection
{
    public static class CellSelector
    {
        // MAIN SELECTION
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
                    CellsInRange.SelectCells(start, endGridCoord, selection, selectionParams);
                    break;
                case CellSelectionAlgorithm.LShapedPaths:
                    CellsByLShape.SelectCells(start, endGridCoord, selection, selectionParams);
                    break;
                case CellSelectionAlgorithm.FindShortestPath:
                    CellsByShortestPath.SelectCells(start, endGridCoord, selection, selectionParams);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return selection;
        }

        /// <summary>
        /// Attempts to get the current grid position where a raycast intersects with the floor.
        /// </summary>
        public static bool TryGetCurrentGridCoord(Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings, out Vector3Int gridCoordinate)
        {
            gridCoordinate = Vector3Int.zero;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(ray, cellHits, 300f, floorLayer); 
            if (hits <= 0) return false;
            
            gridCoordinate = Grid.WorldToGridCoordinate(cellHits[0].point, new GridParams(settings.mapOrigin, map.MapWidth, map.MapHeight, settings.cellSize));
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
        
        // SELECT BY AREA
        public static CellSelection SelectCellArea(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings, out Vector3Int end, int stepSize = 1)
        {
            CellSelection selection = new();
            
            if (!TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, map, settings, out end))
                return selection;
            
            IEnumerable<Vector3Int> cells = GetCellArea(start, end, stepSize);
            selection.AddRangeToDictionary(cells, Direction.North, settings);
            
            return selection;
        }

        public static IEnumerable<Vector3Int> GetCellArea(Vector3Int start, Vector3Int end, int stepSize)
        {
            int xMin = Mathf.Min(start.x, end.x);
            int xMax = Mathf.Max(start.x, end.x);
            int zMin = Mathf.Min(start.z, end.z);
            int zMax = Mathf.Max(start.z, end.z);
            
            for (int x = xMin; x <= xMax; x += stepSize)
            for (int z = zMin; z <= zMax; z += stepSize)
                yield return new Vector3Int(x, 0, z);
        }
        
        public static CellSelection SelectCellAreaWithNodes(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, CellSelectionParams csp, bool ignoreType, out Vector3Int end)
        {
            CellSelection selection = new();
            
            if (!TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, csp.Map, csp.Settings, out end))
                return selection;
            
            IEnumerable<Vector3Int> cells = GetCellArea(start, end, csp.StepSize);

            if (!ignoreType)
            {
                foreach (Vector3Int cell in cells)
                {
                    if(csp.NodeMap.TryGetNode(cell.x, cell.z, out Node node))
                        selection.AddCell(cell, node.Direction, node.NodeType, csp.Settings);
                }
            }
            else
            {
                foreach (Vector3Int cell in cells)
                {
                    if(csp.NodeMap.HasNode(cell.x, cell.z))
                        selection.AddCell(cell, Direction.North, NodeType.GenericBelt, csp.Settings);
                }
            }

            return selection; 
        }
        
        // UTILITIES
        
        public static void AddCell(CellSelectionParams selectionParams, int x, int z, HashSet<Cell> cells, Direction direction, NodeType nodeType = NodeType.Straight)
        {
            Vector3Int gridCoord = new (x, 0, z); 
            if (selectionParams.Intersections.Contains(gridCoord)) nodeType = NodeType.Intersection;
            cells.Add(new Cell(gridCoord, direction, nodeType, selectionParams.Settings));
        }
        
        public static void AddSingleCell(CellSelection selection, CellSelectionParams selectionParams, Vector3Int cell, Direction direction, NodeType type = NodeType.Straight)
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
        
        // Lock the position to the step-aligned grid, anchored anchored relative to start
        public static Vector3Int SnapToStepGrid(Vector3Int start, Vector3Int end, int stepSize)
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
        
        // Determine the type of node to be spawned at the start and end of a path
        public static void AddStartEndCell(HashSet<Cell> cells, Vector3Int gridCoord, Direction direction, CellSelectionParams selectionParams, bool end)
        {
            if (selectionParams.Intersections.Contains(gridCoord))
            {
                if (end) cells.Add(new Cell(gridCoord, direction, NodeType.Intersection, selectionParams.Settings));
                return;
            }
            
            NodeType nodeType = CellDefinition.DefinePathCell(cells, gridCoord, direction, selectionParams, end, out Direction finalDirection);
            cells.Add(new Cell(gridCoord, finalDirection, nodeType, selectionParams.Settings));
        }
        
        public static bool IsLeftTurn(Vector3Int start, Vector3Int corner, Vector3Int end)
        {
            // Calculate direction vectors
            Vector3Int firstLeg = corner - start;
            Vector3Int secondLeg = end - corner;
    
            // Calculate cross-product in 2D (ignoring y-axis)
            int crossProduct = (firstLeg.x * secondLeg.z) - (firstLeg.z * secondLeg.x);
    
            // If cross-product is positive, it's a left turn, else right turn
            return crossProduct > 0;
        }
        
        public static Direction GetDirectionFromAxis(bool ascending, Axis axis)
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
        
        public static bool IsValidCell(int fixedCoord, int variableCoord, Axis axis, IMap map)
        {
            return axis == Axis.XAxis 
                ? map.VacantCell(fixedCoord, variableCoord)
                : map.VacantCell(variableCoord, fixedCoord);
        }

        public static Vector3Int CreateCellPosition(int fixedCoord, int variableCoord, Axis axis)
        {
            return axis == Axis.XAxis
                ? new Vector3Int(fixedCoord, 0, variableCoord)
                : new Vector3Int(variableCoord, 0, fixedCoord);
        }
    }
}