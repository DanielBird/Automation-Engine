using System;
using UnityEngine;
using Grid = Construction.Utilities.Grid;

namespace Construction.Maps
{
    public class MapDebug : MonoBehaviour
    {
        public Map map;
        public int tileSize = 2;

        private Vector3 _cubeSize; 
        
        private void Awake()
        {
            _cubeSize = new Vector3(tileSize, tileSize, tileSize);
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
                    
                    Vector3Int pos = Grid.GridToWorldPosition(new Vector3Int(i, 0, j), Vector3Int.zero, tileSize);
                    Gizmos.DrawWireCube(pos, _cubeSize);
                }
            }

        }
    }
}