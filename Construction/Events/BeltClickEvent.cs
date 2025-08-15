using Construction.Nodes;
using UnityEngine;
using Utilities.Events;
using Utilities.Events.Types;

namespace Construction.Events
{
    public class BeltClickEvent : IEvent
    {
        public Vector3Int WorldPosition;
        public readonly BuildRequestType BuildRequestType;
        public BeltClickEvent(Vector3Int worldPosition, BuildRequestType buildRequestType)
        {
            WorldPosition = worldPosition;
            BuildRequestType = buildRequestType;
        }
    }
}