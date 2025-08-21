using System.Collections.Generic;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
{
    public enum DestructionEventType {Cancel, DestroyNode, DestroyEmpty, ClearAll}
    
    public class DestructionEvent : IEvent
    {
        public IEnumerable<Vector3> Positions;
        public DestructionEventType Type;

        public DestructionEvent(IEnumerable<Vector3> positions, DestructionEventType type)
        {
            Positions = positions;
            Type = type;
        }
    }
}