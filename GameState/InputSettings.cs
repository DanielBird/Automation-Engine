using UnityEngine;
using UnityEngine.InputSystem;

namespace GameState
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Scriptable Objects/InputSettings")]
    public class InputSettings : ScriptableObject
    {
        [Header("Key Input References")]
        public InputActionReference place;
        public InputActionReference rotate;
        public InputActionReference cancel;
        
        [Header("Directional Input And Rotation")]
        [Tooltip("How long to wait to confirm that a button click is being held?")]
        public float waitForInputTime = 0.2f;
        public float deadZoneRadius = 10;
        public float sphereCastRadius = 0.1f;

        [Header("Placement")] 
        public float minTimeBetweenClicks = 0.1f;

        [Header("Camera Movement")] public float moveSpeedZoomedIn = 20f;
        public float moveSpeedZoomedOut = 100f;
        public float acceleration = 10f;
        public float dampening = 5f; 

        [Header("Zooming")] public float minZoomIn = 15;
        public float maxZoomOut = 60; 
        [Tooltip("How much orthographic size is changed")] public float zoomIncrements = 12f;
        [Tooltip("How long it takes to change the orthographic size")] public float zoomTime = 0.3f; 
        public float minTimeBetweenInputs = 0.01f;

        [Header("Rotation")] public float rotationTime = 0.6f; 
    }
}
