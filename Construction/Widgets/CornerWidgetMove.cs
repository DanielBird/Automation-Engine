using System;
using System.Collections;
using Construction.Belts;
using Construction.Placement;
using GameState;
using UnityEngine;
using Utilities;

namespace Construction.Widgets
{
    public class CornerWidgetMove : IWidgetMover
    {
        private readonly Widget _widget;
        private readonly MoveType _moveType;         // Should be either Left or Right
        private readonly Transform _transform;
        private readonly Collider _collider;
        private readonly float _moveTime; 
        private readonly EasingFunctions.Function _easeFunc;
        private readonly Action _finaliseMovement;

        public CornerWidgetMove(Widget widget, MoveType moveType, Transform transform, Collider collider, float moveTime, EasingFunctions.Function easeFunc, Action finaliseMovement)
        {
            _widget = widget;
            _moveType = moveType;
            _transform = transform;
            _collider = collider;
            _moveTime = moveTime;
            _easeFunc = easeFunc;
            _finaliseMovement = finaliseMovement;
            
            if(_moveType != MoveType.Right || _moveType != MoveType.Left)
                Debug.LogWarning("A Corner Widget Moved has been created without a move type not set to left or right");
        }
        
        public Coroutine Move(Belt next, Direction direction = Direction.North)
        {
            return _widget.StartCoroutine(MoveWidget(next, _moveTime));
        }

        private IEnumerator MoveWidget(Belt nextBelt, float moveTime)
        {
            _widget.SetMoving(true);

            yield return LerpToCurveMiddle(moveTime, nextBelt);

            _collider.isTrigger = true; 
            _widget.SetMoving(false);
            
            if(_widget.Status != WidgetStatus.Active) yield break; 
            _finaliseMovement();
        }

        private IEnumerator LerpToCurveMiddle(float moveTime, Belt nextBelt)
        {
            Vector3 start = _transform.position;
            Vector3 widgetEnd = nextBelt.WidgetArrivalPoint;
            Vector3 handle = nextBelt.BezierHandle;
            
            float t = 0;

            while (t < moveTime)
            {
                if (CoreGameState.Paused)
                {
                    yield return null;
                    continue;
                } 
                
                if(_widget.IsMoving == false || _widget.Status == WidgetStatus.Inactive) 
                    yield break;
                
                float f = _easeFunc(0, 1, t / moveTime);
                _transform.position = Bezier.QuadraticBernstein(start, handle, widgetEnd, f);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            Vector3 end = Bezier.QuadraticBernstein(start, handle, widgetEnd,1);
            _transform.position = end;
        }
        
        private Vector3 GetBezierHandle(Vector3 start, Vector3 end)
        {
            Vector2 a = new Vector2(start.x, start.z);
            Vector2 b = new Vector2(end.x, end.z);
            Vector2 corner = _moveType == MoveType.Left ? Triangle.GetRightHandCornerToRight(a, b) : Triangle.GetRightHandCornerToLeft(a, b);
            return new Vector3(corner.x, end.y, corner.y);
        }
    }
}