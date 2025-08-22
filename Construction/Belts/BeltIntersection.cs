using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public class BeltIntersection : Belt
    {
        private Direction currentShippingDirection;  

        public override void Receive(Belt target, Resource resource)
        {
            if (!CanReceive)
            {
                if (logFailedResourceReceipt) Debug.Log($"Could not receive {resource.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = resource;
            TimeOfReceipt = Time.time;
            currentShippingDirection = target.Direction;
        }
        
        public override bool ReadyToShip(out Belt target, out Resource resource)
        {
            resource = null; 
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
            
            if (!CanShip(out resource)) 
                return false;
            
            return true;
        }
    }
}