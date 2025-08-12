using Construction.Utilities;
using UnityEngine;
using Grid = Construction.Utilities.Grid;

namespace Construction.Maps
{
    /// <summary>
    /// A class for entities that occupy space on the map but that are not placed by the player
    /// E.g., buildings, fences, barriers, 
    /// They should block construction at their grid coordinate and so they should register with the Map  
    /// </summary>
    public class StaticMapOccupant : MonoBehaviour
    {
        public int gridWidth = 1; 
        public int gridHeight = 1;
        public Map map;
        
        private void Start()
        {
            if (map == null)
                map = GetComponentInParent<Map>();

            if (map == null)
            {
                Debug.LogError("Unable to find map on " + name);
                return;
            }
            
            Vector3Int gridCoord = Grid.WorldToGridCoordinate(transform.position, new GridParams(map.MapOrigin, map.MapWidth, map.MapHeight, map.settings.tileSize));
            
            map.RegisterOccupant(gridCoord.x, gridCoord.z, gridWidth, gridHeight);
        }
    }
}