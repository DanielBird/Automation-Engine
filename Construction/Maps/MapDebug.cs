using System.Collections.Generic;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Maps
{
    public class MapDebug : MonoBehaviour
    {
        public MapManager mapManager;
        private IMap map; 
        private IResourceMap resourceMap;
        
        public PlacementSettings placementSettings;
        public int tileSize = 2;

        private Vector3 _cubeSize; 
        
        [Header("Debug")]
        public bool showOccupancy;
        public bool showResources;
        
        private void Awake()
        {
            _cubeSize = new Vector3(tileSize, tileSize, tileSize);

            if (mapManager == null)
            {
                Debug.LogError("MapDebug: the Map Manager is null");
                return;
            }

            if (placementSettings == null)
            {
                Debug.LogError("MapDebug: placementSettings is null");
                return;
            }

            map = mapManager.Map; 
            resourceMap = mapManager.ResourceMap;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return;
            ShowOccupancy();
            ShowResources();
        }

        private void ShowOccupancy()
        {
            if(!showOccupancy) return;
            if(map == null) return;

            int width = map.MapWidth;
            int height = map.MapHeight;
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Gizmos.color = map.Grid[i, j] == CellStatus.Empty ? Color.green : Color.red;
                    
                    Vector3Int pos = Grid.GridToWorldPosition(new Vector3Int(i, 0, j), placementSettings.mapOrigin, placementSettings.cellSize);
                    Gizmos.DrawWireCube(pos, _cubeSize);
                }
            }
        }

        private void ShowResources()
        {
            if(!showResources) return;
            if(resourceMap == null) return;
            Gizmos.color = Color.green;

            foreach (KeyValuePair<Vector3Int, IResourceSource> pair in resourceMap.Sources)
            {
                Vector3Int pos = Grid.GridToWorldPosition(pair.Key, placementSettings.mapOrigin, placementSettings.cellSize);
                Gizmos.DrawWireCube(pos, _cubeSize);
            }
        }
    }
}