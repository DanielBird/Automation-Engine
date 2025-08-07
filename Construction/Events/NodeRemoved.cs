using Construction.Nodes;
using Utilities.Events;

namespace Construction.Events
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