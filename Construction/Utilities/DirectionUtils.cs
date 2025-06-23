using System;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public static class DirectionUtils
    {
        public static Vector2Int GetGridOffset(Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.East => new Vector2Int(1, 0),
                Direction.South => new Vector2Int(0, -1),
                Direction.West => new Vector2Int(-1, 0),
                _ => new Vector2Int(0, 0)
            };
        }
        
        public static Direction RotateClockwise(Direction direction)
        {
            return (Direction)(((int)direction + 1) % 4);
        }
        
        public static Direction RotateCounterClockwise(Direction direction)
        {
            return (Direction)(((int)direction + 3) % 4);
        }
        
        public static Direction Opposite(Direction direction)
        {
            return (Direction)(((int)direction + 2) % 4);
        }

        public static int RelativeTurn(Direction direction1, Direction direction2)
        {
            // 1 = right
            // 2 = left
            return ((int)direction1 - (int)direction2 + 4) % 4;
        }
        
        public static Vector3 RotationFromDirection(Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector3(0, 0, 0),
                Direction.East => new Vector3(0, 90, 0),
                Direction.South => new Vector3(0, 180, 0),
                Direction.West => new Vector3(0, 270, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            }; 
        }

        public static Direction Increment(Direction direction)
        {
            Direction newDirection = direction + 1;
            if ((int)newDirection > 3) newDirection = 0;
            return newDirection; 
        }

        public static Direction Decrement(Direction direction)
        {
            Direction newDirection = direction - 1;
            if ((int)newDirection < 0) newDirection = (Direction)3;
            return newDirection; 
        }
    }
}