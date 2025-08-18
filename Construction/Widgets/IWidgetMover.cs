using Construction.Belts;
using Construction.Placement;
using UnityEngine;

namespace Construction.Widgets
{
    public interface IWidgetMover
    {
        Coroutine Move(
            Belt next,
            Direction direction = Direction.North);
    }
}