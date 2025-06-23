using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Construction.Visuals
{
    public class PlacementVisuals : MonoBehaviour
    {
        public float placementTime = 0.5f;
        public float removeTime = 0.1f; 
        public Vector3 startingScale = new Vector3(0, 0, 0);
        public Vector3 endScale = new Vector3(1, 1, 1); 
        
        [Space] private EasingFunctions.Function _scaleUpEasing; 
        public EasingFunctions.Ease scaleEasingFunction = EasingFunctions.Ease.EaseOutSine;
        
        [Space] private EasingFunctions.Function _scaleDownEasing; 
        public EasingFunctions.Ease scaleDownEasingFunction = EasingFunctions.Ease.EaseOutSine;

        [Space] public Material floorMaterial;
        private static readonly int UseGrid = Shader.PropertyToID("_useGrid");

        private Dictionary<GameObject, Coroutine> _despawnRoutines = new Dictionary<GameObject, Coroutine>();  

        private void Awake()
        {
            _scaleUpEasing = EasingFunctions.GetEasingFunction(scaleEasingFunction);
            _scaleDownEasing = EasingFunctions.GetEasingFunction(scaleDownEasingFunction);

            if (floorMaterial == null)
            {
                Debug.LogError("Missing floor material");
                return;
            }
            
            floorMaterial.SetInt(UseGrid, 0);
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
            floorMaterial.SetInt(UseGrid, 1); 
        }
        
        public void Hide()
        {
            floorMaterial.SetInt(UseGrid, 0); 
        } 
    }
}