using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Resources;
using Engine.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A belt that extracts resources from a resource source for transportation around a belt network.
    /// Extracts one resource at a time
    /// </summary>
    public class Producer : Belt
    {
        [Header("Producer Setup")]
        private IResourceSource _resourceSource;
        [SerializeField] private ResourceTypeSo myResourceType;
        [SerializeField] private string myResourceName;
        public int poolPreLoadCount = 3; 
        
        private Transform _resourceParent;
        private GameObject _resourcePrefab;
        
        [Header("Spawning")]
        public Vector3 spawnLocation;
        public float attemptSpawnFrequency = 2f; 
        [field: SerializeField] public bool Active { get; private set; }
        
        private CancellationTokenSource _ctx;

        private void OnDisable()
        {
            CtsCtrl.Clear(ref _ctx);
        }
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
        }
        
        public void Activate(IResourceMap resourceMap, Transform parent)
        {
            if (!resourceMap.TryGetResourceSourceAt(GridCoord, out _resourceSource)) return;

            if (!InitialiseTheResource())
            {
                Debug.Log(name + " failed to find a resource");
                return;
            }
            
            Active = true;
            _resourceParent = parent;
            
            InitialiseSpawning();
        }

        private bool InitialiseTheResource()
        {
            myResourceType = _resourceSource.ResourceType;
            
            if (myResourceType == null) 
                return false;
            
            if (myResourceType.resourcePrefab == null)
            {
                Debug.LogWarning($"The resource prefab was not found on {myResourceType.name}. Please assign a prefab in the inspector.");
                return false;
            }
            
            myResourceName = myResourceType.name;
            _resourcePrefab = myResourceType.resourcePrefab;
            return true; 
        }
        
        [Button]
        public void Deactivate() => Active = false;

        private void InitialiseSpawning()
        {
            SimplePool.Preload(_resourcePrefab, _resourceParent, poolPreLoadCount);
            
            CtsCtrl.Clear(ref _ctx);
            _ctx = new CancellationTokenSource();
            
            WaitToSpawn().Forget();
        }

        private async UniTaskVoid WaitToSpawn()
        {
            while (Active)
            {
                if (!Active) return;

                while (IsOccupied)
                {
                    try
                    {
                        await UniTask.WaitForSeconds(0.1f, cancellationToken: _ctx.Token);
                    }
                    catch (ObjectDisposedException )
                    {
                        Debug.Log("ObjectDisposedException on " + name);
                        return;
                    }
                }
                
                await UniTask.WaitForSeconds(attemptSpawnFrequency, cancellationToken: _ctx.Token);
                Spawn();
            }
        }

        private void Spawn()
        {
            if (!isActiveAndEnabled) return;

            if(!_resourceSource.TryExtract(1, out int extracted)) return;
            
            GameObject resourceGo = SimplePool.Spawn(_resourcePrefab, transform.position + spawnLocation, Quaternion.identity, _resourceParent);

            if (resourceGo.TryGetComponent<Resource>(out Resource resource))
            {
                Occupant = resource;
            }
        }
    }
}