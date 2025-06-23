using System.Collections;
using Construction.Placement;
using Construction.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities;

namespace Construction.Visuals
{
    public class BeltArrowManager : MonoBehaviour
    {
        [SerializeField] private GameObject inputArrow;
        [SerializeField] private GameObject outputArrow;
        
        [SerializeField] private CanvasGroup arrowCanvas;
        [SerializeField] private Direction direction;

        private Coroutine _rotationRoutine;
        public float rotationTime = 0.25f;
        private EasingFunctions.Function _ease; 
        public EasingFunctions.Ease rotationEasing = EasingFunctions.Ease.EaseOutSine;
        
        private void Awake()
        {
            arrowCanvas.alpha = 0; 
            _ease = EasingFunctions.GetEasingFunction(rotationEasing); 
        }

        public void Show(Vector3Int position)
        {
            UpdatePosition(position);
            arrowCanvas.alpha = 1;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        [Button]
        public void UpdatePosition(Vector3 position)
        {
            inputArrow.transform.position = position;
            outputArrow.transform.position = position; 
        }

        [Button]
        public void Rotate()
        {
            int current = (int)direction;
            current++;

            if (current > 3) current = 0;
            direction = (Direction)current;
            
            if(_rotationRoutine != null) StopCoroutine(_rotationRoutine);
            _rotationRoutine = StartCoroutine(RunRotation()); 
        }
        
        public void Rotate(Direction newDirection)
        {
            direction = newDirection;
            if(_rotationRoutine != null) StopCoroutine(_rotationRoutine);
            _rotationRoutine = StartCoroutine(RunRotation());
        }
        
        private IEnumerator RunRotation()
        {
            Vector3 start = transform.localRotation.eulerAngles;
            Vector3 end = DirectionUtils.RotationFromDirection(direction);
            end.z = end.y;
            end.y = 0; 
            
            float t = 0;

            while (t < rotationTime)
            {
                float ease = _ease(0, 1, t / rotationTime); 
                float angle = Mathf.LerpAngle(start.z, end.z, ease);
                Quaternion q = Quaternion.Euler(new Vector3(0, 0, angle));
                inputArrow.transform.localRotation = q;
                outputArrow.transform.localRotation = q; 
                
                t += Time.deltaTime;
                yield return null; 
            }
            
            inputArrow.transform.localRotation = Quaternion.Euler(end);
            outputArrow.transform.localRotation = Quaternion.Euler(end);
            _rotationRoutine = null; 
        }

        public void Hide()
        {
            arrowCanvas.alpha = 0; 
        }
    }
}
