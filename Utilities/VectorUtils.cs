using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    public static class VectorUtils
    {
        public static Vector3Int FurthestFrom(List<Vector3Int> list, Vector3Int start)
        {
            Vector3Int furthest = start;
            float maxDistance = 0f; 

            foreach (Vector3Int cell in list)
            {
                float sqrMagnitude = (cell - start).sqrMagnitude;
                if (sqrMagnitude > maxDistance)
                {
                    maxDistance = sqrMagnitude;
                    furthest = cell; 
                }
            }

            return furthest;
        } 
        
        public static Vector3Int LowestVector(List<Vector3Int> list)
        {
            Vector3Int lowest = list[0]; 
            int min = list[0].sqrMagnitude;

            for (int i = 0; i < list.Count; i++)
            {
                int currentMagnitude = list[i].sqrMagnitude; 
                
                if (currentMagnitude < min)
                {
                    min = currentMagnitude;
                    lowest = list[i]; 
                }
            }

            return lowest;
        }
        
        public static Vector3Int HighestVector(List<Vector3Int> list)
        {
            Vector3Int highest = list[0]; 
            int max = list[0].sqrMagnitude;

            for (int i = 0; i < list.Count; i++)
            {
                int currentMagnitude = list[i].sqrMagnitude; 
                
                if (currentMagnitude > max)
                {
                    max = currentMagnitude;
                    highest = list[i]; 
                }
            }

            return highest;
        }
        
        public static bool ApproximatelyEqual(Vector3 v1, Vector3 v2, float tolerance = 0.0001f)
        {
            return (v1 - v2).sqrMagnitude < tolerance * tolerance;
        }
        
        public static bool IsNaN(this Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }
    }
}