using System.Collections.Generic;
using Engine.Construction.Belts;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using Engine.Utilities;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Resources
{
    public enum ResourceStatus
    {
        Active,          
        Inactive  
    }

    public enum MoveType
    {
        Forward,
        Backward,
        Left,
        Right,
    }
    
    [RequireComponent(typeof(Collider))]
    public class Resource : MonoBehaviour
    {
        public ResourceTypeSo ResourceType { get; private set; }
        public int ID { get; private set; }
        public int ResourceIndex { get; private set; }
        public ResourceStatus Status { get; private set; }
        
        [SerializeField] private PlacementSettings settings;
        
        [Header("Movement")]
        public bool IsMoving { get; private set; }
        public float standardMoveTime = 1f;
        public float cornerMoveTime = 1.2f; 
        
        public EasingFunctions.Ease ease = EasingFunctions.Ease.EaseInCubic;
        private EasingFunctions.Function _easeFunc;
        
        private Collider _collider;
        private Dictionary<MoveType, IResourceMover> _movementStrategies;
        private Coroutine _moveCoroutine;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            if(_collider == null)
                Debug.LogError("Missing Collider");
            
            _easeFunc = EasingFunctions.GetEasingFunction(ease);
            
            _movementStrategies = new Dictionary<MoveType, IResourceMover>
            {
                { MoveType.Forward, new BasicResourceMove(this, transform, _collider, standardMoveTime, _easeFunc, FinalizeMovement) },
                { MoveType.Right, new CornerResourceMove(this, MoveType.Right, transform, _collider, cornerMoveTime, _easeFunc, FinalizeMovement) },
                { MoveType.Left, new CornerResourceMove(this, MoveType.Left, transform, _collider, cornerMoveTime, _easeFunc, FinalizeMovement) },
            };
        }

        private void OnDisable()
        {
            if(_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        }

        public Resource(int id) => ID = id;

        public void SetResourceType(ResourceTypeSo rType)
        {
            ResourceType = rType;
            ResourceIndex = ResourceType.index;
        } 

        public void Move(MoveType moveType, Belt current, Belt next)
        {
            if (!_movementStrategies.ContainsKey(moveType))
            {
                Debug.Log("Missing a movement strategy for : " + moveType);
                return;
            }
            
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = _movementStrategies[moveType].Move(next, current.Direction);
        }
        
        public void SetMoving(bool status) => IsMoving = status;

        private void FinalizeMovement() => _moveCoroutine = null;

        public void CancelMovement()
        {
            if(_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        }

        public bool IsInCell(Vector3Int cell)
        {
            Vector3Int current = Grid.WorldToGridCoordinate(transform.position, new GridParams(settings.mapOrigin, settings.mapWidth, settings.mapHeight, settings.cellSize));
            return cell == current; 
        }
    }
}