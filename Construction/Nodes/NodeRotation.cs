﻿using System.Threading;
using Construction.Interfaces;
using Construction.Placement;
using Construction.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Construction.Nodes
{
    public class NodeRotation 
    {
        private readonly Node _node;
        private readonly float _rotationTime; 
        private readonly EasingFunctions.Function _ease;

        private CancellationTokenSource _cts; 
        
        public NodeRotation(Node node, float rotationTime, EasingFunctions.Function ease)
        {
            _node = node;
            _rotationTime = rotationTime;
            _ease = ease; 
        }
        
        public void Rotate()
        {
            Direction newDirection = (Direction)(((int)_node.Direction + 1) % 4);
            Rotate(newDirection);
        }

        public void Rotate(Direction direction)
        {
            _node.SetDirection(direction);
            Vector3 currentRotation = _node.transform.localRotation.eulerAngles;
            Vector3 targetRotation = DirectionUtils.RotationFromDirection(direction);
            if(VectorUtils.ApproximatelyEqual(currentRotation,targetRotation)) return;
            
            Quaternion start = GetStartingRotation(currentRotation, targetRotation);
            Quaternion end = Quaternion.Euler(targetRotation);
            
            DisposeToken();
            _cts = new CancellationTokenSource(); 
            NewRunRotation(start, end, _cts).Forget();
        }

        private Quaternion GetStartingRotation(Vector3 start, Vector3 end)
        {
            return Quaternion.RotateTowards(
                Quaternion.Euler(end),    // current orientation
                Quaternion.Euler(start),  // target orientation
                90f        // max degrees to move
            );
        }

        public void RotateInstant(Direction direction)
        {
            _node.SetDirection(direction);
            DisposeToken();
            _node.transform.localRotation = Quaternion.Euler(DirectionUtils.RotationFromDirection(direction));
        }

        private void DisposeToken()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
        
        private async UniTaskVoid RunRotation(Vector3 start,  Vector3 end, CancellationTokenSource ctx)
        {
            if (VectorUtils.ApproximatelyEqual(start, end)) return; 
            
            float t = 0;

            while (t < _rotationTime)
            {
                float ease = _ease(0, 1, t / _rotationTime); 
                float angle = Mathf.LerpAngle(start.y, end.y, ease);
                _node.transform.localRotation = Quaternion.Euler(new Vector3(0, angle, 0)); 
                
                t += Time.deltaTime;
                await UniTask.Yield(ctx.Token);
            }
            
            _node.transform.localRotation = Quaternion.Euler(end);
        }
        
        private async UniTaskVoid NewRunRotation(Quaternion start,  Quaternion end, CancellationTokenSource ctx)
        {
            if (Quaternion.Dot(start, end) < 0f)
                end = new Quaternion(-end.x, -end.y, -end.z, -end.w);
            
            float t = 0;

            while (t < _rotationTime)
            {
                float ease = _ease(0, 1, t / _rotationTime); 
                _node.transform.localRotation = Quaternion.Slerp(start, end, ease);
                
                t += Time.deltaTime;
                await UniTask.Yield(ctx.Token);
            }
            
            _node.transform.localRotation = end;
        }
    }
}