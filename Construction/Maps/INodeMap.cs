using System.Collections.Generic;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Maps
{
    internal interface INodeMap
    {
        public HashSet<Node> GetNodes();
        public bool NodeIsRegistered(Node node);
        public bool NodeIsRegisteredAt(Node node, int x, int z);
        public bool TryRegisterNodeAt(Node node, int x, int z);
        public bool TryDeregisterNode(Node node);
        public bool TryGetNode(int x, int z, out Node node);
        public bool HasNode(int x, int z);
        public bool GetNeighbour(int x, int z, Direction direction, out Node node);
        public bool GetNeighbourAt(Vector2Int position, out Node node);
    }
}