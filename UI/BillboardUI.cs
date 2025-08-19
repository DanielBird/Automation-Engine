using UnityEngine;

namespace Engine.UI
{
    public class BillboardUI : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        private void Start()
        {
            if(mainCamera == null) mainCamera = Camera.main;
        }

        public void InitialiseCamera(Camera cameraMain) => mainCamera = cameraMain;
        
        private void LateUpdate()
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
