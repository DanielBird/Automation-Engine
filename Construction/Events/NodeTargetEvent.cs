using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    public class NodeTargetEvent : IEvent
    {
        public Node Node { get; private set; }
        public Node Target { get; private set; }

        public NodeTargetEvent(Node node, Node target)
        {
            Node = node;
            Target = target;
        }
    }
}