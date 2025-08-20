using System.Collections.Generic;
using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Drag.Selection
{
    public class CellsAlongNodePath
    {
        public static CellSelection SelectCells(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, CellSelectionParams csp, out Vector3Int end)
        {
            CellSelection selection = new();
            
            if (!CellSelector.TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, csp.Map, csp.Settings, out end))
                return selection;
            
            if (!csp.NodeMap.TryGetNode(end.x, end.z, out Node endNode))
                return selection;
            
            HashSet<Node> toAddNodes = new();
            FollowPath(endNode, start, toAddNodes);

            foreach (Node n in toAddNodes)
            {
                selection.AddCell(n.GridCoord, n.Direction, n.NodeType, csp.Settings);
            }
            
            return selection;
        }

        private static void FollowPath(Node startingNode, Vector3Int target, HashSet<Node> toAddNodes)
        {
            // Memory: Can this node reach the target Grid?
            Dictionary<Node, bool> connectsBack = new();
            HashSet<Node> visited = new();
            
            Stack<Node> stack = new();
            stack.Push(startingNode);
  
            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                
                if (current == null) continue;
                if (!visited.Add(current)) continue;
                
                if (!LeadsToTarget(current, target, connectsBack)) continue;
                
                toAddNodes.Add(current);

                // Stop expanding at the target
                if (current.GridCoord == target) continue;
                
                foreach (Node n in current.TargetNodes)
                {
                    if (LeadsToTarget(n, target, connectsBack))
                        stack.Push(n);
                }
            }
        }
        
        private static bool LeadsToTarget(Node n, Vector3Int target, Dictionary<Node, bool> leadsMemory)
        {
            if (n.GridCoord == target) return true;
            if (leadsMemory.TryGetValue(n, out bool cached)) return cached;

            foreach (Node t in n.TargetNodes)
            {
                if (LeadsToTarget(t, target, leadsMemory))
                {
                    leadsMemory[t] = true;
                    return true;
                }
            }
            leadsMemory[n] = false;   
            return false;
        }
    }
}