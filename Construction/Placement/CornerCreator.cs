using Engine.Construction.Drag;
using Engine.Construction.Interfaces;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Placement
{
    public class CornerCreator
    {
        private readonly IMap _map;
        private readonly INodeMap _nodeMap;
        private readonly PlacementState _state;
        private readonly PlacementSettings _settings;
        private readonly Transform _transform;
        
        private const int RightTurn = 1;
        private const int LeftTurn = 3;

        public CornerCreator(IMap map, INodeMap nodeMap, PlacementState state, PlacementSettings settings, Transform transform)
        {
            _map = map; 
            _nodeMap = nodeMap;
            _state = state;
            _settings = settings;
            _transform = transform;
        }
        
        public void Replace(Node toReplace, Node otherNode, Target target, DragPos? dragPosition = null)
        {
            _nodeMap.DeregisterNode(toReplace);

            Direction oldDirection;
            Direction newDirection;

            if (dragPosition.HasValue)
            {
                Node oldNode = dragPosition.Value == DragPos.Start ? otherNode : toReplace;
                Node newNode = dragPosition.Value == DragPos.Start ? toReplace : otherNode;

                oldDirection = oldNode.Direction;
                newDirection = newNode.Direction;
            }
            else
            {
                oldDirection = toReplace.Direction;
                newDirection = otherNode.Direction;
            }

            SimplePool.Despawn(toReplace.gameObject);

            NodeType nodeType;

            Direction prefabOld = oldDirection;
            Direction prefabNew = newDirection;

            // In neighbour replacement case (no dragPosition provided), flip order unless target is Backward
            if (!dragPosition.HasValue && target != Target.Backward)
            {
                prefabOld = newDirection;
                prefabNew = oldDirection;
            }

            GameObject prefab = GetCornerPrefab(prefabOld, prefabNew, out nodeType);

            Vector3 position = dragPosition.HasValue ? toReplace.transform.position : (Vector3)toReplace.GridCoord;

            SpawnCorner(oldDirection, newDirection, target, prefab, toReplace.GridCoord, position, nodeType);
        }
        
        private GameObject GetCornerPrefab(Direction oldDirection, Direction newDirection, out NodeType nodeType)
        {
            // Calculate the relative turn; adding 4 ensures a positive number before modulo.
            int turn = ((int)newDirection - (int)oldDirection + 4) % 4;

            if (turn == RightTurn)
            {
                nodeType = NodeType.RightCorner; 
                return GetPrefab(nodeType);
            }

            if (turn == LeftTurn)
            {
                nodeType = NodeType.LeftCorner; 
                return GetPrefab(nodeType);
            }
            
            Debug.Log($"Attempting to spawn a prefab based on invalid directions: {oldDirection} and {newDirection}");
            
            nodeType = NodeType.Straight;
            if (_settings.prefabRegistry.FoundPrefab(nodeType, out GameObject prefab))
                return prefab;
                
            Debug.LogWarning($"Unable to find the prefab for {nodeType} in the prefab registry");
            return null; 
        }

        private GameObject GetPrefab(NodeType nodeType)
        {
            if (_settings.prefabRegistry.FoundPrefab(nodeType, out GameObject prefabToSpawn)) 
                return prefabToSpawn;
            
            Debug.LogWarning($"Unable to find the prefab for a the {nodeType} node in the prefab registry");
            return null;
        }
        
        private void SpawnCorner(Direction oldDirection, Direction newDirection, Target target, GameObject prefab, Vector3Int gridCoord, Vector3 position, NodeType nodeType)
        {
            if(prefab == null)
                return;
            
            GameObject cornerNode = SimplePool.Spawn(prefab, position, Quaternion.identity, _transform);
            cornerNode.name = $"{nodeType} _ {position.x} _ {position.z}";
            
            UpdateTheState(cornerNode);
            
            /*
            if (!cornerNode.TryGetComponent(out IPlaceable occupant))
            {
                Debug.LogError("Failed to get IOccupy component from corner prefab");
                return;
            }
            occupant.Place(gridCoord, _nodeMap);
            if (occupant is Node node)
            {
                Direction d = target != Target.Forward ? newDirection : oldDirection;
                NodeConfiguration config = NodeConfiguration.Create(_map, _nodeMap, nodeType, d, true, true);
                node.Initialise(config);
                node.Visuals.HideArrows();
            }*/
        }

        private void UpdateTheState(GameObject occupant)
        {
            _state.SetGameObject(occupant);

            if (_state.PlaceableFound)
            {
                _state.MainPlaceable.Reset();
                
                if(_state.MainPlaceable is Node node) 
                    node.Visuals.ShowArrows();
            }
        }
    }
}