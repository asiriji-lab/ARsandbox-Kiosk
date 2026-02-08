using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ARSandbox.UI
{
    public class ROIEditorView : MonoBehaviour, IPointerClickHandler
    {
        // Programmatic References
        private RawImage _cameraPreviewImage;
        private RectTransform _pointsContainer;
        private GameObject _uiRoot;
        private Button _confirmButton;
        private Button _clearButton;
        private Button _undoButton;
        private Button _cancelButton; // Exit without saving
        private Text _statusLabel;
        private Text _instructionLabel;
        
        [Header("State")]
        public List<Vector2> CapturedPoints = new List<Vector2>(); // UV coordinates (0-1)
        public bool IsActive = false;
        
        // Factory for visualized points and lines
        private List<GameObject> _visualPoints = new List<GameObject>();
        private List<GameObject> _visualLines = new List<GameObject>();
        
        public System.Action<List<Vector2>> OnBoundarySaved;
        public System.Action OnClose;

        private SandboxSettingsSO _settings;
        private SandboxViewModel _viewModel;

        public void Initialize(SandboxSettingsSO settings, SandboxViewModel viewModel)
        {
            _settings = settings;
            _viewModel = viewModel;
        }

        void Start()
        {
            BuildUI();
            
            if(_confirmButton) _confirmButton.onClick.AddListener(SaveBoundary);
            if(_clearButton) _clearButton.onClick.AddListener(ClearPoints);
            if(_undoButton) _undoButton.onClick.AddListener(UndoLastPoint);
            if(_cancelButton) _cancelButton.onClick.AddListener(Hide);
            
            // Default State
            Hide();
        }

        public void Show()
        {
            // Ensure MonoBehaviour is active so Update/Coroutines work if needed
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // Lazy Init UI if Start() hasn't run yet
            if (_uiRoot == null) BuildUI();

            if (_uiRoot) _uiRoot.SetActive(true);
            IsActive = true;
            Debug.Log("[ROI] Editor Shown. Mode: " + (_viewModel.Controller.IsSimulationEnabled ? "SIMULATION" : "HARDWARE"));
            UpdateStatus();
            ShowInstructions("Tap the 4 corners of the active sand area.");

            // Hide Terrain Mesh to prevent visual feedback loop / occlusion
            if (_viewModel != null && _viewModel.Controller != null)
            {
                var mb = _viewModel.Controller as MonoBehaviour;
                if (mb != null)
                {
                    var mr = mb.GetComponent<MeshRenderer>();
                    if (mr) 
                    {
                        mr.enabled = false;
                        Debug.Log("[ROI] Disabled Terrain MeshRenderer.");
                    }
                    else
                    {
                        Debug.LogWarning("[ROI] Could not find MeshRenderer on Controller!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[ROI] ViewModel or Controller is NULL! VM={_viewModel}, Ctrl={(_viewModel != null ? _viewModel.Controller : null)}");
            }

            // Camera Texture will be updated in Update() for live feed
            RefreshCameraTexture();
            
            // Clear any old points from previous session
            ClearVisuals();
            CapturedPoints.Clear();

            // Load existing points
            if (_settings != null && _settings.BoundaryPoints != null && _settings.BoundaryPoints.Length == 4)
            {
                foreach(var pt in _settings.BoundaryPoints)
                {
                    CapturedPoints.Add(pt);
                    
                    if (_cameraPreviewImage != null)
                    {
                        // Convert UV to Local Point for visualization
                        RectTransform rt = _cameraPreviewImage.rectTransform;
                        float x = (pt.x - 0.5f) * rt.rect.width;
                        float y = (pt.y - 0.5f) * rt.rect.height;
                        SpawnVisualPoint(new Vector2(x, y));
                    }
                }
                Debug.Log($"[ROI] Loaded {CapturedPoints.Count} existing boundary points.");
            }
            
            // Update status AFTER loading points
            UpdateStatus();
        }

        public void Hide()
        {
            if (_uiRoot) _uiRoot.SetActive(false);
            IsActive = false;
            OnClose?.Invoke();

            RestoreMeshRenderer();
        }
        
        void OnDisable()
        {
            // Safety: Ensure mesh is restored if this object is disabled/destroyed
            RestoreMeshRenderer();
        }

        void RestoreMeshRenderer()
        {
            if (_viewModel != null && _viewModel.Controller != null)
            {
                var mb = _viewModel.Controller as MonoBehaviour;
                if (mb != null)
                {
                    var mr = mb.GetComponent<MeshRenderer>();
                    if (mr && !mr.enabled) 
                    {
                        mr.enabled = true;
                        Debug.Log("[ROI] Restored Terrain MeshRenderer.");
                    }
                }
            }
        }

        void Update()
        {
            if (!IsActive) return;
            
            // Continuously refresh the camera texture for live feed
            RefreshCameraTexture();
        }

        void RefreshCameraTexture()
        {
            if (_cameraPreviewImage == null || _viewModel == null || _viewModel.Controller == null) return;
            
            // Try color first (Kinect mode), fallback to depth
            var colorTex = _viewModel.Controller.GetColorTexture();
            if (colorTex != null)
                {
                    if (_cameraPreviewImage.texture != colorTex) {
                        _cameraPreviewImage.texture = colorTex;
                        Debug.Log("[ROI] Texture Refresh: Switched to COLOR stream.");
                    }
                }
                else
                {
                    var depthTex = _viewModel.Controller.GetRawDepthTexture();
                    if (_cameraPreviewImage.texture != depthTex) {
                        _cameraPreviewImage.texture = depthTex;
                        Debug.Log("[ROI] Texture Refresh: Switched to DEPTH/DEBUG stream.");
                    }
                }
            _cameraPreviewImage.uvRect = new Rect(0, 0, 1, 1);
        }

        void ClearVisuals()
        {
            foreach(var p in _visualPoints) if(p) Destroy(p);
            _visualPoints.Clear();
            
            foreach(var l in _visualLines) if(l) Destroy(l);
            _visualLines.Clear();
        }

        private void BuildUI()
        {
            // 1. Create Canvas (Admin/Overlay Layer)
            GameObject canvasObj = new GameObject("ROI_Editor_Canvas");
            canvasObj.transform.SetParent(transform, false);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above everything

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            _uiRoot = canvasObj;

            // 2. Background (Dark Overlay)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // 3. Camera Preview (RawImage)
            // Center it, keep aspect ratio roughly 4:3 or 16:9?
            // For now, let's make it large but with padding.
            GameObject rawTop = new GameObject("CameraPreview");
            rawTop.transform.SetParent(bgObj.transform, false);
            
            _cameraPreviewImage = rawTop.AddComponent<RawImage>();
            _cameraPreviewImage.color = Color.black; // Will be assigned texture later
            
            RectTransform rawRT = _cameraPreviewImage.GetComponent<RectTransform>();
            rawRT.anchorMin = new Vector2(0.1f, 0.1f);
            rawRT.anchorMax = new Vector2(0.9f, 0.9f);
            rawRT.offsetMin = Vector2.zero;
            rawRT.offsetMax = Vector2.zero;
            
            // 4. Points Container (Child of RawImage so points move with it)
            GameObject ptsContainer = new GameObject("PointsContainer");
            ptsContainer.transform.SetParent(rawTop.transform, false);
            _pointsContainer = ptsContainer.AddComponent<RectTransform>();
            _pointsContainer.anchorMin = Vector2.zero;
            _pointsContainer.anchorMax = Vector2.one;
            _pointsContainer.offsetMin = Vector2.zero;
            _pointsContainer.offsetMax = Vector2.zero;

            // 5. Controls (Buttons)
            CreateControls(bgObj.transform);
            
            // 6. Info Overlay (Top)
            CreateTopBar(bgObj.transform);
        }

        private void CreateTopBar(Transform parent)
        {
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(parent, false);
            
            RectTransform rt = topBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.9f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Status (Top Left)
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(topBar.transform, false);
            _statusLabel = statusObj.AddComponent<Text>();
            _statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusLabel.fontSize = 24;
            _statusLabel.color = Color.yellow;
            _statusLabel.alignment = TextAnchor.MiddleLeft;
            RectTransform sRT = statusObj.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.05f, 0);
            sRT.anchorMax = new Vector2(0.3f, 1);
            sRT.offsetMin = Vector2.zero; sRT.offsetMax = Vector2.zero;

            // Instructions (Center)
            GameObject instrObj = new GameObject("Instructions");
            instrObj.transform.SetParent(topBar.transform, false);
            _instructionLabel = instrObj.AddComponent<Text>();
            _instructionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _instructionLabel.fontSize = 32;
            _instructionLabel.color = Color.white;
            _instructionLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform iRT = instrObj.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.3f, 0);
            iRT.anchorMax = new Vector2(0.7f, 1);
            iRT.offsetMin = Vector2.zero; iRT.offsetMax = Vector2.zero;
        }

        private void CreateControls(Transform parent)
        {
            // Bottom Bar
            GameObject bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(parent, false);
            
            RectTransform rt = bottomBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup layout = bottomBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 50;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Undo Button
            _undoButton = CreateButton(bottomBar.transform, "UNDO", Color.yellow);

            // Clear Button
            _clearButton = CreateButton(bottomBar.transform, "CLEAR ALL", Color.red);
            
            // Confirm Button
            _confirmButton = CreateButton(bottomBar.transform, "SAVE & EXIT", Color.green);
            
            // Cancel Button (exit without saving)
            _cancelButton = CreateButton(bottomBar.transform, "CANCEL", new Color(0.5f, 0.5f, 0.5f));
        }

        private Button CreateButton(Transform parent, string label, Color color)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.fontSize = 20;
            
            RectTransform txtRT = txtObj.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            return btn;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive || _cameraPreviewImage == null) return;
            
            if (CapturedPoints.Count >= 4) return; // Max 4 points

            // Convert Screen Point to Local Point in RawImage
            Vector2 localPoint;
            RectTransform rt = _cameraPreviewImage.rectTransform;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // Normalize to UV (0-1)
                // Local Point (0,0) is Pivot (Center).
                // Range is [-width/2, +width/2].
                
                float u = (localPoint.x / rt.rect.width) + 0.5f;
                float v = (localPoint.y / rt.rect.height) + 0.5f;
                
                // Clamp
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
                
                CapturedPoints.Add(new Vector2(u, v));
                
                // Visualize
                SpawnVisualPoint(localPoint);
                UpdateStatus();
                
                Debug.Log($"[ROI] Interaction - Point Added: Index={CapturedPoints.Count-1}, Screen={eventData.position}, UV=({u:F3}, {v:F3})");
            }
        }
        
        void SpawnVisualPoint(Vector2 localPos)
        {
            GameObject p = new GameObject($"Point_{_visualPoints.Count}");
            p.transform.SetParent(_pointsContainer ? _pointsContainer : transform, false);
            
            Image img = p.AddComponent<Image>();
            img.color = Color.red;
            
            RectTransform rt = p.GetComponent<RectTransform>();
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(25, 25);
            
            _visualPoints.Add(p);
            
            RebuildLines();
        }

        void RebuildLines()
        {
            // Clear old lines
            foreach(var l in _visualLines) if(l) Destroy(l);
            _visualLines.Clear();

            if (_visualPoints.Count < 2) return;

            // Connect 0->1, 1->2, etc.
            // If we want a closed loop only on 4 points? 
            // Let's connect sequentially for feedback.
            
            for (int i = 0; i < _visualPoints.Count - 1; i++)
            {
                CreateLine(_visualPoints[i].GetComponent<RectTransform>(), _visualPoints[i+1].GetComponent<RectTransform>());
            }
            
            // Loop back if we have 4 points (Complete shape)
            if (_visualPoints.Count == 4)
            {
                CreateLine(_visualPoints[3].GetComponent<RectTransform>(), _visualPoints[0].GetComponent<RectTransform>());
            }
        }

        void CreateLine(RectTransform pA, RectTransform pB)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(_pointsContainer ? _pointsContainer : transform, false);
            lineObj.transform.SetAsFirstSibling(); // Behind dots

            Image img = lineObj.AddComponent<Image>();
            img.color = new Color(1, 0, 0, 0.5f);

            RectTransform rect = lineObj.GetComponent<RectTransform>();
            Vector2 dir = (pB.anchoredPosition - pA.anchoredPosition).normalized;
            float dist = Vector2.Distance(pB.anchoredPosition, pA.anchoredPosition);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(dist, 4f); // 4px thick width
            rect.anchoredPosition = pA.anchoredPosition + dir * dist * 0.5f;
            rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            
            _visualLines.Add(lineObj);
        }

        void ClearPoints()
        {
            CapturedPoints.Clear();
            foreach(var p in _visualPoints) if(p) Destroy(p);
            _visualPoints.Clear();
            
            foreach(var l in _visualLines) if(l) Destroy(l);
            _visualLines.Clear();
            
            UpdateStatus();
        }
        
        void UndoLastPoint()
        {
            if (CapturedPoints.Count > 0)
            {
                int lastIdx = CapturedPoints.Count - 1;
                CapturedPoints.RemoveAt(lastIdx);
                
                // Remove Visuals
                if (lastIdx < _visualPoints.Count)
                {
                    Destroy(_visualPoints[lastIdx]);
                    _visualPoints.RemoveAt(lastIdx);
                }
                
                RebuildLines();
                UpdateStatus();
                Debug.Log("[ROI] Undo Performed.");
            }
        }

        void UpdateStatus()
        {
            if (_statusLabel)
            {
                int count = CapturedPoints.Count;
                if (count == 4) 
                {
                    _statusLabel.text = "STATUS: READY TO SAVE";
                    _statusLabel.color = Color.green;
                }
                else
                {
                    _statusLabel.text = $"STATUS: {count}/4 POINTS";
                    _statusLabel.color = Color.yellow;
                }
            }
        }
        
        void ShowInstructions(string text)
        {
            if (_instructionLabel)
            {
                _instructionLabel.text = text;
                _instructionLabel.color = Color.white;
                // Simple fade out coroutine could go here, or just leave it for 5s
                StopAllCoroutines();
                StartCoroutine(FadeInstructions());
            }
        }
        
        System.Collections.IEnumerator FadeInstructions()
        {
            yield return new WaitForSeconds(3.0f);
            float alpha = 1.0f;
            while(alpha > 0)
            {
                alpha -= Time.deltaTime;
                if(_instructionLabel) _instructionLabel.color = new Color(1,1,1, alpha);
                yield return null;
            }
        }

        void SaveBoundary()
        {
            if (CapturedPoints.Count == 4)
            {
                if (_settings != null)
                {
                    _settings.BoundaryPoints = CapturedPoints.ToArray();
                    ARSandbox.Core.SandboxSettingsManager.Save(_settings);
                    
                    string ptsStr = "";
                    foreach(var p in CapturedPoints) ptsStr += $"({p.x:F3},{p.y:F3}) ";
                    Debug.Log($"[ROI] Persistence - Saved 4 points: {ptsStr}");
                }

                OnBoundarySaved?.Invoke(new List<Vector2>(CapturedPoints));
                Hide();
            }
            else
            {
                Debug.LogWarning("[ROI] Need exactly 4 points to save boundary.");
                ShowInstructions("ERROR: You must define exactly 4 corners!");
                if(_statusLabel) _statusLabel.color = Color.red;
            }
        }
    }
}
