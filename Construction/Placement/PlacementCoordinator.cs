using System.Collections.Generic;
using Construction.Drag;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Visuals;
using UnityEngine;

namespace Construction.Placement
{
    public class PlacementCoordinator
    {
        private readonly List<IPlacementStrategy> _strategies;
        private PlacementState _state;
        private PlacementVisuals _visuals; 

        public PlacementCoordinator(
            DragManager dragManager,
            IMap map,
            INodeMap nodeMap,
            NeighbourManager neighbourManager,
            PlacementState state,
            PlacementVisuals visuals)
        {
            _strategies = new List<IPlacementStrategy>
            {
                new DraggablePlacementStrategy(dragManager, state, visuals),
                new StandardPlacementStrategy(map, nodeMap, neighbourManager, state, visuals)
            };

            _state = state; 
            _visuals = visuals;
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
    }
} 