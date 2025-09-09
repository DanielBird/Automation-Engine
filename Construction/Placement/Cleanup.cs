using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public static class Cleanup
    {
        public static void RemovePlaceable(PlacementState state, IMap map)
        {
            if (state.PlaceableIsNode)
            {
                RemoveNode(state.Node, state.TargetGridCoordinate, map);
            }
            else
            {
                // Double check that the Placeable is not a node
                if (state.CurrentObject.TryGetComponent(out Node node))
                {
                    RemoveNode(node, node.GridCoord, map);
                }
                else
                {
                    SimplePool.Despawn(state.CurrentObject);
                }
            }
        }
        
        public static void RemoveNode(Node node, Vector3Int gridCoord, IMap map)
        {
            if (node == null)
            {
                Debug.Log("Attempted to remove a null node!");
                return;
            }
            
            if (!node.gameObject.activeInHierarchy)
                return;
            
            if (!node.isRemovable)
            {
                if(node.ParentNode == null) return;
                gridCoord = node.ParentNode.GridCoord;
                node = node.ParentNode;
            }
            
            node.OnRemoval();
            map.DeregisterOccupant(gridCoord.x, gridCoord.z, node.GridWidth, node.GridHeight);
            SimplePool.Despawn(node.gameObject);
        }
    }
}