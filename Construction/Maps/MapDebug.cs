using System.Collections.Generic;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Maps
{
    public class MapDebug : MonoBehaviour
    {
        public MapManager mapManager;
        private IMap map; 
        private INodeMap nodeMap;
        private IResourceMap resourceMap;
        
        public PlacementSettings placementSettings;
        public int tileSize = 2;

        private Vector3 _cubeSize; 
        
        [Header("Debug")]
        public bool showOccupancy;
        public bool showResources;
        public bool showPaths;
        
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
            nodeMap = mapManager.NodeMap;
            resourceMap = mapManager.ResourceMap;
        }
        
        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            ShowOccupancy();
            ShowResources();
            ShowPaths();
            #endif
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

        private void ShowPaths()
        {
            if (!showPaths) return;
            if (nodeMap == null) return; 
            
            GUIStyle style = new() { normal = { textColor = Color.magenta } };
            Vector3Int offset = new Vector3Int(0, 1, 0);
            
            foreach (Node node in nodeMap.GetNodes())
            {
                Handles.Label(node.transform.position + offset, node.PathId.ToString(), style);
            }
        }

        [Button]
        private void CheckForNullNodes()
        {
            int nullNodes = 0;
            foreach (Node n in nodeMap.GetNodes())
            {
                if (n == null) nullNodes++;
            }
            
            Debug.Log("Null nodes found: " + nullNodes);
        }
    }
}