﻿using Construction;
using Construction.Placement;
using UnityEngine;

namespace Utilities
{
    public static class MouseCoordinates
    {
        private static readonly Vector2[] Directions = new Vector2[]
        {
            new Vector2(-1,1),   // north
            new Vector2(1,1),   // east
            new Vector2(1, -1), // south
            new Vector2(-1,-1),  // west
        };
        
        public static Direction RelativeCardinalDirectionOfMouse(Vector3 start, UnityEngine.Camera camera, float deadzoneRadius = 0.1f, Direction defaultDirection = Direction.South)
        {
            int directionAsInt = CalculateCoordinate(start, camera, deadzoneRadius, defaultDirection);

            return directionAsInt switch
            {
                0 => Direction.North,
                1 => Direction.East,
                2 => Direction.South,
                3 => Direction.West,
                _ => Direction.South,
            };
        }
        
        private static int CalculateCoordinate(Vector3 start, UnityEngine.Camera camera, float deadzoneRadius, Direction defaultDirection)
        {
            Vector3 position = camera.WorldToScreenPoint(start);
            Vector3 mouse = Input.mousePosition;
            Vector3 difference = mouse - position;

            if (difference.sqrMagnitude < deadzoneRadius * deadzoneRadius) return (int)defaultDirection; 
            
            difference.Normalize();
            
            if(difference == Vector3.zero) return -1;

            float maxDot = float.MinValue;
            int bestDirection = 0;

            for (int i = 0; i < Directions.Length; i++)
            {
                float dot = Vector3.Dot(difference, Directions[i]);

                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestDirection = i; 
                }
            }

            return bestDirection; 
        }
        
    }
}