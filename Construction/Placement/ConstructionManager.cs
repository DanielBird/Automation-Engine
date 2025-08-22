using System.Threading;
using Engine.Construction.Maps;
using Engine.Construction.Utilities;
using Engine.Construction.Visuals;
using Engine.GameState;
using Engine.Utilities;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Placement
{
    /// <summary>
    /// Abstract parent class to Placement Manager and Removal Manager 
    /// </summary>
    public abstract class ConstructionManager
    {
        protected readonly IMap Map;
        protected readonly INodeMap NodeMap;
        protected readonly IResourceMap ResourceMap;
        
        protected readonly PlacementSettings Settings;
        protected readonly InputSettings InputSettings;
        protected readonly PlacementVisuals Visuals;
        protected readonly Camera MainCamera;
        
        public ConstructionManager(PlacementContext ctx)
        {
            Map = ctx.Map;
            NodeMap = ctx.NodeMap;
            ResourceMap = ctx.ResourceMap;
            Settings = ctx.PlacementSettings;
            InputSettings = ctx.InputSettings; 
            Visuals = ctx.Visuals;
            MainCamera = ctx.MainCamera;
        }
        
        
        /// <summary>
        /// Try to find a position aligned to the grid from a raycast intersection with the floor layer 
        /// </summary>
        public bool TryGetGridAlignedWorldPosition(out Vector3Int alignedPosition)
        {
            alignedPosition = new Vector3Int(); 
            if (!TryGetWorldPosition(out Vector3 position)) return false;
            alignedPosition = Grid.GridAlignedWorldPosition(position, new GridParams(Settings.mapOrigin, Map.MapWidth, Map.MapHeight, Settings.cellSize));
            return true;
        }

        public Vector3Int WorldAlignedPosition(Vector3Int gridCoord) => Grid.GridToWorldPosition(gridCoord, Settings.mapOrigin, Settings.cellSize);
        
        /// <summary>
        /// Try to find a grid coordinate from a raycast intersection with the floor layer 
        /// </summary>
        public bool TryGetGridCoordinate(out Vector3Int gridCoordinate)
        {
            gridCoordinate = new Vector3Int(); 
            if (!TryGetWorldPosition(out Vector3 position)) return false;
            gridCoordinate = Grid.WorldToGridCoordinate(position, new GridParams(Settings.mapOrigin, Map.MapWidth, Map.MapHeight, Settings.cellSize));
            return true;
        }
        
        /// <summary>
        /// Try raycast hit the floor layer, casting a ray from the main camera through the mouse position. 
        /// </summary>
        public bool TryGetWorldPosition(out Vector3 position)
        {
            bool positionFound = FloorPosition.Get(MainCamera, Settings.raycastDistance, Settings, out Vector3 foundPosition);
            position = foundPosition; 
            return positionFound;
        }

        protected void ClearTokenSource(ref CancellationTokenSource tokenSource) => CtsCtrl.Clear(ref tokenSource);
    }
}