using UnityEngine;

namespace Engine.Construction.Resources
{
    [CreateAssetMenu(fileName = "ResourceType", menuName = "Scriptable Objects/ResourceType", order = 0)]
    public class ResourceTypeSo : ScriptableObject
    {
        [Tooltip("Should be a unique number.")]
        public int index;
        
        [Tooltip("Should be a unique string.")]
        public string resourceName;
        
        [Tooltip("The game object that will be transported along a belt path.")]
        public GameObject resourcePrefab;
        
        [Tooltip("The game object displayed by a consumer when a delivery has been processed by it.")]
        public GameObject consumptionDisplayPrefab;
    }
}