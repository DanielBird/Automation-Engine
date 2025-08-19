using System;
using UnityEngine;

namespace Engine.Construction.Utilities
{
    // Used instead of a cell when there is no need to record a direction or node type
    // e.g. for selecting cells for node removal via the Removal Manager
    
    public readonly struct GridWorldCoordPair : IEquatable<GridWorldCoordPair>
    {
        public readonly Vector3Int GridCoordinate;
        public readonly Vector3Int WorldPosition;

        public GridWorldCoordPair(Vector3Int gridCoordinate, Vector3Int worldPosition)
        {
            GridCoordinate = gridCoordinate;  
            WorldPosition = worldPosition;
        }

        public bool Equals(GridWorldCoordPair other)
        {
            return GridCoordinate.Equals(other.GridCoordinate) &&
                   WorldPosition.Equals(other.WorldPosition);
        }

        public override bool Equals(object obj)
        {
            return obj is GridWorldCoordPair other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(GridCoordinate, WorldPosition);

        public static bool operator ==(GridWorldCoordPair left, GridWorldCoordPair right) => left.Equals(right);

        public static bool operator !=(GridWorldCoordPair left, GridWorldCoordPair right) => !(left == right);
  
    }
}