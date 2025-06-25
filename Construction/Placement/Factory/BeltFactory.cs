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
        
        public bool Create(out GameObject prefab, out Vector3Int gridPos)
        {
            prefab = null; 
            gridPos = Vector3Int.zero;
            
            if(!PlacementManager.TryGetGridPosition(out Vector3Int gridPosition)) return false;
            
            gridPos = gridPosition;
            prefab  = SimplePool.Spawn(PlacementSettings.standardBeltPrefab, gridPos, Quaternion.identity, PlacementManager.transform);
            prefab.name = PlacementSettings.standardBeltPrefab.name + "_" + gridPos.x + "_" + gridPos.z; 
            
            return true;
        }
    }
}