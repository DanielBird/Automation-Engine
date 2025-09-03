using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Utilities;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using ZLinq;

namespace Engine.Construction.Placement
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
        
        private HashSet<Vector3> _pendingDestructions = new();
        private HashSet<Vector3> _pendingEmptyDestructions = new();
        
        private CancellationTokenSource _rightClickDragTokenSource;
        
        public RemovalManager(PlacementContext ctx) : base(ctx)
        {
            InputSettings.cancel.action.performed += RemoveNodes;
        }

        public void Disable()
        {
            InputSettings.cancel.action.performed -= RemoveNodes; 
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
            DetectRightClickDown(_rightClickDragTokenSource.Token).Forget();
        }

        private async UniTaskVoid DetectRightClickDown(CancellationToken token)
        {
            await UniTask.WaitForSeconds(InputSettings.waitForInputTime, cancellationToken: token);

            if (!TryGetGridCoordinate(out Vector3Int start))
            {
                Debug.Log("Failed to detect grid coordinate");
                return;
            }

            GridWorldCoordPair startingPair = new (start, WorldAlignedPosition(start));
            
            if (!InputSettings.cancel.action.IsPressed())
            {
                if (!NodeMap.TryGetNode(start.x, start.z, out Node node)) return;
                RemoveSingleNode(startingPair, node);
                return;
            }

            Visuals.Show(false);
            
            while (InputSettings.cancel.action.IsPressed())
            {
                _cellSelection = SelectCells(start, out Vector3Int endGridCoord);
                _selectedPos = _cellSelection.GetGridWorldPairs(Settings);

                if (_selectedPos.Any())
                {
                    UpdateHits();
                }
                else
                {
                    _oldHits = _registeredHits.ToList();
                    _newHits.Clear(); // prevent re-adding previously processed pairs
                }
                
                if(_newHits.Any())
                    ProcessNewHits();
                
                if(_oldHits.Any())
                    ProcessOldHits();
                    
                await UniTask.Yield(token);
            }
            
            ProcessFinalHits();
            Visuals.Hide(false);
        }
        
        private void RemoveSingleNode(GridWorldCoordPair delete, Node node) => RemoveNode(node, delete.GridCoordinate);

        private CellSelection SelectCells(Vector3Int start, out Vector3Int end)
            // => CellSelector.SelectCellArea(start, mainCamera, settings.floorLayer, _cellHits, Map, settings, out end);
             => CellSelector.SelectCellAreaWithNodes(start, MainCamera, Settings.floorLayer, _cellHits, new CellSelectionParams(Map, NodeMap, Settings, 1), true, out end);
        
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
                if (!NodeMap.TryGetNode(pair.GridCoordinate.x, pair.GridCoordinate.z, out Node node))
                {
                    _pendingEmptyDestructions.Add(pair.WorldPosition);
                }
                else
                {
                    Vector3 key = pair.WorldPosition + Offset(node);

                    if (!node.isRemovable)
                    {
                        if(node.ParentNode == null) continue;
                        node = node.ParentNode;
                        key = node.WorldPosition + Offset(node);
                    }
                    
                    if (_pendingDestructions.Add(key))
                    {
                        if (!_registeredNodes.ContainsKey(pair))
                            _registeredNodes.Add(pair, node);
                    }
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
            
            // Clear everything. 
            EventBus<DestructionEvent>.Raise(new DestructionEvent(new List<Vector3>(), DestructionEventType.ClearAll));
            
            _cellSelection.Clear();
            _registeredHits.Clear();
            _registeredNodes.Clear();
        }

        private void TriggerEvent(IEnumerable<GridWorldCoordPair> pairs, DestructionEventType eventType)
        {
            List<Vector3> worldPositions = new List<Vector3>();
            foreach (GridWorldCoordPair pair in pairs)
            {
                if(_registeredNodes.TryGetValue(pair, out Node node))
                    worldPositions.Add(node.WorldPosition + Offset(node));
                else
                    worldPositions.Add(pair.WorldPosition);
            }
            
            EventBus<DestructionEvent>.Raise(new DestructionEvent(worldPositions, eventType));
        }
        
        private Vector3 Offset(Node node)
        {
            if(Settings.FoundOffset(node.NodeType, out Vector3 offset))
                return offset;
            
            return Vector3.zero;
        }
    }
}