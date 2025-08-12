using UnityEngine;
using Utilities.Events;
using Utilities.Events.Types;

namespace UI
{
    public class ButtonEventTrigger : MonoBehaviour
    {
        public BuildRequestType requestType;
        
        public void TriggerEvent()
        {
            EventBus<UiButtonClick>.Raise(new UiButtonClick(requestType));
        }
    }
}