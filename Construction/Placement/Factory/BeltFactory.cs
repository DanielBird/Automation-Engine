using Construction.Nodes;
using UnityEngine;
using Utilities;

namespace Construction.Placement.Factory
{
    public class BeltFactory : IPlaceableFactory
    {
        public PlacementManager PlacementManager { get; }
        public PlacementSettings PlacementSettings { get; }

        public BeltFactory(PlacementManager placementManager, PlacementSettings settings)
        {
            PlacementManager = placementManager;
            PlacementSettings = settings;
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
            if (!PlacementSettings.prefabRegistry.FoundPrefab(NodeType.Straight, out GameObject prefabToSpawn))
            {
                Debug.LogWarning($"Unable to find the prefab for a straight node in the prefab registry");
                return null; 
            }
            
            GameObject prefab = SimplePool.Spawn(prefabToSpawn, alignedWorldPosition, Quaternion.identity, PlacementManager.transform);
            return prefab;
        }
    }
}