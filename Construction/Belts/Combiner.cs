using System;
using Construction.Nodes;
using Construction.Placement;
using Construction.Utilities;
using UnityEngine;

namespace Construction.Belts
{
    public class Combiner : Belt
    {
        [Header("Combiner")] [TextArea(5, 15)]
        public string explanation = "Combiners are made up of two belts. " +
                                    "The Combiner class itself and a child belt. " +
                                    "The Combiner receives deliveries from the child belt. " +
                                    "And any belt at the backward position. " +
                                    "The child belt should be assigned in the inspector";
        public Belt childBelt; 
        
        [Tooltip("How many steps along the grid should the splitter look for its target Nodes?")]
        public int stepSize = 1; 
        
        private Vector2Int _leftGridPosition; 
        
        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            
            if (childBelt == null)
            {
                Debug.LogWarning("Every Combiner requires a child belt. The child belt is currently missing");
                return;
            }
            
            if (!RegisterChildBelt(config)) return;

            Direction childDirection = DirectionUtils.RotateClockwise(Direction);
            NodeConfiguration nodeConfig = NodeConfiguration.Create(config.Map, config.NodeMap, NodeType.RightCorner, childDirection, true); 
            
            childBelt.Initialise(nodeConfig);
            childBelt.UpdateTargetNode();
        }
        
        private bool RegisterChildBelt(NodeConfiguration config)
        {            
            _leftGridPosition = PositionByDirection.GetLeftPosition(GridCoord, Direction, stepSize);
            Vector3Int childGridCoord = new (_leftGridPosition.x, 0, _leftGridPosition.y);

            Vector2Int size = childBelt.GetSize();
            if (!config.Map.RegisterOccupant(_leftGridPosition.x, _leftGridPosition.y, size.x, size.y))
            {
                childBelt.FailedPlacement();
                return false;
            }
            
            childBelt.Place(childGridCoord, config.NodeMap);
            return true;
        }
    }
}