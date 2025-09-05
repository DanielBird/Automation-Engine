using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Belts;
using Engine.Construction.Drag;
using Engine.Construction.Drag.Selection;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Placement.Strategies
{
    /// <summary>
    /// Placement strategy for standard (non-draggable) Nodes
    /// </summary>
    public class StandardPlacementStrategy : IPlacementStrategy
    {
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly IResourceMap _resourceMap;
        private readonly PlacementSettings _placementSettings;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;
        private readonly NeighbourManager _neighbourManager;
        private readonly Transform _resourceParent;

        private CancellationTokenSource _cts; 

        public StandardPlacementStrategy(PlacementContext ctx, NeighbourManager neighbourManager, Transform resourceParent)
        {
            _map = ctx.Map;
            _nodeMap = ctx.NodeMap;
            _resourceMap = ctx.ResourceMap;
            _placementSettings = ctx.PlacementSettings;
            _state = ctx.State;
            _visuals = ctx.Visuals;
            _neighbourManager = neighbourManager;
            _resourceParent = resourceParent;
        }

        public bool CanHandle(IPlaceable placeable) => !placeable.Draggable;
        
        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (!Place(gridCoordinate, placeable)) return;
            
            FinalisePosition(_state.CurrentObject, placeable, gridCoordinate, _state.WorldAlignedPosition, _placementSettings.moveSpeed).Forget();
        }

        private void SetupNode(Node node, Vector3Int gridCoordinate)
        {
            CellSelectionParams selectionParams = new CellSelectionParams(_map, _nodeMap, _placementSettings, node.GridWidth); 
            NodeType nodeType = CellDefinition.DefineCell(gridCoordinate, node.Direction, selectionParams, out Direction finalDirection);
            node.RotateInstant(finalDirection);
            
            NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, nodeType); 
            node.Initialise(config);
            node.Visuals.HideArrows();
                
            if(node is Producer p) p.Activate(_resourceMap, _resourceParent);
        }

        public void CancelPlacement(IPlaceable placeable)
        {
            SimplePool.Despawn(_state.CurrentObject);
            _visuals.Hide();
        }

        private bool Place(Vector3Int gridCoord, IPlaceable placeable)
        {
            Vector2Int size = placeable.GetSize();
            if (!_map.RegisterOccupant(gridCoord.x, gridCoord.z, size.x, size.y))
            {
                placeable.FailedPlacement(gridCoord);
                return false;
            }
            placeable.Place(gridCoord, _nodeMap);
            return true;
        }
        
        private async UniTaskVoid FinalisePosition(GameObject go, IPlaceable placeable, Vector3Int gridPosition, Vector3Int worldPosition, float moveSpeed)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            while(DistanceAboveThreshold(go, worldPosition))
            {
                LerpPosition(go, worldPosition, moveSpeed);
                await UniTask.Yield(_cts.Token); 
            }
            
            go.transform.position = worldPosition;
            CtsCtrl.Clear(ref _cts);
            
            FinaliseSetup(placeable, gridPosition);
        }
        
        private bool DistanceAboveThreshold(GameObject obj, Vector3 targetPos, float threshold = 0.001f)
        {
            float distance = Vector3.Distance(obj.transform.position, targetPos);
            if(distance < threshold) return true;
            return false;
        }
        
        private void LerpPosition(GameObject obj, Vector3Int targetPos, float moveSpeed)
        {
            Transform t = obj.transform; 
            t.position = Vector3.Lerp(t.position, targetPos, moveSpeed * Time.deltaTime);
        }

        private void FinaliseSetup(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (placeable is Node node)
            {
                SetupNode(node, gridCoordinate);
            }
            
            _state.StopRunning();
            //_neighbourManager.UpdateToCorner(placeable, gridCoordinate, DragPos.Start);
            _visuals.Hide();
        }

        public void CleanUpOnDisable() => CtsCtrl.Clear(ref _cts);
    }
} 