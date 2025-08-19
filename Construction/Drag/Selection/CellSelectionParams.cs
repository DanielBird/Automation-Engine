using System.Collections.Generic;
using Engine.Construction.Maps;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Drag.Selection
{
    public class CellSelectionParams
    {
        public readonly IMap Map;
        public readonly INodeMap NodeMap;
        public readonly PlacementSettings Settings;
        public readonly int StepSize;
        public HashSet<Vector3Int> Intersections { get; private set; }

        public CellSelectionParams(IMap map, INodeMap nodeMap, PlacementSettings settings, int stepSize)
        {
            Map = map;
            NodeMap = nodeMap;
            Settings = settings;
            
            if (stepSize == 0) stepSize = 1; 
            StepSize = stepSize;
            
            Intersections = new HashSet<Vector3Int>();
        }

        public void AddIntersection(Vector3Int intersection) => Intersections.Add(intersection);
        public void RemoveIntersection(Vector3Int intersection) => Intersections.Remove(intersection);
        public void ClearIntersections() => Intersections.Clear();
        public void FilterIntersections(List<Vector3Int> path) => Intersections.IntersectWith(path); 
    }
}