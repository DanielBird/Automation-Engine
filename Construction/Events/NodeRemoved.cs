using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    public class NodeRemoved : IEvent
    {
        public Node Node { get; private set; }

        public NodeRemoved(Node node)
        {
            Node = node;
        }
    }
}