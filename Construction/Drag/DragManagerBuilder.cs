using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Maps;
using Engine.Construction.Placement;
using Engine.Construction.Visuals;
using Engine.GameState;
using UnityEngine;

namespace Engine.Construction.Drag
{
    public class DragManagerBuilder
    {
        private PlacementSettings _settings;
        private InputSettings _inputSettings;
        private IMap _map;
        private PlacementVisuals _visuals;
        private INodeMap _nodeMap;
        private NeighbourManager _neighbourManager;
        private Camera _mainCamera;
        private GameObject _floorDecal;
        private PlacementState _state;


        public DragManagerBuilder WithSettings(PlacementSettings settings)
        {
            _settings = settings;
            return this;
        }

        public DragManagerBuilder WithInputSettings(InputSettings inputSettings)
        {
            _inputSettings = inputSettings;
            return this;
        }
        
        public DragManagerBuilder WithMap(IMap map)
        {
            _map = map;
            return this;
        }

        public DragManagerBuilder WithVisuals(PlacementVisuals visuals)
        {
            _visuals = visuals;
            return this;
        }
        
        public DragManagerBuilder WithNodeMap(INodeMap nodeMap)
        {
            _nodeMap = nodeMap;
            return this;
        }
        
        public DragManagerBuilder WithNeighbourManager(NeighbourManager neighbourManager)
        {
            _neighbourManager = neighbourManager;
            return this;
        }
        
        public DragManagerBuilder WithCamera(UnityEngine.Camera camera)
        {
            _mainCamera = camera;
            return this;
        }
        
        public DragManagerBuilder WithFloorDecal(GameObject floorDecal)
        {
            _floorDecal = floorDecal;
            return this;
        }

        public DragManagerBuilder WithState(PlacementState state)
        {
            _state = state;
            return this;
        }
        
        private void ValidateRequiredComponents()
        {
            var missingComponents = new List<string>();
            if (_settings == null) missingComponents.Add("PlacementSettings");
            if (_inputSettings == null) missingComponents.Add("InputSettings");
            if (_map == null) missingComponents.Add("IMap");
            if (_visuals == null) missingComponents.Add("PlacementVisuals");
            if (_nodeMap == null) missingComponents.Add("NodeMap");
            if (_neighbourManager == null) missingComponents.Add("NeighbourManager");
            if (_floorDecal == null) missingComponents.Add("FloorDecal");
            if (_state == null) missingComponents.Add("PlacementState");

            if (missingComponents.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot build DragSession. Missing required components: {string.Join(", ", missingComponents)}");
            }
        }

        public DragManager Build()
        {
            ValidateRequiredComponents();
            return new DragManager(_settings, _inputSettings, _map, _visuals, _nodeMap, _neighbourManager, _mainCamera, _floorDecal, _state);
        }
    }
}