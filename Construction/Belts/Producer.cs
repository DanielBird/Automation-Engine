using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Nodes;
using Engine.Construction.Widgets;
using Engine.GameState;
using Engine.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A belt that spawns widgets for transportation around a belt network.
    /// Widgets automatically start spawning when the player places the Producer.
    /// (... which triggers Initialise)  
    /// </summary>
    public class Producer : Belt
    {
        [Header("Producer Setup")]
        public WidgetPrefabsSO widgetPrefabs;
        public int poolPreLoadCount = 3; 
        [SerializeField] private int _widgetType;
        [SerializeField] private GameObject widgetPrefab;
        private Transform _widgetParent;
        
        [Header("Spawning")]
        public Vector3 spawnLocation;
        public float attemptSpawnFrequency = 2f; 
        [field: SerializeField] public bool Active { get; private set; }
        
        private CancellationTokenSource _ctx;

        private void OnDisable()
        {
            ClearToken();
        }
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            
            Activate(CoreGameState.ProducerCount);
            
            // Temporary Code for generating new widgets based purely on the number of producers in the scene
            CoreGameState.ProducerCount++; 
            if(CoreGameState.ProducerCount >= widgetPrefabs.widgets.Count) CoreGameState.ProducerCount = 0;
        }
        
        public void SetWidgetParent(Transform parent) => _widgetParent = parent;
        
        [Button]
        public void Activate() => Activate(CoreGameState.ProducerCount);
        
        public void Activate(int widgetType)
        {
            Active = true;
            _widgetType = widgetType;
            
            if (!WidgetPrefabFound(widgetType)) return;

            ClearToken();
            _ctx = new CancellationTokenSource();
            WaitToSpawn().Forget(); 
        }

        private bool WidgetPrefabFound(int widgetType)
        {
            if (widgetPrefabs.widgets.Count == 0)
            {
                Debug.LogWarning("No widget prefabs found in the scriptable object");
                return false;
            }
            
            widgetPrefab = widgetPrefabs.widgets[widgetType];
            if (widgetPrefab == null)
            {
                Debug.LogWarning("Missing widget prefab");
                Active = false;
                return false;
            }
            
            SimplePool.Preload(widgetPrefab, _widgetParent, poolPreLoadCount);
            return true;
        }

        private void ClearToken()
        {
            if (_ctx == null) return;
            _ctx.Cancel();
            _ctx.Dispose();
            _ctx = null;
        }

        [Button]
        public void Deactivate()
        {
            Active = false;
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

            GameObject widgetGo = SimplePool.Spawn(widgetPrefab, transform.position + spawnLocation, Quaternion.identity, _widgetParent);

            if (widgetGo.TryGetComponent<Widget>(out Widget widget))
            {
                Occupant = widget;
            }
        }
    }
}