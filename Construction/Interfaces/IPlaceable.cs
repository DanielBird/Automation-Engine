using Construction.Maps;
using UnityEngine;

namespace Construction.Interfaces
{
    public interface IPlaceable
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Draggable { get; set; }
        public Vector3Int Position { get; set; }

        public void Place(Vector3Int position, INodeMap map);

        public void FailedPlacement();

        public void Reset();
        
        public Vector2Int GetSize(); 
    }
}