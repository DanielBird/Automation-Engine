using Engine.Construction.Resources;
using UnityEngine;

namespace Engine.Construction.Maps
{
    public interface IResourceMap
    {
        bool TryGetResourceSourceAt(Vector3Int gridCoord, out IResourceSource src);
        void Disable();
    }
}