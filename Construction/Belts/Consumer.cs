using Engine.Construction.Events;
using Engine.Construction.Resources;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine.Events;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A belt that despawns widgets transported around a belt network.
    /// Widgets are automatically despawned when they arrive at the Consumer.
    /// Widget arrival triggers the OnWidgetCollected event.
    /// </summary>
    public class Consumer : Belt
    {
        public UnityEvent<ResourceTypeSo> onConsumed;
        
        // Consumers can ship if they have an occupant
        public override bool ReadyToShip(out Belt target, out Resource resource)
        {
            target = this; 
            resource = null;
            
            if (!IsOccupied)
            {
                return false;
            }
            
            if (!CanShip(out resource)) 
                return false;
            
            return true; 
        }
        
        // Ship = despawn occupant
        public override void Ship(Belt target, Resource resource)
        {
            if(resource == null)
                return;
            
            EventBus<ResourceCollected>.Raise(new ResourceCollected(resource.ResourceIndex));
            onConsumed?.Invoke(resource.ResourceType);
            
            SimplePool.Despawn(resource.gameObject);
            Occupant = null;
        }
        
        public override void OnPlayerSelect()
        {
            // Do nothing.
            // Consumers don't have paths of nodes leading away from them.
        }
    }
}