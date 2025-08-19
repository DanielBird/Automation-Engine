using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    public class WidgetCollected : IEvent
    {
        public int WidgetType { get; private set; }

        public WidgetCollected(int widgetType)
        {
            WidgetType = widgetType;
        }
    }
}