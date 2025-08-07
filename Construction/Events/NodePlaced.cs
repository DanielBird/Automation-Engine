using Construction.Nodes;
using Utilities.Events;

namespace Construction.Events
{
    public class NodePlaced : IEvent 
    {
        public Node Node { get; private set; }

        public NodePlaced(Node node)
        {
            Node = node;
        }
    }
}