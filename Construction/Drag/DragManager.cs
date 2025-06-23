using System.Collections.Generic;
using System.Linq;
using Construction.Maps;
using Construction.Nodes;
using Construction.Placement;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Construction.Drag
{
    public class DragManager
    {
        private readonly IMap _map;
        private readonly PlacementVisuals _visuals;
        private readonly INodeMap _nodeMap;
        private readonly NeighbourManager _neighbourManager;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;

        private readonly Dictionary<Vector3Int, TempNode> _spawned = new();
        private readonly DragSession _dragSession;

        public DragManager(
            PlacementSettings settings,
            IMap map,
            PlacementVisuals visuals,
            INodeMap nodeMap,
            NeighbourManager neighbourManager,
            UnityEngine.Camera mainCamera,
            GameObject floorDecal,
            PlacementState state)
        {
            _map = map;
            _visuals = visuals;
            _nodeMap = nodeMap;
            _neighbourManager = neighbourManager;
            _floorDecal = floorDecal;
            _state = state;
            
            _dragSession = new DragSessionBuilder()
                .WithSettings(settings)
                .WithMap(_map)
                .WithVisuals(_visuals)
                .WithCamera(mainCamera)
                .WithFloorDecal(_floorDecal)
                .WithState(_state)
                .Build();
        }

        public void Disable()
        {
            _dragSession.Disable();
        }
        
        public async UniTaskVoid HandleDrag(GameObject initialObject, Node startingNode, Vector3Int start)
        {
            InitialiseDrag(initialObject, startingNode, start);
            await _dragSession.RunDrag(initialObject, startingNode, start, _spawned); 
            FinaliseDrag(start);
        }
        
        private void InitialiseDrag(GameObject initialObject, Node startingNode, Vector3Int start)
        {
            _spawned.Clear();
            _spawned.Add(start, new TempNode(initialObject, startingNode));
            _state.CurrentAxis = Axis.XAxis;
        }
        
        private void FinaliseDrag(Vector3Int start)
        {
            // Register the start node of the drag
            if (_spawned.TryGetValue(start, out TempNode value))
            {
                RegisterDraggedOccupant(start, value.Node, DragPos.Start, true);
            }
            
            List<Vector3Int> cellPositions = _spawned.Keys.ToList();
            Vector3Int end = VectorUtils.FurthestFrom(cellPositions, start); 
            cellPositions.Remove(start);
            cellPositions.Remove(end);

            // Register the middle nodes of the drag
            foreach (var vector in cellPositions)
            {
                RegisterDraggedOccupant(vector, _spawned[vector].Node, DragPos.Middle);
            }
            
            // Register the end node of the drag -  must come last
            if (_spawned.TryGetValue(end, out TempNode endValue))
            {
                RegisterDraggedOccupant(end, endValue.Node, DragPos.End, true);
            }
            
            CleanupDrag();
        }

        private void RegisterDraggedOccupant(Vector3Int position, Node node, DragPos dragPos , bool manageNeighbours = false)
        {
            Vector2Int size = node.GetSize();
            if (!_map.RegisterOccupant(position.x, position.z, size.x, size.y)) node.FailedPlacement();
            else
            {
                node.Place(position, _nodeMap);
                node.Visuals.HideArrows();
                node.Initialise(_nodeMap, NodeType.Straight, node.Direction, false);

                if (!manageNeighbours) return;
                if (_neighbourManager.UpdateToCorner(node, position, dragPos))
                    _nodeMap.DeregisterNode(node);
            }
        }
        
        private void CleanupDrag()
        {
            _spawned.Clear();
            _floorDecal.SetActive(false);
            _visuals.Hide();
        }

        public void DespawnAll()
        {
            if (_spawned.Count > 1)
            {
                List<TempNode> spawned = _spawned.Values.ToList();
                foreach (TempNode tn in spawned)
                {
                    SimplePool.Despawn(tn.Prefab);
                }
            }
            else if (_state.CurrentObject != null)
            {
                SimplePool.Despawn(_state.CurrentObject);
            }
        }
    }
} 