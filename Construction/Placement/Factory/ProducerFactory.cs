using Construction.Nodes;
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
            if (!PlacementSettings.prefabRegistry.FoundPrefab(NodeType.Producer, out GameObject prefabToSpawn))
            {
                Debug.LogWarning($"Unable to find the prefab for a producer node in the prefab registry");
                return null; 
            }
            
            GameObject prefab = SimplePool.Spawn(prefabToSpawn, alignedWorldPosition, Quaternion.identity, PlacementManager.transform);
            prefab.name = prefabToSpawn.name + "_" + alignedWorldPosition.x + "_" + alignedWorldPosition.z;
            return prefab;
        }
    }
}