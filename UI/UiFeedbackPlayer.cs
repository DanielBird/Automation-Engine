using UnityEngine;
using UnityEngine.EventSystems;

namespace Engine.UI
{
    [RequireComponent(typeof(UiScaleLerp))]
    public class UiFeedbackPlayer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        public UiScaleLerp uiScaleLerp;

        [Space]
        public bool playOnEnter; 
        public bool playOnExit;
        public bool playOnClick;
        
        [Space]
        public Vector2 scaleOnEnter = Vector2.one;
        public Vector2 scaleOnExit = Vector2.one;
        public Vector2 scaleOnClick = Vector2.one;

        [Space] public float tweenDuration = 0.2f; 
        
        private void Awake()
        {
            if (uiScaleLerp == null)
                uiScaleLerp = GetComponent<UiScaleLerp>();
        }


        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if(!playOnEnter) return;
            uiScaleLerp.DoTween(scaleOnEnter, tweenDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!playOnClick) return;
            uiScaleLerp.DoTween(scaleOnClick, tweenDuration);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!playOnExit) return;
            uiScaleLerp.DoTween(scaleOnExit, tweenDuration);
        }
    }
}
