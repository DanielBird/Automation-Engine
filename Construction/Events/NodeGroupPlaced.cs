using System.Collections.Generic;
using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    // Called when the player creates a path of nodes during a drag session
    public class NodeGroupPlaced : IEvent
    {
        public readonly HashSet<Node> NodeGroup;
        public readonly Node StartNode;
        public readonly Node EndNode;
        public readonly int PathId;

        public NodeGroupPlaced(HashSet<Node> nodeGroup, Node startNode, Node endNode, int pathId)
        {
            NodeGroup = nodeGroup;
            StartNode = startNode;
            EndNode = endNode;
            PathId = pathId;
        }
    }
}