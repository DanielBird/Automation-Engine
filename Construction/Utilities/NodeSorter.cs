using System.Collections.Generic;
using Construction.Nodes;
using UnityEngine;

namespace Construction.Utilities
{
    public static class NodeSorter
    {
        // Kahn’s Algorithm for Topological Sorting
        
        public static List<Node> TopologicalSort(IEnumerable<Node> nodes)
        {
            // 1) Compute the in-degree of each belt.
            //    inDegree[belt] = number of belts that feed into 'belt'.
            Dictionary<Node, int> inDegree = new Dictionary<Node, int>();
            
            // Initialize each belt's in-degree to zero to start
            foreach (Node node in nodes)
            {
                inDegree[node] = 0;
            }
            
            // For each belt, increment the in-degree of its downstream belts
            foreach (Node node in inDegree.Keys)
            {
                foreach (Node next in node.TargetNodes)
                {
                    if (inDegree.ContainsKey(next))
                    {
                        inDegree[next]++;
                    }
                    else
                    {
                        // If 'next' is not in the set of belts, you might handle that by adding it or ignoring it.
                        // For now, we assume all next belts are in the 'allBelts' collection.
                    }
                }
            }
            
            // 2) Initialize a queue for belts with in-degree = 0 (no upstream belts).
            Queue<Node> queue = new Queue<Node>();
            foreach (KeyValuePair<Node, int> kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }
            
            // This will store the final sorted order
            List<Node> sortedList = new List<Node>();
            
            // 3) Process the queue
            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                sortedList.Add(current);

                // For each belt that 'current' feeds into, reduce its in-degree by 1
                foreach (Node next in current.TargetNodes)
                {
                    inDegree[next]--;

                    // If that belt's in-degree has now reached zero, enqueue it
                    if (inDegree[next] == 0)
                        queue.Enqueue(next);
                }
            }
            
            // 4) If the sorted list contains all belts, we have a valid ordering.
            //    Otherwise, there's at least one cycle.
            if (sortedList.Count < inDegree.Count)
            {
                // There are belts we never got to with in-degree zero => cycle exists
                Debug.Log("Warning: A cycle was detected in the belt graph. " +
                          "Topological sort may be incomplete.");
            }

            return sortedList;
        }
    }
}