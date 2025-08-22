using Engine.Construction.Events;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Maps
{
    /// <summary>
    /// A class for entities that occupy space on the map but that are not placed by the player
    /// E.g., buildings, fences, barriers, 
    /// They should block construction at their grid coordinate and so they should register with the Map  
    /// </summary>
    public class StaticMapOccupant : MonoBehaviour
    {
        public int gridWidth = 1; 
        public int gridHeight = 1;
        
        private void Start()
        {
            EventBus<RegisterOccupantEvent>.Raise(
                new RegisterOccupantEvent(transform.position, gridWidth, gridHeight, gameObject)
            );
        }
    }
}