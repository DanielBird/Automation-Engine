using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Events;
using Engine.Construction.Placement;
using Engine.UI;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public struct IconReference : IEquatable<IconReference>
    {
        public Vector3 MyPos;
        public GameObject IconObject;
        public TweenPosition PositionTween;

        public IconReference(Vector3 myPos, GameObject iconObject, TweenPosition positionTween)
        {
            MyPos = myPos;
            IconObject = iconObject;
            PositionTween = positionTween;
        }

        public bool Equals(IconReference other) => MyPos.Equals(other.MyPos) && Equals(IconObject, other.IconObject) && Equals(PositionTween, other.PositionTween);
        public override bool Equals(object obj) => obj is IconReference other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MyPos, IconObject, PositionTween);
    }
    
    public class DestructionUI : MonoBehaviour
    {
        public Camera mainCamera; 
        
        public PlacementSettings placementSettings;
        
        public int preloadCount = 5;

        private float iconPopUpLength; 
        private Vector3 destructionIconOffset;

        private HashSet<IconReference> _destructionIcons = new HashSet<IconReference>();
        private EventBinding<DestructionEvent> _onDestructionEvent;
        private bool _eventsRegistered;
        
        private void Start()
        {
            if(mainCamera == null)
                mainCamera = Camera.main;
            
            iconPopUpLength = placementSettings.iconPopUpLength;
            destructionIconOffset = placementSettings.destructionIconOffset;
            
            SimplePool.Preload(placementSettings.destructionIconPrefab, transform, preloadCount);
            SimplePool.Preload(placementSettings.emptyDestructionIconPrefab, transform, preloadCount);

            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            _onDestructionEvent = new EventBinding<DestructionEvent>(OnDestruct);
            EventBus<DestructionEvent>.Register(_onDestructionEvent);
            _eventsRegistered = true;
        }

        private void OnDisable()
        {
            if (_eventsRegistered)
            {
                EventBus<DestructionEvent>.Deregister(_onDestructionEvent);
                _eventsRegistered = false;
            }
        }

        private void OnDestruct(DestructionEvent e)
        {
            switch (e.Type)
            {
                case DestructionEventType.Cancel:
                    Clear(e.Positions);
                    break;
                case DestructionEventType.DestroyNode:
                    Spawn(e.Positions, placementSettings.destructionIconPrefab);
                    break;
                case DestructionEventType.DestroyEmpty:
                    Spawn(e.Positions, placementSettings.emptyDestructionIconPrefab);
                    break;
                case DestructionEventType.ClearAll:
                    DespawnAllIcons();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Clear(IEnumerable<Vector3> positions)
        {
            foreach (Vector3 pos in positions)
            {
                DespawnIcon(pos);
            }
        }

        private void Spawn(IEnumerable<Vector3> positions, GameObject prefab)
        {
            foreach (Vector3 pos in positions)
            {
                SpawnIcon(pos, prefab);
            }
        }
        
        private void SpawnIcon(Vector3 pos, GameObject prefab)
        {
            if (_destructionIcons.Any(r => r.MyPos == pos))
                return; 

            Vector3 endPos = pos + destructionIconOffset;
            
            GameObject icon = SimplePool.Spawn(prefab, endPos, Quaternion.identity, transform);
            
            if(icon.TryGetComponent(out BillboardUI billboard)) billboard.InitialiseCamera(mainCamera);
            if(icon.TryGetComponent(out TweenPosition tween)) tween.DoTween(pos, endPos, iconPopUpLength, 0);
            
            _destructionIcons.Add(new IconReference(pos, icon, tween));
        }

        private void DespawnIcon(Vector3 position)
        {
            if (_destructionIcons.All(r => r.MyPos != position))
            {
                Debug.LogWarning("Destruction icon not found");
                return;
            }
            
            IconReference iconRef = _destructionIcons.First(r => r.MyPos == position);
            TweenAndDespawn(position, iconRef);
            _destructionIcons.Remove(iconRef);
        }

        private void TweenAndDespawn(Vector3 position, IconReference iconRef)
        {
            if (iconRef.PositionTween == null)
                OnComplete(iconRef.IconObject);
            else
                iconRef.PositionTween.DoTween(position-destructionIconOffset, iconPopUpLength, 0, OnComplete, iconRef.IconObject);
        }

        private void DespawnAllIcons()
        {
            HashSet<IconReference> positions = new();
            foreach (IconReference ir in _destructionIcons)
            {
                Vector3 position = ir.MyPos;
                TweenAndDespawn(position, ir);
                positions.Add(ir);
            }
            
            foreach(IconReference ir in positions) _destructionIcons.Remove(ir);
        }

        private void OnComplete(GameObject icon)
        {
            SimplePool.Despawn(icon);
        }
    }
}