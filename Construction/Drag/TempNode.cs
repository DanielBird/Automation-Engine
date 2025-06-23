using Construction.Nodes;
using UnityEngine;

namespace Construction.Drag
{
    public readonly struct TempNode
    {
        public GameObject Prefab { get; }
        public Node Node { get; }
        
        public TempNode(GameObject prefab, Node node)
        {
            Prefab = prefab;
            Node = node;
        }
    }
} 