using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using Engine.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSettings = Engine.GameState.InputSettings;

namespace Engine.Construction.Placement.Strategies
{
    public class DraggablePlacementStrategy : IPlacementStrategy
    {
        private readonly DragManager _dragManager;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;
        private readonly InputSettings _inputSettings;
        private readonly StandardPlacementStrategy _standardPlacementStrategy;

        private CancellationTokenSource _cts; 
        
        public DraggablePlacementStrategy(PlacementContext ctx, DragManager dragManager, StandardPlacementStrategy standardPlacementStrategy)
        {
            _state = ctx.State;
            _visuals = ctx.Visuals;
            _dragManager = dragManager;
            _inputSettings = ctx.InputSettings;
            _standardPlacementStrategy = standardPlacementStrategy;
        }

        public bool CanHandle(IPlaceable placeable) => placeable.Draggable;

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            _state.StopRunning();
            if(placeable is Node node) 
            {
                // Check if user intended a drag or single placement
                CheckPlacementIntent(placeable, node, gridCoordinate).Forget();
            }
        }
        
        private async UniTaskVoid CheckPlacementIntent(IPlaceable placeable, Node node, Vector3Int gridCoordinate)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource(); 
            
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            float waitTime = _inputSettings.waitForInputTime; 
            await UniTask.WaitForSeconds(waitTime, cancellationToken: _cts.Token);
            
            // If still pressed after wait time, treat as drag
            if (_inputSettings.place.action.IsPressed())
            {
                _dragManager.HandleDrag(_state.CurrentObject, node, gridCoordinate).Forget();
            }
            else if (Vector2.Distance(mousePos, Mouse.current.position.ReadValue()) > _inputSettings.dragThreshold)
            {
                _dragManager.HandleDrag(_state.CurrentObject, node, gridCoordinate).Forget();
            }
            else
            {
                // Revert to standard placement
                _standardPlacementStrategy.HandlePlacement(placeable, gridCoordinate);
            }
            
            CtsCtrl.Clear(ref _cts);
        }
        
        public void CancelPlacement(IPlaceable placeable)
        {
            _dragManager.DespawnAll();
            _visuals.Hide();
        }

        public void CleanUpOnDisable()
        {
            CtsCtrl.Clear(ref _cts); 
            _dragManager.Disable();
        } 
    }
} 