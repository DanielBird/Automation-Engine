using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Events;
using Engine.Construction.Utilities;
using Engine.Utilities;
using Engine.Utilities.Events;
using NUnit.Framework;
using Sirenix.Utilities;
using UnityEngine;
using Direction = Engine.Construction.Placement.Direction;

namespace Engine.Construction.Nodes
{
    public struct PathInfo
    {
        public int PathId;
        public HashSet<Node> Nodes; 
        public DateTime LastModified;

        public PathInfo(int pathId)
        {
            PathId = pathId;
            Nodes = new HashSet<Node>();
            LastModified = DateTime.Now;
        } 
    }
    
    public class NodePathManager
    {
        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeGroupPlaced> _onNodeGroupPlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        
        private int _nextPathId = 1;
        private readonly Dictionary<int, PathInfo> _paths = new();
        private readonly Dictionary<Node, int> _nodeToPathId = new();

        private readonly NodeRelationshipManager _relationshipManager;
        private CancellationTokenSource _cts;
        private bool _eventsRegistered;
        
        public NodePathManager(NodeRelationshipManager relationshipManager)
        {
            _relationshipManager = relationshipManager;

            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            _onNodePlaced = new EventBinding<NodePlaced>(OnNodePlaced);
            _onNodeGroupPlaced = new EventBinding<NodeGroupPlaced>(OnNodeGroupPlaced);
            _onNodeRemoved = new EventBinding<NodeRemoved>(OnNodeRemoved);
            
            EventBus<NodePlaced>.Register(_onNodePlaced);
            EventBus<NodeGroupPlaced>.Register(_onNodeGroupPlaced);
            EventBus<NodeRemoved>.Register(_onNodeRemoved);
            _eventsRegistered = true;
        }

        public void Disable()
        {
            if (_eventsRegistered)
            {
                EventBus<NodePlaced>.Deregister(_onNodePlaced);
                EventBus<NodeGroupPlaced>.Deregister(_onNodeGroupPlaced);
                EventBus<NodeRemoved>.Deregister(_onNodeRemoved);
                _eventsRegistered = false;
            }
            
            CtsCtrl.Clear(ref _cts);
        }
        
        // SINGLE NODE PLACEMENT
        private void OnNodePlaced(NodePlaced obj)
        {
            Node newNode = obj.Node;
            if (newNode == null)
            {
                Debug.LogError("Node is null");
                return;
            }
                
            WaitAFrame(CompleteNodePlaced, newNode).Forget();
        }
        
        private void CompleteNodePlaced(Node node) => AssignPathId(node);

        private async UniTaskVoid WaitAFrame<T>(Action<T> action, T parameter)
        {
            while (_cts != null)
            {
                await UniTask.NextFrame(cancellationToken: _cts.Token);
            }
            _cts = new CancellationTokenSource();
            
            await UniTask.NextFrame(cancellationToken: _cts.Token);

            action(parameter); 
            CtsCtrl.Clear(ref _cts);
        }
        
        private void AssignPathId(Node newNode)
        {
            // Get all connected nodes (forward and backward)
            HashSet<Node> connectedNodes = GetConnectedNodes(newNode);
            HashSet<int> connectedPathIds = GetUniquePathIds(connectedNodes);
            
            switch (connectedPathIds.Count)
            {
                case 0:
                    // No connected paths - create a new path
                    CreateNewPath(newNode);
                    break;
                    
                case 1:
                    // Connected to one path - join that path
                    int pathId = connectedPathIds.First();
                    AddNodeToPath(newNode, pathId);
                    break;
                    
                default:
                    // Connected to multiple paths - merge them
                    MergePaths(newNode, connectedPathIds);
                    break;
            }
        }
        
