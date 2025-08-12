using System;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public struct Cell : IEquatable<Cell>
    {
        public readonly Vector3Int GridCoordinate;
        public readonly Vector3Int WorldPosition;
        public readonly Direction Direction;
        public readonly NodeType Type;

        public Cell(Vector3Int gridCoordinate, Direction direction, NodeType type, PlacementSettings settings)
        {
            GridCoordinate = gridCoordinate;
            WorldPosition = Grid.GridToWorldPosition(gridCoordinate, settings.mapOrigin, settings.cellSize);
            Direction = direction;
            Type = type;
        }
        
        public bool Equals(Cell other) => GridCoordinate == other.GridCoordinate && Direction == other.Direction && Type == other.Type;
        
        public override bool Equals(object obj) => obj is Cell cell && Equals(cell);
        
        public override int GetHashCode() => HashCode.Combine(GridCoordinate, Direction, Type);
        
        public static bool operator ==(Cell left, Cell right) => left.Equals(right);
        
        public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
    }
}