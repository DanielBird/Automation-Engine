using System.Collections.Generic;
using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Visuals;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class PlacementCoordinator
    {
        private readonly List<IPlacementStrategy> _strategies;
        private PlacementState _state;
        private PlacementVisuals _visuals; 

        public PlacementCoordinator(PlacementContext placementContext, DragManager dragManager, NeighbourManager neighbourManager, Transform widgetParent)
        {
            _strategies = new List<IPlacementStrategy>
            {
                new DraggablePlacementStrategy(placementContext, dragManager),
                new StandardPlacementStrategy(placementContext, neighbourManager, widgetParent)
            };

            _state = placementContext.State; 
            _visuals = placementContext.Visuals;
        }

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                if (!strategy.CanHandle(placeable)) continue;
                
                _state.IsRunning = false;
                strategy.HandlePlacement(placeable, gridCoordinate);
                _visuals.DeactivateFloorDecal();
                return;
            }
        }

        public void CancelPlacement(IPlaceable placeable)
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                if (!strategy.CanHandle(placeable)) continue;
                
                strategy.CancelPlacement(placeable);
                _visuals.DeactivateFloorDecal();
                return;
            }
        }

        public void RegisterOnDisable()
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                strategy.CleanUpOnDisable();
            }
        }
    }
} 