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
        public ConstructionEngine constructionEngine;
        private IWorld world;
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

            if (constructionEngine == null)
            {
                Debug.LogError("MapDebug: the Construction Engine is null");
                return;
            }

            if (placementSettings == null)
            {
                Debug.LogError("MapDebug: placementSettings is null");
                return;
            }

            world = constructionEngine.World; 
            resourceMap = constructionEngine.ResourceMap;
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
            if(world == null) return;

            Vector2Int dimensions = world.MapDimensions();
            int width = dimensions.x;
            int height = dimensions.y;
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Gizmos.color = world.Grid()[i, j] == CellStatus.Empty ? Color.green : Color.red;
                    
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
            if (world == null) return; 
            
            GUIStyle style = new() { normal = { textColor = Color.magenta } };
            Vector3Int offset = new Vector3Int(0, 1, 0);
            
            foreach (Node node in world.GetNodes())
            {
                Handles.Label(node.transform.position + offset, node.PathId.ToString(), style);
            }
        }

        [Button]
        private void CheckForNullNodes()
        {
            int nullNodes = 0;
            foreach (Node n in world.GetNodes())
            {
                if (n == null) nullNodes++;
            }
            
            Debug.Log("Null nodes found: " + nullNodes);
        }
    }
}