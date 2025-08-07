using System;
using UnityEngine;

namespace Construction.Drag
{
    public readonly struct GridWorldCoordPair : IEquatable<GridWorldCoordPair>
    {
        public Vector3Int GridCoord { get; }
        public Vector3Int WorldPosition { get; }

        public GridWorldCoordPair(Vector3Int gridCoord, Vector3Int worldPosition)
        {
            GridCoord = gridCoord;  
            WorldPosition = worldPosition;
        }

        public bool Equals(GridWorldCoordPair other)
        {
            return GridCoord.Equals(other.GridCoord) &&
                   WorldPosition.Equals(other.WorldPosition);
        }

        public override bool Equals(object obj)
        {
            return obj is GridWorldCoordPair other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GridCoord, WorldPosition);
        }

        public static bool operator ==(GridWorldCoordPair left, GridWorldCoordPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridWorldCoordPair left, GridWorldCoordPair right)
        {
            return !(left == right);
        }
    }
}