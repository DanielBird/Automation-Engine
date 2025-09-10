using Engine.Construction.Maps;
using Engine.Construction.Placement;

namespace Engine.Construction.Nodes
{
    public class NodeConfiguration
    {
        public IWorld World; 
        public NodeType NodeType;
        public Direction Direction;
        public bool UpdateDirection;
        public bool UpdateRotation;
        
        public static NodeConfiguration Create(IWorld world, NodeType nodeType, Direction direction = Direction.North, bool updateDirection = false, bool updateRotation = false)
        {
            return new NodeConfiguration()
            {
                World = world,
                NodeType = nodeType,
                Direction = direction,
                UpdateDirection =  updateDirection,
                UpdateRotation = updateRotation
                
            }; 
        }
    }
}