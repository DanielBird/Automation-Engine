using Construction.Nodes;
using UnityEngine;
using Utilities;

namespace Construction.Placement.Factory
{
    public class GenericFactory : IPlaceableFactory
    {
        public PlacementManager PlacementManager { get; }
        public PlacementSettings PlacementSettings { get; }
        public NodeType NodeType { get; }

        public GenericFactory(PlacementManager placementManager, PlacementSettings settings, NodeType nodeType)
        {
            PlacementManager = placementManager;
            PlacementSettings = settings;
            NodeType = nodeType;
        }
        
        public bool Create(out GameObject prefab, out Vector3Int alignedWorldPosition)
        {
            prefab = null; 
            alignedWorldPosition = Vector3Int.zero;
            
            if(!PlacementManager.TryGetGridAlignedWorldPosition(out alignedWorldPosition)) return false;
            prefab = CreateAt(alignedWorldPosition);
            
            return true;
        }

        public GameObject CreateAt(Vector3Int alignedWorldPosition)
        {
            if (!PlacementSettings.prefabRegistry.FoundPrefab(NodeType, out GameObject prefabToSpawn))
            {
                Debug.LogWarning($"Unable to find the prefab for a {NodeType} node in the prefab registry");
                return null; 
            }
            
            GameObject prefab = SimplePool.Spawn(prefabToSpawn, alignedWorldPosition, Quaternion.identity, PlacementManager.transform);
            return prefab;
        }
    }
}