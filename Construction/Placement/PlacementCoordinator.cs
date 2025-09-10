using System.Collections.Generic;
using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Placement.Strategies;
using Engine.Construction.Visuals;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class PlacementCoordinator
    {
        private readonly List<IPlacementStrategy> _strategies;
        private PlacementState _state;
        private PlacementVisuals _visuals; 

        public PlacementCoordinator(PlacementContext placementContext, PlacementManager placementManager, DragManager dragManager, Transform resourceParent)
        {
            StandardPlacementStrategy standard = new StandardPlacementStrategy(placementContext, placementManager, resourceParent);
            DraggablePlacementStrategy draggable = new DraggablePlacementStrategy(placementContext, dragManager, standard); 
            
            _strategies = new List<IPlacementStrategy>
            {
                draggable,
                standard
            };

            _state = placementContext.State; 
            _visuals = placementContext.Visuals;
        }

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                if (!strategy.CanHandle(placeable)) continue;
                
                _state.StopRunning();
                strategy.HandlePlacement(placeable, gridCoordinate);
                return;
            }
        }

        public void CancelPlacement(IPlaceable placeable)
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                if (!strategy.CanHandle(placeable)) continue;
                
                strategy.CancelPlacement(placeable);
                return;
            }
        }

        public void Disable()
        {
            foreach (IPlacementStrategy strategy in _strategies)
            {
                strategy.CleanUpOnDisable();
            }
        }
    }
} 