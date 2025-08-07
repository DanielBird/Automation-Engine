using System.Collections;
using GameState;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
using InputSettings = GameState.InputSettings;

namespace CameraCtrl
{
    public class CameraZoom : MonoBehaviour
    {
        [Header("Setup")]
        public Camera mainCamera;
        [Space] public InputSettings inputSettings;

        [Header("Input")] public InputActionReference mouseWheel;
        public InputActionReference toggleZoom; 

        [field: SerializeField] private bool zoomedIn; 
        private float _timeOfLastInput;

        [Space] private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease easingFunction = EasingFunctions.Ease.EaseOutSine;
        
        private Coroutine _zoom; 
        
        private void Awake()
        {
            if (inputSettings == null)
            {
                Debug.LogWarning("No input settings found");
                inputSettings = ScriptableObject.CreateInstance<InputSettings>();
            }
            
            mouseWheel.action.performed += Zoom; 
            toggleZoom.action.performed += ToggleZoom; 

            zoomedIn = true; 
            _timeOfLastInput = Time.unscaledTime - 100; 
            _ease = EasingFunctions.GetEasingFunction(easingFunction);
        }

        private void OnDisable()
        {
            mouseWheel.action.performed -= Zoom; 
            toggleZoom.action.performed -= ToggleZoom; 
        }

        private void Zoom(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>(); 
            if (input == Vector2.zero) return;
            
            if(UiState.uiOpen) return;
            
            if(Time.unscaledTime < _timeOfLastInput + inputSettings.minTimeBetweenInputs) return;
            _timeOfLastInput = Time.unscaledTime;

            
            
            if (input.y > 0.9f)
            {
                if(_zoom != null) StopCoroutine(_zoom);
                float end = mainCamera.orthographicSize - inputSettings.zoomIncrements; 
                if(end < inputSettings.minZoomIn + inputSettings.zoomIncrements) end = inputSettings.minZoomIn;
                _zoom = StartCoroutine(LerpZoom(end)); 
            }

            if (input.y < -0.9f)
            {
                if(_zoom != null) StopCoroutine(_zoom);
                float end = mainCamera.orthographicSize + inputSettings.zoomIncrements; 
                if(end > inputSettings.maxZoomOut - inputSettings.zoomIncrements) end = inputSettings.maxZoomOut;
                _zoom = StartCoroutine(LerpZoom(end)); 
            }
        }

        private void ToggleZoom(InputAction.CallbackContext context)
        {
            if (zoomedIn)
            {
                zoomedIn = false;
                if(_zoom != null) StopCoroutine(_zoom);
                _zoom = StartCoroutine(LerpZoom(inputSettings.maxZoomOut)); 
            }
            else
            {
                ZoomIn();
            }
        }

        public void ZoomIn()
        {
            zoomedIn = true; 
            if(_zoom != null) StopCoroutine(_zoom);
            _zoom = StartCoroutine(LerpZoom(inputSettings.minZoomIn));
        }
        
        private IEnumerator LerpZoom(float end)
        {
            if (end > inputSettings.maxZoomOut)
            {
                end = inputSettings.maxZoomOut;
                zoomedIn = false; 
            }

            if (end < inputSettings.minZoomIn)
            {
                end = inputSettings.minZoomIn;
                zoomedIn = true;
            } 
            
            float start = mainCamera.orthographicSize;
            
            float t = 0;
            while (t < inputSettings.zoomTime)
            {
                float ease = _ease(0, 1, t / inputSettings.zoomTime);
                mainCamera.orthographicSize = Mathf.Lerp(start, end, ease); 
                
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            mainCamera.orthographicSize = end;
            _zoom = null; 
        }
        
    }
}
