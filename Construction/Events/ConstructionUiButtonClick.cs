using Construction.Nodes;
using Utilities.Events;

namespace Construction.Events
{
    public class ConstructionUiButtonClick: IEvent
    {
        public NodeType RequestType { get; private set; }
        
        public ConstructionUiButtonClick(NodeType requestType)
        {
            RequestType = requestType;
        }
    }
}