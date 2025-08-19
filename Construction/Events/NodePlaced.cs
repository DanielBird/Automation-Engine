using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
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