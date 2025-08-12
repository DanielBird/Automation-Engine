using UnityEngine;

namespace Construction.Placement.Factory
{
    public interface IPlaceableFactory
    {
        PlacementManager PlacementManager { get; }
        PlacementSettings PlacementSettings { get; }
        
        bool Create(out GameObject prefab, out Vector3Int alignedWorldPosition); 
        
        GameObject CreateAt(Vector3Int alignedWorldPosition);
    }
}