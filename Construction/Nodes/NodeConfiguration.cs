using Construction.Maps;
using Construction.Placement;

namespace Construction.Nodes
{
    public class NodeConfiguration
    {
        public INodeMap NodeMap;
        public NodeType NodeType;
        public Direction Direction;
        public bool UpdateRotation;
        
        public static NodeConfiguration Create(INodeMap map, NodeType nodeType, Direction direction, bool updateRotation = true)
        {
            return new NodeConfiguration()
            {
                NodeMap = map,
                NodeType = nodeType,
                Direction = direction,
                UpdateRotation = updateRotation
            }; 
        }
    }
}