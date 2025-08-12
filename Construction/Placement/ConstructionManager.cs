using System.Threading;
using Construction.Maps;
using Construction.Utilities;
using Construction.Visuals;
using GameState;
using UnityEngine;
using Utilities;
using Grid = Construction.Utilities.Grid;

namespace Construction.Placement
{
    /// <summary>
    /// Abstract parent class to Placement Manager and Removal Manager 
    /// </summary>
    [RequireComponent(typeof(IMap))]
    [RequireComponent(typeof(INodeMap))]
    public abstract class ConstructionManager : MonoBehaviour
    {
        protected IMap Map;
        protected INodeMap NodeMap;
        
        [Header("Settings")]
        [SerializeField] protected PlacementSettings settings;
        [SerializeField] protected InputSettings inputSettings;
        
        [Header("Visuals")] 
        public PlacementVisuals visuals;
        
        [Header("Camera")]
        public Camera mainCamera;

        protected virtual void Awake()
        {
            Map = GetComponent(typeof(IMap)) as IMap;
            NodeMap = GetComponent<INodeMap>();
            if (visuals == null) visuals = GetComponent<PlacementVisuals>();

            if (settings == null)
            {
                Debug.LogWarning("Missing placement settings");
                settings = ScriptableObject.CreateInstance<PlacementSettings>();
            }

            if (inputSettings == null)
            {
                Debug.LogWarning("Missing input settings");
                inputSettings = ScriptableObject.CreateInstance<InputSettings>();
            }
        }
        
        /// <summary>
        /// Try to find a position aligned to the grid from a raycast intersection with the floor layer 
        /// </summary>
        public bool TryGetGridAlignedWorldPosition(out Vector3Int alignedPosition)
        {
            alignedPosition = new Vector3Int(); 
            if (!TryGetWorldPosition(out Vector3 position)) return false;
            alignedPosition = Grid.GridAlignedWorldPosition(position, new GridParams(settings.mapOrigin, Map.MapWidth, Map.MapHeight, settings.cellSize));
            return true;
        }

        public Vector3Int WorldAlignedPosition(Vector3Int gridCoord) => Grid.GridToWorldPosition(gridCoord, settings.mapOrigin, settings.cellSize);
        
        /// <summary>
        /// Try to find a grid coordinate from a raycast intersection with the floor layer 
        /// </summary>
        public bool TryGetGridCoordinate(out Vector3Int gridCoordinate)
        {
            gridCoordinate = new Vector3Int(); 
            if (!TryGetWorldPosition(out Vector3 position)) return false;
            gridCoordinate = Grid.WorldToGridCoordinate(position, new GridParams(settings.mapOrigin, Map.MapWidth, Map.MapHeight, settings.cellSize));
            return true;
        }
        
        /// <summary>
        /// Try raycast hit the floor layer, casting a ray from the main camera through the mouse position. 
        /// </summary>
        protected bool TryGetWorldPosition(out Vector3 position)
        {
            bool positionFound = FloorPosition.Get(mainCamera, settings.raycastDistance, settings, out Vector3 foundPosition);
            position = foundPosition; 
            return positionFound;
        }

        protected void ClearTokenSource(ref CancellationTokenSource tokenSource) => CtsCtrl.Clear(ref tokenSource);
    }
}