using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Construction.Maps;
using Construction.Nodes;
using Construction.Placement;
using Construction.Utilities;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities;
using ZLinq;

namespace Construction.Drag
{
    public class DragSession
    {
        private readonly PlacementSettings _settings;
        private readonly IMap _map;
        private readonly PlacementVisuals _visuals;
        private readonly UnityEngine.Camera _mainCamera;
        private readonly GameObject _floorDecal;
        private readonly PlacementState _state;

        private CellSelection _cellSelection; 
        private List<Vector3Int> _selectedCells = new();
        private List<Vector3Int> _newHits = new();
        private List<Vector3Int> _oldHits = new();
        private RaycastHit[] _cellHits = new RaycastHit[1];
        private Vector3Int _cornerCell = new Vector3Int(-1, -1, -1);
        private Corner _corner = Corner.None;
        
        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        
        public DragSession(
            PlacementSettings settings,
            IMap map,
            PlacementVisuals visuals,
            UnityEngine.Camera mainCamera,
            GameObject floorDecal,
            PlacementState state)
        {
            _settings = settings;
            _map = map;
            _visuals = visuals;
            _mainCamera = mainCamera;
            _floorDecal = floorDecal;
            _state = state;
        }
        
        public void Disable()
        {
            _disableCancellation.Cancel();
            _disableCancellation.Dispose();
        }
        
        public async UniTask RunDrag(GameObject initialObject, Node startingNode, Vector3Int start, Dictionary<Vector3Int, TempNode> spawned)
        {
            Direction direction = _state.CurrentDirection; 

            await UniTask.WaitForSeconds(_settings.inputSettings.blockDirectionInputDuration, cancellationToken: _disableCancellation.Token);

            while (_settings.place.action.IsPressed())
            {
                _cellSelection = SelectCells(start);
                _selectedCells = _cellSelection.GetCells(_settings); 

                if (_state.CurrentDirection != direction)
                {
                    direction = _state.CurrentDirection; 
                    startingNode.Rotate(direction, false);
                }
                
                UpdateHits(spawned);

                if (_newHits.Any() || _oldHits.Any())
                {
                    ProcessNewHits(initialObject, spawned);
                    SetCorner(initialObject, spawned);
                    RemoveOldHits(spawned);
                    UpdateRotations(spawned);
                    UpdateFloorDecalPosition();
                }
                
                await UniTask.Yield(_disableCancellation.Token);
            }
            
            _cellSelection.Clear();
        }

        private CellSelection SelectCells(Vector3Int start)
        {
            CellSelection selection = Utilities.Grid.SelectCells(start, _mainCamera, _settings.floorLayer, _cellHits, _map, _settings);
            _state.CurrentAxis = selection.Axis;
            _state.CurrentDirection = selection.Direction;
            return selection;
        }

        private void UpdateHits(Dictionary<Vector3Int, TempNode> spawned)
        {
            _newHits = _selectedCells
                .AsValueEnumerable()
                .Except(spawned.Keys)
                .ToList(); 
            
            _oldHits = spawned.Keys
                .AsValueEnumerable()
                .Except(_selectedCells)
                .ToList();
        }
         
        private void ProcessNewHits(GameObject initialObject, Dictionary<Vector3Int, TempNode> spawned)
        {
            foreach (Vector3Int hit in _newHits)
            {
                PlacePrefab(initialObject, _settings.standardBeltPrefab, spawned, hit);
            }
        }

        private void PlacePrefab(GameObject initialObject, GameObject prefab, Dictionary<Vector3Int, TempNode> spawned, Vector3Int hit)
        {
            GameObject newGameObject = SimplePool.Spawn(prefab, hit, Quaternion.identity, initialObject.transform.parent);
            newGameObject.name = $"{prefab.name}_{hit.x}_{hit.z}";

            if (newGameObject.TryGetComponent(out Node node))
            {
                _visuals.Place(newGameObject);
                spawned.Add(hit, new TempNode(newGameObject, node));
                node.Visuals.ShowArrows();
            }
        }

        private void UpdateRotations(Dictionary<Vector3Int, TempNode> spawned)
        {
            foreach (KeyValuePair<Vector3Int,TempNode> pair in spawned)
            {
                pair.Value.Node.Rotate(_settings.useLShapedPaths ? _cellSelection.DirectionFromHit(pair.Key, _state.CurrentDirection) : _state.CurrentDirection, false);
            }
        }

        private void RemoveOldHits(Dictionary<Vector3Int, TempNode> spawned)
        {
            foreach (Vector3Int hit in _oldHits)
            {
                if (!spawned.TryGetValue(hit, out TempNode temp)) continue;
                RemovePrefab(spawned, temp.Prefab, hit);
            }
        }

        private static void RemovePrefab(Dictionary<Vector3Int, TempNode> spawned, GameObject go, Vector3Int position)
        {
            SimplePool.Despawn(go);
            spawned.Remove(position);
        }

        private void SetCorner(GameObject initialObject, Dictionary<Vector3Int, TempNode> spawned)
        {
            if(_cellSelection.Corner == Corner.None) return;
            if(_cellSelection.CornerCell == _cornerCell && _cellSelection.Corner == _corner) return;
            _corner = _cellSelection.Corner;
            
            // Reset the old corner cell
            if (!_oldHits.Contains(_cornerCell) && spawned.TryGetValue(_cornerCell, out TempNode value))
            {
                RemovePrefab(spawned, value.Prefab, _cornerCell);
                PlacePrefab(initialObject, _settings.standardBeltPrefab, spawned, _cornerCell);
            }
            
            // Set the new corner cell
            _cornerCell = _cellSelection.CornerCell;
            if(spawned.TryGetValue(_cornerCell, out TempNode value2))
                RemovePrefab(spawned, value2.Prefab, _cornerCell);
            
            GameObject newPrefab = _cellSelection.Corner == Corner.Right ? _settings.rightBeltPrefab : _settings.leftBeltPrefab;
            PlacePrefab(initialObject, newPrefab, spawned, _cornerCell);
        }
        
        private void UpdateFloorDecalPosition()
        {
            if (!TryGetFloorPosition(out Vector3 position)) return;
            _state.TargetPosition = GetGridPosition(position);
            _floorDecal.transform.position = _state.TargetPosition;
        }
        
        private Direction DragDirection(Vector3Int start)
        {
            if (!TryGetFloorPosition(out Vector3 mousePosition)) return Direction.North;
            Vector3Int currentPos = GetGridPosition(mousePosition);
    
            // Calculate drag vector
            Vector3 dragVector = currentPos - start;
    
            // Determine primary axis and direction
            if (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.z))
            {
                return dragVector.x > 0 ? Direction.East : Direction.West;
            }

            return dragVector.z > 0 ? Direction.North : Direction.South;
        }

        private Vector3Int GetGridPosition(Vector3 position)
        {
            return Utilities.Grid.Position(position, _settings.gridOrigin, _map.MapWidth, _map.MapHeight, _settings.tileSize);
        }

        private bool TryGetFloorPosition(out Vector3 position) 
        {
            bool positionFound = FloorPosition.Get(_mainCamera, _settings.raycastDistance, _settings.floorLayer, out Vector3 foundPosition);
            position = foundPosition;
            return positionFound;
        }
    }
}