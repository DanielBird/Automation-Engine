using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public class PlacementVisuals
    {
        private readonly ConstructionEngine _engine;
        private readonly IWorld _world;

        private readonly bool _updatePlacementHighlight; 
        public readonly GameObject PlacementHighlight;
        
        private readonly float _placementTime;
        private readonly Vector3 _startingScale;
        private readonly Vector3 _endScale; 
        
        private readonly EasingFunctions.Function _scaleUpEasing; 
        private EasingFunctions.Function _scaleDownEasing;

        private readonly bool _updateFloorMaterial; 
        private readonly Material _floorMaterial;
        private readonly float _lerpAlphaTime; 
        
        private static readonly int GridAlpha = Shader.PropertyToID("_gridAlpha");
        private readonly float minAlpha; 
        private readonly float maxAlpha;
        
        private Coroutine _alphaRoutine;
        private Coroutine _scaleRoutine;

        private bool _scalingUp; 
        private CancellationTokenSource _scaleCts; 
        private CancellationTokenSource _alphaCts;
        
        public PlacementVisuals(ConstructionEngine engine, IWorld world, GameObject placementHighlight, PlacementVisualSettings vs)
        {
            _engine = engine;
            _world = world;
            
            if (placementHighlight == null)
            {
                Debug.LogError("The placement highlight was not found on the Construction Engine. Add a game object to have a highlight updated during placement.");
                _updatePlacementHighlight = false;
            }
            else
            {
                _updatePlacementHighlight = true;
                PlacementHighlight = placementHighlight;
                PlacementHighlight.SetActive(false);
                _placementTime = vs.placementTime;
            }
            
            _startingScale = vs.startingScale;
            _endScale = vs.endScale;
            
            _scaleUpEasing = EasingFunctions.GetEasingFunction(vs.scaleEasingFunction);
            _scaleDownEasing = EasingFunctions.GetEasingFunction(vs.scaleDownEasingFunction);
            
            if (vs.floorMaterial == null)
            {
                Debug.Log("The floor material was not found on the Construction Engine. Add a Floor Material to have it updated during placement.");
                _updateFloorMaterial = false;
            }
            else
            {
                _updateFloorMaterial = true;
                _floorMaterial = vs.floorMaterial;
                _floorMaterial.SetFloat(GridAlpha, vs.minGridAlpha);
                minAlpha = vs.minGridAlpha;
                maxAlpha = vs.maxGridAlpha;
                _lerpAlphaTime = vs.lerpAlphaTime;
            }
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

        public void ShowPlacementVisuals(Vector3Int highlightPosition)
        {
            SetHighlightPos(highlightPosition);
            ShowGrid();
            ActivateHighlight();
            ShowAllNodeArrows();
        }

        public void HidePlacementVisuals()
        {
            HideGrid();
            DeactivateHighlight();
            HideAllNodeArrows();
        }
        
        public void SetHighlightPos(Vector3Int pos)
        {
            if (!_updatePlacementHighlight) return;
            PlacementHighlight.transform.position = pos;
        } 
        public void ActivateHighlight()
        {
            if (!_updatePlacementHighlight) return;
            PlacementHighlight.SetActive(true);
        }

        public void DeactivateHighlight()
        {
            if (!_updatePlacementHighlight) return;
            PlacementHighlight.SetActive(false); 
        } 
        
        public void ShowGrid()
        {
            #if UNITASK
            LerpGridAlphaUni(maxAlpha, _lerpAlphaTime).Forget();
            #else
            if(_alphaRoutine != null) _engine.StopCoroutine(_alphaRoutine);
           _alphaRoutine = _engine.StartCoroutine(LerpGridAlpha(1, _lerpAlphaTime/3)); 
            #endif
        }
        
        public void HideGrid()
        {
            # if UNITASK
            LerpGridAlphaUni(minAlpha, _lerpAlphaTime).Forget();
            #else
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
            _alphaRoutine = StartCoroutine(LerpGridAlpha(0, lerpAlphaTime));
            #endif
        }

        private IEnumerator LerpGridAlpha(float end, float lerpTime)
        {
            if (!_updateFloorMaterial) yield break;
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
            if (!_updateFloorMaterial) return;
            CtsCtrl.Clear(ref _alphaCts);
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

        public void ShowAllNodeArrows()
        {
            foreach (Node node in _world.GetNodes())
            {
                if(node == null) continue;
                node.Visuals.ShowArrows();
            }
        }
        
        public void HideAllNodeArrows()
        {
            foreach (Node node in _world.GetNodes())
            {
                if (node == null) continue;
                node.Visuals.HideArrows();
            }
            
        }
    }
}