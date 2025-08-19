using Engine.Construction.Placement;
using UnityEngine;

namespace Engine.Construction.Utilities
{
    public static class FloorPosition
    {
        /// <summary>
        /// Casts a ray from the main camera through the mouse position to find where it intersects with the floor.
        /// </summary>
        /// <returns>True if the ray hit the floor, false otherwise</returns>
        public static bool Get(UnityEngine.Camera mainCamera, float raycastDistance, PlacementSettings settings, out Vector3 position)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, raycastDistance, settings.floorLayer))
            {
                position = hit.point;
                return true;
            }

            position = Vector3.zero;
            return false;
        }
    }
}