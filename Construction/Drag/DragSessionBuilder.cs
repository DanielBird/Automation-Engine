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
    public class DragSessionBuilder
    {
        private PlacementSettings _settings;
        private InputSettings _inputSettings;
        private IMap _map;
        private INodeMap _nodeMap;
        private PlacementVisuals _visuals;
        private Camera _mainCamera;
        private GameObject _floorDecal;
        private PlacementState _state;
        
        public DragSessionBuilder WithSettings(PlacementSettings settings)
        {
            _settings = settings;
            return this;
        }
        
        public DragSessionBuilder WithInputSettings(InputSettings inputSettings)
        {
            _inputSettings = inputSettings;
            return this;
        }

        public DragSessionBuilder WithMap(IMap map)
        {
            _map = map;
            return this;
        }

        public DragSessionBuilder WithNodeMap(INodeMap map)
        {
            _nodeMap = map;
            return this;
        }
        
        public DragSessionBuilder WithVisuals(PlacementVisuals visuals)
        {
            _visuals = visuals;
            return this;
        }

        public DragSessionBuilder WithCamera(UnityEngine.Camera camera)
        {
            _mainCamera = camera;
            return this;
        }

        public DragSessionBuilder WithFloorDecal(GameObject floorDecal)
        {
            _floorDecal = floorDecal;
            return this;
        }

        public DragSessionBuilder WithState(PlacementState state)
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
            if (_nodeMap == null) missingComponents.Add("INodeMap");
            if (_visuals == null) missingComponents.Add("PlacementVisuals");
            if (_mainCamera == null) missingComponents.Add("Camera");
            if (_floorDecal == null) missingComponents.Add("FloorDecal");
            if (_state == null) missingComponents.Add("PlacementState");

            if (missingComponents.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot build DragSession. Missing required components: {string.Join(", ", missingComponents)}");
            }
        }

        public DragSession Build()
        {
            ValidateRequiredComponents();

            return new DragSession(
                _settings,
                _inputSettings,
                _map,
                _nodeMap,
                _visuals,
                _mainCamera,
                _floorDecal,
                _state
            );
        }
    }
}