using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Construction.Drag;
using Construction.Maps;
using Construction.Nodes;
using Construction.Visuals;
using Cysharp.Threading.Tasks;
using Events;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
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

    [RequireComponent(typeof(IMap))]
    [RequireComponent(typeof(PlacementVisuals))]
    [RequireComponent(typeof(INodeMap))]
    public class PlacementManager : MonoBehaviour
    {
        private IMap _map;
        private INodeMap _nodeMap;
        public NeighbourManager neighbourManager; 
        private PlacementCoordinator _placementCoordinator;
        
        [Header("Settings")]
        [SerializeField] private PlacementSettings settings;

        [Header("State")] 
        [SerializeField] private PlacementState state = new();

        [Header("Visuals")] 
        public PlacementVisuals visuals;
        public GameObject floorDecal; 

        [Header("Camera")]
        public UnityEngine.Camera mainCamera;

        private Coroutine _drag;
        private DragManager _dragManager;

        private CancellationTokenSource _disableCancellation = new CancellationTokenSource();
        
        // EVENTS
        private EventBinding<UiButtonClick> onButtonClick; 

        private void Awake()
        {
            _map = GetComponent(typeof(IMap)) as IMap;
            _nodeMap = GetComponent<INodeMap>();
            if (visuals == null) visuals = GetComponent<PlacementVisuals>();
            
            neighbourManager = new NeighbourManager(
                _map, 
                _nodeMap, 
                settings, 
                transform);
            
            _dragManager = new DragManagerBuilder()
                .WithSettings(settings)
                .WithMap(_map)
                .WithVisuals(visuals)
                .WithNodeMap(_nodeMap)
                .WithNeighbourManager(neighbourManager)
                .WithCamera(mainCamera)
                .WithFloorDecal(floorDecal)
                .WithState(state)
                .Build();
            
            _placementCoordinator = new PlacementCoordinator(
                _dragManager,
                _map,
                _nodeMap,
                neighbourManager,
                state,
                visuals,
                floorDecal
            );
            
            settings.place.action.performed += ConfirmPlacement;
            settings.rotate.action.performed += Rotate;
            settings.cancel.action.performed += CancelPlacement; 
            
            floorDecal.SetActive(false);
            
            RegisterEvents();
        }
        
        private void OnDisable()
        {
            settings.place.action.performed -= ConfirmPlacement;
            settings.rotate.action.performed -= Rotate;
            settings.cancel.action.performed -= CancelPlacement; 
            
            _disableCancellation.Cancel();
            _disableCancellation.Dispose();
            _dragManager.Disable();
            
            UnRegisterEvents();
        }

        private void RegisterEvents()
        {
            onButtonClick = new EventBinding<UiButtonClick>(RequestPlacement);
            EventBus<UiButtonClick>.Register(onButtonClick);
        }

        private void UnRegisterEvents()
        {
            EventBus<UiButtonClick>.Deregister(onButtonClick);
        }

        private void RequestPlacement(UiButtonClick e)
        {
            Debug.Log("Request Placement"); 
            
            if (e.ButtonType == UiButtonType.Belt)
            {
                SpawnOccupant();
            }
        }

        private void Update()
        {
            if (!state.IsRunning) return;
            
            if(!TryGetGridPosition(out Vector3Int gridPos)) return;
            state.TargetPosition = gridPos;
            
            if(!_map.VacantCell(state.TargetPosition.x, state.TargetPosition.z)) return;
            if (DistanceAboveThreshold(state.CurrentObject, state.TargetPosition)) return;
            
            LerpPosition(state.CurrentObject, state.TargetPosition);
            
            visuals.Place(state.CurrentObject);

            SetFloorDecalPos(state.TargetPosition); 
        }

        private bool DistanceAboveThreshold(GameObject obj, Vector3 targetPos, float threshold = 0.001f)
        {
            float distance = Vector3.Distance(obj.transform.position, targetPos);
            if(distance < threshold) return true;
            return false;
        }

        private void LerpPosition(GameObject obj, Vector3 targetPos)
        {
            Transform t = obj.transform; 
            t.position = Vector3.Lerp(t.position, targetPos, settings.moveSpeed * Time.deltaTime);
        }
        
        public void SpawnOccupant()
        {
            if(!TryGetGridPosition(out Vector3Int gridPos)) return;
            
            Vector2Int empty = _map.NearestVacantCell(new Vector2Int(gridPos.x, gridPos.z));
            gridPos = new Vector3Int(empty.x, 0, empty.y);
            
            GameObject occupant  = SimplePool.Spawn(settings.standardBeltPrefab, gridPos, Quaternion.identity, transform);
            occupant.name = settings.standardBeltPrefab.name + "_" + gridPos.x + "_" + gridPos.z; 
            
            state.SetGameObject(occupant);

            if (state.PlaceableFound)
            {
                state.MainPlaceable.Reset();
                if(state.MainPlaceable is Node node) node.Visuals.ShowArrows();
            }
            
            visuals.Place(state.CurrentObject);
            visuals.Show();
            
            floorDecal.SetActive(true);
            SetFloorDecalPos(gridPos); 
        }

        private void ConfirmPlacement(InputAction.CallbackContext ctx)
        {
            if (!state.IsRunning) return;
            
            if (state.PlaceableFound)
            {
                _placementCoordinator.HandlePlacement(state.MainPlaceable, state.TargetPosition);
            }

            if (!state.IsRunning)
            {
                FinalisePosition(state.CurrentObject, state.TargetPosition).Forget();
            }
        }

        private async UniTaskVoid FinalisePosition(GameObject go, Vector3 finalPosition)
        {
            while(DistanceAboveThreshold(go, finalPosition))
            {
                LerpPosition(go, finalPosition);
                await UniTask.Yield(_disableCancellation.Token); 
            }
        }
        
        private bool TryGetGridPosition(out Vector3Int gridPosition)
        {
            gridPosition = new Vector3Int(); 
            if (!TryGetPosition(out Vector3 position)) return false;
            gridPosition = Grid.Position(position, settings.gridOrigin, _map.MapWidth, _map.MapHeight, settings.tileSize);
            return true;
        }
        
        private bool TryGetPosition(out Vector3 position)
        {
            bool positionFound = FloorPosition.Get(mainCamera, settings.raycastDistance, settings.floorLayer, out Vector3 foundPosition);
            position = foundPosition; 
            return positionFound;
        }

        private void Rotate(InputAction.CallbackContext ctx)
        {
            if (!state.IsRunning) return;
            if (!state.RotatableFound) return;
            state.MainRotatable.Rotate();
        }

        private void CancelPlacement(InputAction.CallbackContext ctx)
        {
            if (state.IsRunning)
            {
                if (state.PlaceableFound)
                {
                    _placementCoordinator.CancelPlacement(state.MainPlaceable);
                }
                return;
            } 
            
            if(!TryGetGridPosition(out Vector3Int delete)) return;

            if (_nodeMap.GetNode(delete.x, delete.z, out Node nodeToDelete))
            {
                _map.DeregisterOccupant(delete.x, delete.z, nodeToDelete.Width, nodeToDelete.Height);
                _nodeMap.DeregisterNode(nodeToDelete);
                SimplePool.Despawn(nodeToDelete.gameObject);
            }
        }
        
        private void SetFloorDecalPos(Vector3 pos) => floorDecal.transform.position = pos;
        

    }
}