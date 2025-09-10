using System.Collections.Generic;
using Engine.Construction.Events;
using Engine.Construction.Interfaces;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public class World : IWorld
    {
        private readonly IOccupancyMap occupancyMap;
        private readonly INodeMap _nodeMap;
        
        private Vector2Int mapDimensions;
        private GridParams gridParams; 
        private int _version;
        
        private EventBinding<RegisterOccupantEvent> _onRequestOccupation;
        private bool _eventsRegistered;

        public World(PlacementSettings ps)
        {
            occupancyMap = new OccupancyMap(ps);
            _nodeMap = new NodeMap(ps); 
            mapDimensions = new Vector2Int(ps.mapWidth, ps.mapHeight);
            gridParams = new GridParams(ps.mapOrigin, ps.mapWidth, ps.mapHeight, ps.cellSize);
        
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            _onRequestOccupation = new EventBinding<RegisterOccupantEvent>(OnRequestOccupation);
            EventBus<RegisterOccupantEvent>.Register(_onRequestOccupation);
            _eventsRegistered = true;
        }
        
        private void OnRequestOccupation(RegisterOccupantEvent ev)
        {
            Vector3Int gridCoord = Utilities.Grid.WorldToGridCoordinate(ev.WorldPosition, gridParams);

            if (!occupancyMap.TryPlaceOccupant(gridCoord.x, gridCoord.z, ev.GridWidth, ev.GridHeight))
            {
                Debug.LogWarning($"Failed to register occupant: {ev.Occupant.name}");
            }
        }

        public void Disable()
        {
            if (_eventsRegistered)
            {
                EventBus<RegisterOccupantEvent>.Deregister(_onRequestOccupation);
                _eventsRegistered = false;
            }
        }
        
        // Utilities
        
        public int Version() => _version;
        
        public int MapWidth() => mapDimensions.x;
        
        public int MapHeight() => mapDimensions.y;
        
        public Vector2Int MapDimensions() => mapDimensions;

        public bool InBounds(int x, int y) => occupancyMap.InBounds(x, y); 
        
        // General Occupants

        public CellStatus[,] Grid() => occupancyMap.Grid; 
        
        public bool TryPlaceOccupant(Vector3Int gridCoord, IPlaceable placeable)
        {
            if (placeable is Node node)
            {
                Debug.Log("Attempting to place a Node as a regular occupant. It will be registered as a Node instead.");
                return TryPlaceNodeAt(node, gridCoord.x, gridCoord.z);
            }
            
            Vector2Int size = placeable.GetSize();
            bool success = occupancyMap.TryPlaceOccupant(gridCoord.x, gridCoord.z, size.x, size.y);
            if (success) _version++; 
            return success;
        }

        public void RemoveOccupant(int x, int z, int width, int height)
        {
            occupancyMap.RemoveOccupant(x, z, width, height);
            _version++;
        }
        
        public bool VacantSpace(int x, int z, int width, int height) =>
            occupancyMap.VacantSpace(x, z, width, height); 

        public bool VacantCell(int x, int z) =>
            occupancyMap.VacantCell(x, z);

        public Vector2Int NearestVacantCell(Vector2Int start) =>
            occupancyMap.NearestVacantCell(start);
        
        // Nodes

        public bool TryPlaceNode(Node node)
        {
            if (!node.IsPlaced)
            {
                Debug.Log($"Attempting to the place {node.name} using grid coordinates that have not been initialized.");
                return false;
            }
            
            int x = node.GridCoord.x;
            int z = node.GridCoord.z;
            return TryPlaceNodeAt(node, x, z);
        }

        public bool TryPlaceNodeAt(Node node, int x, int z)
        {
            Vector2Int size = node.GetSize();
            
            if (!occupancyMap.VacantSpace(x, z, size.x, size.y))
                return false;
            
            if (_nodeMap.NodeIsRegisteredAt(node, x, z))
                return false;
            
            if (!occupancyMap.TryPlaceOccupant(x, z, size.x, size.y))
                return false;

            if (!_nodeMap.TryRegisterNodeAt(node, x, z))
            {
                occupancyMap.RemoveOccupant(x, z, size.x, size.y);
                return false;
            }
            
            _version++;
            return true;
        }

        public bool TryRemoveNode(Node node)
        {
            int x = node.GridCoord.x;
            int z = node.GridCoord.z;
            Vector2Int size = node.GetSize();
            
            if (!_nodeMap.NodeIsRegisteredAt(node, x, z))
                return false;
            
            if (!_nodeMap.TryDeregisterNode(node))
                return false;
            
            occupancyMap.RemoveOccupant(x, z, size.x, size.y);
            
            _version++;
            return true;
        }
        
        public bool TryGetNode(int x, int z, out Node node) => _nodeMap.TryGetNode(x, z, out node);
        
        public HashSet<Node> GetNodes() => _nodeMap.GetNodes();
        
        public bool HasNode(int x, int z) => _nodeMap.HasNode(x, z);
        
        public bool GetNeighbour(int x, int z, Direction direction, out Node node) => _nodeMap.GetNeighbour(x, z, direction, out node);
        
        public bool GetNeighbourAt(Vector2Int position, out Node node) => _nodeMap.GetNeighbourAt(position, out node);
    }
}