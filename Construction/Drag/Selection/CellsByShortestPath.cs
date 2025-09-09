using System;
using System.Collections.Generic;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Drag.Selection
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
            selection.Corner = Corner.None; // multi-corner paths: don't use single-corner UI
        }

        private readonly struct Record : IEquatable<Record>
        {
            public readonly Vector3Int Pos;
            public readonly byte OccupiedStreak; // 0 or 1
            public readonly int LastPathId;      // -1 if none

            public Record(Vector3Int pos, byte occupiedStreak, int lastPathId)
            {
                Pos = pos;
                OccupiedStreak = occupiedStreak;
                LastPathId = lastPathId;
            }

            public bool Equals(Record other)
                => Pos.Equals(other.Pos) && OccupiedStreak == other.OccupiedStreak && LastPathId == other.LastPathId;

            public override bool Equals(object obj) => obj is Record other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Pos, OccupiedStreak, LastPathId);
        }
        
        private static List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, CellSelectionParams p)
        {
            int w = p.Map.MapWidth;
            int h = p.Map.MapHeight;
            int step = Mathf.Max(1, p.StepSize);
            
            Queue<Record> q = new();
            HashSet<Record> visited = new();
            Dictionary<Vector3Int, Record> parent = new();

            Record startingRecord = new Record(start, 0, -1); 
            q.Enqueue(startingRecord);
            visited.Add(startingRecord);

            while (q.Count > 0)
            {
                Record current = q.Dequeue();
                if (current.Pos == goal)
                {
                    return GetPath(p, parent, start, goal);
                }
                
                bool isAtStart = current.Pos == start;

                foreach (Vector3Int nb in Grid.GetNeighbours(current.Pos, step, w, h))
                {
                    bool isGoal = nb == goal;
                    CellClass klass = ClassifyCell(nb.x, nb.z, p,  out Node nbNode);

                    if (isGoal && klass == CellClass.Blocked)
                        return GetPath(p, parent, start, goal);
                    
                    if (klass == CellClass.Blocked) continue;
                    
                    // Prevent immediately retracing an existing path
                    if (isAtStart && isGoal && klass == CellClass.PassableOccupied)
                        continue;

                    byte nextOccupiedStreak = 0; 
                    int nextLastPathId = current.LastPathId;

                    if (klass == CellClass.PassableOccupied)
                    {
                        if (current.OccupiedStreak != 0) continue; // prevent riding along an existing path of nodes
                        nextOccupiedStreak = 1;
                        nextLastPathId = nbNode?.PathId ?? -1; 
                    }
                    
                    Record next = new(nb, nextOccupiedStreak, nextLastPathId);
                    if (!visited.Add(next)) continue;
                    if (!parent.ContainsKey(nb)) parent[nb] = current;
                    q.Enqueue(next);
                }
            }

            Debug.Log("No path");
            return null;
        }

        private static List<Vector3Int> GetPath(CellSelectionParams p, Dictionary<Vector3Int, Record> parent, Vector3Int start, Vector3Int goal)
        {
            // Reconstruct the path
            List<Vector3Int> path = new();
            Vector3Int current = goal;
            
            int maxSteps = (p.Map.MapWidth * p.Map.MapHeight) + 8;
            HashSet<Vector3Int> visited = new();

            for (int i = 0; i < maxSteps; i++)
            {
                path.Add(current);

                if (current == start)
                    break;

                if (!parent.TryGetValue(current, out Record prev))
                    break;
                
                if (!visited.Add(prev.Pos))
                    break;
                
                current = prev.Pos;
            }
            
            if (path.Count == 0 || path[0] != goal || path[^1] != start)
            {
                path.Clear();
                return path; // invalid or incomplete path
            }
            
            path.Reverse();

            // Block a single step back along an existing path and forming an intersection
            if (path.Count == 2 && !p.Map.VacantCell(path[^1].x, path[^1].z))
            {
                path.Clear();
                return path;
            }
            
            // Apply intersections to selection params
            p.ClearIntersections();
            foreach (Vector3Int pos in path)
            {
                if (!p.Map.VacantCell(pos.x, pos.z) && IsIntersection(pos.x, pos.z, p))
                {
                    p.AddIntersection(pos);
                }
            }
            
            return path;
        }
        
        private enum CellClass { Vacant, PassableOccupied, Blocked }
        private static CellClass ClassifyCell(int x, int z, CellSelectionParams p, out Node node)
        {
            node = null;
            
            if (p.Map.VacantCell(x, z)) 
                return CellClass.Vacant;

            if (!p.NodeMap.TryGetNode(x, z, out node))
                return CellClass.Blocked; // occupied by something that is not a node

            switch (node.NodeType)
            {
                case NodeType.LeftCorner:
                case NodeType.RightCorner:
                case NodeType.Producer:
                case NodeType.Splitter:
                case NodeType.Combiner:
                case NodeType.Consumer:
                    return CellClass.Blocked;
            }
            
            return CellClass.PassableOccupied;
        }

        private static bool IsIntersection(int x, int z, CellSelectionParams p)
        {
            if (!p.NodeMap.TryGetNode(x, z, out Node node))
                return false;
            
            switch (node.NodeType)
            {
                case NodeType.LeftCorner:
                case NodeType.RightCorner:
                case NodeType.Producer:
                case NodeType.Splitter:
                case NodeType.Combiner:
                case NodeType.Consumer:
                    return false;
            }

            return true;
        }
        
        private static void AddPathCells(List<Vector3Int> path, CellSelection selection, CellSelectionParams selectionParams)
        {
            int n = path.Count;
            HashSet<Cell> cells = new();

            if (n == 1)
            {
                // Single cell
                CellSelector.AddSingleCell(selection, selectionParams, path[0], Direction.North);
                selection.AddCells(cells);
                return;
            }

            // First cell (start)
            Direction firstDir = DirectionUtils.DirectionBetween(path[0], path[1]);
            CellSelector.AddStartEndCell(cells, path[0], firstDir, selectionParams, false);

            // Middle cells
            for (int i = 1; i < n - 1; i++)
            {
                Direction prevDir = DirectionUtils.DirectionBetween(path[i - 1], path[i]);
                Direction nextDir = DirectionUtils.DirectionBetween(path[i], path[i + 1]);

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
            Direction lastDir = DirectionUtils.DirectionBetween(path[n - 2], path[n - 1]);
            CellSelector.AddStartEndCell(cells, path[n - 1], lastDir, selectionParams, true);

            selection.AddCells(cells);
        }
    }
}