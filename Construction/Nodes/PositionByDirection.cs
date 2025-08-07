using System;
using Construction.Placement;
using Construction.Utilities;
using UnityEngine;

namespace Construction.Nodes
{
    public static class PositionByDirection
    {
        public static Vector2Int Get(int x, int z, Direction direction, int size) => direction switch
        {
            Direction.North => new Vector2Int(x, z + size),
            Direction.East => new Vector2Int(x + size, z),
            Direction.South => new Vector2Int(x, z - size),
            Direction.West => new Vector2Int(x - size, z),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        
        public static Vector3Int Get(Vector3Int basePosition, Direction direction, int size) => direction switch
        {
            Direction.North => new Vector3Int(basePosition.x, 0, basePosition.z + size),
            Direction.East  => new Vector3Int(basePosition.x + size, 0, basePosition.z),
            Direction.South => new Vector3Int(basePosition.x, 0, basePosition.z - size),
            Direction.West  => new Vector3Int(basePosition.x - size, 0, basePosition.z),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        
        public static Vector2Int GetForwardPosition(Vector3Int basePosition, Direction currentDirection, Node node)
        {
            int size = node.GridWidth; 
            return Get(basePosition.x, basePosition.z, currentDirection, size);
        }
        
        public static Vector2Int GetBackwardPosition(Vector3Int basePosition, Direction currentDirection, Node node)
        {
            Direction opposite = DirectionUtils.Opposite(currentDirection);
            return Get(basePosition.x, basePosition.z, opposite, node.GridWidth);
        }
        
        public static Vector2Int GetRightPosition(Vector3Int basePosition, Direction currentDirection, Node node)
        {
            Direction right = DirectionUtils.Increment(currentDirection);
            return Get(basePosition.x, basePosition.z, right, node.GridWidth);
        }
        
        public static Vector2Int GetLeftPosition(Vector3Int basePosition, Direction currentDirection, Node node)
        {
            Direction left = DirectionUtils.Decrement(currentDirection);
            return Get(basePosition.x, basePosition.z, left, node.GridWidth);
        }
        
        public static Vector3Int GetForwardPositionVector3(Vector3Int basePosition, Direction currentDirection, int size) => Get(basePosition, currentDirection, size);
    }
}