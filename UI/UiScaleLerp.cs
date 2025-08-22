using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Utilities;
using UnityEngine;

namespace Engine.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UiScaleLerp : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        [Header("On Start")] 
        public bool setScaleOnStart;
        public Vector2 startScale = Vector2.one;
        
        [Header("Tweening")]
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease easingFunction;
        
        [Space]
        public float delayBeforeTween;

        private CancellationTokenSource _ctx; 
        
        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            
            if (setScaleOnStart) SetScale(startScale);
        }

        private void Start()
        {
            _ease = EasingFunctions.GetEasingFunction(easingFunction);
        }

        private void OnDisable()
        {
            if (_ctx != null)
            {
                _ctx.Cancel();
                _ctx.Dispose();
                _ctx = null;
            }
        }

        public void SetScale(Vector2 scale)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            
            rectTransform.localScale = scale;
        }
        
        public void SetWidth(float width)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            
            Vector2 scale = rectTransform.localScale;
            rectTransform.localScale = new Vector2(width, scale.y); 
        }
        
        public void SetHeight(float height)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            
            Vector2 scale = rectTransform.localScale;
            rectTransform.localScale = new Vector2(scale.x, height); 
        }

        public void DoTween(Vector2 end, float duration)
        {
            if(rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            SetupCancellation();
            
            RunTween(end, duration).Forget();
        }

        private void SetupCancellation()
        {
            if (_ctx != null)
            {
                _ctx.Cancel();
                _ctx.Dispose();
            }

            _ctx = new CancellationTokenSource();
        }

        private async UniTaskVoid RunTween(Vector2 end, float duration)
        {
            await UniTask.WaitForSeconds(delayBeforeTween, cancellationToken: _ctx.Token);

            Vector2 start = rectTransform.localScale;
            
            float t = 0;
            while (t < duration)
            {
                float ease = _ease(0, 1, t / duration); 
                Vector2 newScale = Vector2.Lerp(start, end, ease);
                
                rectTransform.localScale = newScale;
                
                t += Time.unscaledDeltaTime; 
                await UniTask.NextFrame(cancellationToken: _ctx.Token);
            }
            
            rectTransform.localScale = end;
        }
        
    }
}