using Construction.Maps;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public static class Grid
    {
        public static Vector3Int Position(Vector3 position, Vector3 gridOrigin, int mapWidth, int mapHeight, float tileSize)
        {
            Vector3 relative = position - gridOrigin;

            int gridX = Mathf.FloorToInt(relative.x / tileSize);
            int gridZ = Mathf.FloorToInt(relative.z / tileSize);

            gridX = Mathf.Clamp(gridX, 0, mapWidth - 1);
            gridZ = Mathf.Clamp(gridZ, 0, mapHeight - 1);

            return new Vector3Int(gridX, 0, gridZ);
        }

        public static CellSelection SelectCells(Vector3Int start, UnityEngine.Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings)
        {
            CellSelection selection = new CellSelection();
            
            if (!TryGetInitialHit(mainCamera, floorLayer, cellHits, map, settings, out Vector3Int initialGridPosition))
                return selection;
            
            DetermineSelectionAxis(start, initialGridPosition, selection);
            
            if(settings.useLShapedPaths) 
                SelectCellsAsLShapedPath(start, initialGridPosition, selection, map);
            else
                SelectCellsInRange(start, initialGridPosition, selection, map);

            return selection;
        }

        /// <summary>
        /// Attempts to get the initial grid position where the user clicked on the floor.
        /// 
        /// # How it works
        /// 1. Creates a ray from the camera through the mouse position
        /// 2. Casts the ray to detect intersections with the floor layer
        /// 3. If a hit is found, converts the hit point to grid coordinates
        /// 
        /// </summary>
        private static bool TryGetInitialHit(UnityEngine.Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, IMap map, PlacementSettings settings, out Vector3Int gridPosition)
        {
            gridPosition = Vector3Int.zero;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(ray, cellHits, 300f, floorLayer); 
            
            if (hits <= 0) return false;
            
            gridPosition = Position(cellHits[0].point, settings.gridOrigin, map.MapWidth, map.MapHeight, settings.tileSize);
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
        
        private static void SelectCellsInRange(Vector3Int start, Vector3Int end, CellSelection selection, IMap map)
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
            AddCellsInRange(fixedCoord, ascending ? min : max, ascending ? max : min, ascending, selection, map);
        }

        private static void AddCellsInRange(int fixedCoord, int start, int end, bool ascending, CellSelection selection, IMap map)
        {
            if (ascending)
            {
                for (int i = start; i <= end; i++)
                {
                    if (!IsValidCell(fixedCoord, i, selection.Axis, map)) return;

                    Vector3Int position = CreateCellPosition(fixedCoord, i, selection.Axis);
                    selection.AddCell(position);
                }
            }
            else
            {
                for (int i = start; i >= end; i--)
                {
                    if (!IsValidCell(fixedCoord, i, selection.Axis, map)) return;
                    selection.AddCell(CreateCellPosition(fixedCoord, i, selection.Axis));
                }
            }
        }
        
        private static void SelectCellsAsLShapedPath(Vector3Int start, Vector3Int end, CellSelection selection, IMap map, Direction defaultDirection = Direction.North, bool horizontalFirst = true)
        {
            if (start == end)
            {
                AddCell(start, defaultDirection, selection, map);
                return;
            }

            if (IsStraightLine(start, end))
            {
                AddStraightLine(start, end, selection, map);
                return;
            }
            
            AddLShapedPath(start, end, selection, map, horizontalFirst);
        }

        private static bool IsStraightLine(Vector3Int a, Vector3Int b)
        {
            return a.x == b.x || a.z == b.z;
        }
        
        private static void AddStraightLine(Vector3Int start, Vector3Int end, CellSelection selection, IMap map)
        {
            if (start.x == end.x)
                AddLineAlongAxis(start, end, Axis.ZAxis, selection, map);
            else
                AddLineAlongAxis(start, end, Axis.XAxis, selection, map);
            
            selection.Corner = Corner.None; 
        }
        
        private static void AddLineAlongAxis(Vector3Int from, Vector3Int to, Axis axis, CellSelection selection, IMap map)
        {
            bool ascending = axis == Axis.XAxis ? from.x < to.x : from.z < to.z;
            int step = ascending ? 1 : -1;
            Direction direction = GetDirectionFromAxis(ascending, axis);

            if (axis == Axis.XAxis)
            {
                for (int x = from.x; x != to.x; x += step)
                    AddCell(new Vector3Int(x, 0, from.z), direction, selection, map);
            }
            else
            {
                for (int z = from.z; z != to.z; z += step)
                    AddCell(new Vector3Int(from.x, 0, z), direction, selection, map);
            }

            AddCell(to, direction, selection, map);
        }

        private static void AddLShapedPath(Vector3Int start, Vector3Int end, CellSelection selection, IMap map, bool horizontalFirst)
        {
            Vector3Int corner;

            if (horizontalFirst)
            {
                AddLineAlongAxis(start, new Vector3Int(end.x, 0, start.z), Axis.XAxis, selection, map);
                AddLineAlongAxis(new Vector3Int(end.x, 0, start.z), end, Axis.ZAxis, selection, map);
                corner = new Vector3Int(end.x, 0, start.z);
            }
            else
            {
                AddLineAlongAxis(start, new Vector3Int(start.x, 0, end.z), Axis.ZAxis, selection, map);
                AddLineAlongAxis(new Vector3Int(start.x, 0, end.z), end, Axis.XAxis, selection, map);
                corner = new Vector3Int(start.x, 0, end.z);
            }

            selection.Corner = IsLeftTurn(start, corner, end) ? Corner.Left : Corner.Right; 
            selection.CornerCell = corner;
        }
        
        private static bool IsLeftTurn(Vector3Int start, Vector3Int corner, Vector3Int end)
        {
            // Calculate direction vectors
            Vector3Int firstLeg = corner - start;
            Vector3Int secondLeg = end - corner;
    
            // Calculate cross product in 2D (ignoring y-axis)
            int crossProduct = (firstLeg.x * secondLeg.z) - (firstLeg.z * secondLeg.x);
    
            // If cross product is positive, it's a left turn, else right turn
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
        
        private static void AddCell(Vector3Int cell, Direction direction, CellSelection selection, IMap map)
        { 
            if (!map.VacantCell(cell.x, cell.z)) return;
            selection.AddCell(cell, direction);
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
    }
}