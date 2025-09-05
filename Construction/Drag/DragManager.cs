using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Events;
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
        private readonly PlacementState _state;
        private readonly PlacementSettings _settings;

        private readonly Dictionary<Cell, TempNode> _spawned = new();
        private readonly DragSession _dragSession;

        public DragManager(PlacementContext ctx)
        {
            _map = ctx.Map;
            _visuals = ctx.Visuals;
            _nodeMap = ctx.NodeMap;
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
            HashSet<Node> newNodes = new();
            
            // Register the start node of the drag
            if (_spawned.TryGetValue(startingCell, out TempNode value))
            {
                InitialiseNode(startingCell.GridCoordinate, startingCell, value.Node, newNodes);
            }
            
            List<Cell> allCells = _spawned.Keys.ToList();
            List<Vector3Int> gridPositions = allCells
                                                .AsValueEnumerable()
                                                .Select(pair => pair.GridCoordinate)
                                                .ToList();
            
            Vector3Int endGridCoord = VectorUtils.FurthestFrom(gridPositions, startingCell.GridCoordinate);
            Cell endCell = allCells.First(cell => cell.GridCoordinate == endGridCoord);
            
            List<Cell> orderedMiddleCells = allCells
                .Where(pair => pair != startingCell && pair != endCell)
                .ToList();
            
            // Register the middle nodes of the drag in correct order
            foreach (Cell cell in orderedMiddleCells)
            {
                InitialiseNode(cell.GridCoordinate, cell, _spawned[cell].Node, newNodes);
            }
            
            // Register the end node of the drag -  must come last
            if (_spawned.TryGetValue(endCell, out TempNode endValue))
            {
                if(startingCell.GridCoordinate != endCell.GridCoordinate) 
                    InitialiseNode(endCell.GridCoordinate, endCell, endValue.Node, newNodes);
            }
            
            RegisterNodes(newNodes);
            CleanupDrag();
        }
        
        private void InitialiseNode(Vector3Int gridCoord, Cell cell, Node node, HashSet<Node> newNodes)
        {
            Vector2Int size = node.GetSize();
            if (!_map.RegisterOccupant(gridCoord.x, gridCoord.z, size.x, size.y)) 
                node.FailedPlacement(gridCoord);
            else
            {
                node.Place(gridCoord, _nodeMap);
                node.Visuals.HideArrows();
                NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, cell.NodeType); 
                node.Initialise(config);
                newNodes.Add(node);
            }
        }

        // ALl the nodes in the path need to be added to the Node Map (via node.Initialise()). 
        // Before raising the event that flags that a new node has been placed. 
        private void RegisterNodes(HashSet<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                EventBus<NodePlaced>.Raise(new NodePlaced(node));
            }
        }
        
        private void CleanupDrag()
        {
            _spawned.Clear();
            _visuals.HidePlacementVisuals();
        }

        public void DespawnAll()
        {
            if (_spawned.Count > 1)
            {
                foreach (KeyValuePair<Cell, TempNode> pair in _spawned)
                {
                    Node node = pair.Value.Node;
                    Vector3Int gridCoord = pair.Key.GridCoordinate; 
                    Cleanup.RemoveNode(node, gridCoord, _map);
                }
            }
            else if (_state.CurrentObject != null)
            {
                Cleanup.RemovePlaceable(_state, _map);
            }
        }
    }
} 