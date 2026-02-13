using UnityEngine;
using ARSandbox.Core;
using UnityEngine.UI;

namespace ARSandbox.Systems
{
    public class CalibrationSystem : MonoBehaviour
    {
        private const float HANDLE_GRAB_THRESHOLD = 50f;

        private CalibrationLogic _logic = new CalibrationLogic();
        private SandboxViewModel _viewModel;
        
        private int _draggingIndex = -1;
        private RectTransform[] _handles;
        private RawImage _overlay;

        public bool IsCalibrating { get; private set; } = false;

        public void Initialize(SandboxViewModel vm, RectTransform[] handles, RawImage overlay)
        {
            _viewModel = vm;
            _handles = handles;
            _overlay = overlay;
        }

        public void SetCalibrationMode(bool active)
        {
            IsCalibrating = active;
            if (_overlay) _overlay.gameObject.SetActive(active);

            if (active)
            {
                // Refresh Visuals from Data
                RefreshHandles();
            }
        }

        public void RefreshHandles()
        {
            if (_viewModel == null || _handles == null) return;

            for (int i = 0; i < 4; i++)
            {
                if (i >= _viewModel.Settings.CalibrationPoints.Length || i >= _handles.Length) continue;

                Vector2 normPos = _viewModel.Settings.CalibrationPoints[i];
                // Logic: Convert Data (0-1, InvertedY) -> Visual (Screen Px)
                // InvertY matches the logic used in SandboxUI originally (1.0 - norm.y)
                Vector2 visualPos = _logic.ProcessDataToVisual(normPos, Screen.width, Screen.height, true);
                
                if (_handles[i] != null)
                    _handles[i].position = visualPos;
            }
        }

        public void ProcessInput(Vector2 mousePos, bool wasPressedThisFrame, bool isPressed, bool wasReleased)
        {
            if (!IsCalibrating) return;

            RefreshOverlayTexture();

            if (wasPressedThisFrame && _draggingIndex == -1)
            {
                Vector2[] handlePositions = ExtractHandlePositions();
                _draggingIndex = _logic.GetClosestHandle(mousePos, handlePositions, HANDLE_GRAB_THRESHOLD);
            }

            if (isPressed && _draggingIndex != -1)
            {
                // Dragging
                // 1. Update Visual immediately
                if (_handles[_draggingIndex])
                    _handles[_draggingIndex].position = mousePos;

                // 2. Update Data
                // Visual (Screen) -> Data (Normalized, Inverted Y)
                Vector2 uv = _logic.CalculateUV(mousePos, Screen.width, Screen.height);
                Vector2 dataPos = _logic.ProcessVisualToData(uv, true); // Invert Y

                _viewModel.Settings.CalibrationPoints[_draggingIndex] = dataPos;
            }

            if (wasReleased)
            {
                if (_draggingIndex != -1)
                {
                    _draggingIndex = -1;
                    _viewModel.SaveSettings();
                }
            }
        }

        private void RefreshOverlayTexture()
        {
            if (_viewModel == null || _viewModel.Controller == null || _overlay == null) return;

            Texture targetTex = _viewModel.Controller.GetColorTexture();
            if (targetTex == null) targetTex = _viewModel.Controller.GetRawDepthTexture();

            if (_overlay.texture != targetTex)
            {
                _overlay.texture = targetTex;
                _overlay.uvRect = new Rect(0, 1, 1, -1);
            }
        }

        private Vector2[] ExtractHandlePositions()
        {
            Vector2[] positions = new Vector2[_handles.Length];
            for (int i = 0; i < _handles.Length; i++)
            {
                if (_handles[i]) positions[i] = _handles[i].position;
            }
            return positions;
        }
    }
}
