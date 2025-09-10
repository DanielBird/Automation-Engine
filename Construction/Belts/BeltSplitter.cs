using System;
using Engine.Construction.Nodes;
using Engine.Construction.Resources;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public class BeltSplitter : ParentBelt
    {
        [Header("Splitter")] [TextArea(5, 15)]
        public string explanation = "Splitters are made up of two belts. " +
                                    "The Splitter class itself and a child belt. " +
                                    "The Splitter ships to its forward belt and the child belt. " +
                                    "The child belt should be assigned in the inspector";
   
        
        // 0 means left next, 1 means right next
        [SerializeField] private int nextIndex;

        [Space] 
        [SerializeField] private bool shipByResourceType;
        [SerializeField] private int resourceTypeOne = -1;
        [SerializeField] private int resourceTypeTwo = -1;
        private int lastResourceSet; 
        
        private Vector2Int _forwardGridPosition;

        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            _forwardGridPosition = PositionByDirection.GetForwardPosition(GridCoord, Direction, stepSize);
        }

        [Button]
        public void ToggleShippingMethod() => shipByResourceType = !shipByResourceType;
        
        public override bool ReadyToShip(out Belt target, out Resource resource)
        {
            resource = null; 
            target = null;
            
            // No widget found
            if (!IsOccupied)
            {
                if (logInabilityToShip) Debug.Log($"{name} is unable to ship.");
                return false;
            }
            
            // Widget found but it is null
            if (!CanShip(out resource)) 
                return false;
            
            UpdateResourceTypes(resource);

            // No target belts found
            if (!GetTarget(ref target, resource)) return false;

            if (target == null || !target.CanReceive)
            {
                if (logInabilityToShip) Debug.Log($"{name} is unable to ship.");
                return false;
            }
            
            // Advance the round-robin if we decided to ship.
            nextIndex = (nextIndex + 1) % 2;
            
            return true;
        }

        private bool GetTarget(ref Belt target, Resource currentResource)
        {
            return shipByResourceType ? GetTargetBasedOnResource(ref target, currentResource) : GetAlternatingTarget(ref target);
        }

        private bool GetTargetBasedOnResource(ref Belt target, Resource currentResource)
        { 
            SplitterNeighbourStatus status = TryGetTargetNodes(out Belt rightBelt);

            switch (status)
            {
                case SplitterNeighbourStatus.NoneFound:
                    return false; 
                case SplitterNeighbourStatus.LeftFound:
                    target = childBelt;
                    break;
                case SplitterNeighbourStatus.RightFound:
                    target = rightBelt;
                    break;
                case SplitterNeighbourStatus.BothFound:
                    if (currentResource.ResourceIndex == resourceTypeOne) target = rightBelt;
                    else target = childBelt;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }
        
        private bool GetAlternatingTarget(ref Belt target)
        {
            SplitterNeighbourStatus status = TryGetTargetNodes(out Belt rightBelt);

            switch (status)
            {
                case SplitterNeighbourStatus.NoneFound:
                    return false; 
                case SplitterNeighbourStatus.LeftFound:
                    target = childBelt;
                    break;
                case SplitterNeighbourStatus.RightFound:
                    target = rightBelt;
                    break;
                case SplitterNeighbourStatus.BothFound:
                    if (nextIndex == 0) target = childBelt;
                    else target = rightBelt;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        // The forward left node is at the grid position a step forward (following the delivery direction)
        // And then a turn counterclockwise. 
        private SplitterNeighbourStatus TryGetTargetNodes(out Belt rightBelt)
        {
            rightBelt = null;
            
            if (World.GetNeighbourAt(_forwardGridPosition, out Node rightNode) && rightNode is Belt rb)
                rightBelt = rb;

            return (childBelt != null, rightBelt != null) switch
            {
                (true,  true)  => SplitterNeighbourStatus.BothFound,
                (true,  false) => SplitterNeighbourStatus.LeftFound,
                (false, true)  => SplitterNeighbourStatus.RightFound,
                _              => SplitterNeighbourStatus.NoneFound
            };
        }

        private void UpdateResourceTypes(Resource resource)
        {
            int type = resource.ResourceIndex; 
            
            // Matches existing widgets
            if(resourceTypeOne == type || resourceTypeTwo == type) return;
            
            // Doesn't match either type
            if (lastResourceSet == 0)
            {
                resourceTypeOne = type;
                lastResourceSet = 1;
            }
            else
            {
                resourceTypeTwo = type;
                lastResourceSet = 0;
            }
        }
    }
}