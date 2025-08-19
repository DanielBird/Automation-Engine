using Engine.Construction.Events;
using Engine.Construction.Widgets;
using Engine.Utilities;
using Engine.Utilities.Events;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A belt that despawns widgets transported around a belt network.
    /// Widgets are automatically despawned when they arrive at the Consumer.
    /// Widget arrival triggers the OnWidgetCollected event.
    /// </summary>
    public class Consumer : Belt
    {
        // Consumers can ship if they have an occupant
        public override bool ReadyToShip(out Belt target, out Widget widget)
        {
            target = this; 
            widget = null;
            
            if (!IsOccupied)
            {
                return false;
            }
            
            if (!CanShip(out widget)) 
                return false;
            
            return true; 
        }
        
        // Ship = despawn occupant
        public override void Ship(Belt target, Widget widget)
        {
            if(widget == null)
                return;
            
            EventBus<WidgetCollected>.Raise(new WidgetCollected(widget.widgetType));
            
            SimplePool.Despawn(widget.gameObject);
            Occupant = null;
        }
    }
}