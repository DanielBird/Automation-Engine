using Engine.Construction.Belts;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Widgets
{
    public interface IWidgetMover
    {
        Coroutine Move(
            Belt next,
            Direction direction = Direction.North);
    }
}