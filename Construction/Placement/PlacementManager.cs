using System;
using System.Collections.Generic;
using System.Threading;
using Construction.Drag;
using Construction.Events;
using Construction.Nodes;
using Construction.Placement.Factory;
using Construction.Utilities;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Events;
using Utilities.Events.Types;
using Grid = Construction.Utilities.Grid;

namespace Construction.Placement
{
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    public enum Axis
    {
        XAxis,
        YAxis,
        ZAxis,
    }
    
    [RequireComponent(typeof(PlacementVisuals))]
    public class PlacementManager : ConstructionManager
    {
        public NeighbourManager NeighbourManager; 
        private PlacementCoordinator _placementCoordinator;
        
        private Dictionary<BuildRequestType, IPlaceableFactory> _factories;

        [Header("State")] 
        [SerializeField] private PlacementState state = new();
        
        private Coroutine _drag;
        private DragManager _dragManager;

        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        private CancellationTokenSource _waitTokenSource = new CancellationTokenSource();
        
        // EVENTS
        private EventBinding<UiButtonClick> _onButtonClick; 
        private EventBinding<BeltClickEvent> _onBeltClick;

        protected override void Awake()
        {
            base.Awake();
            
            NeighbourManager = new NeighbourManager(
                Map, 
                NodeMap, 
                settings, 
                transform);
            
            _dragManager = new DragManagerBuilder()
                .WithSettings(settings)
                .WithInputSettings(inputSettings)
                .WithMap(Map)
                .WithVisuals(visuals)
                .WithNodeMap(NodeMap)
                .WithNeighbourManager(NeighbourManager)
                .WithCamera(mainCamera)
                .WithFloorDecal(visuals.floorDecal)
                .WithState(state)
                .Build();
            
            _placementCoordinator = new PlacementCoordinator(
                _dragManager,
                Map,
                NodeMap,
                NeighbourManager,
                state,
                visuals
            );

            _factories = new Dictionary<BuildRequestType, IPlaceableFactory>
            {
                { BuildRequestType.Belt, new BeltFactory(this, settings)},
                { BuildRequestType.Producer, new ProducerFactory(this, settings)}
            };
            
            RegisterEvents();
        }
        
        private void OnDisable()
        {
            ClearTokenSource(ref _disableCancellation);
            ClearTokenSource(ref _waitTokenSource);
            _dragManager.Disable();
            
            UnRegisterEvents();
        }

        private void RegisterEvents()
        {            
            settings.place.action.performed += ConfirmPlacement;
            settings.rotate.action.performed += Rotate;
            settings.cancel.action.performed += CancelPlacement; 
            
            _onButtonClick = new EventBinding<UiButtonClick>(RequestPlacement);
            _onBeltClick = new EventBinding<BeltClickEvent>(RequestDrag); 
            
            EventBus<UiButtonClick>.Register(_onButtonClick);
            EventBus<BeltClickEvent>.Register(_onBeltClick);
        }

        private void UnRegisterEvents()
        {
            settings.place.action.performed -= ConfirmPlacement;
            settings.rotate.action.performed -= Rotate;
            settings.cancel.action.performed -= CancelPlacement; 
            
            EventBus<UiButtonClick>.Deregister(_onButtonClick);
            EventBus<BeltClickEvent>.Deregister(_onBeltClick);
        }

        // The start of spawning game objects via Ui Button clicks 
        private void RequestPlacement(UiButtonClick e)
        {
            if (_factories.TryGetValue(e.BuildRequestType, out IPlaceableFactory factory))
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

            Vector3Int gridCoordinate = Grid.WorldToGridCoordinate(e.WorldPosition, new GridParams(settings.mapOrigin, Map.MapWidth, Map.MapHeight, settings.tileSize));
            state.IsRunning = false; 
            state.TargetGridCoordinate = gridCoordinate;
            state.WorldAlignedPosition = e.WorldPosition; 
            
            GameObject build = factory.CreateAt(e.WorldPosition); 
            SpawnOccupant(e.WorldPosition, build);
        }

        private void Update()
        {
            if (!state.IsRunning) return;
            
            if(!TryGetGridCoordinate(out Vector3Int gridCoord)) return;
            state.TargetGridCoordinate = gridCoord;
            state.WorldAlignedPosition = WorldAlignedPosition(gridCoord);
            
            if (!Map.VacantCell(state.TargetGridCoordinate.x, state.TargetGridCoordinate.z)) return;
            if (DistanceAboveThreshold(state.CurrentObject, state.TargetGridCoordinate)) return;
            
            LerpPosition(state.CurrentObject, state.WorldAlignedPosition);
            
            visuals.Place(state.CurrentObject);
            visuals.SetFloorDecalPos(state.WorldAlignedPosition); 
        }

        private bool DistanceAboveThreshold(GameObject obj, Vector3 targetPos, float threshold = 0.001f)
        {
            float distance = Vector3.Distance(obj.transform.position, targetPos);
            if(distance < threshold) return true;
            return false;
        }

        private void LerpPosition(GameObject obj, Vector3Int targetPos)
        {
            Transform t = obj.transform; 
            t.position = Vector3.Lerp(t.position, targetPos, settings.moveSpeed * Time.deltaTime);
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
            
            visuals.Place(state.CurrentObject);
            visuals.Show();
            visuals.SetFloorDecalPos(alignedPosition); 
        }

        private void ConfirmPlacement(InputAction.CallbackContext ctx)
        {
            if (!state.IsRunning) return;
            
            if (state.PlaceableFound)
            {
                _placementCoordinator.HandlePlacement(state.MainPlaceable, state.TargetGridCoordinate);
            }

            if (!state.IsRunning)
            {
                ClearTokenSource(ref _disableCancellation);
                _disableCancellation = new CancellationTokenSource(); 
                FinalisePosition(state.CurrentObject, state.WorldAlignedPosition, _disableCancellation.Token).Forget();
            }
        }

        private async UniTaskVoid FinalisePosition(GameObject go, Vector3Int finalPosition, CancellationToken token)
        {
            while(DistanceAboveThreshold(go, finalPosition))
            {
                LerpPosition(go, finalPosition);
                await UniTask.Yield(token); 
            }
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

        [Button]
        private void ShowGridAndDecal() => visuals.Show();
    }
}