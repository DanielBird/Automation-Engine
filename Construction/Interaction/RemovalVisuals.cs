using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Placement;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSettings = Engine.GameState.InputSettings;

namespace Engine.Construction.Interaction
{
    [RequireComponent(typeof(RemovalManager))]
    public class RemovalVisuals : MonoBehaviour
    {
        private static readonly int HalfExtents = Shader.PropertyToID("_halfExtents");
        [SerializeField] protected InputSettings inputSettings;
        private CancellationTokenSource _rightClickDragTokenSource;
        
        [SerializeField] private RemovalManager removalManager;
        public Material material;  
        
        private GameObject quad; 
        private MeshRenderer quadRenderer;
        
        [Space]
        public float yOffset = 0.01f;  
        public Vector2 gridOffset = new (0.5f, 0.5f);
        public float lerpSpeed = 35f; 
        
        private void Awake()
        {
            removalManager = GetComponent<RemovalManager>();
            quad = SpawnQuad(); 
            inputSettings.cancel.action.performed += ShowVisuals;
        }

        private void OnDisable()
        {
            inputSettings.cancel.action.performed -= ShowVisuals;
            ClearToken();
        }
        
        private void ClearToken()
        {
            if(_rightClickDragTokenSource == null) return; 
            _rightClickDragTokenSource.Cancel();
            _rightClickDragTokenSource.Dispose();
            _rightClickDragTokenSource = null;
        }

        private void ShowVisuals(InputAction.CallbackContext ctx)
        {
            ClearToken();
            _rightClickDragTokenSource = new CancellationTokenSource();
            DetectRightClickDown(_rightClickDragTokenSource.Token).Forget();
        }

        private async UniTaskVoid DetectRightClickDown(CancellationToken token)
        {
            await UniTask.WaitForSeconds(inputSettings.waitForInputTime, cancellationToken: token);

            if (!inputSettings.cancel.action.IsPressed())
                return;
            
            if (!removalManager.TryGetGridAlignedWorldPosition(out Vector3Int start))
            {
                Debug.Log("Failed to detect grid coordinate");
                return;
            }

            SetupQuad(start);
            
            while (inputSettings.cancel.action.IsPressed())
            {
                if(removalManager.TryGetGridAlignedWorldPosition(out Vector3Int current))
                    UpdateQuad(start, current);
                
                await UniTask.Yield(token);
            }
            
            CleanUpQuad();
            ClearToken();
        }

        private void SetupQuad(Vector3 start)
        {
            quad.transform.position = start;
            quad.transform.localScale = Vector3.zero;
            quadRenderer.enabled = true;
        }

        private void UpdateQuad(Vector3 a, Vector3 b)
        {
            Transform t = quad.transform;
            Vector3 pos = t.position;
            Vector3 scale = t.localScale;
            
            Vector2 a2 = new (a.x, a.z);
            Vector2 b2 = new (b.x, b.z);

            Vector2 min = Vector2.Min(a2, b2) - gridOffset;
            Vector2 max = Vector2.Max(a2, b2) + gridOffset;
            
            Vector2 size = max - min;
            Vector2 center2 = (min + max) * 0.5f;

            Vector3 newPos = new Vector3(center2.x, a.y + yOffset, center2.y);
            Vector3 newScale = new Vector3(Mathf.Max(size.x, 0.001f), 1f, Mathf.Max(size.y, 0.001f));
            
            t.position = Vector3.Lerp(pos, newPos, lerpSpeed * Time.deltaTime);
            t.localScale = Vector3.Lerp(scale, newScale, lerpSpeed * Time.deltaTime);
            
            material.SetVector(HalfExtents, new Vector4(size.x/2, size.y/2, 0, 0));
        }

        private void CleanUpQuad()
        {
            quadRenderer.enabled = false;
        }
        
        private GameObject SpawnQuad()
        {
            // Build a lightweight 1x1 quad on XZ centered at origin
            GameObject g = new ("DragQuad", typeof(MeshFilter), typeof(MeshRenderer));
            MeshFilter mf = g.GetComponent<MeshFilter>();
            mf.mesh = BuildUnitQuadXZ();
            EnsureMaterial(g);
            g.transform.parent = transform;
            return g;
        }
        
        private void EnsureMaterial(GameObject go)
        {
            quadRenderer = go.GetComponent<MeshRenderer>();
            if (quadRenderer != null && material != null)
            {
                quadRenderer.enabled = false;
                quadRenderer.sharedMaterial = material;
            }
        }
        
        private static Mesh BuildUnitQuadXZ()
        {
            Mesh m = new Mesh();
            m.name = "UnitQuadXZ";
            m.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f, -0.5f),
                new Vector3( 0.5f, 0f,  0.5f),
            };
            m.uv = new[]
            {
                new Vector2(0,0), new Vector2(0,1),
                new Vector2(1,0), new Vector2(1,1),
            };
            m.triangles = new[] { 0,1,2, 2,1,3 };
            m.RecalculateNormals();
            return m;
        }
    }
}