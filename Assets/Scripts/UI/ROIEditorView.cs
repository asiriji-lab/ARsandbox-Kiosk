using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ARSandbox.UI
{
    public class ROIEditorView : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        public RawImage CameraPreviewImage;
        public RectTransform PointsContainer; // Parent for point markers
        public Button ConfirmButton;
        public Button ClearButton;
        
        [Header("State")]
        public List<Vector2> CapturedPoints = new List<Vector2>(); // UV coordinates (0-1)
        public bool IsActive = false;
        
        // Factory for visualized points (simple UI Image)
        private List<GameObject> _visualPoints = new List<GameObject>();
        
        public System.Action<List<Vector2>> OnBoundarySaved;
        public System.Action OnClose;

        private SandboxSettingsSO _settings;

        public void Initialize(SandboxSettingsSO settings)
        {
            _settings = settings;
        }

        void Start()
        {
            if(ConfirmButton) ConfirmButton.onClick.AddListener(SaveBoundary);
            if(ClearButton) ClearButton.onClick.AddListener(ClearPoints);
            ClearPoints();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            IsActive = true;
            ClearPoints();

            // Load existing points
            if (_settings != null && _settings.BoundaryPoints != null && _settings.BoundaryPoints.Length == 4)
            {
                foreach(var pt in _settings.BoundaryPoints)
                {
                    CapturedPoints.Add(pt);
                    
                    if (CameraPreviewImage != null)
                    {
                        // Convert UV to Local Point for visualization
                        RectTransform rt = CameraPreviewImage.rectTransform;
                        float x = (pt.x - 0.5f) * rt.rect.width;
                        float y = (pt.y - 0.5f) * rt.rect.height;
                        SpawnVisualPoint(new Vector2(x, y));
                    }
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            IsActive = false;
            OnClose?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive || CameraPreviewImage == null) return;
            
            if (CapturedPoints.Count >= 4) return; // Max 4 points

            // Convert Screen Point to Local Point in RawImage
            Vector2 localPoint;
            RectTransform rt = CameraPreviewImage.rectTransform;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // Normalize to UV (0-1)
                float u = (localPoint.x / rt.rect.width) + 0.5f;
                float v = (localPoint.y / rt.rect.height) + 0.5f;
                
                // Clamp
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
                
                CapturedPoints.Add(new Vector2(u, v));
                
                // Visualize
                SpawnVisualPoint(localPoint);
                
                Debug.Log($"[ROI] Point Added: UV({u:F2}, {v:F2})");
            }
        }
        
        void SpawnVisualPoint(Vector2 localPos)
        {
            GameObject p = new GameObject($"Point_{_visualPoints.Count}");
            p.transform.SetParent(PointsContainer ? PointsContainer : transform, false);
            
            Image img = p.AddComponent<Image>();
            img.color = Color.red;
            
            RectTransform rt = p.GetComponent<RectTransform>();
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(20, 20); // 20px dot
            
            _visualPoints.Add(p);
        }

        void ClearPoints()
        {
            CapturedPoints.Clear();
            foreach(var p in _visualPoints) if(p) Destroy(p);
            _visualPoints.Clear();
        }

        void SaveBoundary()
        {
            if (CapturedPoints.Count == 4)
            {
                // Persistence Logic
                if (_settings != null)
                {
                    _settings.BoundaryPoints = CapturedPoints.ToArray();
                    ARSandbox.Core.SandboxSettingsManager.Save(_settings);
                    Debug.Log("[ROI] Boundary saved to persistent storage.");
                }

                OnBoundarySaved?.Invoke(new List<Vector2>(CapturedPoints));
                Hide();
            }
            else
            {
                Debug.LogWarning("[ROI] Need exactly 4 points to save boundary.");
            }
        }
    }
}
