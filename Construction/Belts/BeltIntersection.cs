using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Belts
{
    public class BeltIntersection : Belt
    {
        [Header("Intersections")]
        [SerializeField] private Direction currentShippingDirection;  

        public override void Receive(Belt target, Resource resource)
        {
            if (!CanReceive)
            {
                if (logFailedResourceReceipt) Debug.Log($"Could not receive {resource.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = resource;
            TimeOfReceipt = Time.time;

            // Use the spatial relationship to determine the shipping direction rather than relying on the target.Direction.
            // currentShippingDirection = target.Direction;
            currentShippingDirection = DirectionUtils.DirectionBetween(target.GridCoord, GridCoord); 
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
        
        public override void OnPlayerSelect()
        {
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
            
            if(NodeType == NodeType.LeftCorner || NodeType == NodeType.RightCorner)
                return;
            
            HashSet<Vector3Int> openNeighbors = new(); 
            Vector2Int mapDimensions = NodeMap.MapDimensions();
            int step = nodeTypeSo.width;

            foreach (Vector3Int v in Grid.GetNeighbours(GridCoord, step, mapDimensions.x, mapDimensions.y))
            {
                if(!NodeMap.HasNode(v.x, v.z))
                    openNeighbors.Add(v);
            }
            
            if(openNeighbors.Count == 0)
                return;

            Vector3Int neighbour = openNeighbors.First(); 
            
            EventBus<BeltClickEvent>.Raise(new BeltClickEvent(neighbour, NodeType.GenericBelt, this));
        }
    }
}