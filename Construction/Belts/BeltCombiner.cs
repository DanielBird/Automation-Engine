using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;

namespace Engine.Construction.Belts
{
    public class BeltCombiner : ParentBelt
    {
        [Header("Combiner")] [TextArea(5, 15)]
        public string explanation = "Combiners are made up of two belts. " +
                                    "The Combiner class itself and a child belt. " +
                                    "The Combiner receives deliveries from the child belt. " +
                                    "And any belt at the backward position. " +
                                    "The child belt should be assigned in the inspector";
        
        protected override NodeConfiguration GetChildNodeConfiguration(NodeConfiguration config)
        {
            Direction childDirection = DirectionUtils.RotateClockwise(Direction);
            return NodeConfiguration.Create(config.World, NodeType.RightCorner, childDirection, true); 
        }
    }
}