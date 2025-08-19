using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Drag
{
    public readonly struct TempNode
    {
        public readonly GameObject Prefab;
        public readonly Node Node;
        
        public TempNode(GameObject prefab, Node node)
        {
            Prefab = prefab;
            Node = node;
        }
    }
} 