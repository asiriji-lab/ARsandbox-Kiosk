using UnityEngine;
using ARSandbox.Core;

namespace ARSandbox.UI
{
    public class CalibrationView : MonoBehaviour
    {
        [Header("References")]
        public AlignmentSystem AlignmentSystem;
        public GameObject CrosshairPrefab; // Prefab with a SpriteRenderer
        public Material FeedbackMaterial; // Material to change color based on state
        
        private GameObject _crosshairInstance;
        private SpriteRenderer _crosshairRenderer;
        
        void Start()
        {
            if (AlignmentSystem == null) AlignmentSystem = FindFirstObjectByType<AlignmentSystem>();
            
            // Create a simple crosshair if no prefab
            if (CrosshairPrefab == null)
            {
                _crosshairInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(_crosshairInstance.GetComponent<Collider>());
                _crosshairInstance.name = "CalibrationCrosshair";
                _crosshairRenderer = _crosshairInstance.AddComponent<SpriteRenderer>();
                // Load a default circle or cross logic here if needed, or just use colored quad
                _crosshairRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            else
            {
                _crosshairInstance = Instantiate(CrosshairPrefab);
                _crosshairRenderer = _crosshairInstance.GetComponent<SpriteRenderer>();
            }
            
            _crosshairInstance.SetActive(false);
        }
        
        void Update()
        {
            if (AlignmentSystem == null || !AlignmentSystem.IsCalibrating)
            {
                if (_crosshairInstance.activeSelf) _crosshairInstance.SetActive(false);
                return;
            }
            
            // Show Crosshair
            if (!_crosshairInstance.activeSelf) _crosshairInstance.SetActive(true);
            
            // Position Crosshair
            if (AlignmentSystem.CurrentPointIndex < AlignmentSystem.TargetUVs.Count)
            {
                Vector2 uv = AlignmentSystem.TargetUVs[AlignmentSystem.CurrentPointIndex];
                
                // Convert UV (0-1) to Screen/World Position
                // Assuming Orthographic Projector Camera or UI Overlay
                // For a projector-based system, we usually render this to a Canvas that fills the screen.
                // Let's assume World Space UI for now at Z=0 (Projector Plane).
                
                Camera cam = AlignmentSystem.ProjectorCamera;
                if (cam != null)
                {
                    // ViewportToWorldPoint requires Z depth. 
                    // We put it at specific distance or use Overlay Canvas.
                    Vector3 pos = cam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, 2.0f)); 
                    _crosshairInstance.transform.position = pos;
                    _crosshairInstance.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
                }
            }
            
            // Feedback Color (Green = Processing/Good, White = Waiting)
            // Ideally AlignmentSystem exposes a "DiskDetected" state.
             _crosshairRenderer.color = Color.white; 
        }
        
        public void OnStartCalibrationClick()
        {
            AlignmentSystem.StartCalibration();
        }
    }
}
