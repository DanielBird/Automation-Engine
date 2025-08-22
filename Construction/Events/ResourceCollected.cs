using Engine.Utilities.Events;

namespace Engine.Construction.Events
{
    public class ResourceCollected : IEvent
    {
        public int ResourceType { get; private set; }

        public ResourceCollected(int resourceType)
        {
            ResourceType = resourceType;
        }
    }
}