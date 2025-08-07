using System;
using System.Collections.Generic;
using Construction.Drag;
using Construction.Events;
using UI;
using UnityEngine;
using Utilities;
using Utilities.Events;

namespace Construction.Visuals
{
    public class DestructionUI : MonoBehaviour
    {
        public Camera mainCamera; 
        
        public GameObject destructionIconPrefab;
        public GameObject emptyDestructionIconPrefab;
        public int preloadCount = 5;
        
        public Vector3 destructionIconOffset = new Vector3(0, 1.5f, 0);

        private Dictionary<Vector3, GameObject> _destructionIcons = new Dictionary<Vector3, GameObject>();
        
        private EventBinding<DestructionEvent> _onDestructionEvent;
        private void Start()
        {
            if(mainCamera == null)
                mainCamera = Camera.main;
            
            SimplePool.Preload(destructionIconPrefab, transform, preloadCount);
            SimplePool.Preload(emptyDestructionIconPrefab, transform, preloadCount);

            _onDestructionEvent = new EventBinding<DestructionEvent>(OnDestruct);
            EventBus<DestructionEvent>.Register(_onDestructionEvent);
        }

        private void OnDisable()
        {
            EventBus<DestructionEvent>.Deregister(_onDestructionEvent);
        }

        private void OnDestruct(DestructionEvent e)
        {
            switch (e.Type)
            {
                case DestructionEventType.Cancel:
                    Clear(e.Positions);
                    break;
                case DestructionEventType.DestroyNode:
                    Spawn(e.Positions, destructionIconPrefab);
                    break;
                case DestructionEventType.DestroyEmpty:
                    Spawn(e.Positions, emptyDestructionIconPrefab);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Clear(IEnumerable<Vector3Int> positions)
        {
            foreach (Vector3Int pos in positions)
            {
                DespawnIcon(pos);
            }
        }

        private void Spawn(IEnumerable<Vector3Int> positions, GameObject prefab)
        {
            foreach (Vector3Int pos in positions)
            {
                SpawnIcon(pos, prefab);
            }
        }
        
        private void SpawnIcon(Vector3Int pos, GameObject prefab)
        {
            if (_destructionIcons.ContainsKey(pos))
                return; 

            GameObject icon = SimplePool.Spawn(prefab, pos + destructionIconOffset, Quaternion.identity, transform);
            _destructionIcons.Add(pos, icon);
            
            if(icon.TryGetComponent(out BillboardUI billboard)) billboard.InitialiseCamera(mainCamera);
        }

        private void DespawnIcon(Vector3 position)
        {
            if (!_destructionIcons.TryGetValue(position, out GameObject icon))
            {
                return; 
            }
            
            SimplePool.Despawn(icon);
            _destructionIcons.Remove(position);
        }
    }
}