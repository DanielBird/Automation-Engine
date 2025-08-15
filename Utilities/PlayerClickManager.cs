using UnityEngine;
using UnityEngine.InputSystem;

namespace Utilities
{
    public class PlayerClickManager : MonoBehaviour
    {
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private Camera mainCamera;
        
        public InputActionReference mouseClick;
        
        [SerializeField] private float maxRaycastDistance = 100f;
        private IClickable clickable; 
        
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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit raycastHit, maxRaycastDistance, buildingLayer))
            {
                if (clickable != null)
                {
                    clickable.OnPlayerDeselect();
                    clickable = null;
                }
                
                return;
            }
            
            
            if (raycastHit.transform.TryGetComponent(out clickable))
                clickable.OnPlayerSelect();
        }
        
    }
}