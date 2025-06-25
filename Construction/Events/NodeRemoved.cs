using Construction.Nodes;
using Events;

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