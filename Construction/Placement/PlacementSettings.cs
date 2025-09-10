using System.Collections.Generic;
using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public enum CellSelectionAlgorithm {FindShortestPath, StraightLinesOnly, LShapedPaths }
    
    [CreateAssetMenu(fileName = "PlacementSettings", menuName = "Automation Engine/PlacementSettings")]
    public class PlacementSettings : ScriptableObject
    {
        [Header("Grid Settings")] 
        [Tooltip("The number of grid cells along the X axis.")]
        public int mapWidth = 50; 
        [Tooltip("The number of grid cells along the Z axis.")]
        public int mapHeight = 50;
        [Tooltip("The starting point for all grid cells in world space")]
        public Vector3Int mapOrigin = Vector3Int.zero;
        [Tooltip("The size of each grid cell in world units")]
        public float cellSize = 1f;
        [Tooltip("How are paths for placing nodes (like belts) calculated?")]
        public CellSelectionAlgorithm cellSelectionAlgorithm; 

        [Header("Movement Settings")]
        [Tooltip("How quickly do spawned game objects follow the player's cursor and snap to their grid position?")]
        public float moveSpeed = 30f;
        public float raycastDistance = 1000f;
        [Tooltip("The threshold at which a game object is considered to be arrived at it's target position.")]
        public float arrivedThreshold = 0.02f; 
        
        [Header("Layer Settings")]
        public LayerMask floorLayer;

        [Header("Prefabs")] 
        public NodePrefabRegistry prefabRegistry; 
        
        [Header("Removal Ui")]
        public GameObject destructionIconPrefab;
        
        [Tooltip("Used if displaying destruction icons above cells without nodes. Currently not in use.")]
        public GameObject emptyDestructionIconPrefab;
        
        [Tooltip("How long should it take for the icon to tween between its spawn and final location?")]
        public float iconPopUpLength = 0.3f; 
        
        [Tooltip("Where should the icon end up, relative to its spawn location?")]
        public Vector3 destructionIconOffset = new Vector3(0, 1f, 0);

        [Space] 
        [Tooltip("Should the spawn location of a destruction icon be offset by a given amount for different node types?")]
        public List<NodeOffsetBinding> removalIndicatorOffsets;
        
        public bool FoundOffset(NodeType nodeType, out Vector3 offset)
        {
            offset = Vector3.zero;
            if(!removalIndicatorOffsets.Exists(binding => binding.nodeType == nodeType)) return false;
            
            offset = removalIndicatorOffsets.Find(binding => binding.nodeType == nodeType).offset;
            return true;
        }
    }

    [System.Serializable]
    public struct NodeOffsetBinding
    {
        public NodeType nodeType;
        public Vector3 offset;
    }
} 