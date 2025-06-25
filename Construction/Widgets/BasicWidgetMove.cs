using System;
using System.Collections;
using Construction.Nodes;
using Construction.Placement;
using GameState;
using UnityEngine;
using Utilities;

namespace Construction.Widgets
{
    public class BasicWidgetMove : IWidgetMover
    {
        private readonly Widget _widget;
        private readonly Transform _transform;
        private readonly Collider _collider; 
        private readonly EasingFunctions.Function _easeFunc;
        private readonly Action _finaliseMovement;

        public BasicWidgetMove(Widget widget, Transform transform, Collider collider, EasingFunctions.Function easeFunc, Action finaliseMovement)
        {
            _widget = widget;
            _transform = transform;
            _collider = collider;
            _easeFunc = easeFunc;
            _finaliseMovement = finaliseMovement;
        }
        
        public Coroutine Move(Vector3 target, float moveTime, Direction direction = Direction.North)
        {
            return _widget.StartCoroutine(MoveWidget(target, moveTime)); 
        }

        private IEnumerator MoveWidget(Vector3 target, float moveTime)
        {
            float t = 0;
            Vector3 start = _transform.position; 

            _widget.SetMoving(true); 
            while (t < moveTime)
            {
                if (CoreGameState.paused)
                {
                    yield return null;
                    continue;
                } 
                
                if(_widget.IsMoving == false || _widget.Status == WidgetStatus.Inactive) yield break;
                float f = _easeFunc(0, 1, t / moveTime); 
                _transform.position =  Vector3.Lerp(start, target, f);

                t += Time.deltaTime; 
                yield return null;
            }

            _transform.position = target;
            _collider.isTrigger = true; 
            
            _widget.SetMoving(false); 

            if(_widget.Status != WidgetStatus.Active) yield break; 
            _finaliseMovement();
        }
    }
}