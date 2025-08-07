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
        
        public bool Create(out GameObject prefab, out Vector3Int alignedPosition)
        {
            prefab = null; 
            alignedPosition = Vector3Int.zero;
            
            if(!PlacementManager.TryGetGridAlignedPosition(out alignedPosition)) return false;
            
            prefab  = SimplePool.Spawn(PlacementSettings.standardBeltPrefab, alignedPosition, Quaternion.identity, PlacementManager.transform);
            prefab.name = PlacementSettings.standardBeltPrefab.name + "_" + alignedPosition.x + "_" + alignedPosition.z; 
            
            return true;
        }
    }
}