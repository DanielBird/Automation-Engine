using System;
using Engine.Construction.Interaction;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Visuals;
using Engine.GameState;
using Engine.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Placement
{
    [RequireComponent(typeof(MapManager))]
    public class ConstructionEngine : MonoBehaviour
    {
        private IMap Map;
        private INodeMap NodeMap;
        private IResourceMap ResourceMap;
        
        [Header("Settings")]
        [SerializeField] private PlacementSettings placementSettings;
        [SerializeField] private InputSettings inputSettings;
        private PlacementState placementState;
        
        [Header("Visuals - Placement")] 
        public GameObject floorDecal;
        
        private PlacementVisuals _placementVisuals;
        private RemovalVisuals _removalVisuals; 
        
        [Space]
        public float placementTime = 0.6f;
        public Vector3 startingScale = Vector3.zero;
        public Vector3 endScale = Vector3.one; 
        
        private EasingFunctions.Function _scaleUpEasing; 
        private EasingFunctions.Function _scaleDownEasing; 
        public EasingFunctions.Ease scaleEasingFunction = EasingFunctions.Ease.EaseOutElastic;
        public EasingFunctions.Ease scaleDownEasingFunction = EasingFunctions.Ease.Linear;
        
        [Header("Visuals - Floor Material")]
        public Material floorMaterial;
        public float lerpAlphaTime = 1f;
        public float minGridAlpha = 0; 
        public float maxGridAlpha = 1;
        
        [Header("Visuals - Removal")]
        public Material destructionIndicatorMaterial;
        public float yOffset = 0.01f;  
        public Vector2 gridOffset = new (0.5f, 0.5f);
        public float lerpSpeed = 35f;
        
        [Header("Camera")]
        public Camera mainCamera;
        
        [Header("Resources")]
        public Transform resourceParent; 

        private PlacementContext _context; 
        private PlacementManager _placementManager;
        private RemovalManager _removalManager;
        private NodeRelationshipManager _relationshipManager;
        private NodePathManager _pathManager;

        private void Awake()
        {
            InitialiseProperties();
            CreatePlacementVisuals();
            CreatePlacementContext();   // requires Visuals
            CreateManagers();           // requires Placement Context
            CreateRemovalVisuals();     // requires Managers
        }

        private void InitialiseProperties()
        {
            MapManager mapManager = GetComponent<MapManager>();
            if(!mapManager.Initialised) mapManager.Initialise();
            
            Map = mapManager.Map;
            NodeMap = mapManager.NodeMap;
            ResourceMap = mapManager.ResourceMap;
            
            if (placementSettings == null)
            {
                Debug.LogWarning("Missing placement settings");
                placementSettings = ScriptableObject.CreateInstance<PlacementSettings>();
            }

            if (inputSettings == null)
            {
                Debug.LogWarning("Missing input settings");
                inputSettings = ScriptableObject.CreateInstance<InputSettings>();
            }
        }

        private void CreatePlacementVisuals()
        {
            _scaleUpEasing = EasingFunctions.GetEasingFunction(scaleEasingFunction);
            _scaleDownEasing = EasingFunctions.GetEasingFunction(scaleDownEasingFunction);
            
            if (floorMaterial == null)
            {
                Debug.LogError("Missing floor material");
            }

            _placementVisuals = new PlacementVisuals(
                this,
                NodeMap,
                floorDecal, 
                placementTime, 
                startingScale, 
                endScale, 
                _scaleUpEasing, 
                _scaleDownEasing, 
                floorMaterial, 
                lerpAlphaTime,
                minGridAlpha,
                maxGridAlpha);
        }
        
        private void CreatePlacementContext()
        {
            placementState = new PlacementState(); 
            
            _context = new PlacementContext(
                Map,
                NodeMap,
                ResourceMap,
                inputSettings,
                placementSettings,
                placementState,
                _placementVisuals,
                mainCamera
            );
        }
        
        private void CreateManagers()
        {
            _placementManager = new PlacementManager(_context, transform, resourceParent); 
            _removalManager = new RemovalManager(_context);
            _relationshipManager = new NodeRelationshipManager(); 
            _pathManager = new NodePathManager(_relationshipManager);
        }

        private void CreateRemovalVisuals()
        {
            _removalVisuals = new RemovalVisuals(
                inputSettings,
                _removalManager,
                destructionIndicatorMaterial,
                yOffset,
                lerpSpeed,
                gridOffset,
                transform
            );
        }

        private void Update()
        {
            _placementManager.Tick();
        }

        private void OnDisable()
        {
            _placementManager.Disable();
            _removalManager.Disable();
            _placementVisuals.Disable();
            _removalVisuals.Disable();
            _relationshipManager.Disable();
            _pathManager.Disable();
        }
        
        [Button]
        private void ShowPlacementVisuals() => _placementVisuals.ShowPlacementVisuals();
        
        [Button]
        private void HidePlacementVisuals() => _placementVisuals.HidePlacementVisuals();
    }
}