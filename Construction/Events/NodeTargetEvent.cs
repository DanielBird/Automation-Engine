using Construction.Nodes;
using Utilities.Event_Bus;

namespace Construction.Events
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