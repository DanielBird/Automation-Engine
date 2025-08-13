using Construction.Nodes;
using Construction.Placement;
using Construction.Widgets;
using UnityEngine;

namespace Construction.Belts
{
    public class Intersection : Belt
    {
        private Direction currentShippingDirection;  

        public override void Receive(Belt target, Widget widget)
        {
            if (!CanReceive)
            {
                if (logFailedWidgetReceipt) Debug.Log($"Could not receive {widget.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = widget;
            TimeOfReceipt = Time.time;
            currentShippingDirection = target.Direction;
        }
        
        public override bool ReadyToShip(out Belt target, out Widget widget)
        {
            widget = null; 
            target = null;
            
            if(!TryGetNeighbour(currentShippingDirection, out Node targetNode))
                return false;
            
            if(targetNode is not Belt belt)
                return false;
            
            target = belt;

            if (!IsOccupied || target == null || !target.CanReceive)
            {
                if (logInabilityToShip) Debug.Log($"{name} is unable to ship.");
                return false;
            }
            
            if (!CanShip(out widget)) 
                return false;
            
            return true;
        }
        
    }
}