using System.Collections.Generic;
using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Utilities
{
    public static class LoopDetection
    {
        // Does adding a connection between two nodes create a loop?
        public static bool WillLoop(Node start, Node end)
        {
            Stack<Node> stack = new Stack<Node>();
            HashSet<Node> visited = new HashSet<Node>(); 
            
            end.TargetNodes.ForEach(node => stack.Push(node));

            while (stack.Count > 0)
            {
                Node current = stack.Pop();

                if (current == start)
                    return true;

                if (visited.Contains(current)) continue;
                visited.Add(current);
                current.TargetNodes.ForEach(node => stack.Push(node));
            }

            return false;
        }
        
        
        public static bool WillBecomeLoopDebug(Node start, Node end)
        {
            Stack<Node> stack = new Stack<Node>();
            HashSet<Node> visited = new HashSet<Node>(); 
            
            Debug.Log("Starting loop detection. With starting node: " + start.name + " and to end node: " + end.name);
            
            end.TargetNodes.ForEach(node => stack.Push(node));

            while (stack.Count > 0)
            {
                Node current = stack.Pop();

                if (current == start)
                {
                    Debug.Log($"Loop Detected: Current node {current.name} is {start.name}. ");
                    return true;
                }

                Debug.Log($"Loop NOT Detected: Current node {current.name} is not {start.name}");
                
                if (visited.Contains(current))
                {
                    Debug.Log("Visited contains: " + current.name + " so skipping over.");
                    continue;
                }
                
                visited.Add(current);

                foreach (Node node in current.TargetNodes)
                {
                    stack.Push(node);
                    Debug.Log($"Adding the target node of {current.name} to the stack. ({node.name} was added)");
                }
            }
            return false;
        }
    }
}