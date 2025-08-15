using System.Collections.Generic;
using System.Linq;
using Construction.Maps;
using Construction.Nodes;
using Construction.Placement;
using Construction.Utilities;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using GameState;
using UnityEngine;
using Utilities;
using ZLinq;

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
        private readonly PlacementSettings _settings;

        private readonly Dictionary<Cell, TempNode> _spawned = new();
        private readonly DragSession _dragSession;

        public DragManager(
            PlacementSettings settings,
            InputSettings inputSettings,
            IMap map,
            PlacementVisuals visuals,
            INodeMap nodeMap,
            NeighbourManager neighbourManager,
            Camera mainCamera,
            GameObject floorDecal,
            PlacementState state)
        {
            _map = map;
            _visuals = visuals;
            _nodeMap = nodeMap;
            _neighbourManager = neighbourManager;
            _floorDecal = floorDecal;
            _state = state;
            _settings = settings;
            
            _dragSession = new DragSessionBuilder()
                .WithSettings(settings)
                .WithInputSettings(inputSettings)
                .WithMap(_map)
                .WithNodeMap(nodeMap)
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
        
        public async UniTaskVoid HandleDrag(GameObject initialObject, Node startingNode, Vector3Int startingGridCoord)
        {
            InitialiseDrag(initialObject, startingNode, startingGridCoord, out Cell startingCell);
            await _dragSession.RunDrag(initialObject, startingNode, startingGridCoord, _spawned); 
            FinaliseDrag(startingCell);
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
                RegisterDraggedOccupant(startingCell.GridCoordinate, value.Node, DragPos.Start, true);
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
                RegisterDraggedOccupant(cell.GridCoordinate, _spawned[cell].Node, DragPos.Middle);
            }
            
            // Register the end node of the drag -  must come last
            if (_spawned.TryGetValue(endCell, out TempNode endValue))
            {
                if(startingCell.GridCoordinate != endCell.GridCoordinate) 
                    RegisterDraggedOccupant(endCell.GridCoordinate, endValue.Node, DragPos.End);
            }
            
            CleanupDrag();
        }

        private void RegisterDraggedOccupant(Vector3Int gridCoord, Node node, DragPos dragPos , bool manageNeighbours = false)
        {
            Vector2Int size = node.GetSize();
            if (!_map.RegisterOccupant(gridCoord.x, gridCoord.z, size.x, size.y)) node.FailedPlacement();
            else
            {
                node.Place(gridCoord, _nodeMap);
                node.Visuals.HideArrows();

                NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, NodeType.Straight); 
                node.Initialise(config);

                if (!manageNeighbours) return;
                if (_neighbourManager.UpdateToCorner(node, gridCoord, dragPos))
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