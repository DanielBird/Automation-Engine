using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Utilities.Events;
using Engine.Utilities.Events.Types;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Engine.Utilities
{
    public class PlayerClickManager : MonoBehaviour
    {
        [SerializeField] private bool canClick = true; 
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private Camera mainCamera;
        
        public InputActionReference mouseClick;
        public bool clickClosestHits = true; 
        
        [SerializeField] private float maxRaycastDistance = 100f;
        [CanBeNull] private IClickable clickable; 
        private List<IClickable> clickables = new List<IClickable>();

        [Header("Debug")] 
        public bool logClicks; 
        public bool showClosestHit = true;
        public Vector3 gizmoScale = Vector3.one;
        private Transform closestHit; 
        
        RaycastHit[] _results = new RaycastHit[5];

        private EventBinding<PlayerDragEvent> _onPlayerDrag; 
        private bool _eventsRegistered;
        
        private void Awake()
        {
            if(mainCamera == null)
                mainCamera = Camera.main;
            
            RegisterEvents();

            canClick = true;
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            
            if(mouseClick != null)
                mouseClick.action.performed += OnMouseClick;

            _onPlayerDrag = new EventBinding<PlayerDragEvent>(OnPlayerDragEvent); 
            EventBus<PlayerDragEvent>.Register(_onPlayerDrag);
            
            _eventsRegistered = true;
        }

        private void OnDisable()
        {
            if (_eventsRegistered)
            {
                if(mouseClick != null)
                    mouseClick.action.performed -= OnMouseClick; 
            
                EventBus<PlayerDragEvent>.Deregister(_onPlayerDrag);
                
                _eventsRegistered = false;
            }
        }
        
        private void OnPlayerDragEvent(PlayerDragEvent e)
        {
            if(e.Started)
                canClick = false;
            else
                canClick = true;
        }

        private void OnMouseClick(InputAction.CallbackContext obj)
        {
            if(!canClick)
                return;
            
            // Return if the cursor is over a UI element
            if(MouseUtils.IsOverUI()) return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(ray, _results, maxRaycastDistance, buildingLayer);
            
            if(logClicks)
                Debug.Log("Hit Count during last click: " + hits);

            if(clickClosestHits)
                ClickClosest(hits);
            else
                ClickEverythingHit(hits);
            
            Array.Clear(_results, 0, _results.Length); 
        }
        
        private void ClickClosest(int hits)
        {
            if (clickable != null)
            {
                clickable.OnPlayerDeselect();
                clickable = null;
            }
            
            if (hits == 0) return;

            IClickable closest = null; 
            float closestDistance = float.MaxValue;
                
            for (int i = 0; i < hits; i++)
            {
                Transform t = _results[i].transform;
                
                if(!t.TryGetComponent(out IClickable c))
                    continue;
                
                float d = _results[i].distance;
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closest = c;
                    closestHit = t;
                    if (logClicks)
                        Debug.Log("Closest hit is " + t.gameObject.name);
                }
            }

            if (closest == null) return;
            clickable = closest;
            clickable.OnPlayerSelect();
        }

        private void ClickEverythingHit(int hits)
        {
            foreach (IClickable c in clickables) c.OnPlayerDeselect();
            clickables.Clear();

            if (hits == 0) return; 

            HashSet<IClickable> newHits = new ();
            for (int i = 0; i < hits; i++)
            {
                Transform t = _results[i].transform;
                if(t.TryGetComponent(out IClickable c))
                    newHits.Add(c);
            }

            List<IClickable> toDeselect = clickables.Except(newHits).ToList();
            foreach (IClickable c in toDeselect) c.OnPlayerDeselect();

            List<IClickable> toSelect = newHits.Except(clickables).ToList();
            foreach (IClickable c in toSelect) c.OnPlayerSelect();

            clickables.Clear();
            clickables.AddRange(newHits);
        }

        private void OnDrawGizmos()
        {
            if (showClosestHit && closestHit != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f); 
                Gizmos.DrawCube(closestHit.transform.position, gizmoScale);
            }
             
        }
    }
}