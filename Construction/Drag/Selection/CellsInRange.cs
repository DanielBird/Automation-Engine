using Construction.Placement;
using UnityEngine;

namespace Construction.Drag.Selection
{
    public static class CellsInRange
    {
        public static void SelectCells(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
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
                if (!CellSelector.IsValidCell(fixedCoord, i, selection.Axis, selectionParams.Map)) return;
                Vector3Int position = CellSelector.CreateCellPosition(fixedCoord, i, selection.Axis);
                selection.AddCell(position, selectionParams.Settings);
            }
        }
    }
}