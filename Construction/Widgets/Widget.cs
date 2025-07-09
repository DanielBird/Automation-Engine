using System;
using System.Collections.Generic;
using Construction.Nodes;
using UnityEngine;
using Utilities;

namespace Construction.Widgets
{
    public enum WidgetStatus
    {
        Active,          
        Inactive  
    }

    public enum MoveType
    {
        Standard
    }
    
    [RequireComponent(typeof(Collider))]
    public class Widget : MonoBehaviour
    {
        public int ID { get; private set; }
        public int widgetType;
        public WidgetStatus Status { get; private set; }
        
        [Header("Movement")]
        public bool IsMoving { get; private set; }
        public float standardMoveSpeed = 1f; 
        
        public EasingFunctions.Ease ease = EasingFunctions.Ease.EaseInCubic;
        private EasingFunctions.Function _easeFunc;

        private Collider _collider;
        private Dictionary<MoveType, IWidgetMover> _movementStrategies;
        private Coroutine _moveCoroutine;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if(_collider == null)
                Debug.LogError("Missing Collider");
            
            _easeFunc = EasingFunctions.GetEasingFunction(ease);
            
            _movementStrategies = new Dictionary<MoveType, IWidgetMover>
            {
                { MoveType.Standard, new BasicWidgetMove(this, transform, _collider, _easeFunc, FinalizeMovement) }
            };
        }

        private void OnDisable()
        {
            if(_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        }

        public Widget(int id)
        {
            ID = id;
        }

        public void Move(MoveType moveType, Belt current, Belt next)
        {
            if (!_movementStrategies.ContainsKey(moveType))
            {
                Debug.Log("Missing a movement strategy for : " + moveType);
                return;
            }
            
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = _movementStrategies[moveType].Move(next.widgetTarget, standardMoveSpeed, current.Direction);
        }
        
        public void SetMoving(bool status) => IsMoving = status;

        private void FinalizeMovement() => _moveCoroutine = null;
    }
}