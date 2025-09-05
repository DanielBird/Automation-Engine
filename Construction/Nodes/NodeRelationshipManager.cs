using System;
using System.Collections.Generic;
using Engine.Construction.Events;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Nodes
{
    public class NodeRelationshipManager : MonoBehaviour
    {        
        private EventBinding<NodePlaced> _onNodePlaced;
        private EventBinding<NodeRemoved> _onNodeRemoved;
        private EventBinding<NodeTargetEvent> _onNodeTargetChange;

        // Which nodes (Value) does each node (Key) target?
        private Dictionary<Node, HashSet<Node>> _nodeTargets = new();
        // Which nodes (Value) target this node (Key)? 
        private Dictionary<Node, HashSet<Node>> _nodeSources = new();
        
        private void Awake()
        {
            _onNodePlaced = new EventBinding<NodePlaced>(OnNodePlaced);
            _onNodeRemoved = new EventBinding<NodeRemoved>(OnNodeRemoved);
            _onNodeTargetChange = new EventBinding<NodeTargetEvent>(OnNodeTargetChange);
            
            EventBus<NodePlaced>.Register(_onNodePlaced);
            EventBus<NodeRemoved>.Register(_onNodeRemoved);
            EventBus<NodeTargetEvent>.Register(_onNodeTargetChange);
        }

        private void OnDisable()
        {
            EventBus<NodePlaced>.Deregister(_onNodePlaced);
            EventBus<NodeRemoved>.Deregister(_onNodeRemoved);
            EventBus<NodeTargetEvent>.Deregister(_onNodeTargetChange);
        }

        private void OnNodePlaced(NodePlaced placedEvent)
        {
            Node node = placedEvent.Node;
            
            // Handle forward relationship (this node -> forward node)
            if (node.TryGetForwardNode(out Node forwardNode))
            {
                AddRelationship(node, forwardNode);
            }
            
            // Handle backward relationship (backward node -> this node)
            if (node.TryGetBackwardNode(out Node backwardNode))
            {
                AddRelationship(backwardNode, node);
            }
        }
        
        private void AddRelationship(Node shippingNode, Node targetNode)
        {
            EnsureNodeExists(shippingNode);
            EnsureNodeExists(targetNode);
            
            // Don't add the relationship if it can't be formed, e.g., due to loops
            if (!shippingNode.AddTargetNode(targetNode)) return;

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

        private void OnNodeTargetChange(NodeTargetEvent targetEvent)
        {
            
        }
        
    }
}