using Engine.Construction.Maps;
using Engine.Construction.Visuals;
using Engine.GameState;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class PlacementContext
    {
        public readonly IWorld World;
        public readonly IResourceMap ResourceMap;
        public readonly InputSettings InputSettings;
        public readonly PlacementSettings PlacementSettings;
        public readonly PlacementState State;
        public readonly PlacementVisuals Visuals;
        public readonly Camera MainCamera;

        public PlacementContext(IWorld world, IResourceMap resourceMap, InputSettings inputSettings, PlacementSettings placementSettings, PlacementState state, PlacementVisuals visuals, Camera mainCamera)
        {
            World = world;
            ResourceMap = resourceMap;
            InputSettings = inputSettings;
            PlacementSettings = placementSettings;
            State = state;
            Visuals = visuals;
            MainCamera = mainCamera;
        }
    }
}