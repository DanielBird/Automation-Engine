using Engine.Construction.Nodes;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
{
    public class BeltClickEvent : IEvent
    {
        public Vector3Int GridCoord;
        public readonly NodeType BuildRequestType;
        public Node RequestingNode;
        public BeltClickEvent(Vector3Int gridCoord, NodeType buildRequestType, Node requestingNode)
        {
            GridCoord = gridCoord;
            BuildRequestType = buildRequestType;
            RequestingNode = requestingNode;
        }
    }
}