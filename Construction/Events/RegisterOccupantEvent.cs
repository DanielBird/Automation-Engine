using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Events
{
    public class RegisterOccupantEvent : IEvent
    {
        public Vector3 WorldPosition { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public GameObject Occupant { get; private set; }

        public RegisterOccupantEvent(Vector3 worldPosition, int gridWidth, int gridHeight, GameObject occupant)
        {
            WorldPosition = worldPosition;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            Occupant = occupant;
        }
    }
}
