using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public class TweenPosition : MonoBehaviour
    {
        private EasingFunctions.Function _easing; 
        public EasingFunctions.Ease easingFunction = EasingFunctions.Ease.EaseInOutSine;

        private CancellationTokenSource _cts; 

        private void Awake()
        {
            _easing = EasingFunctions.GetEasingFunction(easingFunction);
        }

        private void OnDisable()
        {
            CtsCtrl.Clear(ref _cts);
        }

        public void DoTween(Vector3 start, Vector3 end, float duration, float delay)
        {
            StopAndCreateNewCts();
            Tween(start, end, duration, delay).Forget();
        }
        
        public void DoTween<T>(Vector3 start, Vector3 end, float duration, float delay, Action<T> onComplete, T value)
        {
            StopAndCreateNewCts();
            Tween(start, end, duration, delay, onComplete, value).Forget();
        }
        
        public void DoTween(Vector3 end, float duration, float delay)
        {
            Vector3 start = transform.position;
            StopAndCreateNewCts();
            Tween(start, end, duration, delay).Forget();
        }
        
        public void DoTween<T>(Vector3 end, float duration, float delay, Action<T> onComplete, T value)
        {
            Vector3 start = transform.position;
            StopAndCreateNewCts();
            Tween(start, end, duration, delay, onComplete, value).Forget();
        }

        private void StopAndCreateNewCts()
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
        }

        private async UniTaskVoid Tween(Vector3 start, Vector3 end, float duration, float delay)
        {
            if (delay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            float t = 0;
            while (t < duration)
            {
                float ease = _easing(0, 1, t / duration);
                transform.position = Vector3.Lerp(start, end, ease);
                
                t += Time.deltaTime;
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
            
            transform.position = end;
            CtsCtrl.Clear(ref _cts);
        }
        
        private async UniTaskVoid Tween<T>(Vector3 start, Vector3 end, float duration, float delay, Action<T> onComplete, T value)
        {
            if (delay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            float t = 0;
            while (t < duration)
            {
                float ease = _easing(0, 1, t / duration);
                transform.position = Vector3.Lerp(start, end, ease);
                
                t += Time.deltaTime;
                if(_cts.Token.IsCancellationRequested) onComplete.Invoke(value);
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
            
            transform.position = end;
            CtsCtrl.Clear(ref _cts);
            onComplete?.Invoke(value);
        }
    }
}