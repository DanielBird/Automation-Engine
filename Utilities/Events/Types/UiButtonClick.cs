namespace Utilities.Events.Types
{
    public enum UiButtonType {None, Belt, Producer }
    
    public class UiButtonClick: IEvent, global::Events.IEvent
    {
        public UiButtonType ButtonType { get; private set; }
        
        public UiButtonClick(UiButtonType buttonType)
        {
            ButtonType = buttonType;
        }
    }
}