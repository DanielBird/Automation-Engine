using Engine.Construction.Resources;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
{
    public class RegisterResourceEvent : IEvent
    {
        public IResourceSource Source { get; private set; }
        public Vector3 Position { get; private set; }

        public RegisterResourceEvent(IResourceSource src, Vector3 pos)
        {
            Source = src;
            Position = pos;
        }
    }
}