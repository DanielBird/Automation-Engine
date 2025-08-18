using System;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;
using Grid = Construction.Utilities.Grid;

namespace Construction.Drag.Selection
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly Vector3Int GridCoordinate;
        public readonly Vector3Int WorldPosition;
        public readonly Direction Direction;
        public readonly NodeType NodeType;

        public Cell(Vector3Int gridCoordinate, Direction direction, NodeType nodeType, PlacementSettings settings)
        {
            GridCoordinate = gridCoordinate;
            WorldPosition = Grid.GridToWorldPosition(gridCoordinate, settings.mapOrigin, settings.cellSize);
            Direction = direction;
            NodeType = nodeType;
        }
        
        public bool Equals(Cell other) => GridCoordinate == other.GridCoordinate && Direction == other.Direction && NodeType == other.NodeType;
        
        public override bool Equals(object obj) => obj is Cell cell && Equals(cell);
        
        public override int GetHashCode() => HashCode.Combine(GridCoordinate, Direction, NodeType);
        
        public static bool operator ==(Cell left, Cell right) => left.Equals(right);
        
        public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
    }
}