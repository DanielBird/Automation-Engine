using Engine.Construction.Nodes;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
{
    public class BeltClickEvent : IEvent
    {
        public Vector3Int WorldPosition;
        public readonly NodeType BuildRequestType;
        public Node RequestingNode;
        public BeltClickEvent(Vector3Int worldPosition, NodeType buildRequestType, Node requestingNode)
        {
            WorldPosition = worldPosition;
            BuildRequestType = buildRequestType;
            RequestingNode = requestingNode;
        }
    }
}