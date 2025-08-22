using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement.Factory;
using Engine.Construction.Visuals;
using Engine.Utilities;
using Engine.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Placement
{
    public class PlacementManager : ConstructionManager
    {
        private readonly PlacementCoordinator _placementCoordinator;
        private readonly Dictionary<NodeType, IPlaceableFactory> _factories;

        private readonly PlacementState state;
        private Coroutine _drag;
        public Transform myTransform;
        private Transform resourceParent; 
        
        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        private CancellationTokenSource _waitTokenSource = new CancellationTokenSource();

        private float _timeOfLastClick;
        
        // EVENTS
        private EventBinding<ConstructionUiButtonClick> _onButtonClick; 
        private EventBinding<BeltClickEvent> _onBeltClick;
        
        public PlacementManager(PlacementContext ctx, Transform transform, Transform resourceParent) : base(ctx)
        {
            state = ctx.State;
            myTransform = transform;
            NeighbourManager neighbourManager = new(ctx, transform);
            DragManager dragManager = new(ctx, neighbourManager);
            _placementCoordinator = new PlacementCoordinator(ctx, dragManager, neighbourManager, resourceParent);
            
            _factories = new Dictionary<NodeType, IPlaceableFactory>
            {
                { NodeType.GenericBelt, new GenericFactory(this, Settings, NodeType.Straight)},
                { NodeType.Straight, new GenericFactory(this, Settings, NodeType.Straight)},
                { NodeType.Producer, new GenericFactory(this, Settings, NodeType.Producer)},
                { NodeType.Consumer, new GenericFactory(this, Settings, NodeType.Consumer)},
                { NodeType.Splitter, new GenericFactory(this, Settings, NodeType.Splitter)}, 
                { NodeType.Combiner, new GenericFactory(this, Settings, NodeType.Combiner)},
            };
            
            RegisterEvents();
        }
        
        public void Disable()
        {
            ClearTokenSource(ref _disableCancellation);
            ClearTokenSource(ref _waitTokenSource);
            UnRegisterEvents();
            _placementCoordinator.RegisterOnDisable();
        }

        private void RegisterEvents()
        {            
            InputSettings.place.action.performed += ConfirmPlacement;
            InputSettings.rotate.action.performed += Rotate;
            InputSettings.cancel.action.performed += CancelPlacement; 
            
            _onButtonClick = new EventBinding<ConstructionUiButtonClick>(RequestPlacement);
            _onBeltClick = new EventBinding<BeltClickEvent>(RequestDrag); 
            
            EventBus<ConstructionUiButtonClick>.Register(_onButtonClick);
            EventBus<BeltClickEvent>.Register(_onBeltClick);
        }

        private void UnRegisterEvents()
        {
            InputSettings.place.action.performed -= ConfirmPlacement;
            InputSettings.rotate.action.performed -= Rotate;
            InputSettings.cancel.action.performed -= CancelPlacement; 
            
            EventBus<ConstructionUiButtonClick>.Deregister(_onButtonClick);
            EventBus<BeltClickEvent>.Deregister(_onBeltClick);
        }

        // The start of spawning game objects via Ui Button clicks 
        private void RequestPlacement(ConstructionUiButtonClick e)
        {
            if (_factories.TryGetValue(e.RequestType, out IPlaceableFactory factory))
            {
                ClearTokenSource(ref _waitTokenSource);
                _waitTokenSource = new CancellationTokenSource();
                WaitToStartPlacement(factory, _waitTokenSource.Token).Forget();
            }
        }
        
        private async UniTaskVoid WaitToStartPlacement(IPlaceableFactory factory, CancellationToken token)
        {
            while (!TryGetGridAlignedWorldPosition(out Vector3Int pos))
            {
                await UniTask.WaitForSeconds(0.1f, cancellationToken: token); 
            }

            if (factory.Create(out GameObject prefab, out Vector3Int alignedPosition))
            {
                SpawnOccupant(alignedPosition, prefab);
            }
        }

        // The start of spawning a draggable node by clicking on a node already placed
        private void RequestDrag(BeltClickEvent e)
        {
            if(!_factories.TryGetValue(e.BuildRequestType, out IPlaceableFactory factory)) return;
            
            Vector3Int gridCoordinate = e.GridCoord;
            Vector3Int worldPosition = Grid.GridToWorldPosition(gridCoordinate, Settings.mapOrigin, Settings.cellSize);
            
            state.IsRunning = false; 
            state.TargetGridCoordinate = gridCoordinate;
            state.WorldAlignedPosition = worldPosition; 
            
            GameObject build = factory.CreateAt(worldPosition); 
            
            if(build.TryGetComponent(out Node node))
                node.RotateInstant(e.RequestingNode.Direction);
            
            SpawnOccupant(worldPosition, build);
            _placementCoordinator.HandlePlacement(state.MainPlaceable, state.TargetGridCoordinate);
        }

        public void Tick()
        {
            if (!state.IsRunning) return;
            
            if(!TryGetGridCoordinate(out Vector3Int gridCoord)) return;
            state.TargetGridCoordinate = gridCoord;
            state.WorldAlignedPosition = WorldAlignedPosition(gridCoord);
            
            if (!Map.VacantCell(state.TargetGridCoordinate.x, state.TargetGridCoordinate.z)) return;
            
            // If the current object is roughly at the target grid coordinate, then return
            if (Arrived(state.CurrentObject, state.WorldAlignedPosition)) return;
            
            LerpPosition(state.CurrentObject, state.WorldAlignedPosition);
            
            Visuals.Place(state.CurrentObject);
            Visuals.SetFloorDecalPos(state.WorldAlignedPosition); 
        }

        private bool Arrived(GameObject obj, Vector3 targetPos, float threshold = 0.02f)
        {
            float distance = Vector3.Distance(obj.transform.position, targetPos);
            if (distance < threshold) return true;
            return false;
        }

        private void LerpPosition(GameObject obj, Vector3Int targetPos)
        {
            Transform t = obj.transform; 
            t.position = Vector3.Lerp(t.position, targetPos, Settings.moveSpeed * Time.deltaTime);
        }
        
        private void SpawnOccupant(Vector3Int alignedPosition, GameObject occupant)
        {
            Vector2Int empty = Map.NearestVacantCell(new Vector2Int(alignedPosition.x, alignedPosition.z));
            alignedPosition = new Vector3Int(empty.x, 0, empty.y);

            state.SetGameObject(occupant);

            if (state.PlaceableFound)
            {
                state.MainPlaceable.Reset();
                if(state.MainPlaceable is Node node) node.Visuals.ShowArrows();
            }
            
            Visuals.Place(state.CurrentObject);
            Visuals.Show();
            Visuals.SetFloorDecalPos(alignedPosition); 
        }

        private void ConfirmPlacement(InputAction.CallbackContext ctx)
        {
            // Return if the cursor is over a UI element
            if(MouseUtils.IsOverUI()) return;

            // Return if this is the second click of a double click
            if(Time.time < _timeOfLastClick + InputSettings.minTimeBetweenClicks) return;
            _timeOfLastClick = Time.time; 
            
            if (!state.IsRunning) return;
            if (!state.PlaceableFound) return;
            
            _placementCoordinator.HandlePlacement(state.MainPlaceable, state.TargetGridCoordinate);
        }
        
        private void Rotate(InputAction.CallbackContext ctx)
        {
            if (!state.IsRunning) return;
            if (!state.RotatableFound) return;
            state.MainRotatable.Rotate();
        }

        private void CancelPlacement(InputAction.CallbackContext ctx)
        {
            if (state.IsRunning && state.PlaceableFound)
                _placementCoordinator.CancelPlacement(state.MainPlaceable);
        }
    }
}