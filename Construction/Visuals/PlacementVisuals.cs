using System;
using System.Collections;
using UnityEngine;
using Utilities;

namespace Construction.Visuals
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
        }

        public void Place(GameObject go)
        {
            StartCoroutine(LerpScale(go, startingScale, endScale, placementTime, _scaleUpEasing)); 
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
        }

        public void Show()
        {
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
           _alphaRoutine = StartCoroutine(LerpGridAlpha(1, lerpAlphaTime/3)); 
            floorDecal.SetActive(true);
        }
        
        public void Hide()
        {
            if(_alphaRoutine != null) StopCoroutine(_alphaRoutine);
            _alphaRoutine = StartCoroutine(LerpGridAlpha(0, lerpAlphaTime)); 
            floorDecal.SetActive(false);
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
        
        public void SetFloorDecalPos(Vector3Int pos) => floorDecal.transform.position = pos;
        
        public void DeactivateFloorDecal() => floorDecal.SetActive(false);
    }
}