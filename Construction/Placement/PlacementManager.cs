using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement.Factory;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Placement
{
    public class PlacementManager : ConstructionManager
    {
        private readonly PlacementCoordinator _placementCoordinator;
        private readonly Dictionary<NodeType, IPlaceableFactory> _factoryCache = new();
        private readonly Dictionary<NodeType, Func<IPlaceableFactory>> _factoryOverrides = new();

        private readonly PlacementState state;
        private bool _viablePlacement; 
        
        private Coroutine _drag;
        public readonly Transform MyTransform;
        
        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        private CancellationTokenSource _waitTokenSource = new CancellationTokenSource();

        private bool _newGridCell; 
        private float _timeOfLastClick;
        
        // EVENTS
        private EventBinding<ConstructionUiButtonClick> _onButtonClick; 
        private EventBinding<BeltClickEvent> _onBeltClick;
        private bool _eventsRegistered; 
        
        public PlacementManager(PlacementContext ctx, Transform transform, Transform resourceParent) : base(ctx)
        {
            state = ctx.State;
            MyTransform = transform;

            DragManager dragManager = new(ctx);
            _placementCoordinator = new PlacementCoordinator(ctx, this, dragManager, resourceParent);
            
            RegisterEvents();
        }
        
        public void Disable()
        {
            ClearTokenSource(ref _disableCancellation);
            ClearTokenSource(ref _waitTokenSource);
            UnRegisterEvents();
            _placementCoordinator.Disable();
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            InputSettings.place.action.performed += ConfirmPlacement;
            InputSettings.rotate.action.performed += Rotate;
            InputSettings.cancel.action.performed += CancelPlacement; 
            
            _onButtonClick = new EventBinding<ConstructionUiButtonClick>(RequestPlacement);
            _onBeltClick = new EventBinding<BeltClickEvent>(RequestDrag); 
            
            EventBus<ConstructionUiButtonClick>.Register(_onButtonClick);
            EventBus<BeltClickEvent>.Register(_onBeltClick);
            
            _eventsRegistered = true;
        }

        private void UnRegisterEvents()
        {
            if (!_eventsRegistered) return;
            
            InputSettings.place.action.performed -= ConfirmPlacement;
            InputSettings.rotate.action.performed -= Rotate;
            InputSettings.cancel.action.performed -= CancelPlacement; 
            
            EventBus<ConstructionUiButtonClick>.Deregister(_onButtonClick);
            EventBus<BeltClickEvent>.Deregister(_onBeltClick);
            
            _eventsRegistered = false;
        }
        
        public void RegisterFactoryOverride(NodeType type, Func<IPlaceableFactory> factory) => _factoryOverrides[type] = factory;

        private IPlaceableFactory ResolveFactory(NodeType type)
        {
            if (_factoryCache.TryGetValue(type, out IPlaceableFactory f)) 
                return f;
            
            if (_factoryOverrides.TryGetValue(type, out Func<IPlaceableFactory> make))
                return _factoryCache[type] = make();
            
            return _factoryCache[type] = new GenericFactory(this, Settings, type);
        }
        
        // The start of spawning game objects via Ui Button clicks 
        private void RequestPlacement(ConstructionUiButtonClick e)
        {
            IPlaceableFactory factory = ResolveFactory(e.RequestType);
            ClearTokenSource(ref _waitTokenSource);
            _waitTokenSource = new CancellationTokenSource();
            WaitToStartPlacement(factory, _waitTokenSource.Token).Forget();
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
            Vector3Int gridCoordinate = e.GridCoord;
            Vector3Int worldPosition = Grid.GridToWorldPosition(gridCoordinate, Settings.mapOrigin, Settings.cellSize);
            
            state.StopRunning(); 
            state.TargetGridCoordinate = gridCoordinate;
            state.WorldAlignedPosition = worldPosition; 
            state.SetPathId(e.RequestingNode.PathId);
            
            if (e.ReplaceRequestingNode)
            {
                Cleanup.RemoveNode(e.RequestingNode, gridCoordinate, World);
            }
            
            IPlaceableFactory factory = ResolveFactory(e.BuildRequestType);
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
            
            Visuals.SetHighlightPos(state.WorldAlignedPosition);

            int x = state.TargetGridCoordinate.x;
            int z = state.TargetGridCoordinate.z;
            
            _viablePlacement = World.VacantCell(x, z);
            
            // If the game object is allowed to replace an existing node, continue
            if (!_viablePlacement)
            {
                if(!World.HasNode(x, z)) return;
                if(!state.PlaceableReplacesNodes) return;
                _viablePlacement = true; // reset bool
            }
            
            // If the current object is roughly at the target grid coordinate, then return
            if (IsArrived(state.CurrentObject, state.WorldAlignedPosition))
            {
                if (_newGridCell) UpdateState();
                _newGridCell = false;
                return;
            }
            
            _newGridCell = true;
            LerpPosition(state.CurrentObject, state.WorldAlignedPosition);
            Visuals.Place(state.CurrentObject);
        }
        
        private void UpdateState()
        {
            if(!state.UpdateState(World, Settings, out NodeType newNodeType, out Direction newDirection))
                return;
            
            IPlaceableFactory factory = ResolveFactory(newNodeType);
            Cleanup.RemovePlaceable(state, World);
            
            if(!factory.Create(out GameObject newGameObject, out Vector3Int alignedPosition)) return;
            
            state.SetGameObject(newGameObject);
            
            if (!state.PlaceableFound) return;
            state.MainPlaceable.Reset();
            
            if (state.MainPlaceable is Node node)
            {
                node.Visuals.ShowArrows();
                node.RotateInstant(newDirection);
            }
        }
        
        private void SpawnOccupant(Vector3Int alignedPosition, GameObject occupant)
        {
            Vector2Int empty = World.NearestVacantCell(new Vector2Int(alignedPosition.x, alignedPosition.z));
            alignedPosition = new Vector3Int(empty.x, 0, empty.y);

            state.SetGameObject(occupant);
            state.ClearPathId();

            if (state.PlaceableFound)
            {
                state.MainPlaceable.Reset();
                
                if(state.MainPlaceable is Node node)
                {
                    node.Visuals.ShowArrows();
                }
            }
            
            Visuals.Place(state.CurrentObject);
            Visuals.ShowPlacementVisuals(alignedPosition);
        }
        
        // Triggered by the left mouse down input action
        private void ConfirmPlacement(InputAction.CallbackContext ctx)
        {
            // Return if the cursor is over a UI element
            if(MouseUtils.IsOverUI()) return;
            if(!_viablePlacement) return;

            // Return if this is the second click of a double click
            if(Time.time < _timeOfLastClick + InputSettings.minTimeBetweenClicks) return;
            _timeOfLastClick = Time.time; 
            
            if (!state.IsRunning) return;
            if (!state.PlaceableFound) return;
            
            if(state.PlaceableIsNode && state.PlaceableReplacesNodes) 
                ReplaceNode();

            _placementCoordinator.HandlePlacement(state.MainPlaceable, state.TargetGridCoordinate);
        }

        private void ReplaceNode()
        {
            int x = state.TargetGridCoordinate.x;
            int z = state.TargetGridCoordinate.z;

            if(World.VacantCell(x, z)) 
                return;
            
            if(!World.TryGetNode(x, z, out Node node))
                return;

            Direction d = node.Direction; 
            if(state.Node.Direction != d)
                state.Node.RotateInstant(d);
            
            RemoveNode(node, state.TargetGridCoordinate);
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
            
            Visuals.HidePlacementVisuals();
        }
    }
}