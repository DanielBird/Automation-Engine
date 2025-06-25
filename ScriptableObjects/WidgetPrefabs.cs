using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "WidgetPrefabs", menuName = "Scriptable Objects/WidgetPrefabs", order = 0)]
    public class WidgetPrefabs : ScriptableObject
    {
        public List<GameObject> widgets;
    }
}