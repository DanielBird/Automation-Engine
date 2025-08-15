using Construction.Events;
using Construction.Nodes;
using UnityEngine;
using Utilities.Events;

namespace Construction.UI
{
    public class ButtonEventTrigger : MonoBehaviour
    {
        public NodeType requestedNodeType;
        
        public void TriggerEvent()
        {
            EventBus<ConstructionUiButtonClick>.Raise(new ConstructionUiButtonClick(requestedNodeType));
        }
    }
}