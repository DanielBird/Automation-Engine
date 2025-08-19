using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public interface INodeMap
    {
        public void RegisterNode(Node node);

        public void DeregisterNode(Node node);

        public void CheckNode(int x, int z);

        public bool TryGetNode(int x, int z, out Node node);
        
        public bool HasNode(int x, int z);

        public bool GetNeighbour(int x, int z, Direction direction, out Node node);

        public bool GetNeighbourAt(Vector2Int position, out Node node);
        
        public bool InBounds(int x, int y);
    }
}