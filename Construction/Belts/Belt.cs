using Construction.Maps;
using Construction.Nodes;
using Construction.Widgets;
using UnityEngine;

namespace Construction.Belts
{
    public class Belt : Node
    {
        [Header("Transportation")]
        [field: SerializeField] public Widget Occupant { get; protected set; }
        public bool IsOccupied => Occupant != null;
        public bool CanReceive => Occupant == null;
        
        public Vector3 widgetTargetOffset = new Vector3(0, 1, 0);
        public Vector3 widgetTarget { get; private set; }

        [Header("Debug")] public bool drawWidgetTarget; 
        public bool logFailedWidgetReceipt;
        public bool logInabilityToShip; 
        
        public override void Place(Vector3Int gridCoord, INodeMap map)
        {
            NodeMap = map; 
            GridCoord = gridCoord;
            widgetTarget = gridCoord + widgetTargetOffset;
        }
        
        public void Receive(Widget widget)
        {
            if (!CanReceive)
            {
                if (logFailedWidgetReceipt) Debug.Log($"Could not receive {widget.name} due to occupancy with {Occupant.name} at {Time.frameCount}.");
                return;
            }
            
            Occupant = widget;
            Debug.Log("Recieve " + widget.name);
        }

        public bool ReadyToShip(out Belt target, out Widget widget)
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

        private bool CanShip(out Widget widget)
        {
            widget = null;
            if(Occupant == null)
                return false;
            
            widget = Occupant;
            Occupant = null;
            return true;
        }
        
        public virtual void Ship(Belt target, Widget widget)
        {
            target.Receive(widget);
            widget.Move(MoveType.Standard, this, target);
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (drawWidgetTarget)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position + widgetTargetOffset, 0.15f);
            }
        }
    }
}