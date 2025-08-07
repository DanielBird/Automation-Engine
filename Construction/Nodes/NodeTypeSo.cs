using UnityEngine;

namespace Construction.Nodes
{
    [CreateAssetMenu(fileName = "NodeTpye", menuName = "Scriptable Objects/NodeType", order = 0)]
    public class NodeTypeSo : ScriptableObject
    {
        public int width;
        public int height;
        public bool draggable;
    }
}