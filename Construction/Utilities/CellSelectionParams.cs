using Construction.Maps;
using Construction.Placement;

namespace Construction.Utilities
{
    public class CellSelectionParams
    {
        public readonly IMap Map;
        public readonly INodeMap NodeMap;
        public readonly PlacementSettings Settings;
        public readonly int StepSize;

        public CellSelectionParams(IMap map, INodeMap nodeMap, PlacementSettings settings, int stepSize)
        {
            Map = map;
            NodeMap = nodeMap;
            Settings = settings;
            
            if (stepSize == 0) stepSize = 1; 
            StepSize = stepSize;
        }
    }
}