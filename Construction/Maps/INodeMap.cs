using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Maps
{
    public interface INodeMap
    {
        public void RegisterNode(Node node);

        public void DeregisterNode(Node node);

        public void CheckNode(int x, int z);

        public bool GetNode(int x, int z, out Node node);

        public bool GetNeighbour(int x, int z, Direction direction, out Node node);

        public bool GetNeighbourAt(Vector2Int position, out Node node);
    }
}