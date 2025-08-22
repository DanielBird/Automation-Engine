using System;
using System.Collections;
using Engine.Construction.Belts;
using Engine.Construction.Placement;
using Engine.GameState;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Resources
{
    public class BasicResourceMove : IResourceMover
    {
        private readonly Resource resource;
        private readonly Transform _transform;
        private readonly Collider _collider;
        private readonly float _moveTime; 
        private readonly EasingFunctions.Function _easeFunc;
        private readonly Action _finaliseMovement;

        public BasicResourceMove(Resource resource, Transform transform, Collider collider, float moveTime, EasingFunctions.Function easeFunc, Action finaliseMovement)
        {
            this.resource = resource;
            _transform = transform;
            _collider = collider;
            _moveTime = moveTime;
            _easeFunc = easeFunc;
            _finaliseMovement = finaliseMovement;
        }
        
        public Coroutine Move(Belt next, Direction direction = Direction.North)
        {
            return resource.StartCoroutine(MoveResource(next.ResourceArrivalPoint, _moveTime)); 
        }

        private IEnumerator MoveResource(Vector3 target, float moveTime)
        {
            float t = 0;
            Vector3 start = _transform.position; 

            resource.SetMoving(true); 
            while (t < moveTime)
            {
                if (CoreGameState.Paused)
                {
                    yield return null;
                    continue;
                } 
                
                if(resource.IsMoving == false || resource.Status == ResourceStatus.Inactive) yield break;
                float f = _easeFunc(0, 1, t / moveTime); 
                _transform.position =  Vector3.Lerp(start, target, f);

                t += Time.deltaTime; 
                yield return null;
            }

            _transform.position = target;
            _collider.isTrigger = true; 
            
            resource.SetMoving(false); 

            if(resource.Status != ResourceStatus.Active) yield break; 
            _finaliseMovement();
        }
    }
}