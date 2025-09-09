using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    // Called when the player places a single node
    public class NodePlaced : IEvent 
    {
        public Node Node { get; private set; }

        public NodePlaced(Node node)
        {
            Node = node;
        }
    }
}