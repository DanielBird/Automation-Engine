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
        private readonly PlacementVisuals _visuals;
        private readonly Camera _mainCamera;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;

        private CellSelection _cellSelection = new(); 
        private HashSet<GridWorldCoordPair> _selectedPos = new();
        private List<GridWorldCoordPair> _newPos = new();
        private List<GridWorldCoordPair> _oldPos = new();
        private RaycastHit[] _cellHits = new RaycastHit[1];
        private GridWorldCoordPair _cornerCell = new (Vector3Int.zero, Vector3Int.zero);
        private Corner _corner = Corner.None;
        
        private StringBuilder _nameBuilder = new(64);
        
        private CancellationTokenSource _disableCancellation = new();
        
        public DragSession(
            PlacementSettings settings,
            InputSettings inputSettings,
            IMap map,
            PlacementVisuals visuals,
            Camera mainCamera,
            GameObject floorDecal,
            PlacementState state)
        {
            _settings = settings;
            _inputSettings = inputSettings;
            _map = map;
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
        ///
        /// SpawnedPos is a dictionary of Grid World Coord Pairs and Temporary Nodes
        /// </summary>
        public async UniTask RunDrag(GameObject initialObject, Node startingNode, Vector3Int startGridCoord, Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            var direction = _state.CurrentDirection;
            int stepSize = startingNode.GridWidth;

            _disableCancellation = new CancellationTokenSource();
            await UniTask.WaitForSeconds(_inputSettings.waitForInputTime, cancellationToken: _disableCancellation.Token);
            
            while (_settings.place.action.IsPressed())
            {
                _cellSelection = SelectCells(startGridCoord, stepSize);
                _selectedPos = _cellSelection.GetGridWorldPairs(_settings); 

                if (_state.CurrentDirection != direction)
                {
                    direction = _state.CurrentDirection; 
                    startingNode.Rotate(direction, false);
                }
                
                UpdateHits(spawnedPos);

                if (_newPos.Any() || _oldPos.Any())
                {
                    ProcessNewHits(initialObject, spawnedPos);
                    SetCorner(initialObject, spawnedPos);
                    RemoveOldHits(spawnedPos);
                    UpdateRotations(spawnedPos);
                    UpdateFloorDecalPosition();
                }
                
                await UniTask.Yield(_disableCancellation.Token);
            }
            
            _cellSelection.Clear();
            CtsCtrl.Clear(ref _disableCancellation);
        }
        
        /// <summary>
        /// Generates a CellSelection which contains a list of Vector3Ints
        /// The hit positions are grid coordinates, NOT world positions. 
        /// </summary>
        private CellSelection SelectCells(Vector3Int startGridCoord, int stepSize)
        {
            CellSelection selection = Grid.SelectCells(startGridCoord, _mainCamera, _settings.floorLayer, _cellHits, _map, _settings, stepSize);
            _state.SetAxis(selection.Axis);
            _state.SetDirection(selection.Direction);
            return selection;
        }
        
        private void UpdateHits(Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            _newPos = _selectedPos
                .AsValueEnumerable()
                .Except(spawnedPos.Keys)
                .ToList(); 
            
            _oldPos = spawnedPos.Keys
                .AsValueEnumerable()
                .Except(_selectedPos)
                .ToList();
        }
         
        private void ProcessNewHits(GameObject initialObject, Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            foreach (GridWorldCoordPair pair in _newPos)
            {
                PlacePrefab(initialObject, _settings.standardBeltPrefab, spawnedPos, pair);
            }
        }

        private void PlacePrefab(GameObject initialObject, GameObject prefab, Dictionary<GridWorldCoordPair, TempNode> spawnedPos, GridWorldCoordPair pair)
        {
            GameObject newGameObject = SimplePool.Spawn(prefab, pair.WorldPosition, Quaternion.identity, initialObject.transform.parent);
            SetGameObjectName(newGameObject, prefab, pair.GridCoord);
            
            if (newGameObject.TryGetComponent(out Node node))
            {
                _visuals.Place(newGameObject);
                spawnedPos.Add(pair, new TempNode(newGameObject, node));
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

        private void UpdateRotations(Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            var direction = _state.CurrentDirection;
            
            if (_settings.useLShapedPaths)
            {
                foreach ((GridWorldCoordPair pair, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(_cellSelection.DirectionFromHit(pair.GridCoord, direction), false);
                }
            }
            else
            {
                foreach ((GridWorldCoordPair pair, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(direction, false);
                }
            }
        }

        private void RemoveOldHits(Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            foreach (GridWorldCoordPair pair in _oldPos)
            {
                if (!spawnedPos.TryGetValue(pair, out TempNode temp)) continue;
                RemovePrefab(spawnedPos, temp.Prefab, pair);
            }
        }

        private static void RemovePrefab(Dictionary<GridWorldCoordPair, TempNode> spawnedPos, GameObject go, GridWorldCoordPair pair)
        {
            SimplePool.Despawn(go);
            spawnedPos.Remove(pair);
        }

        private void SetCorner(GameObject initialObject, Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            if(_cellSelection.CornerGridCoord == _cornerCell.GridCoord && _cellSelection.Corner == _corner) return;
            
            ResetOldCorners(initialObject, spawnedPos);
            
            if(_cellSelection.Corner == Corner.None) return;
            
            _corner = _cellSelection.Corner;
            
            // Set the new corner cell
            Vector3Int cornerGridCoord = _cellSelection.CornerGridCoord;
            Vector3Int cornerWorldPos = Grid.GridToWorldPosition(cornerGridCoord, _settings.gridOrigin, _settings.tileSize);
            
            _cornerCell = new GridWorldCoordPair(cornerGridCoord, cornerWorldPos);
            if(spawnedPos.TryGetValue(_cornerCell, out TempNode value2))
                RemovePrefab(spawnedPos, value2.Prefab, _cornerCell);
            
            GameObject newPrefab = _cellSelection.Corner == Corner.Right ? _settings.rightBeltPrefab : _settings.leftBeltPrefab;
            PlacePrefab(initialObject, newPrefab, spawnedPos, _cornerCell);
        }

        private void ResetOldCorners(GameObject initialObject, Dictionary<GridWorldCoordPair, TempNode> spawnedPos)
        {
            // Reset the old corner cell
            if (!_oldPos.Contains(_cornerCell) && spawnedPos.TryGetValue(_cornerCell, out TempNode value))
            {
                RemovePrefab(spawnedPos, value.Prefab, _cornerCell);
                PlacePrefab(initialObject, _settings.standardBeltPrefab, spawnedPos, _cornerCell);
            }
        }

        private void UpdateFloorDecalPosition()
        {
            if (!TryGetFloorPosition(out Vector3 position)) return;
            _state.TargetGridCoordinate = GridAlignedWorldPosition(position);
            _floorDecal.transform.position = _state.TargetGridCoordinate;
        }
        
        private Vector3Int GridAlignedWorldPosition(Vector3 position)
        {
            return Grid.GridAlignedWorldPosition(position, _settings.gridOrigin, _map.MapWidth, _map.MapHeight, _settings.tileSize);
        }

        private Vector3Int GetGridPosition(Vector3Int position)
        {
            return Grid.WorldToGridCoordinate(position, _settings.gridOrigin, _map.MapWidth, _map.MapHeight, _settings.tileSize);
        }
        
        private bool TryGetFloorPosition(out Vector3 position) 
        {
            bool positionFound = FloorPosition.Get(_mainCamera, _settings.raycastDistance, _settings, out Vector3 foundPosition);
            position = foundPosition;
            return positionFound;
        }
    }
}