using Construction.Interfaces;
using UnityEngine;

namespace Construction.Placement
{
    public interface IPlacementStrategy
    {
        bool CanHandle(IPlaceable placeable);
        void HandlePlacement(IPlaceable placeable, Vector3Int position);
        void CancelPlacement(IPlaceable placeable);
    }
} 