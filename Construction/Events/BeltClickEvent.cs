using UnityEngine;
using Utilities.Events;
using Utilities.Events.Types;

namespace Construction.Events
{
    public class BeltClickEvent : IEvent
    {
        public Vector3Int WorldPosition { get; set; }
        public BuildRequestType BuildRequestType { get; set; }

        public BeltClickEvent(Vector3Int worldPosition, BuildRequestType buildRequestType)
        {
            WorldPosition = worldPosition;
            BuildRequestType = buildRequestType;
        }
    }
}