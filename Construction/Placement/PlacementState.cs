using Engine.Construction.Interfaces;
using UnityEngine;

namespace Engine.Construction.Placement
{
    [System.Serializable]
    public class PlacementState
    {
        public bool IsRunning { get; set; }
        public GameObject CurrentObject { get; private set; }
        public Vector3Int TargetGridCoordinate { get; set; }
        public Vector3Int WorldAlignedPosition { get; set; }
        public Direction CurrentDirection { get; private set; }
        public Axis CurrentAxis { get; private set; }
        
        public bool PlaceableFound { get; private set; }
        public IPlaceable MainPlaceable { get; private set; }
        
        public bool RotatableFound { get; private set; }
        public IRotatable MainRotatable { get; private set; }

        public void SetGameObject(GameObject gameObject)
        {
            CurrentObject = gameObject;
            IsRunning = true; 
            
            if (CurrentObject.TryGetComponent(out IPlaceable placeable))
                SetPlaceable(placeable); 
            
            if (CurrentObject.TryGetComponent(out IRotatable rotatable))
                SetRotatable(rotatable);
        }
        
        private void SetPlaceable(IPlaceable placeable)
        {
            MainPlaceable = placeable;
            PlaceableFound = true;
        }
        
        private void SetRotatable(IRotatable rotatable)
        {
            MainRotatable = rotatable;
            RotatableFound = true;
        }
        public void SetDirection(Direction direction) => CurrentDirection = direction;
        
        public void SetAxis(Axis axis) => CurrentAxis = axis;
    }
} 