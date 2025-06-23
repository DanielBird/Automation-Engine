using System.Threading;
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

            DisposeToken();
            _cts = new CancellationTokenSource(); 
            RunRotation(currentRotation, targetRotation, _cts).Forget();
        }

        public void RotateInstant(Direction direction)
        {
            _node.SetDirection(direction);
            DisposeToken();
            _node.transform.localRotation = Quaternion.Euler(DirectionUtils.RotationFromDirection(direction));
        }

        private void DisposeToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
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
    }
}