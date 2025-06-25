using UnityEngine;
using Utilities;

namespace Construction.Placement.Factory
{
    public class ProducerFactory : IPlaceableFactory
    {
        public PlacementManager PlacementManager { get; }
        public PlacementSettings PlacementSettings { get; }

        public ProducerFactory(PlacementManager placementManager, PlacementSettings placementSettings)
        {
            PlacementManager = placementManager;
            PlacementSettings = placementSettings;
        }
        
        public bool Create(out GameObject prefab, out Vector3Int gridPos)
        {
            prefab = null; 
            gridPos = Vector3Int.zero;
            
            if(!PlacementManager.TryGetGridPosition(out Vector3Int gridPosition)) return false;
            
            gridPos = gridPosition;
            prefab  = SimplePool.Spawn(PlacementSettings.producerPrefab, gridPos, Quaternion.identity, PlacementManager.transform);
            prefab.name = PlacementSettings.producerPrefab.name + "_" + gridPos.x + "_" + gridPos.z; 
            
            return true;
        }
    }
}