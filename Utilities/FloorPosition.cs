using UnityEngine;

namespace Utilities
{
    public static class FloorPosition
    {
        public static bool Get(UnityEngine.Camera mainCamera, float raycastDistance, LayerMask floorLayer, out Vector3 position)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, raycastDistance, floorLayer))
            {
                position = hit.point;
                return true;
            }

            position = Vector3.zero;
            return false;
        }
    }
}