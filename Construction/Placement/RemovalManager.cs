using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Construction.Drag;
using Construction.Events;
using Construction.Nodes;
using Construction.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
using Utilities.Events;
using ZLinq;

namespace Construction.Placement
{
    public class RemovalManager : ConstructionManager
    {
        private RaycastHit[] _cellHits = new RaycastHit[1];
        private CellSelection _cellSelection = new(); 
        
        private Dictionary<GridWorldCoordPair, Node> _registeredNodes = new();
        
        private HashSet<GridWorldCoordPair> _selectedPos = new();
        private HashSet<GridWorldCoordPair> _registeredHits = new();
        private List<GridWorldCoordPair> _newHits = new();
        private List<GridWorldCoordPair> _oldHits = new();
        
        private HashSet<Vector3Int> _pendingDestructions = new();
        private HashSet<Vector3Int> _pendingEmptyDestructions = new();
        
        private CancellationTokenSource _rightClickDragTokenSource;
        
        protected override void Awake()
        {
            base.Awake();
            inputSettings.cancel.action.performed += RemoveNodes;
        }

        private void OnDisable()
        {
            inputSettings.cancel.action.performed -= RemoveNodes; 
            ClearToken();
        }

        private void ClearToken()
        {
            if(_rightClickDragTokenSource == null) return;
            _rightClickDragTokenSource.Cancel();
            _rightClickDragTokenSource.Dispose();
            _rightClickDragTokenSource = null;
        }

        private void RemoveNodes(InputAction.CallbackContext ctx)
        {
            ClearToken();
            _rightClickDragTokenSource = new CancellationTokenSource();
            DetectRightClickDown(_rightClickDragTokenSource.Token);
        }

        private async UniTaskVoid DetectRightClickDown(CancellationToken token)
        {
            await UniTask.WaitForSeconds(inputSettings.waitForInputTime, cancellationToken: token);

            if (!TryGetGridCoordinate(out Vector3Int start))
            {
                Debug.Log("Failed to detect grid coordinate");
                return;
            }

            GridWorldCoordPair startingPair = new (start, WorldAlignedPosition(start));
            
            if (!inputSettings.cancel.action.IsPressed())
            {
                if (!NodeMap.GetNode(start.x, start.z, out Node node)) return;
                RemoveSingleNode(startingPair, node);
                return;
            }

            visuals.Show();
            
            while (inputSettings.cancel.action.IsPressed())
            {
                _cellSelection = SelectCells(start, out Vector3Int endGridCoord);
                _selectedPos = _cellSelection.GetGridWorldPairs(settings); 
                
                Vector3Int endWorldPos = WorldAlignedPosition(endGridCoord);
                visuals.SetFloorDecalPos(endWorldPos);
                
                if (_selectedPos.Any())
                    UpdateHits();
                
                if(_newHits.Any())
                    ProcessNewHits();
                
                if(_oldHits.Any())
                    ProcessOldHits();
                    
                await UniTask.Yield(token);
            }
            
            ProcessFinalHits();
            visuals.Hide();
        }
        
        private void RemoveSingleNode(GridWorldCoordPair delete, Node node)
        {
            Vector3Int gridCoord = delete.GridCoordinate;
            Map.DeregisterOccupant(gridCoord.x, gridCoord.z, node.GridWidth, node.GridHeight);
            NodeMap.DeregisterNode(node);
            SimplePool.Despawn(node.gameObject);
        }
        
        private CellSelection SelectCells(Vector3Int start, out Vector3Int end)
            => Utilities.Grid.SelectCellArea(start, mainCamera, settings.floorLayer, _cellHits, Map, settings, out end);

        private void UpdateHits()
        {
            _newHits = _selectedPos
                .AsValueEnumerable()
                .Except(_registeredHits)
                .ToList();

            _oldHits = _registeredHits
                .AsValueEnumerable()
                .Except(_selectedPos)
                .ToList();
        }

        private void ProcessNewHits()
        {
            _pendingDestructions.Clear();
            _pendingEmptyDestructions.Clear();
            
            foreach (GridWorldCoordPair pair in _newHits)
            {
                if (!NodeMap.GetNode(pair.GridCoordinate.x, pair.GridCoordinate.z, out Node node))
                {
                    _pendingEmptyDestructions.Add(pair.WorldPosition);
                }
                else
                {
                    _pendingDestructions.Add(pair.WorldPosition);
                    _registeredNodes.Add(pair, node);
                }
                
                _registeredHits.Add(pair);
            }
            
            EventBus<DestructionEvent>.Raise(new DestructionEvent(_pendingDestructions, DestructionEventType.DestroyNode));
            EventBus<DestructionEvent>.Raise(new DestructionEvent(_pendingEmptyDestructions, DestructionEventType.DestroyEmpty));
        }

        private void ProcessOldHits()
        {
            TriggerEvent(_oldHits, DestructionEventType.Cancel);
            
            foreach (GridWorldCoordPair pair in _oldHits)
            {
                _registeredHits.Remove(pair); 
                
                if(_registeredNodes.ContainsKey(pair))
                    _registeredNodes.Remove(pair);
            }
        }

        private void ProcessFinalHits()
        {
            foreach (var kvp in _registeredNodes)
            {
                RemoveSingleNode(kvp.Key, kvp.Value);
            }
            
            if(_registeredHits.Any())
                TriggerEvent(_registeredHits, DestructionEventType.Cancel);
            
            _cellSelection.Clear();
            _registeredHits.Clear();
            _registeredNodes.Clear();
        }

        private void TriggerEvent(IEnumerable<GridWorldCoordPair> pairs, DestructionEventType eventType)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>();
            foreach (GridWorldCoordPair pair in pairs)
            {
                worldPositions.Add(pair.WorldPosition);
            }
            
            EventBus<DestructionEvent>.Raise(new DestructionEvent(worldPositions, eventType));
        }
    }
}