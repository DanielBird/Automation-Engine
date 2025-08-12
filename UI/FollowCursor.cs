using System;
using UnityEngine;

namespace UI
{
    public class FollowCursor : MonoBehaviour
    {
        public Camera mainCamera; 
        private RectTransform _rect;
        public int z = 5; 

        private void Awake()
        {
            _rect = GetComponent<RectTransform>(); 
        }

        private void Update()
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = z;
            _rect.transform.position = mainCamera.ScreenToWorldPoint(mouse); 
        }
    }
}
