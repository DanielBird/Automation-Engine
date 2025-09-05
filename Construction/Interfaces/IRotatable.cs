using Engine.Construction.Placement;

namespace Engine.Construction.Interfaces
{
    public interface IRotatable
    {
        void Rotate();
        void Rotate(Direction direction);
        void RotateInstant(Direction direction);
    }
}