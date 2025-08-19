using Engine.Construction.Nodes;
using Engine.Utilities.Events;

namespace Engine.Construction.Events
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