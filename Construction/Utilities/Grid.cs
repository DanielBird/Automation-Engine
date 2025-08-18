using System;
using System.Collections.Generic;
using System.Linq;
using Construction.Maps;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public static class Grid
    {
        /// <summary>
        /// Converts a world position to a grid-aligned world position, centered on the nearest tile.
        /// </summary>
        public static Vector3Int GridAlignedWorldPosition(Vector3 position, GridParams gridParams)
        {
            Vector3Int gridPos = WorldToGridCoordinate(position, gridParams);
            Vector3Int worldPosition = GridToWorldPosition(gridPos, gridParams.Origin, gridParams.TileSize); 
            return worldPosition;
        }
        
        /// <summary>
        /// Converts a world position to grid coordinate, within the map bounds
        /// </summary>
        public static Vector3Int WorldToGridCoordinate(Vector3 position, GridParams gp)
        {
            Vector3 relative = position - gp.Origin;

            int gridX = Mathf.FloorToInt(relative.x / gp.TileSize);
            int gridZ = Mathf.FloorToInt(relative.z / gp.TileSize);

            gridX = Mathf.Clamp(gridX, 0, gp.Width - 1);
            gridZ = Mathf.Clamp(gridZ, 0, gp.Height - 1);

            return new Vector3Int(gridX, 0, gridZ);
        }
        
        /// <summary>
        /// Converts a grid coordinate to a world position, aligned to the grid.
        /// </summary>
        public static Vector3Int GridToWorldPosition(Vector3Int gridCoord, Vector3Int gridOrigin, float tileSize)
        {
            int worldX = Mathf.FloorToInt(gridOrigin.x + (gridCoord.x * tileSize) + (tileSize * 0.5f));
            int worldZ = Mathf.FloorToInt(gridOrigin.z + (gridCoord.z * tileSize) + (tileSize * 0.5f));
            return new Vector3Int(worldX, 0, worldZ);
        }

        /// <summary>
        /// Converts a list of grid coordinates to world positions.
        /// </summary>
        public static List<Vector3Int> GridToWorldPositions(List<Vector3Int> gridPositions, Vector3Int gridOrigin, float tileSize)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>(gridPositions.Count);
            float halfTileSize = tileSize * 0.5f;
            float originX = gridOrigin.x;
            float originZ = gridOrigin.z;

            foreach (var gridPos in gridPositions)
            {
                int worldX = Mathf.FloorToInt(originX + (gridPos.x * tileSize) + halfTileSize);
                int worldZ = Mathf.FloorToInt(originZ + (gridPos.z * tileSize) + halfTileSize);
                worldPositions.Add(new Vector3Int(worldX, 0, worldZ));
            }

            return worldPositions;
        }
        
        public static bool IsStraightLine(Vector3Int a, Vector3Int b)
        {
            return a.x == b.x || a.z == b.z;
        }
    }
}