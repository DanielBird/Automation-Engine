using UnityEngine;
using UnityEngine.InputSystem;
using InputSettings = ScriptableObjects.InputSettings;

namespace Construction.Placement
{
    [CreateAssetMenu(fileName = "PlacementSettings", menuName = "Construction/PlacementSettings")]
    public class PlacementSettings : ScriptableObject
    {
        [Header("Grid Settings")]
        public Vector3 gridOrigin = Vector3.zero;
        public float tileSize = 1f;
        public bool useLShapedPaths; 

        [Header("Movement Settings")]
        public float moveSpeed = 30f;
        public float raycastDistance = 1000f;

        [Header("Input Settings")]
        public InputSettings inputSettings;
        public InputActionReference place;
        public InputActionReference rotate;
        public InputActionReference cancel;

        [Header("Layer Settings")]
        public LayerMask floorLayer;

        [Header("Prefabs")]
        public GameObject standardBeltPrefab;
        public GameObject leftBeltPrefab;
        public GameObject rightBeltPrefab;
        public GameObject producerPrefab;
    }
} 