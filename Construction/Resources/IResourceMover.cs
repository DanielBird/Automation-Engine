using Engine.Construction.Belts;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Resources
{
    public interface IResourceMover
    {
        Coroutine Move(
            Belt next,
            Direction direction = Direction.North);
    }
}