        private HashSet<Node> GetConnectedNodes(Node node)
        {
            HashSet<Node> connected = new();
            
            /*
            // Check forward connection
            bool forwardFound = node.TryGetForwardNode(out Node forwardNode);
            if (forwardFound && forwardNode != null)
                connected.Add(forwardNode);
            
            // Check backward connection  
            bool backwardFound = node.TryGetBackwardNode(out Node backwardNode);
            if (backwardFound && backwardNode != null)
                connected.Add(backwardNode);*/
            
            if(_relationshipManager.GetNodeTargets(node, out HashSet<Node> targets))
                connected.AddRange(targets);
            
            if(_relationshipManager.GetNodeSource(node, out HashSet<Node> sources))
                connected.AddRange(sources);
            
            return connected;
        }
        
        private HashSet<int> GetUniquePathIds(HashSet<Node> connectedNodes)
        {
            HashSet<int> uniquePathIds = new();
    
            foreach (Node node in connectedNodes)
            {
                // Debug.Log($"Check {node} which has a path id is: {node.HasPathId()}. The Id is: {node.PathId}");
                // Only consider nodes that already have a valid path ID
                if (node.HasPathId())
                {
                    uniquePathIds.Add(node.PathId);
                }
            }
    
            return uniquePathIds;
        }
        
        private void CheckAdditionalConnections(Node node, HashSet<Node> connected)
        {
            // Check left and right connections for intersections
            foreach (Direction dir in new[] { DirectionUtils.Increment(node.Direction), DirectionUtils.Decrement(node.Direction)})
            {
                if (node.TryGetNeighbour(dir, out Node neighbor))
                {
                    if(neighbor.HasPathId())
                        connected.Add(neighbor);
                }
            }
        }
        
        private void CreateNewPath(Node node)
        {
            int pathId = _nextPathId++;
            PathInfo pathInfo = new PathInfo(pathId);
            pathInfo.Nodes.Add(node);
    
            _paths[pathId] = pathInfo;
            _nodeToPathId[node] = pathId;
            node.SetPathId(pathId);
    
            // Debug.Log($"Created new path {pathId} for node {node.name}");
        }

        private void AddNodeToPath(Node node, int pathId)
        {
            if (!_paths.TryGetValue(pathId, out PathInfo pathInfo))
            {
                Debug.LogError($"Path {pathId} not found!");
                CreateNewPath(node);
                return;
            }
    
            pathInfo.Nodes.Add(node);
            pathInfo.LastModified = DateTime.Now;
            _paths[pathId] = pathInfo;
    
            _nodeToPathId[node] = pathId;
            node.SetPathId(pathId);
    
            Debug.Log($"Added node {node.name} to existing path {pathId}");
        }
        
        private void MergePaths(Node newNode, HashSet<int> pathIds)
        {
            int primaryPathId = pathIds.Min(); // Use the smallest ID as primary
            HashSet<int> pathsToMerge = new(pathIds);
            pathsToMerge.Remove(primaryPathId);
    
            PathInfo primaryPath = _paths[primaryPathId];
    
            // Merge all other paths into primary
            foreach (int pathId in pathsToMerge)
            {
                if (_paths.TryGetValue(pathId, out PathInfo pathToMerge))
                {
                    // Update all nodes in the merged path
                    foreach (Node node in pathToMerge.Nodes)
                    {
                        node.SetPathId(primaryPathId);
                        _nodeToPathId[node] = primaryPathId;
                        primaryPath.Nodes.Add(node);
                    }
            
                    // Remove the merged path
                    _paths.Remove(pathId);
                    Debug.Log($"Merged path {pathId} into path {primaryPathId}");
                }
            }
    
            // Add the new node to the primary path
            AddNodeToPath(newNode, primaryPathId);
            primaryPath.LastModified = DateTime.Now;
            _paths[primaryPathId] = primaryPath;
    
            Debug.Log($"Path merge completed. New node {newNode.name} added to path {primaryPathId}");
        }
        
