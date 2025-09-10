using System;
using Engine.Construction.Belts;
using Engine.Construction.Interaction;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Resources;
using Engine.Construction.Visuals;
using Engine.GameState;
using Engine.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Placement
{
    /// <summary>
    /// Initializes the Automation Engine.
    /// Ideally should be set early in the Script Execution Order (Edit > Project settings > Script Execution Order).  
    /// </summary>
    
    public class ConstructionEngine : MonoBehaviour
    {
        public IWorld World { get; private set; }
        public IResourceMap ResourceMap { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private PlacementSettings placementSettings;
        [SerializeField] private InputSettings inputSettings;
        [SerializeField] private PlacementVisualSettings visualSettings;
        
        [Header("Placement Highlight")] 
        [Tooltip("A game object that acts as a visual indicator of the current placement position.")]
        public GameObject placementHighlight;
        
        [Header("Camera")]
        public Camera mainCamera;
        
        [Header("Resources")]
        public Transform resourceParent; 
        
        [Header("Belt Settings")]
        [Tooltip("Time between attempts to push all belts forwards.")]
        [SerializeField] private float tickForwardFrequency = 1.2f;
        [Tooltip("Should the belt network start automatically?")]
        [SerializeField] private bool runOnStart = true;

        [Header("Belt Status")] 
        public bool beltActive;
        public float timeOfLastBeltTick; 
        
        private PlacementState placementState;
        private PlacementVisuals _placementVisuals;
        private RemovalVisuals _removalVisuals; 
        private PlacementContext _context; 
        private PlacementManager _placementManager;
        private RemovalManager _removalManager;
        private NodeRelationshipManager _relationshipManager;
        private NodePathManager _pathManager;
        private BeltManager _beManager;

        private void Awake()
        {
            InitialiseProperties();                                     // creates World and Resource Map.
            CreatePlacementVisuals();                                   // creates Placement Visuals.
            CreatePlacementContext();   // requires Visuals.            // creates Placement State and Context.
            CreateManagers();           // requires Placement Context.  // creates Managers (Placement, Removal, Relationships, Paths).
            CreateRemovalVisuals();     // requires Managers.
        }

        private void InitialiseProperties()
        {
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
            
            World = new World(placementSettings);
            ResourceMap = new ResourceMap(placementSettings);
        }

        private void CreatePlacementVisuals()
        {
            _placementVisuals = new PlacementVisuals(this, World, placementHighlight, visualSettings);
        }
        
        private void CreatePlacementContext()
        {
            placementState = new PlacementState(); 
            
            _context = new PlacementContext(
                World,
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
            _beManager = new BeltManager(this, tickForwardFrequency, runOnStart); 
        }

        private void CreateRemovalVisuals()
        {
            _removalVisuals = new RemovalVisuals(inputSettings, _removalManager, visualSettings, transform);
        }

        private void Start()
        {
            _beManager.Start();
        }

        private void Update()
        {
            _placementManager.Tick();
        }

        private void OnDisable()
        {
            World.Disable();
            ResourceMap.Disable();
            
            _placementManager.Disable();
            _removalManager.Disable();
            _placementVisuals.Disable();
            _removalVisuals.Disable();
            _relationshipManager.Disable();
            _pathManager.Disable();
            _beManager.Disable();
        }
        
        // Requires Odin Inspector 
        
        [Button]
        public void CheckMapOccupancy(int x, int z, int width, int height)
        {
            string s = World.VacantSpace(x, z, width, height) ? "Space Empty" : "Space Occupied";
            Debug.Log(s);
        }
        
        [Button]
        public void CheckCellOccupancy(int x, int z)
        {
            string s = World.VacantCell(x, z) ? "Cell Empty" : "Cell Occupied";
            Debug.Log(s);
        }
        
        [Button]
        public void CheckNode(int x, int z)
        {
            string s = World.TryGetNode(x, z, out Node node) ? $"{node.name} was found" : $"no node found at {x} _ {z}";
            Debug.Log(s);
        }

        [Button]
        public void CheckResource(int x, int z)
        {
            string s = ResourceMap.TryGetResourceSourceAt(new Vector3Int(x, 0, z), out IResourceSource src) ? $"{src.ResourceType.resourceName} was found" : $"no node found at {x} _ {z}";
            Debug.Log(s);
        }
        
        [Button]
        private void ShowPlacementVisuals() => _placementVisuals.ShowPlacementVisuals(Vector3Int.zero);
        
        [Button]
        private void HidePlacementVisuals() => _placementVisuals.HidePlacementVisuals();
        
        [Button]
        private void StartBelts() => _beManager.Run();
        
        [Button]
        private void StopBelts() => _beManager.Stop();
    }
}