using UnityEngine;

namespace Engine.Utilities
{
    public class Bezier
    {
        /// <summary>
        /// Evaluate a quadratic Bezier at parameter t in [0,1] (de Casteljau form). 
        /// P0, P1, P2 are the control points.
        /// </summary>
        public static Vector3 Quadratic(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            Vector2 a = Vector3.Lerp(p0, p1, t);
            Vector2 b = Vector3.Lerp(p1, p2, t);
            return Vector3.Lerp(a, b, t);
        }
        
        /// <summary>
        /// Evaluate a cubic Bezier at parameter t in [0,1] (de Casteljau form). 
        /// P0, P1, P2, P3 are the control points.
        /// </summary>
        public static Vector3 Cubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 c = Vector3.Lerp(p2, p3, t);
            Vector3 d = Vector3.Lerp(a, b, t);
            Vector3 e = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(d, e, t);
        }
        
        /// <summary>
        /// Evaluate a quadratic Bézier curve in Bernstein polynomial form.
        /// P0 = start point, P1 = control handle, P2 = end point.
        /// </summary>
        public static Vector3 QuadraticBernstein(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);

            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;

            return (uu * p0) +
                   (2f * u * t * p1) +
                   (tt * p2);
        }
        
        /// <summary>
        /// Evaluate a cubic Bézier curve in Bernstein polynomial form.
        /// P0, P1, P2, P3 are the control points.
        /// </summary>
        public static Vector3 CubicBernstein(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u  = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0) +
                   (3f * uu * t * p1) +
                   (3f * u * tt * p2) +
                   (ttt * p3);
        }
    }
}