using System.Collections.Generic;
using System.Linq;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public enum Corner { None, Left, Right }
    
    public class CellSelection
    {
        // Used when drawing straight paths
        public List<Vector3Int> HitCells { get; private set; }

        // Used when drawing L shaped paths
        public Dictionary<Vector3Int, Direction> HitCellsDictionary { get; private set; }
        
        public Corner Corner { get; set; }
        public Vector3Int CornerCell { get; set; }
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

        public List<Vector3Int> GetCells(PlacementSettings settings) => !settings.useLShapedPaths ? HitCells : HitCellsDictionary.Keys.ToList();
        
        public Direction DirectionFromHit(Vector3Int hit, Direction defaultDirection) => HitCellsDictionary.GetValueOrDefault(hit, defaultDirection);
        
        public void Clear()
        {
            HitCells.Clear();
            HitCellsDictionary.Clear();
        } 
    }
}