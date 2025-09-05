namespace Engine.Utilities.Events.Types
{
    public class PlayerDragEvent : IEvent
    {
        public readonly bool Started;

        public PlayerDragEvent(bool started)
        {
            Started = started;
        }
    }
}