using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public static class Cleanup
    {
        public static void RemovePlaceable(PlacementState state, IWorld world)
        {
            if (state.PlaceableIsNode)
            {
                RemoveNode(state.Node, state.TargetGridCoordinate, world);
            }
            else
            {
                // Double-check that the Placeable is not a node
                if (state.CurrentObject.TryGetComponent(out Node node))
                {
                    RemoveNode(node, node.GridCoord, world);
                }
                else
                {
                    SimplePool.Despawn(state.CurrentObject);
                }
            }
        }
        
        public static void RemoveNode(Node node, Vector3Int gridCoord, IWorld world, bool deregister = true)
        {
            if (node == null)
            {
                Debug.Log("Attempted to remove a null node!");
                return;
            }

            if (!node.gameObject.activeInHierarchy)
            {
                Debug.Log("Attempted to remove a non-active node!");
                return;
            }
            
            if (!node.isRemovable)
            {
                if(node.ParentNode == null) return;
                gridCoord = node.ParentNode.GridCoord;
                node = node.ParentNode;
            }
            
            node.OnRemoval();
            world.TryRemoveNode(node);
            SimplePool.Despawn(node.gameObject);
        }
    }
}