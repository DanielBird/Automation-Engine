using System.Collections.Generic;
using UnityEngine;

namespace Engine.Construction.Resources
{
    [CreateAssetMenu(fileName = "WidgetPrefabs", menuName = "Scriptable Objects/WidgetPrefabs", order = 0)]
    public class ResourcePrefabsSo : ScriptableObject
    {
        public List<GameObject> resources;
    }
}