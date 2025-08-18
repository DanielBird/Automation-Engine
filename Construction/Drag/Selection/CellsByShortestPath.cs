using System;
using System.Collections.Generic;
using Construction.Nodes;
using Construction.Placement;
using UnityEngine;

namespace Construction.Drag.Selection
{
    public static class CellsByShortestPath
    {
        public static void SelectCells(Vector3Int start, Vector3Int end, CellSelection selection, CellSelectionParams selectionParams)
        {
            // Snap end to step grid so path expands in multiples of StepSize
            Vector3Int adjustedEnd = CellSelector.SnapToStepGrid(start, end, selectionParams.StepSize);

            // Single cell selection
            if (start == adjustedEnd)
            {
                CellSelector.AddSingleCell(selection, selectionParams, start, Direction.North);
                selection.Corner = Corner.None;
                return;
            }

            List<Vector3Int> path = FindShortestPath(start, adjustedEnd, selectionParams);
            if (path == null || path.Count == 0)
                return;

            AddPathCells(path, selection, selectionParams);
            // selectionParams.FilterIntersections(path);
            selection.Corner = Corner.None; // multi-corner paths: don't use single-corner UI
        }

        private readonly struct Record : IEquatable<Record>
        {
            public readonly Vector3Int Pos; 
            public readonly bool PrevWasIntersection;
            public Record(Vector3Int pos, bool prevWasIntersection) { Pos = pos; PrevWasIntersection = prevWasIntersection; }
            public bool Equals(Record other) => Pos.Equals(other.Pos) && PrevWasIntersection == other.PrevWasIntersection;
            public override bool Equals(object obj) => obj is Record other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Pos, PrevWasIntersection);
        }
        
        private static List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, CellSelectionParams p)
        {
            int w = p.Map.MapWidth;
            int h = p.Map.MapHeight;
            int step = Mathf.Max(1, p.StepSize);

            // State
            Queue<Record> q = new();
            HashSet<Record> visited = new();
            Dictionary<Vector3Int, Record> parent = new();

            Record startingRecord = new Record(start, false); 
            q.Enqueue(startingRecord);
            visited.Add(startingRecord);

            while (q.Count > 0)
            {
                Record current = q.Dequeue();
                if (current.Pos == goal)
                {
                    return GetPath(p, current, parent);
                }

                foreach (Vector3Int nb in GetNeighbours(current.Pos, step, w, h))
                {
                    bool isGoal = nb == goal;
                    bool passable = IsPassable(nb.x, nb.z, p, current.PrevWasIntersection, out bool addedIntersection);

                    if (isGoal && !passable)
                    {
                        return GetPath(p, current, parent);
                    }
                    
                    if (!passable) continue;

                    Record next = new(nb, addedIntersection);
                    if (!visited.Add(next)) continue;
                    if (!parent.ContainsKey(nb)) parent[nb] = current;
                    q.Enqueue(next);
                }
            }

            Debug.Log("No path");
            // No path
            return null;
        }

        private static List<Vector3Int> GetPath(CellSelectionParams p, Record current, Dictionary<Vector3Int, Record> parent)
        {
            List<Vector3Int> path = new();
            HashSet<Vector3Int> intersections = new(); 
                    
            Record t = current;
            while (true)
            {
                path.Add(t.Pos);
                if (t.PrevWasIntersection) intersections.Add(t.Pos);
                if (!parent.TryGetValue(t.Pos, out Record next)) break;
                t = next;
            }
            path.Reverse();
            p.ClearIntersections();
            foreach (Vector3Int i in intersections) p.AddIntersection(i);
            return path;
        }

        private static IEnumerable<Vector3Int> GetNeighbours(Vector3Int c, int step, int width, int height)
        {
            // 4-connected moves by step
            int nx;

            nx = c.x + step; if (nx >= 0 && nx < width) yield return new Vector3Int(nx, 0, c.z);
            nx = c.x - step; if (nx >= 0 && nx < width) yield return new Vector3Int(nx, 0, c.z);

            int nz;
            nz = c.z + step; if (nz >= 0 && nz < height) yield return new Vector3Int(c.x, 0, nz);
            nz = c.z - step; if (nz >= 0 && nz < height) yield return new Vector3Int(c.x, 0, nz);
        }

        private static bool IsPassable(int x, int z, CellSelectionParams p, bool previouslyAddedIntersection, out bool addedIntersection)
        {
            addedIntersection = false;
            // Allow moving through vacant cells; 
            if (p.Map.VacantCell(x, z)) return true;
            
            // Allow intersections
            if (ViableIntersection(x, z, p, previouslyAddedIntersection))
            {
                // addedIntersection = p.Intersections.Add(new Vector3Int(x, 0, z));
                addedIntersection = true;
                return true;
            }
            
            return false;
        }

        private static bool ViableIntersection(int x, int z, CellSelectionParams p, bool previouslyAddedIntersection)
        {
            if (previouslyAddedIntersection) return false; 
            if (!p.NodeMap.TryGetNode(x, z, out Node node)) return false;
            if (node.NodeType != NodeType.Straight) return false;
            if (!node.TryGetForwardNode(out Node forwardNode)) return false;
            if (!node.TryGetBackwardNode(out Node backwardNode)) return false;
            if (forwardNode.InputDirection() != node.Direction) return false;
            if (backwardNode.Direction != node.InputDirection()) return false;
            if (forwardNode.NodeType == NodeType.Intersection || backwardNode.NodeType == NodeType.Intersection) return false;
            return true;
        }
        
        private static void AddPathCells(List<Vector3Int> path, CellSelection selection, CellSelectionParams selectionParams)
        {
            int n = path.Count;
            int step = Mathf.Max(1, selectionParams.StepSize);
            HashSet<Cell> cells = new();

            if (n == 1)
            {
                // Single cell
                CellSelector.AddSingleCell(selection, selectionParams, path[0], Direction.North);
                selection.AddCells(cells);
                return;
            }

            // First cell (start)
            Direction firstDir = DirectionBetween(path[0], path[1]);
            CellSelector.AddStartEndCell(cells, path[0], firstDir, selectionParams, false);

            // Middle cells
            for (int i = 1; i < n - 1; i++)
            {
                Direction prevDir = DirectionBetween(path[i - 1], path[i]);
                Direction nextDir = DirectionBetween(path[i], path[i + 1]);

                if (prevDir == nextDir)
                {
                    // Straight
                    CellSelector.AddCell(selectionParams, path[i].x, path[i].z, cells, nextDir);
                }
                else
                {
                    // Corner at path[i]
                    bool leftTurn = CellSelector.IsLeftTurn(path[i - 1], path[i], path[i + 1]);
                    NodeType cornerType = leftTurn ? NodeType.LeftCorner : NodeType.RightCorner;
                    CellSelector.AddCell(selectionParams, path[i].x, path[i].z, cells, nextDir, cornerType);
                }
            }

            // Last cell (end)
            Direction lastDir = DirectionBetween(path[n - 2], path[n - 1]);
            CellSelector.AddStartEndCell(cells, path[n - 1], lastDir, selectionParams, true);

            selection.AddCells(cells);
        }
        
        private static Direction DirectionBetween(Vector3Int a, Vector3Int b)
        {
            if (b.x > a.x) return Direction.East;
            if (b.x < a.x) return Direction.West;
            if (b.z > a.z) return Direction.North;
            return Direction.South;
        }
    }
}