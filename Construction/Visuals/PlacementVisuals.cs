using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public class PlacementVisuals : MonoBehaviour
    {
        public GameObject floorDecal;
        
        [Space]
        public float placementTime = 0.5f;
        public Vector3 startingScale = new Vector3(0, 0, 0);
        public Vector3 endScale = new Vector3(1, 1, 1); 
        
        [Space] private EasingFunctions.Function _scaleUpEasing; 
        public EasingFunctions.Ease scaleEasingFunction = EasingFunctions.Ease.EaseOutSine;
        
        [Space] private EasingFunctions.Function _scaleDownEasing; 
        public EasingFunctions.Ease scaleDownEasingFunction = EasingFunctions.Ease.EaseOutSine;

        [Space] public Material floorMaterial;
        public float lerpAlphaTime = 1f; 
        
        private static readonly int GridAlpha = Shader.PropertyToID("_gridAlpha");
        private Coroutine _alphaRoutine;
        private Coroutine _scaleRoutine;

        private bool _scalingUp; 
        private CancellationTokenSource _scaleCts; 
        private CancellationTokenSource _alphaCts;

        private void Awake()
        {
            _scaleUpEasing = EasingFunctions.GetEasingFunction(scaleEasingFunction);
            _scaleDownEasing = EasingFunctions.GetEasingFunction(scaleDownEasingFunction);

            if (floorMaterial == null)
            {
                Debug.LogError("Missing floor material");
                return;
            }
            
            floorMaterial.SetFloat(GridAlpha, 0);
            floorDecal.SetActive(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            
            CtsCtrl.Clear(ref _scaleCts);
            CtsCtrl.Clear(ref _alphaCts);
        }

        public void Place(GameObject go)
        {
            #if UNITASK
            if (_scalingUp) return;
            LerpScaleUni(go, startingScale, endScale, placementTime, _scaleUpEasing).Forget();
            
            #else
            if(_scaleRount != null) return;
            _scaleRountine = StartCoroutine(LerpScale(go, startingScale, endScale, placementTime, _scaleUpEasing));
            
            #endif
        }
        
        private IEnumerator LerpScale(GameObject go, Vector3 start, Vector3 end, float time, EasingFunctions.Function easeFunc)
        {
            float t = 0;
            while (t < time)
            {
                float ease = easeFunc(0, 1, t / time);
                Vector3 scale = Vector3.Lerp(start, end, ease);
                go.transform.localScale = scale; 

                t += Time.deltaTime;
                yield return null; 
            }

            go.transform.localScale = end;
            _scaleRoutine = null;
        }

        private async UniTaskVoid LerpScaleUni(GameObject go, Vector3 start, Vector3 end, float time, EasingFunctions.Function easeFunc)
        {
            _scaleCts = new CancellationTokenSource();
            
            _scalingUp = true; 
            float t = 0;
            while (t < time)
            {
                float ease = easeFunc(0, 1, t / time);
                Vector3 scale = Vector3.Lerp(start, end, ease);
                go.transform.localScale = scale; 

                t += Time.deltaTime;
                await UniTask.Yield(cancellationToken: _scaleCts.Token);
            }

            go.transform.localScale = end;
            _scalingUp = false;
            CtsCtrl.Clear(ref _scaleCts);
        }

        public void Show(bool showDecal = true)
        {
            #if UNITASK
            LerpGridAlphaUni(1, lerpAlphaTime/3).Forget();
            
            #else
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
           _alphaRoutine = StartCoroutine(LerpGridAlpha(1, lerpAlphaTime/3)); 
            
            #endif
            
            if(showDecal) floorDecal.SetActive(true);
        }
        
        public void Hide(bool hideDecal = true)
        {
            # if UNITASK
            LerpGridAlphaUni(0, lerpAlphaTime).Forget();
            
            #else
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
            _alphaRoutine = StartCoroutine(LerpGridAlpha(0, lerpAlphaTime));
            
            #endif
            if(hideDecal) floorDecal.SetActive(false);
        }

        private IEnumerator LerpGridAlpha(float end, float lerpTime)
        {
            float start = floorMaterial.GetFloat(GridAlpha);
            
            float t = 0;
            while (t < lerpTime)
            {
                float alpha = Mathf.Lerp(start, end, t / lerpTime);
                floorMaterial.SetFloat(GridAlpha, alpha);
                
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            
            floorMaterial.SetFloat(GridAlpha, end);
        }

        private async UniTaskVoid LerpGridAlphaUni(float end, float lerpTime)
        {
            _alphaCts = new CancellationTokenSource();
            
            float start = floorMaterial.GetFloat(GridAlpha);
            
            float t = 0;
            while (t < lerpTime)
            {
                float alpha = Mathf.Lerp(start, end, t / lerpTime);
                floorMaterial.SetFloat(GridAlpha, alpha);
                
                t += Time.unscaledDeltaTime;
                await UniTask.Yield(cancellationToken: _alphaCts.Token);
            }
            
            floorMaterial.SetFloat(GridAlpha, end);
            CtsCtrl.Clear(ref _alphaCts);
        }
        
        public void SetFloorDecalPos(Vector3Int pos) => floorDecal.transform.position = pos;
        
        public void DeactivateFloorDecal() => floorDecal.SetActive(false);
    }
}