using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Visuals;
using Engine.GameState;
using Engine.Utilities;
using Engine.Utilities.Events;
using Engine.Utilities.Events.Types;
using UnityEngine;
using ZLinq;

namespace Engine.Construction.Drag
{
    public class DragManager
    {
        private readonly IMap _map;
        private readonly PlacementVisuals _visuals;
        private readonly INodeMap _nodeMap;
        private readonly NeighbourManager _neighbourManager;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;
        private readonly PlacementSettings _settings;

        private readonly Dictionary<Cell, TempNode> _spawned = new();
        private readonly DragSession _dragSession;

        public DragManager(PlacementContext ctx, NeighbourManager neighbourManager)
        {
            _map = ctx.Map;
            _visuals = ctx.Visuals;
            _nodeMap = ctx.NodeMap;
            _neighbourManager = neighbourManager;
            _floorDecal = ctx.Visuals.FloorDecal;
            _state = ctx.State;
            _settings = ctx.PlacementSettings;
            _dragSession = new DragSession(ctx); 
        }

        public void Disable()
        {
            _dragSession.Disable();
        }
        
        public async UniTaskVoid HandleDrag(GameObject initialObject, Node startingNode, Vector3Int startingGridCoord)
        {
            EventBus<PlayerDragEvent>.Raise(new PlayerDragEvent(true));
            InitialiseDrag(initialObject, startingNode, startingGridCoord, out Cell startingCell);
            await _dragSession.RunDrag(initialObject, startingNode, startingGridCoord, _spawned); 
            FinaliseDrag(startingCell);
            EventBus<PlayerDragEvent>.Raise(new PlayerDragEvent(false));
        }
        
        private void InitialiseDrag(GameObject initialObject, Node startingNode, Vector3Int startingGridCoord, out Cell startingCell)
        {
            _spawned.Clear();

            startingCell = new Cell(startingGridCoord, _state.CurrentDirection, NodeType.Straight, _settings);
            _spawned.Add(startingCell, new TempNode(initialObject, startingNode));
            _state.SetAxis(Axis.XAxis);
        }
        
        private void FinaliseDrag(Cell startingCell)
        {
            // Register the start node of the drag
            if (_spawned.TryGetValue(startingCell, out TempNode value))
            {
                RegisterDraggedOccupant(startingCell.GridCoordinate, startingCell, value.Node, DragPos.Start, true);
            }
            
            List<Cell> allCells = _spawned.Keys.ToList();
            List<Vector3Int> gridPositions = allCells
                                                .AsValueEnumerable()
                                                .Select(pair => pair.GridCoordinate)
                                                .ToList();
            
            Vector3Int endGridCoord = VectorUtils.FurthestFrom(gridPositions, startingCell.GridCoordinate);
            Cell endCell = allCells.First(cell => cell.GridCoordinate == endGridCoord);
            
            // Get middle pairs (exclude start and end)
            List<Cell> middlePairs = allCells
                .Where(pair => pair != startingCell && pair != endCell)
                .ToList();

            // Register the middle nodes of the drag
            foreach (Cell cell in middlePairs)
            {
                RegisterDraggedOccupant(cell.GridCoordinate, cell, _spawned[cell].Node, DragPos.Middle);
            }
            
            // Register the end node of the drag -  must come last
            if (_spawned.TryGetValue(endCell, out TempNode endValue))
            {
                if(startingCell.GridCoordinate != endCell.GridCoordinate) 
                    RegisterDraggedOccupant(endCell.GridCoordinate, endCell, endValue.Node, DragPos.End);
            }
            
            CleanupDrag();
        }

        private void RegisterDraggedOccupant(Vector3Int gridCoord, Cell cell, Node node, DragPos dragPos , bool manageNeighbours = false)
        {
            Vector2Int size = node.GetSize();
            if (!_map.RegisterOccupant(gridCoord.x, gridCoord.z, size.x, size.y)) node.FailedPlacement(gridCoord);
            else
            {
                node.Place(gridCoord, _nodeMap);
                node.Visuals.HideArrows();

                NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, cell.NodeType); 
                node.Initialise(config);

                /* if (!manageNeighbours) return;
                if (_neighbourManager.UpdateToCorner(node, gridCoord, dragPos))
                    _nodeMap.DeregisterNode(node);*/
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
                    tn.Node.OnRemoval();
                    SimplePool.Despawn(tn.Prefab);
                }
            }
            else if (_state.CurrentObject != null)
            {
                if(_state.CurrentObject.TryGetComponent(out Node node))
                    node.OnRemoval();
                
                SimplePool.Despawn(_state.CurrentObject);
            }
        }
    }
} 