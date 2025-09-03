using System;
using UnityEngine;
using UnityEngine.Events;

namespace Engine.Utilities
{
    [RequireComponent(typeof(Collider))]
    public class ColliderTriggerEvents : MonoBehaviour
    {
        [SerializeField] private string triggerTag; 
        
        [Space]
        public UnityEvent triggerEnter;
        public UnityEvent triggerStay; 
        public UnityEvent triggerExit;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(triggerTag))
            {
                triggerEnter?.Invoke();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(triggerTag))
            {
                triggerStay?.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(triggerTag))
            {
                triggerExit?.Invoke();
            }
        }
        
    }
}