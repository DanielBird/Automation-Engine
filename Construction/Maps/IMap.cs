using UnityEngine;

namespace Engine.Construction.Maps
{
    public interface IMap
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        
        public bool RegisterOccupant(int x, int z, int width, int height);

        public void DeregisterOccupant(int x, int z, int width, int height);

        public bool VacantSpace(int x, int z, int width, int height);

        public bool VacantCell(int x, int z);

        public Vector2Int NearestVacantCell(Vector2Int start);

        public void Disable();
    }
}