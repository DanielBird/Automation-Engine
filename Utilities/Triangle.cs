using UnityEngine;

namespace Engine.Utilities
{
    public class Triangle
    {
        public static Vector2 GetRightHandCornerToRight(Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            Vector2 perp = new Vector2(-d.y, d.x) * 0.5f;
            return mid - perp; 
        } 
        
        public static Vector2 GetRightHandCornerToLeft(Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            Vector2 perp = new Vector2(-d.y, d.x) * 0.5f;
            return mid + perp; 
        } 
    }
}