using System;
using Construction.Placement;
using Construction.Utilities;
using UnityEngine;

namespace Construction
{
    public static class PositionByDirection
    {
        public static Vector2Int Get(int x, int z, Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector2Int(x, z + 1),
                Direction.East => new Vector2Int(x + 1, z),
                Direction.South => new Vector2Int(x, z - 1),
                Direction.West => new Vector2Int(x - 1, z),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        
        public static Vector3Int Get(Vector3Int basePosition, Direction direction)
        {
            int x = basePosition.x;
            int z = basePosition.z;
            
            return direction switch
            {
                Direction.North => new Vector3Int(x, 0, z + 1),
                Direction.East => new Vector3Int(x + 1, 0, z),
                Direction.South => new Vector3Int(x, 0, z - 1),
                Direction.West => new Vector3Int(x - 1, 0, z),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        
        public static Vector2Int GetForwardPosition(Vector3Int basePosition, Direction currentDirection)
        {
            return Get(basePosition.x, basePosition.z, currentDirection);
        }
        
        public static Vector2Int GetBackwardPosition(Vector3Int basePosition, Direction currentDirection)
        {
            Direction opposite = DirectionUtils.Opposite(currentDirection);
            return Get(basePosition.x, basePosition.z, opposite);
        }
        
        public static Vector2Int GetRightPosition(Vector3Int basePosition, Direction currentDirection)
        {
            Direction right = DirectionUtils.Increment(currentDirection);
            return Get(basePosition.x, basePosition.z, right);
        }
        
        public static Vector2Int GetLeftPosition(Vector3Int basePosition, Direction currentDirection)
        {
            Direction left = DirectionUtils.Decrement(currentDirection);
            return Get(basePosition.x, basePosition.z, left);
        }
        
        public static Vector3Int GetForwardPositionVector3(Vector3Int basePosition, Direction currentDirection) => Get(basePosition, currentDirection);
    }
}