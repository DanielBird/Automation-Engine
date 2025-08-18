using System;
using Construction.Events;
using Construction.Nodes;
using Construction.Widgets;
using UnityEngine;
using Utilities;
using Utilities.Events;

namespace Construction.Belts
{
    
    public class Belt : Node
    {
        [Header("Transportation")]
        [field: SerializeField] public Widget Occupant { get; protected set; }
        [field: SerializeField] public float TimeOfReceipt { get; protected set; }
        public bool IsOccupied => Occupant != null;
        public bool CanReceive => Occupant == null;
        
        [Tooltip("Where should the widget be on arrival?")][SerializeField]  public GameObject arrivalPoint;
        [Tooltip("For corner nodes, where should the bezier handle be?")][SerializeField]  public GameObject bezierHandle; 
        
        public Vector3 WidgetArrivalPoint { get; private set; }
        public Vector3 BezierHandle { get; private set; }
        
        [Header("Debug")] 
        public bool drawWidgetTarget;
        public bool logFailedWidgetReceipt;
        public bool logInabilityToShip; 
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);

            if (arrivalPoint == null)
            {
                Debug.LogWarning("Belts require a widget arrival point to be assigned in the inspector");
                return; 
            }
            
            WidgetArrivalPoint = arrivalPoint.transform.position;

            if (bezierHandle == null)
            {
                if(NodeType == NodeType.LeftCorner || NodeType == NodeType.RightCorner) 
                    Debug.LogWarning("Corner belts require a bezier handle to be assigned in the inspector");
                return;
            }
            
            BezierHandle = bezierHandle.transform.position;
        }

        public virtual void Receive(Belt sender, Widget widget)
        {
            if (!CanReceive)
            {
                if (logFailedWidgetReceipt) Debug.Log($"Could not receive {widget.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = widget;
            TimeOfReceipt = Time.time; 
        }

        public virtual bool ReadyToShip(out Belt target, out Widget widget)
        {
            widget = null; 
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
            
            if (!CanShip(out widget)) 
                return false;
            
            return true;
        }

        protected bool CanShip(out Widget widget)
        {
            widget = null;
            if(Occupant == null)
                return false;
            
            widget = Occupant;
            return true;
        }
        
        public virtual void Ship(Belt target, Widget widget)
        {
            target.Receive(this, widget);
            widget.Move(GetMoveType(target), this, target);
            Occupant = null;
        }

        private MoveType GetMoveType(Belt target) => target.NodeType switch
        {
            NodeType.Straight or NodeType.GenericBelt => MoveType.Forward,
            NodeType.LeftCorner => MoveType.Left,
            NodeType.RightCorner => MoveType.Right,
            NodeType.Intersection or NodeType.Producer or NodeType.Splitter or NodeType.Combiner => MoveType.Forward,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override void OnPlayerSelect()
        {
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
            
            // Allow the player to start a new drag session by clicking on this belt 
            // If this belt is connected to a belt in front, do not continue
            Vector2Int forwardGridPos = PositionByDirection.GetForwardPosition(GridCoord, Direction, GridWidth);
            if (!NodeMap.InBounds(forwardGridPos.x, forwardGridPos.y)) return;
            if (NodeMap.GetNeighbourAt(forwardGridPos, out Node forwardNode)) return;
            
            EventBus<BeltClickEvent>.Raise(new BeltClickEvent(new Vector3Int(forwardGridPos.x, 0, forwardGridPos.y), NodeType.GenericBelt));
        }

        public override void OnRemoval()
        {
            base.OnRemoval();
            
            if(!IsOccupied) return;
            Occupant.CancelMovement();
            SimplePool.Despawn(Occupant.gameObject);
            Occupant = null;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (drawWidgetTarget)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(WidgetArrivalPoint, 0.15f);
            }
        }
    }
}