        // GROUP NODE PLACEMENT
        private void OnNodeGroupPlaced(NodeGroupPlaced e)
        {
            if (e.PathId > 0)
            {
                AssignNodeGroupToPath(e.NodeGroup, e.PathId);
                return; 
            }
            
            WaitAFrame(CompleteNodeGroupPlaced, e).Forget();
        }

        private void CompleteNodeGroupPlaced(NodeGroupPlaced obj)
        {
            HashSet<Node> newNodes = obj.NodeGroup; 
            Node startNode = obj.StartNode;
            Node endNode = obj.EndNode;
            
            if(newNodes == null || newNodes.Count == 0)
            {
                Debug.LogWarning("NodeGroupPlaced event received with empty node group");
                return;
            }
            
            bool startIsNull = !startNode;
            bool endIsNull = !endNode;

            if(startIsNull && endIsNull)
            {
                Debug.LogWarning("Both start and end nodes are null, this shouldn't happen!");
                AssignNodeGroupToPath(newNodes, _nextPathId++);
                return;
            }
            
            HashSet<Node> connectedToStart = startIsNull ? new HashSet<Node>() : GetConnectedNodes(startNode);
            HashSet<int> startPathIds = GetUniquePathIds(connectedToStart);
            
            HashSet<Node> connectedToEnd = endIsNull ? new HashSet<Node>() : GetConnectedNodes(endNode);
            HashSet<int> endPathIds = GetUniquePathIds(connectedToEnd);
            
            // Debug.Log($"Start connected nodes count: {connectedToStart.Count}. End connected nodes count: {connectedToEnd.Count}");
            // Debug.Log($"Start id count : {startPathIds.Count}. End id count : {endPathIds.Count}");
            
            int pathId = -1;
            
            if (startPathIds.Count == 0 && endPathIds.Count == 0)
                pathId = _nextPathId++;

            if (startPathIds.Count == 1 && endPathIds.Count == 0)
                pathId = startPathIds.First();
            
            if (startPathIds.Count == 0 && endPathIds.Count == 1)
                pathId = endPathIds.First();

            if (startPathIds.Count > 0 && endPathIds.Count > 0)
            {
                int lowestStart = startPathIds.Min();
                int lowestEnd = endPathIds.Min();
                pathId = Mathf.Min(lowestStart, lowestEnd);
                bool useStart = lowestStart < lowestEnd;
                MergeMultiplePaths(newNodes, useStart? startPathIds : endPathIds, pathId);
                return;
            }
            
            AssignNodeGroupToPath(newNodes, pathId);
        }

        private void AssignNodeGroupToPath(HashSet<Node> nodes, int id)
        {
            int pathId = id; 
            PathInfo pathInfo = new PathInfo(pathId);
            pathInfo.Nodes = nodes;
    
            _paths[pathId] = pathInfo;

            foreach (Node node in nodes)
            {
                _nodeToPathId[node] = pathId;
                node.SetPathId(pathId);
            }
        }

        private void MergeMultiplePaths(HashSet<Node> newNodes, HashSet<int> pathsToMerge, int pathId)
        {
            PathInfo pathInfo = new PathInfo(pathId);
            pathInfo.Nodes = newNodes;
    
            _paths[pathId] = pathInfo;

            foreach (Node node in newNodes)
            {
                _nodeToPathId[node] = pathId;
                node.SetPathId(pathId);
            }
            
            // Merge all other paths into the primary path
            foreach (int mergePathId in pathsToMerge)
            {
                if (_paths.TryGetValue(mergePathId, out PathInfo pathToMerge))
                {
                    // Update all nodes in the merged path
                    foreach (Node node in pathToMerge.Nodes)
                    {
                        node.SetPathId(pathId);
                        _nodeToPathId[node] = pathId;
                        pathInfo.Nodes.Add(node);
                    }
            
                    // Remove the merged path
                    _paths.Remove(mergePathId);
                    // Debug.Log($"Merged path {mergePathId} into path {pathId} during group placement");
                }
            }
            
            // Update the primary path
            pathInfo.LastModified = DateTime.Now;
            _paths[pathId] = pathInfo;
            
            // Debug.Log($"Group merge completed. {newNodes.Count} new nodes added to path {pathId}, merged {pathsToMerge.Count} existing paths");
        }
        
