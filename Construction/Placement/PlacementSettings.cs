using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public enum CellSelectionAlgorithm {FindShortestPath, StraightLinesOnly, LShapedPaths }
    
    [CreateAssetMenu(fileName = "PlacementSettings", menuName = "Construction/PlacementSettings")]
    public class PlacementSettings : ScriptableObject
    {
        [Header("Grid Settings")] public int mapWidth = 36; 
        public int mapHeight = 36;
        
        public Vector3Int mapOrigin = Vector3Int.zero;
        [Tooltip("The size of each grid cell in world units")]
        public float cellSize = 1f;
        [Tooltip("How are paths for placing nodes (like belts) calculated?")]
        public CellSelectionAlgorithm cellSelectionAlgorithm; 

        [Header("Movement Settings")]
        public float moveSpeed = 30f;
        public float raycastDistance = 1000f;
        
        [Header("Layer Settings")]
        public LayerMask floorLayer;

        [Header("Prefabs")] 
        public NodePrefabRegistry prefabRegistry; 
    }
} 