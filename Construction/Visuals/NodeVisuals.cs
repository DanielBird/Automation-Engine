using Engine.Construction.Nodes;
using UnityEngine;

namespace Engine.Construction.Visuals
{
    public enum ConnectionType {None, InputOutput, InputOnly, OutputOnly}
    
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
                
            
            // HideArrows();
        }
        
        public void ShowArrows()
        {
            // Debug.Log("ShowArrows on " + gameObject.name);
            if (connectionType is ConnectionType.InputOutput or ConnectionType.OutputOnly) ShowOutputArrow();
            if (connectionType is ConnectionType.InputOutput or ConnectionType.InputOnly) ShowInputArrow();
        }

        private void ShowOutputArrow()
        {
            if(!_outputMatFound) return;
            outputArrowMat.SetFloat(Alpha, 1);
        }

        private void ShowInputArrow()
        {
            if(!_inputMatFound) return;
            inputArrowMat.SetFloat(Alpha, 1);
        }
        
        public void HideArrows()
        {
            // Debug.Log("HideArrows on " + gameObject.name);
            if (connectionType is ConnectionType.InputOutput or ConnectionType.OutputOnly) HideOutputArrow();
            if (connectionType is ConnectionType.InputOutput or ConnectionType.InputOnly) HideInputArrow();
        }

        private void HideOutputArrow()
        {
            if(!_outputMatFound) return; 
            outputArrowMat.SetFloat(Alpha, 0);
        }

        private void HideInputArrow()
        {
            if(!_inputMatFound) return;
            inputArrowMat.SetFloat(Alpha, 0);
        }

        public void DisableRenderer()
        {
            if(!_rendererSet) return;
            mainMeshRenderer.enabled = false;
        }

        public void EnableRenderer()
        {
            if(!_rendererSet) return;
            if(mainMeshRenderer != null)
                mainMeshRenderer.enabled = true;
        }
    }
}