using System;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Resources;
using Engine.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public class Belt : Node
    {
        [Header("Transportation")]
        [field: SerializeField] public Resource Occupant { get; protected set; }
        [field: SerializeField] public float TimeOfReceipt { get; protected set; }
        public bool IsOccupied => Occupant != null;
        public bool CanReceive => Occupant == null;
        
        [Tooltip("Where should the resource be on arrival?")] public Vector3 arrivalPointVector; 
        [Tooltip("For corner nodes, where should the bezier handle be?")] public Vector3 bezierHandleVector;
        public Vector3 ResourceArrivalPoint { get; private set; }
        public Vector3 BezierHandle { get; private set; }
        
        [Header("Debug")] 
        public bool logFailedResourceReceipt;
        public bool logInabilityToShip; 
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            SetupResourceMovement();
            IsSelected = false;
        }

        protected void SetupResourceMovement()
        {
            ResourceArrivalPoint = transform.TransformPoint(arrivalPointVector);

            if(NodeType != NodeType.LeftCorner && NodeType != NodeType.RightCorner) 
                return;
            
            BezierHandle = transform.TransformPoint(bezierHandleVector);
        }

        public virtual void Receive(Belt sender, Resource resource)
        {
            if (!CanReceive)
            {
                if (logFailedResourceReceipt) Debug.Log($"Could not receive {resource.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = resource;
            TimeOfReceipt = Time.time; 
        }

        public virtual bool ReadyToShip(out Belt target, out Resource resource)
        {
            resource = null; 
            target = null;
            
            if(!HasForwardNode(out Node targetNode))
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

        protected bool CanShip(out Resource resource)
        {
            resource = null;
            if(Occupant == null)
                return false;
            
            resource = Occupant;
            return true;
        }
        
        public virtual void Ship(Belt target, Resource resource)
        {
            target.Receive(this, resource);
            resource.Move(GetMoveType(target), this, target);
            Occupant = null;
        }

        private MoveType GetMoveType(Belt target) => target.NodeType switch
        {
            NodeType.LeftCorner => MoveType.Left,
            NodeType.RightCorner => MoveType.Right,

            _ => MoveType.Forward
        };

        public override void OnPlayerSelect()
        {
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
            
            if(NodeType == NodeType.LeftCorner || NodeType == NodeType.RightCorner)
                return;
            
            // Allow the player to start a new drag session by clicking on this belt 
            Vector2Int forwardGridPos = PositionByDirection.GetForwardPosition(GridCoord, Direction, GridWidth);
            if (!NodeMap.InBounds(forwardGridPos.x, forwardGridPos.y)) return;
            
            // If there is a belt in front, do not continue
            if (NodeMap.GetNeighbourAt(forwardGridPos, out Node forwardNode)) return;
            
            EventBus<BeltClickEvent>.Raise(new BeltClickEvent(new Vector3Int(forwardGridPos.x, 0, forwardGridPos.y), NodeType.GenericBelt, this));
        }

        public override void OnRemoval()
        {
            base.OnRemoval();
            
            // Despawn any resources that this belt is currently handling
            if(!IsOccupied) return;
            Occupant.CancelMovement();
            SimplePool.Despawn(Occupant.gameObject);
            Occupant = null;
        }

        public void SetParent(Belt parent)
        {
            isRemovable = false; 
            ParentNode = parent;
        }
    }
}