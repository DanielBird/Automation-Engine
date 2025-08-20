using System.Collections.Generic;
using System.Linq;
using Engine.Construction.Nodes;
using UnityEngine;
using ZLinq;

namespace Engine.Construction.Drag.Selection
{
    public static class CellsWithNodesInArea
    {
        public static CellSelection SelectCells(Vector3Int start, Camera mainCamera, LayerMask floorLayer, RaycastHit[] cellHits, CellSelectionParams csp, out Vector3Int end)
        {
            CellSelection selection = new();
            
            if (!CellSelector.TryGetCurrentGridCoord(mainCamera, floorLayer, cellHits, csp.Map, csp.Settings, out end))
                return selection;
            
            List<Vector3Int> cells = CellSelector.GetCellArea(start, end, csp.StepSize).ToList();
            HashSet<Node> insideNodes = new();
            
            foreach (Vector3Int cell in cells)
            {
                if (csp.NodeMap.TryGetNode(cell.x, cell.z, out Node node))
                {
                    insideNodes.Add(node); 
                    selection.AddCell(cell, node.Direction, node.NodeType, csp.Settings);
                }
            }

            AddConnectedNodes(selection, cells, insideNodes, csp);
            
            return selection;
        }

        private static void AddConnectedNodes(CellSelection selection, List<Vector3Int> cells, HashSet<Node> insideNodes, CellSelectionParams csp)
        {
            HashSet<Node> exitNodes = new();
            HashSet<Node> toAddNodes = new();
            
            // Find exit nodes
            foreach (Node insideNode in insideNodes)
            {
                foreach (Node target in insideNode.TargetNodes
                             .AsValueEnumerable()
                             .Where(target => !cells.Contains(target.GridCoord)))
                {
                    exitNodes.Add(target);
                }
            }

            // Add nodes that reach a node that 
            foreach (Node node in exitNodes)
            {
                CollectAlongValidBranches(node, cells, toAddNodes);
            }
            
            // Add the outside nodes to the selection
            foreach (Node n in toAddNodes)
            {
                selection.AddCell(n.GridCoord, n.Direction, n.NodeType, csp.Settings);
            }
        }

        private static void CollectAlongValidBranches(Node node, List<Vector3Int> cells, HashSet<Node> toAddNodes)
        {
            Dictionary<Node, bool> leadsToInsideMemory = new();
            HashSet<Vector3Int> cellSet = new(cells);
            HashSet<Node> visisted = new();
            
            Stack<Node> stack = new();
            stack.Push(node);

            while (stack.Any())
            {
                Node current = stack.Pop();
                
                if (current == null) continue;
                if (!visisted.Add(current)) continue;
                
                // Stop if re-entering the selected cell area
                if (cells.Contains(current.GridCoord)) continue; 
                    
                // Only include nodes that have a path back to the selection
                if (!LeadsToInside(current, cellSet, leadsToInsideMemory)) continue;
                
                toAddNodes.Add(current);
                
                foreach (Node child in current.TargetNodes)
                {
                    if (LeadsToInside(child, cellSet, leadsToInsideMemory))
                        stack.Push(child);
                }
            }
        }
        
        private static bool LeadsToInside(Node n, HashSet<Vector3Int> cellSet, Dictionary<Node, bool> leadsToInsideMemory)
        {
            if (cellSet.Contains(n.GridCoord)) return true;
            if (leadsToInsideMemory.TryGetValue(n, out bool cached)) return cached;

            foreach (Node t in n.TargetNodes)
            {
                if (LeadsToInside(t, cellSet, leadsToInsideMemory))
                {
                    leadsToInsideMemory[t] = true;
                    return true;
                }
            }
            leadsToInsideMemory[n] = false;   
            return false;
        }
    }
}