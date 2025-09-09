using System;
using System.Collections.Generic;
using Engine.Construction.Events;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Nodes
{
    public class NodeRelationshipManager
    {        
        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeGroupPlaced> _onNodeGroupPlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        
        // Which nodes (Value) does each node (Key) target?
        private Dictionary<Node, HashSet<Node>> _nodeTargets = new();
        // Which nodes (Value) target this node (Key)? 
        private Dictionary<Node, HashSet<Node>> _nodeSources = new();
        
        public NodeRelationshipManager()
        {
            _onNodePlaced = new EventBinding<NodePlaced>(OnNodePlaced);
            _onNodeGroupPlaced = new EventBinding<NodeGroupPlaced>(OnNodeGroupPlaced); 
            _onNodeRemoved = new EventBinding<NodeRemoved>(OnNodeRemoved);
            
            EventBus<NodePlaced>.Register(_onNodePlaced);
            EventBus<NodeGroupPlaced>.Register(_onNodeGroupPlaced);
            EventBus<NodeRemoved>.Register(_onNodeRemoved);
        }

        public void Disable()
        {
            EventBus<NodePlaced>.Deregister(_onNodePlaced);
            EventBus<NodeGroupPlaced>.Deregister(_onNodeGroupPlaced);
            EventBus<NodeRemoved>.Deregister(_onNodeRemoved);
        }

        private void OnNodePlaced(NodePlaced placedEvent)
        {
            Node node = placedEvent.Node;
            AddForwardRelationship(node);
        }

        private void OnNodeGroupPlaced(NodeGroupPlaced e)
        {
            // Try to add the backward relationship for the start node
            AddBackwardRelationship(e.StartNode);
            
            // Try to add the forward relationship for all the nodes
            foreach (Node node in e.NodeGroup)
            {
                AddForwardRelationship(node);
            }
        }

        private void AddForwardRelationship(Node node)
        {
            // Handle forward relationship (this node -> forward node)
            if (node.TryGetForwardNode(out Node forwardNode))
                AddRelationship(node, forwardNode);
        }
        
        private void AddBackwardRelationship(Node node)
        {
            // Handle backward relationship (backward node -> this node)
            if (node.TryGetBackwardNode(out Node backwardNode))
                AddRelationship(backwardNode, node);
            
            // string log = backwardNode != null ? $"Found a backward node ({backwardNode.name}) to add {node.name} to" : $"{node.name} failed to find a backward node";
            // Debug.Log(log);
        }

        private void AddRelationship(Node shippingNode, Node targetNode)
        {
            EnsureNodeExists(shippingNode);
            EnsureNodeExists(targetNode);
            
            // Don't add the relationship if it already exists or can't be formed
            if (!shippingNode.AddTargetNode(targetNode))
                return;
            
            // Debug.Log($"Added {targetNode.name} to {shippingNode.name}");
            
            _nodeTargets[shippingNode].Add(targetNode);
            _nodeSources[targetNode].Add(shippingNode);
        }
        
        private void EnsureNodeExists(Node node)
        {
            if (!_nodeTargets.ContainsKey(node))
                _nodeTargets[node] = new HashSet<Node>();
            
            if (!_nodeSources.ContainsKey(node))
                _nodeSources[node] = new HashSet<Node>();
        }

        private void OnNodeRemoved(NodeRemoved removedEvent)
        {
            Node node = removedEvent.Node;
            ClearOutgoing(node);
            ClearIncoming(node);
        }

        private void ClearOutgoing(Node node)
        {
            // For each node recorded as receiving shipments from his node.
            // Remove this node from their list of sources
            
            if (!_nodeTargets.TryGetValue(node, out HashSet<Node> targets)) return;
            
            foreach (Node t in targets)
            {
                if (_nodeSources.TryGetValue(t, out HashSet<Node> refs))
                    refs.Remove(node); 
            }
                
            _nodeTargets.Remove(node);
        }

        private void ClearIncoming(Node node)
        {
            // For each node recorded as shipping to this node.
            // Remove this node from their list of targets.
            
            if (!_nodeSources.TryGetValue(node, out HashSet<Node> sources)) return;
            
            foreach (Node s in sources)
            {
                if(_nodeTargets.TryGetValue(s, out HashSet<Node> refs))
                    refs.Remove(node);
                
                s.RemoveTargetNode(node);
            }
            _nodeSources.Remove(node);
        }

        // Which nodes does this node target?
        public bool GetNodeTargets(Node node, out HashSet<Node> targets) =>  _nodeTargets.TryGetValue(node, out targets);
        
        // Which nodes target this node?
        public bool GetNodeSource(Node node, out HashSet<Node> sources) =>  _nodeSources.TryGetValue(node, out sources);
    }
}