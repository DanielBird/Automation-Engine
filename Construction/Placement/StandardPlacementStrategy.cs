using System.Threading;
using Construction.Drag;
using Construction.Interfaces;
using Construction.Maps;
using Construction.Nodes;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Construction.Placement
{
    /// <summary>
    /// Placement strategy for standard (non-draggable) Nodes
    /// </summary>
    public class StandardPlacementStrategy : IPlacementStrategy
    {
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly NeighbourManager _neighbourManager;
        private readonly PlacementSettings _placementSettings;
        private readonly PlacementState _state;
        private readonly PlacementVisuals _visuals;

        private CancellationTokenSource _cts; 

        public StandardPlacementStrategy(
            IMap map,
            INodeMap nodeMap,
            NeighbourManager neighbourManager,
            PlacementSettings placementSettings,
            PlacementState state,
            PlacementVisuals visuals)
        {
            _map = map;
            _nodeMap = nodeMap;
            _neighbourManager = neighbourManager;
            _placementSettings = placementSettings;
            _state = state;
            _visuals = visuals;
        }

        public bool CanHandle(IPlaceable placeable) => !placeable.Draggable;

        public void HandlePlacement(IPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (!Place(gridCoordinate, placeable)) return;
            
            if (placeable is Node node)
            {
                NodeConfiguration config = NodeConfiguration.Create(_nodeMap, NodeType.Straight, Direction.North, false); 
                node.Initialise(config);
                node.Visuals.HideArrows();
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
                placeable.FailedPlacement();
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