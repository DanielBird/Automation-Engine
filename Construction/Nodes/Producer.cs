﻿using System;
using System.Linq;
using System.Threading;
using Construction.Maps;
using Construction.Placement;
using Construction.Widgets;
using Cysharp.Threading.Tasks;
using ScriptableObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities;

namespace Construction.Nodes
{
    public class Producer : Belt
    {
        [Header("Producer Setup")]
        public WidgetPrefabs widgetPrefabs;
        public int poolPreLoadCount = 3; 
        [SerializeField] private int _widgetType;
        [SerializeField] private GameObject _widgetPrefab;
        
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
            Activate(0);
        }
        
        [Button]
        public void Activate() => Activate(0);
        
        public void Activate(int widgetType)
        {
            Active = true;
            _widgetType = widgetType;
            
            if (!WidgetPrefabFound(widgetType)) return;

            ClearToken();
            _ctx = new CancellationTokenSource();
            WaitToSpawn(); 
        }

        private bool WidgetPrefabFound(int widgetType)
        {
            _widgetPrefab = widgetPrefabs.widgets[widgetType];
            if (_widgetPrefab == null)
            {
                Debug.LogWarning("Missing widget prefab");
                Active = false;
                return false;
            }
            
            SimplePool.Preload(_widgetPrefab, transform, poolPreLoadCount);
            return true;
        }

        private void ClearToken()
        {
            if (_ctx != null)
            {
                _ctx.Cancel();
                _ctx.Dispose();
                _ctx = null;
            }
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

            Transform t = transform; 
            GameObject widgetGo = SimplePool.Spawn(_widgetPrefab, t.position + spawnLocation, Quaternion.identity, t);

            if (widgetGo.TryGetComponent<Widget>(out Widget widget))
            {
                Occupant = widget;
            }
        }
        
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(spawnLocation, 0.15f);
        }
    }
}