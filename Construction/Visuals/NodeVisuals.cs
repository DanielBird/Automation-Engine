using UnityEngine;

namespace Construction.Visuals
{
    public class NodeVisuals : MonoBehaviour
    {
        [Header("Arrows")]
        public MeshRenderer outputArrowRenderer;
        public MeshRenderer inputArrowMeshRenderer;
        [SerializeField] private Material outputArrowMat;
        [SerializeField] private Material inputArrowMat;
        private bool _outputMatFound;
        private bool _inputMatFound; 
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        
        private void Awake()
        {
            if (outputArrowRenderer != null)
            {
                outputArrowMat = outputArrowRenderer.sharedMaterial;
                _outputMatFound = true; 
            }

            if (inputArrowMeshRenderer != null)
            {
                inputArrowMat = inputArrowMeshRenderer.sharedMaterial;
                _inputMatFound = true; 
            }
            
            HideArrows();
        }
        
        public void ShowArrows()
        {
            ShowOutputArrow();
            ShowInputArrow();
        }

        public void ShowOutputArrow()
        {
            if(_outputMatFound) outputArrowMat.SetFloat(Alpha, 1);
        }
        
        public void ShowInputArrow()
        {
            if(_inputMatFound) inputArrowMat.SetFloat(Alpha, 1);
        }
        
        public void HideArrows()
        {
            HideInputArrow();
            HideOutputArrow();
        }

        public void HideOutputArrow()
        {
            if(_outputMatFound) outputArrowMat.SetFloat(Alpha, 0);
        }

        public void HideInputArrow()
        {
            if(_inputMatFound) inputArrowMat.SetFloat(Alpha, 0);
        }
    }
}