using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Grid = Construction.Utilities.Grid;

namespace Construction.Drag
{
    public class DragSession
    {
        private readonly PlacementSettings _settings;
        private readonly InputSettings _inputSettings;
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly PlacementVisuals _visuals;
        private readonly Camera _mainCamera;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;

        private CellSelection _cellSelection = new(); 
        private HashSet<Cell> _selectedCells = new();
        private List<Cell> _newCells = new();
        private List<Cell> _oldCells = new();
        
        private RaycastHit[] _cellHits = new RaycastHit[1];
        private Cell _cornerCell;
        private Corner _corner = Corner.None;
        
        private StringBuilder _nameBuilder = new(64);
        
        private CancellationTokenSource _disableCancellation = new();
        
        public DragSession(
            PlacementSettings settings,
            InputSettings inputSettings,
            IMap map,
            INodeMap nodeMap,
            PlacementVisuals visuals,
            Camera mainCamera,
            GameObject floorDecal,
            PlacementState state)
        {
            _settings = settings;
            _inputSettings = inputSettings;
            _map = map;
            _nodeMap = nodeMap;
            _visuals = visuals;
            _mainCamera = mainCamera;
            _floorDecal = floorDecal;
            _state = state;
        }
        
        public void Disable()
        {
            CtsCtrl.Clear(ref _disableCancellation);
        }
        
        /// <summary>
        /// Handles the drag operation for placing objects in the grid
        /// Does not support placement of rectangular game objects
        /// SpawnedPos is a dictionary of Cells and Temporary Nodes
        /// </summary>
        public async UniTask RunDrag(GameObject initialObject, Node startingNode, Vector3Int startGridCoord, Dictionary<Cell, TempNode> spawnedCells)
        {
            var direction = _state.CurrentDirection;
            int stepSize = startingNode.GridWidth;

            _disableCancellation = new CancellationTokenSource();
            await UniTask.WaitForSeconds(_inputSettings.waitForInputTime, cancellationToken: _disableCancellation.Token);
            
            while (_settings.place.action.IsPressed())
            {
                _cellSelection = SelectCells(startGridCoord, stepSize);
                _selectedCells = _cellSelection.HitCells; 

                if (_state.CurrentDirection != direction)
                {
                    direction = _state.CurrentDirection; 
                    startingNode.Rotate(direction, false);
                }
                
                UpdateHits(spawnedCells);

                if (_newCells.Any() || _oldCells.Any())
                {
                    ProcessNewHits(initialObject, spawnedCells);
                    RemoveOldHits(spawnedCells);
                    UpdateRotations(spawnedCells);
                    UpdateFloorDecalPosition();
                }
                
                await UniTask.Yield(_disableCancellation.Token);
            }
            
            _cellSelection.Clear();
            CtsCtrl.Clear(ref _disableCancellation);
        }
        
        /// <summary>
        /// Generates a CellSelection which holds a HashSet of Cells hit during the current drag
        /// </summary>
        private CellSelection SelectCells(Vector3Int startGridCoord, int stepSize)
        {
            CellSelectionParams selectionParams = new(_map, _nodeMap, _settings, stepSize); 
            CellSelection selection = Grid.SelectCells(startGridCoord, _mainCamera, _settings.floorLayer, _cellHits, selectionParams);
            _state.SetAxis(selection.Axis);
            _state.SetDirection(selection.Direction);
            return selection;
        }
        
        private void UpdateHits(Dictionary<Cell, TempNode> spawnedPos)
        {
            _newCells = _selectedCells
                .AsValueEnumerable()
                .Except(spawnedPos.Keys)
                .ToList(); 
            
            _oldCells = spawnedPos.Keys
                .AsValueEnumerable()
                .Except(_selectedCells)
                .ToList();
        }
         
        private void ProcessNewHits(GameObject initialObject, Dictionary<Cell, TempNode> spawnedCells)
        {
            foreach (Cell c in _newCells)
            {
                PlacePrefab(initialObject, spawnedCells, c);
            }
        }

        private void PlacePrefab(GameObject initialObject, Dictionary<Cell, TempNode> spawnedCells, Cell cell)
        {
            if(!_settings.prefabRegistry.FoundPrefab(cell.Type, out GameObject prefab))
            {
                Debug.LogError($"Prefab \"{cell.Type}\" could not be found.");
                return;
            }
            
            GameObject newGameObject = SimplePool.Spawn(prefab, cell.WorldPosition, Quaternion.identity, initialObject.transform.parent);
            SetGameObjectName(newGameObject, prefab, cell.GridCoordinate);
            
            if (newGameObject.TryGetComponent(out Node node))
            {
                _visuals.Place(newGameObject);
                spawnedCells.Add(cell, new TempNode(newGameObject, node));
                node.Visuals.ShowArrows();
            }
        }
        
        private void SetGameObjectName(GameObject go, GameObject prefab, Vector3Int coord)
        {
            _nameBuilder.Clear();
            _nameBuilder.Append(prefab.name);
            _nameBuilder.Append('_');
            _nameBuilder.Append(coord.x);
            _nameBuilder.Append('_');
            _nameBuilder.Append(coord.z);
            go.name = _nameBuilder.ToString();
        }

        private void UpdateRotations(Dictionary<Cell, TempNode> spawnedPos)
        {
            var direction = _state.CurrentDirection;
            
            if (_settings.cellSelectionAlgorithm != CellSelectionAlgorithm.StraightLinesOnly)
            {
                foreach ((Cell cell, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(_cellSelection.DirectionFromHit(cell.GridCoordinate, direction), false);
                }
            }
            else
            {
                foreach ((Cell cell, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(direction, false);
                }
            }
        }

        private void RemoveOldHits(Dictionary<Cell, TempNode> spawnedCells)
        {
            foreach (Cell c in _oldCells)
            {
                if (!spawnedCells.TryGetValue(c, out TempNode temp)) continue;
                RemovePrefab(spawnedCells, temp.Prefab, c);
            }
        }

        private static void RemovePrefab(Dictionary<Cell, TempNode> spawnedCells, GameObject go, Cell cell)
        {
            SimplePool.Despawn(go);
            spawnedCells.Remove(cell);
        }
        
        private void UpdateFloorDecalPosition()
        {
            if (!TryGetFloorPosition(out Vector3 position)) return;
            _state.TargetGridCoordinate = GridAlignedWorldPosition(position);
            _floorDecal.transform.position = _state.TargetGridCoordinate;
        }
        
        private Vector3Int GridAlignedWorldPosition(Vector3 position)
        {
            return Grid.GridAlignedWorldPosition(position, new GridParams(_settings.gridOrigin, _map.MapWidth, _map.MapHeight, _settings.tileSize));
        }

        private Vector3Int GetGridPosition(Vector3Int position)
        {
            return Grid.WorldToGridCoordinate(position, new GridParams(_settings.gridOrigin, _map.MapWidth, _map.MapHeight, _settings.tileSize));
        }
        
        private bool TryGetFloorPosition(out Vector3 position) 
        {
            bool positionFound = FloorPosition.Get(_mainCamera, _settings.raycastDistance, _settings, out Vector3 foundPosition);
            position = foundPosition;
            return positionFound;
        }
    }
}