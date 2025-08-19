using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Drag;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement.Factory;
using Engine.Construction.Utilities;
using Engine.Construction.Visuals;
using Engine.Utilities.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Placement
{
    [RequireComponent(typeof(PlacementVisuals))]
    public class PlacementManager : ConstructionManager
    {
        public NeighbourManager NeighbourManager; 
        private PlacementCoordinator _placementCoordinator;
        
        private Dictionary<NodeType, IPlaceableFactory> _factories;

        [Header("State")] 
        [SerializeField] private PlacementState state = new();
        
        private Coroutine _drag;
        private DragManager _dragManager;

        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        private CancellationTokenSource _waitTokenSource = new CancellationTokenSource();

        private float _timeOfLastClick; 
        
        // EVENTS
        private EventBinding<ConstructionUiButtonClick> _onButtonClick; 
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
                settings,
                state,
                visuals
            );
            
            _factories = new Dictionary<NodeType, IPlaceableFactory>
            {
                { NodeType.GenericBelt, new GenericFactory(this, settings, NodeType.Straight)},
                { NodeType.Straight, new GenericFactory(this, settings, NodeType.Straight)},
                { NodeType.Producer, new GenericFactory(this, settings, NodeType.Producer)},
                { NodeType.Consumer, new GenericFactory(this, settings, NodeType.Consumer)},
                { NodeType.Splitter, new GenericFactory(this, settings, NodeType.Splitter)}, 
                { NodeType.Combiner, new GenericFactory(this, settings, NodeType.Combiner)},
            };
            
            RegisterEvents();
        }
        
        private void OnDisable()
        {
            ClearTokenSource(ref _disableCancellation);
            ClearTokenSource(ref _waitTokenSource);
            UnRegisterEvents();
            _placementCoordinator.RegisterOnDisable();
        }

        private void RegisterEvents()
        {            
            inputSettings.place.action.performed += ConfirmPlacement;
            inputSettings.rotate.action.performed += Rotate;
            inputSettings.cancel.action.performed += CancelPlacement; 
            
            _onButtonClick = new EventBinding<ConstructionUiButtonClick>(RequestPlacement);
            _onBeltClick = new EventBinding<BeltClickEvent>(RequestDrag); 
            
            EventBus<ConstructionUiButtonClick>.Register(_onButtonClick);
            EventBus<BeltClickEvent>.Register(_onBeltClick);
        }

        private void UnRegisterEvents()
        {
            inputSettings.place.action.performed -= ConfirmPlacement;
            inputSettings.rotate.action.performed -= Rotate;
            inputSettings.cancel.action.performed -= CancelPlacement; 
            
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
            
            Vector3Int gridCoordinate = Grid.WorldToGridCoordinate(e.WorldPosition, new GridParams(settings.mapOrigin, Map.MapWidth, Map.MapHeight, settings.cellSize));
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
            // Return if the cursor is over a UI element
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // Return if this is the second click of a double click
            if(Time.time < _timeOfLastClick + inputSettings.minTimeBetweenClicks) return;
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

        [Button]
        private void ShowGridAndDecal() => visuals.Show();
    }
}