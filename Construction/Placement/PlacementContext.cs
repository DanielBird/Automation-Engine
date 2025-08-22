using Engine.Construction.Maps;
using Engine.Construction.Visuals;
using Engine.GameState;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class PlacementContext
    {
        public readonly IMap Map;
        public readonly INodeMap NodeMap;
        public readonly IResourceMap ResourceMap;
        public readonly InputSettings InputSettings;
        public readonly PlacementSettings PlacementSettings;
        public readonly PlacementState State;
        public readonly PlacementVisuals Visuals;
        public readonly Camera MainCamera;

        public PlacementContext(IMap map, INodeMap nodeMap, IResourceMap resourceMap, InputSettings inputSettings, PlacementSettings placementSettings, PlacementState state, PlacementVisuals visuals, Camera mainCamera)
        {
            Map = map;
            NodeMap = nodeMap;
            ResourceMap = resourceMap;
            InputSettings = inputSettings;
            PlacementSettings = placementSettings;
            State = state;
            Visuals = visuals;
            MainCamera = mainCamera;
        }
    }
}