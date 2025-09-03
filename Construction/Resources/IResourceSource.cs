using System;
using UnityEngine;

namespace Engine.Construction.Resources
{
    public interface IResourceSource
    {
        public ResourceTypeSo ResourceType { get; }
        
        bool IsDepleted { get; }
        event Action<IResourceSource> OnDepleted;
        Vector3Int GridCoord { get; }
        int GridWidth { get; }
        int GridHeight { get; }
        
        void SetGridCoord(Vector3Int coord);
        bool TryExtract(int amount, out int extracted);

        void RegisterProducerPlaced();
    }
}