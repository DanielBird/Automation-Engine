namespace Utilities.Events.Types
{
    public class UiButtonClick: IEvent
    {
        public BuildRequestType BuildRequestType { get; private set; }
        
        public UiButtonClick(BuildRequestType buildRequestType)
        {
            BuildRequestType = buildRequestType;
        }
    }
}