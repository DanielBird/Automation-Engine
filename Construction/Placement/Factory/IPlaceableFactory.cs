using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Placement.Factory
{
    public interface IPlaceableFactory
    {
        PlacementManager PlacementManager { get; }
        PlacementSettings PlacementSettings { get; }
        NodeType NodeType { get; }
        
        bool Create(out GameObject prefab, out Vector3Int alignedWorldPosition); 
        
        GameObject CreateAt(Vector3Int alignedWorldPosition);
    }
}