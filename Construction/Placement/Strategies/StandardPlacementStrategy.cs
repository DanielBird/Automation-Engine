using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Belts;
using Engine.Construction.Drag;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Events;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Placement.Strategies
{
    /// <summary>
    /// Placement strategy for standard (non-draggable) Nodes
    /// </summary>
    public class StandardPlacementStrategy : IPlacementStrategy
    {
        private readonly IWorld _world; 
        private readonly IResourceMap _resourceMap;
        private readonly PlacementManager _placementManager;
        private readonly PlacementSettings _placementSettings;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;
        private readonly Transform _resourceParent;

        private CancellationTokenSource _cts; 

        public StandardPlacementStrategy(PlacementContext ctx, PlacementManager placementManager, Transform resourceParent)
        {
            _world = ctx.World;
            _resourceMap = ctx.ResourceMap;
            _placementSettings = ctx.PlacementSettings;
            _state = ctx.State;
            _visuals = ctx.Visuals;
            _placementManager = placementManager;
            _resourceParent = resourceParent;
        }

        public bool CanHandle(IPlaceable placeable) => !placeable.Draggable;
        
        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (!Place(gridCoordinate, placeable))
            {
                Debug.Log("Failed to place " + placeable);
                return;
            }
            
            FinalisePosition(_state.CurrentObject, placeable, gridCoordinate, _state.WorldAlignedPosition).Forget();
        }
        
        public void CancelPlacement(IPlaceable placeable)
        {
            Cleanup.RemovePlaceable(_state, _world);
            
            _state.StopRunning();
            _visuals.HideAllNodeArrows();
            _visuals.DeactivateHighlight();
        }

        private bool Place(Vector3Int gridCoord, IPlaceable placeable)
        {
            if (placeable is Node node)
            {
                // update the node name for easier debugging
                node.SetGameObjectName(gridCoord);
                
                if (!_world.TryPlaceNodeAt(node, gridCoord.x, gridCoord.z))
                {
                    node.FailedPlacement(gridCoord);
                    return false;
                }
            }
            else
            {
                if (!_world.TryPlaceOccupant(gridCoord, placeable))
                {
                    placeable.FailedPlacement(gridCoord);
                    return false;
                }
            }
            
            placeable.Place(gridCoord, _world);
            return true;
        }
        
        private async UniTaskVoid FinalisePosition(GameObject go, IPlaceable placeable, Vector3Int gridPosition, Vector3Int worldPosition)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            try
            {
                while(!_placementManager.IsArrived(go, worldPosition))
                {
                    _placementManager.LerpPosition(go, worldPosition);
                    await UniTask.Yield(_cts.Token); 
                }
                
                go.transform.position = worldPosition;
            }
            catch (System.OperationCanceledException)
            {
                // Token was cancelled - still finalize setup at current position
                // This ensures the object is properly configured even if animation was interrupted
            }
            finally
            {
                CtsCtrl.Clear(ref _cts);
                FinaliseSetup(placeable, gridPosition);
            }
        }
        
        private void FinaliseSetup(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (placeable is Node node)
            {
                SetupNode(node, gridCoordinate);
            }
            
            _state.StopRunning();
            _visuals.HideAllNodeArrows();
            _visuals.DeactivateHighlight();
        }
        
        private void SetupNode(Node node, Vector3Int gridCoordinate)
        {
            CellSelectionParams selectionParams = new CellSelectionParams(_world, _placementSettings, node.GridWidth); 
            NodeType nodeType = CellDefinition.DefineCell(gridCoordinate, node.Direction, selectionParams, out Direction finalDirection);
            node.RotateInstant(finalDirection);
            
            NodeConfiguration config = NodeConfiguration.Create(_world, nodeType); 
            node.Initialise(config);
            node.Visuals.HideArrows();
            
            EventBus<NodePlaced>.Raise(new NodePlaced(node));
                
            if(node is Producer p) p.Activate(_resourceMap, _resourceParent);
        }

        public void CleanUpOnDisable() => CtsCtrl.Clear(ref _cts);
    }
} 