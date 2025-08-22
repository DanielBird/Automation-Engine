using System.Threading;
using Cysharp.Threading.Tasks;
using Engine.Construction.Placement;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSettings = Engine.GameState.InputSettings;

namespace Engine.Construction.Interaction
{
    public class RemovalVisuals
    {
        private static readonly int HalfExtents = Shader.PropertyToID("_halfExtents");
        private readonly InputSettings _inputSettings;
        private CancellationTokenSource _rightClickDragTokenSource;
        
        private readonly RemovalManager _removalManager;
        private readonly Material _destructionIndicatorMaterial;  
        
        private GameObject quad; 
        private MeshRenderer quadRenderer;

        private readonly float _yOffset;  
        private readonly Vector2 _gridOffset;
        private readonly float _lerpSpeed;
        private readonly Transform _transform;
        
        public RemovalVisuals(InputSettings inputSettings, RemovalManager removalManager, Material destructionIndicatorMaterial, float yOffset, float lerpSpeed, Vector2 gridOffset, Transform myTransform)
        {
            _inputSettings = inputSettings;
            _removalManager = removalManager;
            
            _destructionIndicatorMaterial = destructionIndicatorMaterial;
            _yOffset = yOffset;
            _gridOffset = gridOffset;
            _lerpSpeed = lerpSpeed;
            _transform = myTransform;
            
            quad = SpawnQuad(); 
            _inputSettings.cancel.action.performed += ShowVisuals;
        }
        
        public void Disable()
        {
            _inputSettings.cancel.action.performed -= ShowVisuals;
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
            await UniTask.WaitForSeconds(_inputSettings.waitForInputTime, cancellationToken: token);

            if (!_inputSettings.cancel.action.IsPressed())
                return;
            
            if (!_removalManager.TryGetGridAlignedWorldPosition(out Vector3Int start))
            {
                Debug.Log("Failed to detect grid coordinate");
                return;
            }

            SetupQuad(start);
            
            while (_inputSettings.cancel.action.IsPressed())
            {
                if(_removalManager.TryGetGridAlignedWorldPosition(out Vector3Int current))
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

            Vector2 min = Vector2.Min(a2, b2) - _gridOffset;
            Vector2 max = Vector2.Max(a2, b2) + _gridOffset;
            
            Vector2 size = max - min;
            Vector2 center2 = (min + max) * 0.5f;

            Vector3 newPos = new Vector3(center2.x, a.y + _yOffset, center2.y);
            Vector3 newScale = new Vector3(Mathf.Max(size.x, 0.001f), 1f, Mathf.Max(size.y, 0.001f));
            
            t.position = Vector3.Lerp(pos, newPos, _lerpSpeed * Time.deltaTime);
            t.localScale = Vector3.Lerp(scale, newScale, _lerpSpeed * Time.deltaTime);
            
            _destructionIndicatorMaterial.SetVector(HalfExtents, new Vector4(size.x/2, size.y/2, 0, 0));
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
            g.transform.parent = _transform;
            return g;
        }
        
        private void EnsureMaterial(GameObject go)
        {
            quadRenderer = go.GetComponent<MeshRenderer>();
            if (quadRenderer != null && _destructionIndicatorMaterial != null)
            {
                quadRenderer.enabled = false;
                quadRenderer.sharedMaterial = _destructionIndicatorMaterial;
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