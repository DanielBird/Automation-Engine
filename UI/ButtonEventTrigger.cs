using UnityEngine;
using Utilities.Events;
using Utilities.Events.Types;

namespace UI
{
    public class ButtonEventTrigger : MonoBehaviour
    {
        public UiButtonType buttonType;
        
        public void TriggerEvent()
        {
            Debug.Log("Raise Ui Event");
            EventBus<UiButtonClick>.Raise(new UiButtonClick(buttonType));
        }
    }
}