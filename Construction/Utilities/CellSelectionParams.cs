using System.Collections.Generic;
using Construction.Maps;
using Construction.Placement;
using UnityEngine;

namespace Construction.Utilities
{
    public class CellSelectionParams
    {
        public readonly IMap Map;
        public readonly INodeMap NodeMap;
        public readonly PlacementSettings Settings;
        public readonly int StepSize;
        public HashSet<Vector3Int> Intersections;

        public CellSelectionParams(IMap map, INodeMap nodeMap, PlacementSettings settings, int stepSize)
        {
            Map = map;
            NodeMap = nodeMap;
            Settings = settings;
            
            if (stepSize == 0) stepSize = 1; 
            StepSize = stepSize;
            
            Intersections = new HashSet<Vector3Int>();
        }

        public void FilterIntersections(List<Vector3Int> path)
        {
            Intersections.IntersectWith(path); 
        }
    }
}