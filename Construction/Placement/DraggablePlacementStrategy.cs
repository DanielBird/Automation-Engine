using Construction.Drag;
using Construction.Interfaces;
using Construction.Nodes;
using Construction.Visuals;
using UnityEngine;

namespace Construction.Placement
{
    public class DraggablePlacementStrategy : IPlacementStrategy
    {
        private readonly DragManager _dragManager;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;

        public DraggablePlacementStrategy(
            DragManager dragManager, 
            PlacementState state,
            PlacementVisuals visuals)
        {
            _dragManager = dragManager;
            _state = state;
            _visuals = visuals;
        }

        public bool CanHandle(IPlaceable placeable) => placeable.Draggable;

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            _state.IsRunning = false;
            if(placeable is Node node) _dragManager.HandleDrag(_state.CurrentObject, node, gridCoordinate).Forget();
        }

        public void CancelPlacement(IPlaceable placeable)
        {
            _dragManager.DespawnAll();
            _visuals.Hide();
        }

        public void CleanUpOnDisable() =>_dragManager.Disable();
    }
} 