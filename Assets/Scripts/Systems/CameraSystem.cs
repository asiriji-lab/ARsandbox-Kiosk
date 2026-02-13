using UnityEngine;
using ARSandbox.Core;

namespace ARSandbox.Systems
{
    public class CameraSystem : MonoBehaviour
    {
        private const float ZOOM_MIN = -20f;
        private const float ZOOM_MAX = 15f;

        private CameraLogic _logic = new CameraLogic();
        
        [Header("State")]
        public CamView CurrentView = CamView.Top;
        public float ZoomOffset = 0f;

        private Camera _targetCamera;

        public void Initialize(Camera cam)
        {
            _targetCamera = cam;
        }

        public void CycleView(int direction)
        {
            CurrentView = _logic.GetNextView(CurrentView, direction);
            UpdateTransform();
        }

        public void AdjustZoom(float delta)
        {
            ZoomOffset += delta;
            ZoomOffset = Mathf.Clamp(ZoomOffset, ZOOM_MIN, ZOOM_MAX);
            UpdateTransform();
        }
        
        public void UpdateTransform(float meshSize = 10f)
        {
            if (_targetCamera == null) return;

            Pose pose = _logic.CalculateCameraTransform(CurrentView, meshSize, ZoomOffset);
            _targetCamera.transform.position = pose.position;
            _targetCamera.transform.rotation = pose.rotation;
            
            Debug.Log($"[CameraSystem] View: {CurrentView}, Zoom: {ZoomOffset:F1}");
        }
    }
}
