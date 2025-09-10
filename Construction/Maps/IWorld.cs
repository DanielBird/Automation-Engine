using System.Collections.Generic;
using Engine.Construction.Interfaces;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public interface IWorld
    {
        // Lifecycle
        void Disable();

        // Utilities
        int Version();
        int MapWidth();
        int MapHeight();
        Vector2Int MapDimensions();
        bool InBounds(int x, int y);

        // General Occupants
        CellStatus[,] Grid();
        bool TryPlaceOccupant(Vector3Int gridCoord, IPlaceable placeable);
        void RemoveOccupant(int x, int z, int width, int height);
        bool VacantSpace(int x, int z, int width, int height);
        bool VacantCell(int x, int z);
        Vector2Int NearestVacantCell(Vector2Int start);

        // Nodes
        bool TryPlaceNode(Node node);
        bool TryPlaceNodeAt(Node node, int x, int z);
        bool TryRemoveNode(Node node);
        bool TryGetNode(int x, int z, out Node node);
        HashSet<Node> GetNodes();
        bool HasNode(int x, int z);
        bool GetNeighbour(int x, int z, Direction direction, out Node node);
        bool GetNeighbourAt(Vector2Int position, out Node node);
    }
}


