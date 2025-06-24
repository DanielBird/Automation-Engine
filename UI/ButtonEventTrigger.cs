using Events;
using Events.Types;
using UnityEngine;

namespace UI
{
    public class ButtonEventTrigger : MonoBehaviour
    {
        public UiButtonType buttonType;
        
        public void TriggerEvent()
        {
            EventBus<UiButtonClick>.Raise(new UiButtonClick(buttonType));
        }
    }
}