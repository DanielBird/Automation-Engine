using System.Collections.Generic;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
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