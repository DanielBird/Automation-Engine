using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using Engine.Construction.Visuals;
using Engine.GameState;
using Engine.Utilities;
using UnityEngine;
using ZLinq;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Drag
{
    public class DragSession
    {
        private readonly PlacementSettings _settings;
        private readonly InputSettings _inputSettings;
        private readonly IWorld _world;
        private readonly PlacementVisuals _visuals;
        private readonly Camera _mainCamera;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;

        private CellSelection _cellSelection = new(); 
        private CellSelectionParams _cellSelectionParams;
        private HashSet<Cell> _selectedCells = new();
        private List<Cell> _newCells = new();
        private List<Cell> _oldCells = new();
        private Dictionary<Vector3Int, Node> _replacedByIntersections = new();
        private Dictionary<Vector3Int, Node> _restoreAfterIntersection = new();
        
        private RaycastHit[] _cellHits = new RaycastHit[1];
        private Cell _cornerCell;
        private Vector3Int _currentGridCoord;
        
        private CancellationTokenSource _cts = new();
        
        public DragSession(PlacementContext ctx)
        {
            _settings = ctx.PlacementSettings;
            _inputSettings = ctx.InputSettings;
            _world = ctx.World;
            _visuals = ctx.Visuals;
            _mainCamera = ctx.MainCamera;
            _floorDecal = ctx.Visuals.PlacementHighlight;
            _state = ctx.State;
        }
        
        public void Disable()
        {
            CtsCtrl.Clear(ref _cts);
        }
        
        /// <summary>
        /// Handles the drag operation for placing objects on the grid
        /// Does not support placement of rectangular game objects - square only!
        /// </summary>
        public async UniTask RunDrag(GameObject initialObject, Node startingNode, Vector3Int startGridCoord, Dictionary<Cell, TempNode> spawnedCells)
        {
            Direction direction = _state.CurrentDirection;
            int stepSize = startingNode.GridWidth;
            int pathId = StartingPath(startingNode, startGridCoord);

            _currentGridCoord = startGridCoord; 

            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            while (_inputSettings.place.action.IsPressed())
            {
                if (SameGridPos())
                {
                    await UniTask.Yield(_cts.Token);
                    continue;
                }
                
                _cellSelection = SelectCells(startGridCoord, stepSize, pathId);
                _selectedCells = _cellSelection.HitCells; 

                if (_state.CurrentDirection != direction)
                {
                    direction = _state.CurrentDirection; 
                    startingNode.Rotate(direction);
                }
                
                UpdateHits(spawnedCells);

                if (_newCells.Any() || _oldCells.Any())
                {
                    ManageIntersections();
                    ProcessNewHits(initialObject, spawnedCells);
                    RemoveOldHits(spawnedCells);
                    RestoreAfterIntersection();
                    UpdateRotations(spawnedCells);
                    UpdateFloorDecalPosition();
                }
                
                await UniTask.Yield(_cts.Token);
            }
            
            _cellSelection.Clear();
            CleanUpAfterIntersections();
            CtsCtrl.Clear(ref _cts);
        }

        /// <summary>
        /// Checks whether grid coord hit by the mouse has changed 
        /// </summary>
        private bool SameGridPos()
        {
            if (!CellSelector.TryGetCurrentGridCoord(_mainCamera, _settings.floorLayer, _cellHits, _world, _settings, out Vector3Int gridCoord))
                return true; 
            
            if (gridCoord == _currentGridCoord)
                return true;
            
            _currentGridCoord = gridCoord;
            return false;
        }

        private int StartingPath(Node startingNode, Vector3Int startGridCoord)
        {
            // If the player clicks an existing node to start a drag,
            // The Placement State is updated with that node's pathId 
            if(_state.PathId != -1)
                return _state.PathId;

            // The starting node's grid coord has not yet been set.
            // So we recreate node.TryGetBackwardNode here. 
            int width = startingNode.nodeTypeSo.width;
            int x = startGridCoord.x; 
            int z = startGridCoord.z;
            Direction inputDirection = startingNode.InputDirection();

            Vector2Int backwardPosition = inputDirection switch
            {
                Direction.North => new Vector2Int(x, z + width),
                Direction.East => new Vector2Int(x + width, z),
                Direction.South => new Vector2Int(x, z - width),
                Direction.West => new Vector2Int(x - width, z),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (_world.GetNeighbourAt(backwardPosition, out Node backwardNode))
            {
                _state.SetPathId(backwardNode.PathId);
                return _state.PathId;
            }
            
            return -1;
        }
        
        /// <summary>
        /// Generates a CellSelection which holds a HashSet of Cells hit during the current drag
        /// </summary>
        private CellSelection SelectCells(Vector3Int startGridCoord, int stepSize, int pathId)
        {
            _cellSelectionParams = new CellSelectionParams(_world, _settings, stepSize, pathId); 
            CellSelection selection = CellSelector.SelectCells(startGridCoord, _mainCamera, _settings.floorLayer, _cellHits, _cellSelectionParams);
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

        private void ManageIntersections()
        {
            HashSet<Vector3Int> newIntersections = _cellSelectionParams.Intersections
                                                        .AsValueEnumerable()
                                                        .Except(_replacedByIntersections.Keys)
                                                        .ToHashSet();
            
            Dictionary<Vector3Int, Node> oldIntersections = _replacedByIntersections
                                                                .AsValueEnumerable()
                                                                .Where(kvp => !_cellSelectionParams.Intersections.Contains(kvp.Key))
                                                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            foreach (Vector3Int v in newIntersections)
            {
                if (!_world.TryGetNode(v.x, v.z, out Node node)) continue;
                _replacedByIntersections.Add(v, node);
                node.Visuals.DisableRenderer();
            }

            foreach (var kvp in oldIntersections)
            {
                Node node = kvp.Value;
                node.Visuals.EnableRenderer();
                _replacedByIntersections.Remove(kvp.Key);
                _restoreAfterIntersection.Add(kvp.Key, node);
            }
            
            _cellSelectionParams.ClearIntersections();
        }

        private void CleanUpAfterIntersections()
        {
            foreach (KeyValuePair<Vector3Int, Node> kvp in _replacedByIntersections)
            {
                Vector3Int pos = kvp.Key;
                Node node = kvp.Value;
                RemoveNode(node, pos);
            }
            
            _replacedByIntersections.Clear();
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
            if(!_settings.prefabRegistry.FoundPrefab(cell.NodeType, out GameObject prefab))
            {
                Debug.LogError($"Prefab \"{cell.NodeType}\" could not be found.");
                return;
            }
            
            GameObject newGameObject = SimplePool.Spawn(prefab, cell.WorldPosition, Quaternion.identity, initialObject.transform.parent);
            
            if (newGameObject.TryGetComponent(out Node node))
            {
                _visuals.Place(newGameObject);
                spawnedCells.Add(cell, new TempNode(newGameObject, node));
                node.Visuals.ShowArrows();
            }
        }
        
        private void UpdateRotations(Dictionary<Cell, TempNode> spawnedPos)
        {
            var direction = _state.CurrentDirection;
            
            if (_settings.cellSelectionAlgorithm != CellSelectionAlgorithm.StraightLinesOnly)
            {
                foreach ((Cell cell, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(_cellSelection.DirectionFromHit(cell.GridCoordinate, direction));
                }
            }
            else
            {
                foreach ((Cell cell, TempNode tempNode) in spawnedPos)
                {
                    tempNode.Node.RotateInstant(direction);
                }
            }
        }

        // Disables and deregisters nodes that have been spawned during this drag operation
        private void RemoveOldHits(Dictionary<Cell, TempNode> spawnedCells)
        {
            foreach (Cell c in _oldCells)
            {
                if (!spawnedCells.TryGetValue(c, out TempNode temp)) continue;
                
                RemoveNode(temp.Node, c.GridCoordinate); 
                spawnedCells.Remove(c);
            }
        }

        // Node spawned during a drag session should not have been registered with Map or NodeMap
        private void RemoveNode(Node node, Vector3Int gridCoord) 
            => Cleanup.RemoveNode(node, gridCoord, _world, false);

        // After removing an old hit that is an intersection... 
        // The Map for that cell will be set to vacant. 
        // We need to restore map occupancy. 
        private void RestoreAfterIntersection()
        {
            foreach (KeyValuePair<Vector3Int, Node> pair in _restoreAfterIntersection)
            {
                Vector3Int pos = pair.Key;
                _world.TryPlaceNodeAt(pair.Value, pos.x, pos.z);
            }
            
            _restoreAfterIntersection.Clear();
        }
        
        private void UpdateFloorDecalPosition()
        {
            if (!TryGetFloorPosition(out Vector3 position)) return;
            
            _state.TargetGridCoordinate = GridAlignedWorldPosition(position);
            _floorDecal.transform.position = _state.TargetGridCoordinate;
        }
        
        private Vector3Int GridAlignedWorldPosition(Vector3 position)
        {
            return Grid.GridAlignedWorldPosition(position, new GridParams(_settings.mapOrigin, _world.MapWidth(), _world.MapHeight(), _settings.cellSize));
        }

        private Vector3Int GetGridPosition(Vector3Int position)
        {
            return Grid.WorldToGridCoordinate(position, new GridParams(_settings.mapOrigin,  _world.MapWidth(), _world.MapHeight(), _settings.cellSize));
        }
        
        private bool TryGetFloorPosition(out Vector3 position) 
        {
            bool positionFound = FloorPosition.Get(_mainCamera, _settings.raycastDistance, _settings, out Vector3 foundPosition);
            position = foundPosition;
            return positionFound;
        }
    }
}