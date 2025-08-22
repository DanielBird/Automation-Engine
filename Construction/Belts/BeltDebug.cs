using Engine.Construction.Nodes;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public class BeltDebug : NodeDebug
    {
        [Header("Belt debug")]
        [field: SerializeField] private Belt belt; 
        
        public bool drawResourceArrivalPoint;
        public bool drawResourceBezierHandle;
        public bool drawBezierPathLeft;
        public bool drawBezierPathRight;
        
        protected override void Awake()
        {
            base.Awake();
            
            if(belt == null)
                belt = GetComponent<Belt>();
        }
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (belt is Producer p)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(p.spawnLocation, 0.1f);
            }
            
            if (drawResourceArrivalPoint)
            {
                Vector3 point = transform.TransformPoint(belt.arrivalPointVector);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(point, 0.1f);
            }

            if (drawResourceBezierHandle)
            {
                Vector3 bezier = transform.TransformPoint(belt.bezierHandleVector);
                Gizmos.color = Color.aquamarine;
                Gizmos.DrawWireSphere(bezier, 0.1f);
            }

            if (drawBezierPathLeft || drawBezierPathRight)
            {
                Vector3 end = belt.arrivalPointVector;
                Vector3 handle = belt.bezierHandleVector;
                Vector3 start = new Vector3(-1, end.y, 0);
                if (drawBezierPathRight) start = new Vector3(1, end.y, 0);

                int segments = 20; 
                Vector3 previousPoint = transform.TransformPoint(start);
                
                for (int i = 0; i < segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = Bezier.QuadraticBernstein(start, handle, end, t);
                    point = transform.TransformPoint(point);
                    
                    Gizmos.DrawLine(previousPoint, point);
                    previousPoint = point;
                }
            }
        }
        
    }
}