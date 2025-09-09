using System.Collections.Generic;
using UnityEngine;

namespace Engine.Construction.Utilities
{
    public static class Grid
    {
        /// <summary>
        /// Converts a world position to a grid-aligned world position, centered on the nearest tile.
        /// </summary>
        public static Vector3Int GridAlignedWorldPosition(Vector3 position, GridParams gridParams)
        {
            Vector3Int gridPos = WorldToGridCoordinate(position, gridParams);
            Vector3Int worldPosition = GridToWorldPosition(gridPos, gridParams.Origin, gridParams.CellSize); 
            return worldPosition;
        }
        
        /// <summary>
        /// Converts a world position to grid coordinate, within the map bounds
        /// </summary>
        public static Vector3Int WorldToGridCoordinate(Vector3 position, GridParams gp)
        {
            Vector3 relative = position - gp.Origin;

            int gridX = Mathf.FloorToInt(relative.x / gp.CellSize);
            int gridZ = Mathf.FloorToInt(relative.z / gp.CellSize);

            gridX = Mathf.Clamp(gridX, 0, gp.Width - 1);
            gridZ = Mathf.Clamp(gridZ, 0, gp.Height - 1);

            return new Vector3Int(gridX, 0, gridZ);
        }
        
        /// <summary>
        /// Converts a grid coordinate to a world position, aligned to the grid.
        /// </summary>
        public static Vector3Int GridToWorldPosition(Vector3Int gridCoord, Vector3Int gridOrigin, float cellSize)
        {
            int worldX = Mathf.FloorToInt(gridOrigin.x + (gridCoord.x * cellSize) + (cellSize * 0.5f));
            int worldZ = Mathf.FloorToInt(gridOrigin.z + (gridCoord.z * cellSize) + (cellSize * 0.5f));
            return new Vector3Int(worldX, 0, worldZ);
        }

        /// <summary>
        /// Converts a list of grid coordinates to world positions.
        /// </summary>
        public static List<Vector3Int> GridToWorldPositions(List<Vector3Int> gridPositions, Vector3Int gridOrigin, float cellSize)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>(gridPositions.Count);
            float halfTileSize = cellSize * 0.5f;
            float originX = gridOrigin.x;
            float originZ = gridOrigin.z;

            foreach (var gridPos in gridPositions)
            {
                int worldX = Mathf.FloorToInt(originX + (gridPos.x * cellSize) + halfTileSize);
                int worldZ = Mathf.FloorToInt(originZ + (gridPos.z * cellSize) + halfTileSize);
                worldPositions.Add(new Vector3Int(worldX, 0, worldZ));
            }

            return worldPositions;
        }
        
        public static bool IsStraightLine(Vector3Int a, Vector3Int b)
        {
            return a.x == b.x || a.z == b.z;
        }
        
        /// <summary>
        /// Get 4 cardinal neighbors within map bounds
        /// </summary>
        public static IEnumerable<Vector3Int> GetNeighbours(Vector3Int c, int step, int mapWidth, int mapHeight)
        {
            // 4-connected moves by step
            int nx;
            nx = c.x + step; if (nx >= 0 && nx < mapWidth) yield return new Vector3Int(nx, 0, c.z);
            nx = c.x - step; if (nx >= 0 && nx < mapWidth) yield return new Vector3Int(nx, 0, c.z);

            int nz;
            nz = c.z + step; if (nz >= 0 && nz < mapHeight) yield return new Vector3Int(c.x, 0, nz);
            nz = c.z - step; if (nz >= 0 && nz < mapHeight) yield return new Vector3Int(c.x, 0, nz);
        }
    }
}