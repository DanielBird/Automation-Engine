using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Resources;
using Engine.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.UI
{
    public class ConsumptionDisplay : MonoBehaviour
    {
        public Vector3 spawnLocation = new Vector3(0,1,0); 
        [Space]
        public float displayLength = 1f; 

        [Space]
        public AnimationCurve scaleAnimation;
        public AnimationCurve positionAnimation;

        private CancellationTokenSource _cts;
        
        private void OnDisable()
        {
            CtsCtrl.Clear(ref _cts);
        }

        [Button]
        public void Show(ResourceTypeSo resourceType)
        {
            GameObject indicator = SimplePool.Spawn(resourceType.consumptionDisplayPrefab, spawnLocation, Quaternion.identity, transform);
            ShowIndicator(indicator, spawnLocation).Forget();
        }

        private async UniTaskVoid ShowIndicator(GameObject indicator, Vector3 start)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            indicator.transform.localScale = Vector3.zero;
            
            float t = 0;
            while (t < displayLength)
            {
                float y = start.y + positionAnimation.Evaluate(t / displayLength);
                indicator.transform.localPosition = new Vector3(start.x, y, start.z);
                
                float s = scaleAnimation.Evaluate(t / displayLength);
                indicator.transform.localScale = new Vector3(s, s, s);
                
                t += Time.deltaTime;
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
            
            SimplePool.Despawn(indicator);
            CtsCtrl.Clear(ref _cts);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 position = transform.position + spawnLocation;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(position, 0.25f);
        }
    }
}