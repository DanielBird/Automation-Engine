using System.Collections.Generic;
using Engine.Construction.Resources;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public interface IResourceMap
    {
        public Dictionary<Vector3Int, IResourceSource> Sources { get; set; }  
        bool TryGetResourceSourceAt(Vector3Int gridCoord, out IResourceSource src);
        void Disable();
    }
}