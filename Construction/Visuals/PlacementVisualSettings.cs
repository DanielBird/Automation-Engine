using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    [CreateAssetMenu(fileName = "PlacementVisualSettings", menuName = "Automation Engine/PlacementVisualSettings", order = 0)]
    public class PlacementVisualSettings : ScriptableObject
    {
        [Header("Main")]
        public float placementTime = 0.6f;
        public Vector3 startingScale = Vector3.zero;
        public Vector3 endScale = Vector3.one; 
        
        [Space]
        public EasingFunctions.Ease scaleEasingFunction = EasingFunctions.Ease.EaseOutElastic;
        public EasingFunctions.Ease scaleDownEasingFunction = EasingFunctions.Ease.Linear;
        
        [Header("Floor Material")]
        public Material floorMaterial;
        public float lerpAlphaTime = 0.5f;
        public float minGridAlpha = 0.08f; 
        public float maxGridAlpha = 1;
        
        [Header("Removal")]
        public Material destructionIndicatorMaterial;
        public float yOffset = 0.01f;  
        public Vector2 gridOffset = new (0.5f, 0.5f);
        public float lerpSpeed = 35f;
    }
}