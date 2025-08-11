using Construction.Nodes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Construction.Placement
{
    public enum CellSelectionAlgorithm {FindShortestPath, StraightLinesOnly, LShapedPaths }
    
    [CreateAssetMenu(fileName = "PlacementSettings", menuName = "Construction/PlacementSettings")]
    public class PlacementSettings : ScriptableObject
    {
        [Header("Grid Settings")]
        public Vector3Int gridOrigin = Vector3Int.zero;
        [Tooltip("The size of each grid cell in world units")]
        public float tileSize = 1f;
        [Tooltip("How are paths for placing nodes (like belts) calculated?")]
        public CellSelectionAlgorithm cellSelectionAlgorithm; 

        [Header("Movement Settings")]
        public float moveSpeed = 30f;
        public float raycastDistance = 1000f;

        [Header("Input Settings")]
        public InputActionReference place;
        public InputActionReference rotate;
        public InputActionReference cancel;

        [Header("Layer Settings")]
        public LayerMask floorLayer;

        [Header("Prefabs")] 
        public NodePrefabRegistry prefabRegistry; 
        public GameObject standardBeltPrefab;
        public GameObject leftBeltPrefab;
        public GameObject rightBeltPrefab;
        public GameObject producerPrefab;
    }
} 