        // REMOVAL
        private void OnNodeRemoved(NodeRemoved obj)
        {
            Node removedNode = obj.Node;
            if (!removedNode.HasPathId()) return;
    
            int pathId = removedNode.PathId;
            RemoveNodeFromPath(removedNode, pathId);
    
            // Check if the path should be split
            CheckForPathSplit(pathId);
        }
        
        private void RemoveNodeFromPath(Node node, int pathId)
        {
            if (_paths.TryGetValue(pathId, out PathInfo pathInfo))
            {
                pathInfo.Nodes.Remove(node);
                pathInfo.LastModified = DateTime.Now;
                _paths[pathId] = pathInfo;
            }
    
            _nodeToPathId.Remove(node);
            node.SetPathId(-1);
        }
        
        private void CheckForPathSplit(int pathId)
        {
            if (!_paths.TryGetValue(pathId, out PathInfo pathInfo) || pathInfo.Nodes.Count == 0)
            {
                _paths.Remove(pathId);
                return;
            }
    
            // Use flood fill to find connected components
            List<HashSet<Node>> connectedGroups = FindConnectedGroups(pathInfo.Nodes);
    
            if (connectedGroups.Count <= 1) return; // Still one connected path
    
            // Split into separate paths
            bool first = true;
            foreach (HashSet<Node> group in connectedGroups)
            {
                if (first)
                {
                    // Keep the original path ID for the first group
                    pathInfo.Nodes = group;
                    _paths[pathId] = pathInfo;
                    first = false;
                }
                else
                {
                    // Create a new path for other groups
                    int newPathId = _nextPathId++;
                    PathInfo newPath = new PathInfo(newPathId);
                    newPath.Nodes = group;
                    _paths[newPathId] = newPath;
            
                    // Update node references
                    foreach (Node node in group)
                    {
                        node.SetPathId(newPathId);
                        _nodeToPathId[node] = newPathId;
                    }
            
                    // Debug.Log($"Split path {pathId} - created new path {newPathId}");
                }
            }
        }
        
        private List<HashSet<Node>> FindConnectedGroups(HashSet<Node> nodes)
        {
            List<HashSet<Node>> groups = new();
            HashSet<Node> visited = new();
    
            foreach (Node node in nodes)
            {
                if (visited.Contains(node)) continue;
        
                HashSet<Node> group = new();
                FloodFill(node, group, visited, nodes);
        
                if (group.Count > 0)
                    groups.Add(group);
            }
    
            return groups;
        }

        private void FloodFill(Node start, HashSet<Node> group, HashSet<Node> visited, HashSet<Node> validNodes)
        {
            if (!visited.Add(start) || !validNodes.Contains(start)) return;

            group.Add(start);

            // Check forward connections (existing code)
            foreach (Node target in start.TargetNodes)
            {
                FloodFill(target, group, visited, validNodes);
            }
            
            // NEW: Check backward connections to ensure bidirectional traversal
            if (start.TryGetInputNode(out Node backwardNode) && validNodes.Contains(backwardNode))
            {
                FloodFill(backwardNode, group, visited, validNodes);
            }
            
            // NEW: For intersections, check side connections
            if (start.NodeType == NodeType.Intersection)
            {
                foreach (Direction dir in new[] { 
                    DirectionUtils.Increment(start.Direction), 
                    DirectionUtils.Decrement(start.Direction) 
                })
                {
                    if (start.TryGetNeighbour(dir, out Node neighbor) && validNodes.Contains(neighbor))
                    {
                        FloodFill(neighbor, group, visited, validNodes);
                    }
                }
            }
        }
    }
}