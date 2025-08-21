using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public class TweenPositionRoutine : MonoBehaviour
    {
                private EasingFunctions.Function _easing; 
        public EasingFunctions.Ease easingFunction = EasingFunctions.Ease.EaseInOutSine;

        private CancellationTokenSource _cts; 
        private Coroutine _coroutine;
        
        private void Awake()
        {
            _easing = EasingFunctions.GetEasingFunction(easingFunction);
        }

        private void OnDisable()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }

        public void DoTween(Vector3 start, Vector3 end, float duration)
        {
            _coroutine = StartCoroutine(TweenRoutine(start, end, duration));
        }
        
        public void DoTween<T>(Vector3 start, Vector3 end, float duration, Action<T> onComplete, T value)
        {
            _coroutine = StartCoroutine(TweenRoutine(start, end, duration, onComplete, value));
        }
        
        public void DoTween(Vector3 end, float duration)
        {
            Vector3 start = transform.position;
            _coroutine = StartCoroutine(TweenRoutine(start, end, duration));
        }
        
        public void DoTween<T>(Vector3 end, float duration, Action<T> onComplete, T value)
        {
            Vector3 start = transform.position;
            _coroutine = StartCoroutine(TweenRoutine(start, end, duration, onComplete, value));
        }
        
        private IEnumerator TweenRoutine(Vector3 start, Vector3 end, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                float ease = _easing(0, 1, t / duration);
                transform.position = Vector3.Lerp(start, end, ease);
                
                t += Time.deltaTime;
                yield return null; 
            }
            
            transform.position = end;
            _coroutine = null;
        }
        
        private IEnumerator TweenRoutine<T>(Vector3 start, Vector3 end, float duration, Action<T> onComplete, T value)
        {
            float t = 0;
            while (t < duration)
            {
                float ease = _easing(0, 1, t / duration);
                transform.position = Vector3.Lerp(start, end, ease);
                
                t += Time.deltaTime;
                yield return null; 
            }
            
            transform.position = end;
            _coroutine = null;
            
            onComplete?.Invoke(value);
        }
    }
}