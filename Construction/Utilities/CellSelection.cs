using System.Collections.Generic;
using System.Linq;
using Construction.Drag;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public enum Corner { None, Left, Right }
    
    public class CellSelection
    {
        // Used when drawing straight paths or with Node Removal
        public List<Vector3Int> HitCells { get; private set; }

        // Used when drawing L shaped paths
        public Dictionary<Vector3Int, Direction> HitCellsDictionary { get; private set; }
        
        public Corner Corner { get; set; }
        public Vector3Int CornerGridCoord { get; private set; }
        public Direction Direction { get; private set; }
        public Axis Axis { get; private set; }

        public CellSelection()
        {
            HitCells = new List<Vector3Int>();
            HitCellsDictionary = new Dictionary<Vector3Int, Direction>();
            Axis = Axis.XAxis;
            Direction = Direction.North;
        }

        public void SetDirection(Direction direction) => Direction = direction;
        public void SetAxis(Axis axis) => Axis = axis;
        public void AddCell(Vector3Int cell) => HitCells.Add(cell);
        public void AddCell(Vector3Int cell, Direction direction) => HitCellsDictionary[cell] = direction;
        public void AddRange(IEnumerable<Vector3Int> cells) => HitCells.AddRange(cells);

        public void AddRangeToDictionary(IEnumerable<Vector3Int> cells, Direction direction)
        {
            foreach (Vector3Int cell in cells)
            {
                HitCellsDictionary.Add(cell, direction);
            }
        }
        
        public void SetCornerGridCoord(Vector3Int corner) => CornerGridCoord = corner;
        public List<Vector3Int> GetCells(PlacementSettings settings) => !settings.useLShapedPaths ? HitCells : HitCellsDictionary.Keys.ToList();
        public List<Vector3Int> GetCells() => HitCells;

        public List<Vector3Int> GetCellsInWorldSpace(PlacementSettings settings)
        {
            List<Vector3Int> cells = !settings.useLShapedPaths 
                ? HitCells 
                : HitCellsDictionary.Keys.ToList();

            return Grid.GridToWorldPositions(cells, settings.gridOrigin, settings.tileSize); 
        }

        public HashSet<GridWorldCoordPair> GetGridWorldPairs(PlacementSettings settings)
        {
            List<Vector3Int> gridCoords = GetCells(settings);
            List<Vector3Int> worldPositions = GetCellsInWorldSpace(settings);
            
            HashSet<GridWorldCoordPair> pairs = new (gridCoords.Count);
            for (int i = 0; i < gridCoords.Count; i++)
            {
                pairs.Add(new GridWorldCoordPair(gridCoords[i], worldPositions[i]));
            }
    
            return pairs;
        }
        
        public Direction DirectionFromHit(Vector3Int hit, Direction defaultDirection) => HitCellsDictionary.GetValueOrDefault(hit, defaultDirection);

        public bool HitFound(Vector3Int hit) => HitCellsDictionary.ContainsKey(hit);
        
        public int Count(PlacementSettings settings) => !settings.useLShapedPaths ? HitCells.Count : HitCellsDictionary.Count;
        public int Count(bool useDictionary) => useDictionary ? HitCellsDictionary.Count : HitCells.Count;
        
        public void Clear()
        {
            HitCells.Clear();
            HitCellsDictionary.Clear();
        } 
    }
}