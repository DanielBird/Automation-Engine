using Construction.Drag;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Nodes;
using Construction.Visuals;
using UnityEngine;
using Utilities;

namespace Construction.Placement
{
    public class StandardPlacementStrategy : IPlacementStrategy
    {
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly NeighbourManager _neighbourManager;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;

        public StandardPlacementStrategy(
            IMap map,
            INodeMap nodeMap,
            NeighbourManager neighbourManager,
            PlacementState state,
            PlacementVisuals visuals)
        {
            _map = map;
            _nodeMap = nodeMap;
            _neighbourManager = neighbourManager;
            _state = state;
            _visuals = visuals;
        }

        public bool CanHandle(IPlaceable placeable) => !placeable.Draggable;

        public void HandlePlacement(IPlaceable placeable, Vector3Int position)
        {
            if (!Place(position, placeable)) return;

            // If all Nodes are Draggable, then no placeable that gets here will be a Node
            if (placeable is Node node)
            {
                node.Initialise(_nodeMap, NodeType.Straight, Direction.North, false);
                node.Visuals.HideArrows();
            }

            _state.IsRunning = false;
            _neighbourManager.UpdateToCorner(placeable, position, DragPos.Start);
            _visuals.Hide();
        }

        public void CancelPlacement(IPlaceable placeable)
        {
            SimplePool.Despawn(_state.CurrentObject);
            _visuals.Hide();
        }

        private bool Place(Vector3Int position, IPlaceable placeable)
        {
            Vector2Int size = placeable.GetSize();
            if (!_map.RegisterOccupant(position.x, position.z, size.x, size.y))
            {
                placeable.FailedPlacement();
                return false;
            }
            placeable.Place(position, _nodeMap);
            return true;
        }
    }
} 