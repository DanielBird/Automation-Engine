using Engine.Construction.Placement;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Maps
{
    public class MapDebug : MonoBehaviour
    {
        public MapManager mapManager;
        private Map map; 
        
        public PlacementSettings placementSettings;
        public int tileSize = 2;

        private Vector3 _cubeSize; 
        
        private void Awake()
        {
            _cubeSize = new Vector3(tileSize, tileSize, tileSize);
            
            if(mapManager == null)
                Debug.LogError("MapDebug: the Map Manager is null");
            
            if(placementSettings == null)
                Debug.LogError("MapDebug: placementSettings is null");
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return; 
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
    }
}