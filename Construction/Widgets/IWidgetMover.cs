using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Widgets
{
    public interface IWidgetMover
    {
        Coroutine Move(
            Vector3 target, 
            float moveTime, 
            Direction direction = Direction.North);
    }
}