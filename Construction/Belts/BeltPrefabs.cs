using UnityEngine;

namespace Construction.Belts
{
    [CreateAssetMenu(fileName = "BeltPrefabs", menuName = "Construction/BeltConfiguration")]
    public class BeltPrefabs: ScriptableObject
    {
        public GameObject standardBeltPrefab;
        public GameObject leftBeltPrefab;
        public GameObject rightBeltPrefab;
    }
}