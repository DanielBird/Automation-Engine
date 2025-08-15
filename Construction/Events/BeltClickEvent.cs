using Construction.Nodes;
using UnityEngine;
using Utilities.Events;

namespace Construction.Events
{
    public class BeltClickEvent : IEvent
    {
        public Vector3Int WorldPosition;
        public readonly NodeType BuildRequestType;
        public BeltClickEvent(Vector3Int worldPosition, NodeType buildRequestType)
        {
            WorldPosition = worldPosition;
            BuildRequestType = buildRequestType;
        }
    }
}