using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Belts;
using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Placement
{
    /// <summary>
    /// Placement strategy for standard (non-draggable) Nodes
    /// </summary>
    public class StandardPlacementStrategy : IPlacementStrategy
    {
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly PlacementSettings _placementSettings;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;
        private readonly NeighbourManager _neighbourManager;
        private readonly Transform _widgetParent;

        private CancellationTokenSource _cts; 

        public StandardPlacementStrategy(PlacementContext ctx, NeighbourManager neighbourManager, Transform widgetParent)
        {
            _map = ctx.Map;
            _nodeMap = ctx.NodeMap;
            _placementSettings = ctx.PlacementSettings;
            _state = ctx.State;
            _visuals = ctx.Visuals;
            _neighbourManager = neighbourManager;
            _widgetParent = widgetParent;
        }

        public bool CanHandle(IPlaceable placeable) => !placeable.Draggable;

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (!Place(gridCoordinate, placeable)) return;
            
            if (placeable is Node node)
            {
                NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, NodeType.Straight); 
                node.Initialise(config);
                node.Visuals.HideArrows();
                
                if(node is Producer p) p.SetWidgetParent(_widgetParent); 
            }

            _state.IsRunning = false;
            _neighbourManager.UpdateToCorner(placeable, gridCoordinate, DragPos.Start);
            _visuals.Hide();
            
            FinalisePosition(_state.CurrentObject, _state.WorldAlignedPosition, _placementSettings.moveSpeed).Forget();
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
        
        private async UniTaskVoid FinalisePosition(GameObject go, Vector3Int finalPosition, float moveSpeed)
        {
            CtsCtrl.Clear(ref _cts);
            _cts = new CancellationTokenSource();
            
            while(DistanceAboveThreshold(go, finalPosition))
            {
                LerpPosition(go, finalPosition, moveSpeed);
                await UniTask.Yield(_cts.Token); 
            }
            
            go.transform.position = finalPosition;
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

        public void CleanUpOnDisable() => CtsCtrl.Clear(ref _cts);
    }
} 