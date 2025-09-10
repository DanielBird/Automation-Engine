using Engine.Construction.Maps;
using UnityEngine;

namespace Engine.Construction.Interfaces
{
    public interface IPlaceable
    {
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public Vector3Int GridCoord { get; set; }
        public bool Draggable { get; set; }

        public void Place(Vector3Int gridCoord, IWorld world);

        public void FailedPlacement(Vector3Int gridCoord);

        public void Reset();
        
        public Vector2Int GetSize(); 
    }
}