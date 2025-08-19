using System.Collections.Generic;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Drag.Selection
{
    public static class CellsByLShape
    {
        public static void SelectCells(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams, Direction defaultDirection = Direction.North, bool horizontalFirst = true)
        {
            if (start == end)
            {
                CellSelector.AddSingleCell(selection, selectionParams, start, defaultDirection);
                return;
            }

            if (Grid.IsStraightLine(start, end))
            {
                AddStraightLine(start, end, selection, selectionParams);
                return;
            }
            
            AddLShapedPath(start, end, selection, selectionParams, horizontalFirst);
        }
        
        private static void AddStraightLine(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
        {
            if (start.x == end.x)
                AddLineAlongAxis(start, end, Axis.ZAxis, selection, selectionParams);
            else
                AddLineAlongAxis(start, end, Axis.XAxis, selection, selectionParams);
            
            selection.Corner = Corner.None; 
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
            Direction direction = CellSelector.GetDirectionFromAxis(ascending, axis);

            HashSet<Cell> cells = new(); 
            
            // Add the main cells 
            if (axis == Axis.XAxis)
            {
                int xStart = from.x + step;
                for (int x = xStart; ascending ? x < to.x : x >  to.x; x += step)
                {
                    CellSelector.AddCell(selectionParams, x, from.z, cells, direction);
                }
            }
            else
            {
                int zStart = from.z + step;
                for (int z = zStart; ascending ? z < to.z : z > to.z; z += step)
                {
                    CellSelector.AddCell(selectionParams, from.x, z, cells, direction);
                }
            }

            // Add the 'from' cell to the HashSet
            if (startIsCorner) cells.Add(new Cell(from, direction, cornerType, selectionParams.Settings));
            else CellSelector.AddStartEndCell(cells, from, direction, selectionParams, false);
            
            // Add the 'to' cell to the HashSet
            if (!excludeEnd)
                CellSelector.AddStartEndCell(cells, to, direction, selectionParams, true);
            
            // Add the HashSet of cells to the cell selection class
            selection.AddCells(cells);
        }
        
        private static void AddLShapedPath(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams, bool horizontalFirst)
        {
            Vector3Int corner;
            Vector3Int adjustedEnd = CellSelector.SnapToStepGrid(start, end, selectionParams.StepSize);

            if (horizontalFirst)
            {
                corner = new Vector3Int(adjustedEnd.x, 0, start.z);
                NodeType cornerType =  CellSelector.IsLeftTurn(start, corner, adjustedEnd) ? NodeType.LeftCorner : NodeType.RightCorner;
                AddLineAlongAxis(start, corner, Axis.XAxis, selection, selectionParams, true);
                AddLineAlongAxis(corner, adjustedEnd, Axis.ZAxis, selection, selectionParams, false,true, cornerType);
            }
            else
            {
                corner = new Vector3Int(start.x, 0, adjustedEnd.z);
                NodeType cornerType =  CellSelector.IsLeftTurn(start, corner, adjustedEnd) ? NodeType.LeftCorner : NodeType.RightCorner;
                AddLineAlongAxis(start, corner, Axis.ZAxis, selection, selectionParams, true);
                AddLineAlongAxis(corner, adjustedEnd, Axis.XAxis, selection, selectionParams, false, true, cornerType);
            }
            
            selection.Corner = CellSelector.IsLeftTurn(start, corner, adjustedEnd) ? Corner.Left : Corner.Right; 
            selection.SetCornerGridCoord(corner);
        }
    }
}