using System.Collections;
using Engine.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSettings = Engine.GameState.InputSettings;

namespace Engine.CameraCtrl
{
    public class CameraController : MonoBehaviour
    {
        public Camera mainCamera;
        
        [Header("Input")]
        public InputActionReference moveInput;
        public InputActionReference rotateClockwise;
        public InputActionReference rotateAntiClockwise; 
        
        [Space] public InputSettings inputSettings;

        [field: SerializeField] private Vector3 targetPosition;
        [field: SerializeField] private float inputMoveSpeed;
        [field: SerializeField] private float speed; 
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _lastPosition = Vector3.zero;

        private const float NormalizeThreshold = 1f;
        private const float MinThresholdForMovement = 0.1f; 
        
        [Header("Rotation")]
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease rotationEasing = EasingFunctions.Ease.EaseOutSine;

        private bool _eventsRegistered;
        private Coroutine _rotateRoutine;
        [field: SerializeField] private int rotationIndex;

        private readonly Vector3[] _rotations = new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 90, 0),
            new Vector3(0, 180, 0),
            new Vector3(0, 270, 0),
        };
        
        private void Awake()
        {
            _lastPosition = transform.position;
            RegisterEvents();
            
            _ease = EasingFunctions.GetEasingFunction(rotationEasing);

            if (inputSettings == null)
            {
                Debug.Log("Missing input settings");
                inputSettings = ScriptableObject.CreateInstance<InputSettings>();
            }
        }

        private void RegisterEvents()
        {
            if(_eventsRegistered) return;
            rotateClockwise.action.performed += RotateClockwise;
            rotateAntiClockwise.action.performed += RotateAntiClockwise;
            _eventsRegistered = true;
        }

        private void OnDisable()
        {
            if (_eventsRegistered)
            {
                rotateClockwise.action.performed -= RotateClockwise;
                rotateAntiClockwise.action.performed -= RotateAntiClockwise;
                _eventsRegistered = false;
            }
            
            if(_rotateRoutine != null) StopCoroutine(_rotateRoutine);
        }

        private void Update()
        {
            Vector2 input = moveInput.action.ReadValue<Vector2>();
            
            if(input != Vector2.zero) SetTargetPosition(input);
            
            UpdateVelocity();
            UpdatePosition();
        }

        // Convert input to world space XZ movement relative to the camera
        private void SetTargetPosition(Vector2 input)
        {
            Vector3 inputVector = input.x * CameraRight() + input.y * CameraForward(); 
            if (inputVector.sqrMagnitude > NormalizeThreshold) inputVector.Normalize();
            if (inputVector.sqrMagnitude > MinThresholdForMovement) targetPosition += inputVector; 
        }
        
        private void UpdateVelocity()
        {
            if (Time.deltaTime > Mathf.Epsilon)
            {            
                _velocity = (transform.position - _lastPosition) / Time.deltaTime;
                _velocity.y = 0f;
            }
            else
            {
                _velocity = Vector3.zero;
            }
            
            _lastPosition = transform.position; 
        }

        private void UpdatePosition()
        {
            if (targetPosition.sqrMagnitude > MinThresholdForMovement)
            {
                inputMoveSpeed = RemapValues.ClampedMap(mainCamera.orthographicSize, inputSettings.minZoomIn, inputSettings.maxZoomOut, inputSettings.moveSpeedZoomedIn, inputSettings.moveSpeedZoomedOut);
                
                speed = Mathf.Lerp(speed, inputMoveSpeed, Time.deltaTime * inputSettings.acceleration);
                transform.position += targetPosition * (speed * Time.deltaTime); 
            }
            else
            {
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.deltaTime * inputSettings.dampening);
                transform.position += _velocity * Time.deltaTime; 
            }

            targetPosition = Vector3.zero; 
        }
        
        private Vector3 CameraForward()
        {
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0f;
            return forward; 
        }

        private Vector3 CameraRight()
        {
            Vector3 right = mainCamera.transform.right;
            right.y = 0;
            return right; 
        }

        private void RotateClockwise(InputAction.CallbackContext context)
        {
            rotationIndex++;
            if (rotationIndex >= _rotations.Length) rotationIndex = 0; 
            
            Vector3 start = transform.rotation.eulerAngles;
            Vector3 end = _rotations[rotationIndex];
            
            if(_rotateRoutine != null) StopCoroutine(_rotateRoutine);
            _rotateRoutine = StartCoroutine(Rotate(start, end));
        }

        private void RotateAntiClockwise(InputAction.CallbackContext context)
        {
            rotationIndex--;
            if (rotationIndex < 0) rotationIndex = _rotations.Length - 1; 
            
            Vector3 start = transform.rotation.eulerAngles;
            Vector3 end = _rotations[rotationIndex];
            
            if(_rotateRoutine != null) StopCoroutine(_rotateRoutine);
            _rotateRoutine = StartCoroutine(Rotate(start, end)); 
        }

        private IEnumerator Rotate(Vector3 start, Vector3 end)
        {
            float t = 0;
            while (t < inputSettings.rotationTime)
            {
                float ease = _ease(0, 1, t / inputSettings.rotationTime);
                float angle = Mathf.LerpAngle(start.y, end.y, ease);
                transform.rotation = Quaternion.Euler(new Vector3(0, angle, 0));

                t += Time.deltaTime;
                yield return null; 
            }
            
            transform.rotation = Quaternion.Euler(end);
            _rotateRoutine = null; 
        }
    }
}