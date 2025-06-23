using Construction.Interfaces;
using UnityEngine;

namespace Construction.Placement
{
    [System.Serializable]
    public class PlacementState
    {
        public bool IsRunning { get; set; }
        public GameObject CurrentObject { get; set; }
        public Vector3Int TargetPosition { get; set; }
        public Vector3Int GridPosition { get; set; }
        public Direction CurrentDirection { get; set; }
        public Axis CurrentAxis { get; set; }
        
        
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
    }
} 