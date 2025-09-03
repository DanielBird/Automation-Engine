using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Utilities
{
    public enum TweenType {None, Show, Hide}
    
    public class TweenScale : MonoBehaviour
    {
        public GameObject objectToScale; 
        private Renderer rend;
        
        public float scaleTime = 0.3f;
        public Vector3 maxScale = Vector3.one;

        [Space] 
        public bool zeroScaleOnStart = true; 
        
        [Space]
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease easingFunction = EasingFunctions.Ease.EaseInOutSine;

        private bool _initialised; 
        private TweenType _tweenType;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            if (objectToScale == null)
            {
                Debug.Log("Please assign a game object to the TweenScale on " + name);
                _initialised = false;
                return;
            }
            
            _initialised = true;
            rend = objectToScale.GetComponent<Renderer>();
            _ease = EasingFunctions.GetEasingFunction(easingFunction);
            _tweenType = TweenType.None;
        }

        private void OnDisable()
        {
            CtsCtrl.Clear(ref _cts);
        }

        private void Start()
        {
            if (!zeroScaleOnStart || !_initialised) return;
            objectToScale.transform.localScale = Vector3.zero;
            rend.enabled = false;
        }
        
        [Button]
        public void Show()
        {
            if (_tweenType == TweenType.Show || !_initialised) return; 
            _tweenType = TweenType.Show;
            
            rend.enabled = true;
            Scale(maxScale, scaleTime).Forget();
        }

        [Button]
        public void Hide()
        {
            if (_tweenType == TweenType.Hide || !_initialised) return;
            _tweenType = TweenType.Hide;
            
            Scale(Vector3.zero, scaleTime, true).Forget();
        }

        private async UniTaskVoid Scale(Vector3 end, float duration, bool disableRend = false)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            Vector3 start = objectToScale.transform.localScale;
            
            float t = 0;
            while (t < duration)
            {
                float ease = _ease(0, 1, t / duration); 
                objectToScale.transform.localScale = Vector3.Lerp(start, end, ease);
                
                t += Time.deltaTime;
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
            
            objectToScale.transform.localScale = end;
            if(disableRend) rend.enabled = false;
            CtsCtrl.Clear(ref _cts);
            _tweenType = TweenType.None;
        }
    }
}