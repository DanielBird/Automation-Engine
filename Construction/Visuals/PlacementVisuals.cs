using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Placement;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public class PlacementVisuals
    {
        private readonly ConstructionEngine _engine; 
        public readonly GameObject FloorDecal;
        
        private readonly float _placementTime;
        private readonly Vector3 _startingScale;
        private readonly Vector3 _endScale; 
        
        private readonly EasingFunctions.Function _scaleUpEasing; 
        private EasingFunctions.Function _scaleDownEasing; 

        private readonly Material _floorMaterial;
        private readonly float _lerpAlphaTime; 
        
        private static readonly int GridAlpha = Shader.PropertyToID("_gridAlpha");
        private Coroutine _alphaRoutine;
        private Coroutine _scaleRoutine;

        private bool _scalingUp; 
        private CancellationTokenSource _scaleCts; 
        private CancellationTokenSource _alphaCts;
        
        public PlacementVisuals(ConstructionEngine engine, GameObject floorDecal, float placementTime, Vector3 startingScale, Vector3 endScale, 
            EasingFunctions.Function scaleUp, EasingFunctions.Function scaleDown,
            Material floorMaterial, float lerpAlphaTime)
        {
            _engine = engine;
            FloorDecal = floorDecal;
            FloorDecal.SetActive(false);
            
            _placementTime = placementTime;
            _startingScale = startingScale;
            _endScale = endScale;
            
            _scaleUpEasing = scaleUp;
            _scaleDownEasing = scaleDown;
            
            _floorMaterial = floorMaterial;
            _floorMaterial.SetFloat(GridAlpha, 0);
            
            _lerpAlphaTime = lerpAlphaTime;
        }

        public void Disable()
        {
            CtsCtrl.Clear(ref _scaleCts);
            CtsCtrl.Clear(ref _alphaCts);
            
            if(_alphaRoutine != null) _engine.StopCoroutine(_alphaRoutine);
            if(_scaleRoutine != null) _engine.StopCoroutine(_scaleRoutine);
        }

        public void Place(GameObject go)
        {
            #if UNITASK
            if (_scalingUp) return;
            LerpScaleUni(go, _startingScale, _endScale, _placementTime, _scaleUpEasing).Forget();
            
            #else
            if(_scaleRoutine != null) return;
            _scaleRoutine = _engine.StartCoroutine(LerpScale(go, _startingScale, _endScale, _placementTime, _scaleUpEasing));
            
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
            LerpGridAlphaUni(1, _lerpAlphaTime/3).Forget();
            
            #else
            if(_alphaRoutine != null) _engine.StopCoroutine(_alphaRoutine);
           _alphaRoutine = _engine.StartCoroutine(LerpGridAlpha(1, _lerpAlphaTime/3)); 
            
            #endif
            
            if(showDecal) FloorDecal.SetActive(true);
        }
        
        public void Hide(bool hideDecal = true)
        {
            # if UNITASK
            LerpGridAlphaUni(0, _lerpAlphaTime).Forget();
            
            #else
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
            _alphaRoutine = StartCoroutine(LerpGridAlpha(0, lerpAlphaTime));
            
            #endif
            if(hideDecal) FloorDecal.SetActive(false);
        }

        private IEnumerator LerpGridAlpha(float end, float lerpTime)
        {
            float start = _floorMaterial.GetFloat(GridAlpha);
            
            float t = 0;
            while (t < lerpTime)
            {
                float alpha = Mathf.Lerp(start, end, t / lerpTime);
                _floorMaterial.SetFloat(GridAlpha, alpha);
                
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            
            _floorMaterial.SetFloat(GridAlpha, end);
        }

        private async UniTaskVoid LerpGridAlphaUni(float end, float lerpTime)
        {
            _alphaCts = new CancellationTokenSource();
            
            float start = _floorMaterial.GetFloat(GridAlpha);
            
            float t = 0;
            while (t < lerpTime)
            {
                float alpha = Mathf.Lerp(start, end, t / lerpTime);
                _floorMaterial.SetFloat(GridAlpha, alpha);
                
                t += Time.unscaledDeltaTime;
                await UniTask.Yield(cancellationToken: _alphaCts.Token);
            }
            
            _floorMaterial.SetFloat(GridAlpha, end);
            CtsCtrl.Clear(ref _alphaCts);
        }
        
        public void SetFloorDecalPos(Vector3Int pos) => FloorDecal.transform.position = pos;
        
        public void DeactivateFloorDecal() => FloorDecal.SetActive(false);
    }
}