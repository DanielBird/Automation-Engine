using UnityEngine;

namespace Engine.Construction.Resources
{
    [CreateAssetMenu(fileName = "ResourceType", menuName = "Scriptable Objects/ResourceType", order = 0)]
    public class ResourceTypeSo : ScriptableObject
    {
        public string resourceName;
        public GameObject resourcePrefab;
    }
}