using System.Collections.Generic;
using UnityEngine;

namespace Engine.Construction.Widgets
{
    [CreateAssetMenu(fileName = "WidgetPrefabs", menuName = "Scriptable Objects/WidgetPrefabs", order = 0)]
    public class WidgetPrefabsSO : ScriptableObject
    {
        public List<GameObject> widgets;
    }
}