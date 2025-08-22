using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Maps
{
    /// <summary>
    /// Initialises the different maps used for registering occupancy, resources, etc.
    /// Ideally should be set early in the Script Execution Order (Edit > Project settings > Script Execution Order).  
    /// </summary>
    
    public class MapManager : MonoBehaviour
    {
        public PlacementSettings placementSettings;
        public IMap Map {get; private set;}
        public INodeMap NodeMap {get; private set;}
        public IResourceMap ResourceMap {get; private set;}
        
        public bool Initialised {get; private set;}

        private void Awake()
        {
            Initialise();
        }

        public void Initialise()
        {
            if(Initialised) return;
            Map = new Map(placementSettings);
            NodeMap = new NodeMap(Map); 
            ResourceMap = new ResourceMap(placementSettings);
            Initialised = true;
        }

        [Button]
        public void CheckMapOccupancy(int x, int z, int width, int height)
        {
            string s = Map.VacantSpace(x, z, width, height) ? "Space Empty" : "Space Occupied";
            Debug.Log(s);
        }
        
        [Button]
        public void CheckCellOccupancy(int x, int z)
        {
            string s = Map.VacantCell(x, z) ? "Cell Empty" : "Cell Occupied";
            Debug.Log(s);
        }
        
        [Button]
        public void CheckNode(int x, int z)
        {
            string s = NodeMap.TryGetNode(x, z, out Node node) ? $"{node.name} was found" : $"no node found at {x} _ {z}";
            Debug.Log(s);
        }

        [Button]
        public void CheckResource(int x, int z)
        {
            string s = ResourceMap.TryGetResourceSourceAt(new Vector3Int(x, 0, z), out IResourceSource src) ? $"{src.ResourceType.resourceName} was found" : $"no node found at {x} _ {z}";
            Debug.Log(s);
        }

        private void OnDisable()
        {
            Map.Disable();
            ResourceMap.Disable();
        }
    }
}