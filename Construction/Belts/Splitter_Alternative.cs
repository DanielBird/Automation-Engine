using System;
using Engine.Construction.Events;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public enum SplitterNeighbourStatus {NoneFound, LeftFound, RightFound, BothFound}
    
    /// <summary>
    /// Not recommended:
    /// An alternative implementation of the Splitter class that does not make use of a child belt
    /// Instead, it selects one of two target belts to ship to
    /// This currently ships from the same location, irrespective of the target belt, which is likely not the desired behavior 
    /// </summary>
    public class Splitter_Alternative : Belt
    {
        [Header("Splitter")] 
        [Tooltip("How many steps along the grid should the splitter look for its target Nodes?")]
        public int stepSize = 1; 
        
        // 0 means left next, 1 means right next
        [SerializeField] private int nextIndex;

        private Vector2Int _forwardGridPosition;
        private Vector2Int _forwardLeftGridPosition;

        public float minTimeBetweenSelection = 0.2f; 
        private float _timeOfLastSelection; 
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            
            _forwardGridPosition = PositionByDirection.GetForwardPosition(GridCoord, Direction, stepSize);
            Vector3Int forwardPosV3 = new (_forwardGridPosition.x, 0, _forwardGridPosition.y);
            Direction countClockwiseDirection = DirectionUtils.RotateCounterClockwise(Direction);
            _forwardLeftGridPosition = PositionByDirection.GetForwardPosition(forwardPosV3, countClockwiseDirection, stepSize);
        }
        
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

            // No target belts found
            if (!GetTarget(ref target)) return false;

            if (target == null || !target.CanReceive)
            {
                if (logInabilityToShip) Debug.Log($"{name} is unable to ship.");
                return false;
            }
            
            // Advance the round-robin if we decided to ship.
            nextIndex = (nextIndex + 1) % 2;
            
            return true;
        }

        private bool GetTarget(ref Belt target)
        {
            SplitterNeighbourStatus status = TryGetTargetNodes(out Belt leftBelt, out Belt rightBelt);

            switch (status)
            {
                case SplitterNeighbourStatus.NoneFound:
                    return false; 
                case SplitterNeighbourStatus.LeftFound:
                    target = leftBelt;
                    break;
                case SplitterNeighbourStatus.RightFound:
                    target = rightBelt;
                    break;
                case SplitterNeighbourStatus.BothFound:
                    if (nextIndex == 0) target = leftBelt;
                    else target = rightBelt;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        // The forward left node is at the grid position a step forward (following the delivery direction)
        // And then a turn counterclockwise. 
        private SplitterNeighbourStatus TryGetTargetNodes(out Belt leftBelt, out Belt rightBelt)
        {
            leftBelt = null;
            rightBelt = null;

            if (World.GetNeighbourAt(_forwardLeftGridPosition, out Node leftNode) && leftNode is Belt lb)
                leftBelt = lb;

            if (World.GetNeighbourAt(_forwardGridPosition, out Node rightNode) && rightNode is Belt rb)
                rightBelt = rb;

            return (leftBelt != null, rightBelt != null) switch
            {
                (true,  true)  => SplitterNeighbourStatus.BothFound,
                (true,  false) => SplitterNeighbourStatus.LeftFound,
                (false, true)  => SplitterNeighbourStatus.RightFound,
                _              => SplitterNeighbourStatus.NoneFound
            };
        }

        public void OnPlayerSelect(LeftOrRight leftOrRight)
        {
            // Normally, player selection requires that the player clicks on something else before the belt can be clicked and dragged again.
            // With splitters having two output belts that doesn't make sense. The player is likely to click the splitter twice in a row.
            // Therefore, we use a timer to reset IsSelected.
            
            if (Time.time > _timeOfLastSelection + minTimeBetweenSelection)
                IsSelected = false; 
            
            if(IsSelected || !IsEnabled ) return;
            IsSelected = true;
            _timeOfLastSelection = Time.time;
            
            Vector2Int gridPos = Vector2Int.zero; 
            
            switch (leftOrRight)
            {
                case LeftOrRight.Left:
                    gridPos = _forwardLeftGridPosition;
                    break;
                case LeftOrRight.Right:
                    gridPos = _forwardGridPosition;
                    break;
            }
            
            if (!World.InBounds(gridPos.x, gridPos.y)) return;
            if (World.GetNeighbourAt(gridPos, out Node neighbour)) return;
            EventBus<BeltClickEvent>.Raise(new BeltClickEvent(new Vector3Int(gridPos.x, 0, gridPos.y), NodeType.GenericBelt, this));
        }
    }
}