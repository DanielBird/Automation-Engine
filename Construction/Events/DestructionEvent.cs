using System.Collections.Generic;
using UnityEngine;
using Utilities.Events;

namespace Construction.Events
{
    public enum DestructionEventType {Cancel, DestroyNode, DestroyEmpty}
    
    public class DestructionEvent : IEvent
    {
        public IEnumerable<Vector3Int> Positions;
        public DestructionEventType Type;

        public DestructionEvent(IEnumerable<Vector3Int> positions, DestructionEventType type)
        {
            Positions = positions;
            Type = type;
        }
    }
}