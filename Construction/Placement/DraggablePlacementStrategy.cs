using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class DraggablePlacementStrategy : IPlacementStrategy
    {
        private readonly DragManager _dragManager;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;

        public DraggablePlacementStrategy(PlacementContext ctx, DragManager dragManager)
        {
            _state = ctx.State;
            _visuals = ctx.Visuals;
            _dragManager = dragManager;
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