using Construction.Maps;
using Construction.Placement;

namespace Construction.Nodes
{
    public class NodeConfiguration
    {
        public IMap Map; 
        public INodeMap NodeMap;
        public NodeType NodeType;
        public Direction Direction;
        public bool UpdateDirection;
        public bool UpdateRotation;
        
        public static NodeConfiguration Create(IMap map, INodeMap nodeMap, NodeType nodeType, Direction direction = Direction.North, bool updateDirection = false, bool updateRotation = false)
        {
            return new NodeConfiguration()
            {
                Map = map, 
                NodeMap = nodeMap,
                NodeType = nodeType,
                Direction = direction,
                UpdateDirection =  updateDirection,
                UpdateRotation = updateRotation
                
            }; 
        }
    }
}