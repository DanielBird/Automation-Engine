using Construction.Nodes;
using UnityEngine;

namespace Construction.Visuals
{
    public enum ConnectionType {InputOutput, InputOnly, OutputOnly}
    
    public class NodeVisuals : MonoBehaviour
    {
        [SerializeField] private Node myNode;  
        
        [Header("Arrows")]
        public GameObject mainMesh;
        public GameObject outputArrowPrefab;
        public GameObject inputArrowPrefab;
        
        [SerializeField] private Material outputArrowMat;
        [SerializeField] private Material inputArrowMat;
        
        private MeshRenderer mainMeshRenderer;
        private bool _rendererSet; 
        
        [Space]
        public ConnectionType connectionType = ConnectionType.InputOutput;
        
        private bool _outputMatFound;
        private bool _inputMatFound; 
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        
        private void Start()
        {
            if(myNode == null)
                myNode = GetComponent<Node>();

            if (connectionType is ConnectionType.InputOutput or ConnectionType.OutputOnly && outputArrowPrefab != null)
            {
                outputArrowMat = outputArrowPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                _outputMatFound = true; 
            }

            if (connectionType is ConnectionType.InputOutput or ConnectionType.InputOnly && inputArrowPrefab != null)
            {
                inputArrowMat = inputArrowPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                _inputMatFound = true; 
            }

            if (mainMesh != null)
            {
                mainMeshRenderer = mainMesh.GetComponent<MeshRenderer>();
                _rendererSet = true;
            }
                
            
            HideArrows();
        }
        
        public void ShowArrows()
        {
            if (connectionType is ConnectionType.InputOutput or ConnectionType.OutputOnly) ShowOutputArrow();
            if (connectionType is ConnectionType.InputOutput or ConnectionType.InputOnly) ShowInputArrow();
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
            if (connectionType is ConnectionType.InputOutput or ConnectionType.OutputOnly) HideOutputArrow();
            if (connectionType is ConnectionType.InputOutput or ConnectionType.InputOnly) HideInputArrow();
        }

        public void HideOutputArrow()
        {
            if(_outputMatFound) outputArrowMat.SetFloat(Alpha, 0);
        }

        public void HideInputArrow()
        {
            if(_inputMatFound) inputArrowMat.SetFloat(Alpha, 0);
        }

        public void DisableRenderer()
        {
            if(!_rendererSet) return;
            mainMeshRenderer.enabled = false;
        }

        public void EnableRenderer()
        {
            if(!_rendererSet) return;
            mainMeshRenderer.enabled = true;
        }
    }
}