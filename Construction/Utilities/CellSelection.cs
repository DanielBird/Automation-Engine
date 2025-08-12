using System.Collections.Generic;
using System.Linq;
using Construction.Drag;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public enum Corner { None, Left, Right }
    
    public class CellSelection
    {
        public HashSet<Cell> HitCells { get; private set; }
        public Corner Corner { get; set; }
        public Vector3Int CornerGridCoord { get; private set; }
        public Direction Direction { get; private set; }
        public Axis Axis { get; private set; }

        public CellSelection()
        {
            HitCells = new HashSet<Cell>();
            Axis = Axis.XAxis;
            Direction = Direction.North;
        }

        public void SetDirection(Direction direction) => Direction = direction;
        public void SetAxis(Axis axis) => Axis = axis;
        public void AddCell(Vector3Int cell, PlacementSettings settings) => HitCells.Add(new Cell(cell, Direction.North, NodeType.Straight, settings));
        public void AddCell(Vector3Int cell, Direction direction, NodeType type, PlacementSettings settings) => HitCells.Add(new Cell(cell, direction, type, settings));
        public void AddCells(HashSet<Cell> newCells) => HitCells.UnionWith(newCells);
        public void AddCells(IEnumerable<Vector3Int> cells, PlacementSettings settings)
        {
            foreach (Vector3Int vector3Int in cells)
            {
                AddCell(vector3Int, settings);
            }
        } 

        // This method is primarily used for node removal
        // Node type should not matter
        public void AddRangeToDictionary(IEnumerable<Vector3Int> cellsPositions, Direction direction, PlacementSettings settings, NodeType type = NodeType.Straight)
        {
            foreach (Vector3Int cellPos in cellsPositions)
            {
                HitCells.Add(new Cell(cellPos, direction, type, settings));
            }
        }
        
        public void SetCornerGridCoord(Vector3Int corner) => CornerGridCoord = corner;
        public List<Vector3Int> GetCellPositions() => HitCells.Select(c => c.GridCoordinate).ToList();

        public List<Vector3Int> GetCellsInWorldSpace(PlacementSettings settings)
        {
            List<Vector3Int> cells = HitCells.Select(c => c.GridCoordinate).ToList();
            return Grid.GridToWorldPositions(cells, settings.mapOrigin, settings.tileSize); 
        }

        public HashSet<GridWorldCoordPair> GetGridWorldPairs(PlacementSettings settings)
        {
            List<Vector3Int> gridCoords = GetCellPositions();
            List<Vector3Int> worldPositions = GetCellsInWorldSpace(settings);
            
            HashSet<GridWorldCoordPair> pairs = new (gridCoords.Count);
            for (int i = 0; i < gridCoords.Count; i++)
            {
                pairs.Add(new GridWorldCoordPair(gridCoords[i], worldPositions[i]));
            }
    
            return pairs;
        }
        
        public Direction DirectionFromHit(Vector3Int hit, Direction defaultDirection)
        {
            if (!HitFound(hit)) return defaultDirection;
            return HitCells.First(c => c.GridCoordinate == hit).Direction;
        }

        public bool HitFound(Vector3Int hit) => HitCells.Any(c => c.GridCoordinate == hit);
        
        public int Count() => HitCells.Count;
        
        public void Clear()
        {
            HitCells.Clear();
        } 
    }
}