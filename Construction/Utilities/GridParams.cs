using UnityEngine;

namespace Engine.Construction.Utilities
{
    /// <summary>Immutable description of a grid in world space.</summary>

    public readonly struct GridParams
    {
        public readonly Vector3Int Origin;
        public readonly int Width;
        public readonly int Height;
        public readonly float CellSize;

        public GridParams(Vector3Int origin, int width, int height, float tileSize)
        {
            Origin = origin;
            Width = width;
            Height = height;
            CellSize = tileSize <= 0 ? 1f : tileSize;
        }
    }
}