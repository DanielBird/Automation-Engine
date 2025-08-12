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
            GameObject prefab = SimplePool.Spawn(PlacementSettings.standardBeltPrefab, alignedWorldPosition, Quaternion.identity, PlacementManager.transform);
            prefab.name = PlacementSettings.standardBeltPrefab.name + "_" + alignedWorldPosition.x + "_" + alignedWorldPosition.z; 
            return prefab;
        }
    }
}