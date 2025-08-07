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
        
        public bool Create(out GameObject prefab, out Vector3Int alignedPosition)
        {
            prefab = null; 
            alignedPosition = Vector3Int.zero;
            
            if(!PlacementManager.TryGetGridAlignedPosition(out alignedPosition)) return false;
            
            prefab  = SimplePool.Spawn(PlacementSettings.producerPrefab, alignedPosition, Quaternion.identity, PlacementManager.transform);
            prefab.name = PlacementSettings.producerPrefab.name + "_" + alignedPosition.x + "_" + alignedPosition.z; 
            
            return true;
        }
    }
}