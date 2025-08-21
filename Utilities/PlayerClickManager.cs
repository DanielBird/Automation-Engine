using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Engine.Utilities
{
    public class PlayerClickManager : MonoBehaviour
    {
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private Camera mainCamera;
        
        public InputActionReference mouseClick;
        public bool clickClosestHits = true; 
        
        [SerializeField] private float maxRaycastDistance = 100f;
        private IClickable clickable; 
        private List<IClickable> clickables = new List<IClickable>();

        [Header("Debug")]
        public bool showClosestHit = true;
        public Vector3 gizmoScale = Vector3.one;
        private Transform closestHit; 
        
        RaycastHit[] _results = new RaycastHit[5];   
        
        private void Awake()
        {
            if(mainCamera == null)
                mainCamera = Camera.main;
            
            if(mouseClick != null)
                mouseClick.action.performed += OnMouseClick; 
        }
        
        private void OnDisable()
        {
            if(mouseClick != null)
                mouseClick.action.performed -= OnMouseClick; 
        }

        private void OnMouseClick(InputAction.CallbackContext obj)
        {
            // Return if the cursor is over a UI element
            if (EventSystem.current.IsPointerOverGameObject()) return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(ray, _results, maxRaycastDistance, buildingLayer);

            if(clickClosestHits)
                ClickClosest(hits);
            else
                ClickEverythingHit(hits);
        }
        
        private void ClickClosest(int hits)
        {
            if (hits == 0)
            {
                if (clickable != null)
                {
                    clickable.OnPlayerDeselect();
                    clickable = null;
                }

                return;
            }

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
                }
            }

            if (closest == null) return;
            clickable = closest;
            clickable.OnPlayerSelect();
        }

        private void ClickEverythingHit(int hits)
        {
            if (hits == 0)
            {
                foreach (IClickable c in clickables) c.OnPlayerDeselect();
                clickables.Clear();
                return;
            }

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