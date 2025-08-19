using Engine.Construction.Placement;

namespace Engine.Construction.Interfaces
{
    public interface IRotatable
    {
        void Rotate(bool updateTarget = true);
        void Rotate(Direction direction, bool updateTarget = true);
        void RotateInstant(Direction direction, bool updateTarget = true);
    }
}