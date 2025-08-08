using System.Collections.Generic;
using UnityEngine;

namespace Construction.Nodes
{
    [System.Serializable]
    public struct NodePrefabBinding
    {
        public NodeType nodeType;
        public GameObject prefab;
    }
    
    [CreateAssetMenu(fileName = "NodePrefabBinding", menuName = "Node Prefab Binding")]
    public class NodePrefabRegistry : ScriptableObject
    {
        public List<NodePrefabBinding> nodePrefabs;
        
        public bool FoundPrefab(NodeType nodeType, out GameObject prefab)
        {
            prefab = null;
            if(!nodePrefabs.Exists(binding => binding.nodeType == nodeType)) return false;
            
            prefab = nodePrefabs.Find(binding => binding.nodeType == nodeType).prefab;
            return true;
        }
    }
}