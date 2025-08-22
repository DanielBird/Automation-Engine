using System;
using Engine.Construction.Events;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Resources
{
    public class ResourceDeposit : MonoBehaviour, IResourceSource
    {
        [SerializeField] private ResourceTypeSo resource;

        [Tooltip("Does this area spawn resources for ever?")]
        public bool infiniteSupply;
       
        [Tooltip("The available supply of the resource")]
        [SerializeField] private int amount = 1000; 
        [SerializeField] private Vector3Int gridCoord;
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;
        
        public ResourceTypeSo ResourceType => resource;
        public Vector3Int GridCoord => gridCoord;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public event Action<IResourceSource> OnDepleted;
        public bool IsDepleted => amount == 0;

        private void Awake()
        {
            EventBus<RegisterResourceEvent>.Raise(new RegisterResourceEvent(this, transform.position));
        }
        
        public void SetGridCoord(Vector3Int coord) => gridCoord = coord;
        
        public bool TryExtract(int requested, out int extracted)
        {
            if (infiniteSupply)
            {
                extracted = requested; 
                return true;
            }

            if (amount <= 0)
            {
                extracted = 0;
                return false;
            }
            
            extracted = Mathf.Min(requested, amount);
            amount -= extracted;
            
            if(amount == 0) OnDepleted?.Invoke(this);
            return extracted > 0;
        }

        private void OnDrawGizmosSelected()
        {
            // Currently ignores grid -> world conversion
            Gizmos.color = IsDepleted ? Color.red: Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(gridWidth, 2, gridHeight));
        }
    }
}