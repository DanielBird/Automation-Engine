using UnityEngine;

namespace Engine.Construction.Maps
{
    internal interface IOccupancyMap
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public CellStatus[,] Grid { get; set; }
        public bool TryPlaceOccupant(int x, int z, int width, int height);
        public void RemoveOccupant(int x, int z, int width, int height);
        public bool VacantSpace(int x, int z, int width, int height);
        public bool VacantCell(int x, int z);
        public Vector2Int NearestVacantCell(Vector2Int start);
        public bool InBounds(int x, int y);
    }
}