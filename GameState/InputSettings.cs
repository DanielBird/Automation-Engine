using UnityEngine;
using UnityEngine.InputSystem;

namespace Engine.GameState
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Automation Engine/InputSettings")]
    public class InputSettings : ScriptableObject
    {
        [Header("Key Input References")]
        public InputActionReference place;
        public InputActionReference rotate;
        public InputActionReference cancel;
        
        [Header("Directional Input And Rotation")]
        [Tooltip("How long to wait to confirm that a mouse click should be considered a drag? Recommended: 0.1 - 0.2")]
        [Range(0.05f, 0.5f)]
        public float waitForInputTime = 0.1f;
        [Tooltip("How far away should the mouse be from the original click location to be considered a drag? Recommended: 5 - 10")]
        [Range(1, 20)]
        public float dragThreshold = 5f; 
        
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
