using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.UI
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