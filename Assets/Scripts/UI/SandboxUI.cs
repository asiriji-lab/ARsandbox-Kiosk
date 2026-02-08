using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// Adheres to "Start-Phase" initialization and Programmatic UI standards.
/// Updated: Supports 4-Point Calibration and Auto-Floor.
/// </summary>
namespace ARSandbox.UI
{
    using ARSandbox.Core;
public class SandboxUI : MonoBehaviour
{
    [Header("MVVM")]
    public SandboxViewModel ViewModel;
    
    [Header("Sub-Systems")]
    public ROIEditorView ROIEditor;

    [Header("UI Resources")]
    [FormerlySerializedAs("knobSprite")]
    public Sprite KnobSprite;
    [FormerlySerializedAs("backgroundSprite")]
    public Sprite BackgroundSprite;
    [FormerlySerializedAs("handleSprite")]
    public Sprite HandleSprite; // Assign a square or circle if available, else uses default

    private GameObject _uiRoot;
    private GameObject _viewTab;
    private GameObject _setupTab;
    private GameObject _worldTab;
    private CanvasGroup _canvasGroup;
    private bool _isVisible = false;
    private int _activeTabIndex = 0;

    // Calibration UI
    private bool _isCalibrating = false;
    private RawImage _calibrationOverlay;
    private RectTransform[] _handles; // 0=BL, 1=TL, 2=TR, 3=BR
    private int _draggingHandleIndex = -1;
    private Text[] _handleLabels;

    // Sliders
    private Slider _heightSlider;
    private Slider _minDepthSlider;
    private Slider _maxDepthSlider;
    private Slider _tintStrengthSlider;
    private Slider _sandScaleSlider;
    private Slider _smoothingSlider;
    private Slider _waterLevelSlider;
    private Slider _colorShiftSlider;
    private Slider _contourIntervalSlider;
    private Slider _contourThicknessSlider;
    private Slider _staticStabilitySlider;
    private Slider _motionResponseSlider;
    private Slider _handRejectionSlider;
    private Slider _boundsSizeSlider;
    private Slider _lineSmoothSlider;
    private Slider _causticIntensitySlider;
    private Slider _causticScaleSlider;
    private Slider _causticSpeedSlider;
    private Slider _meshResSlider; // [NEW]
    private Slider _waterOpacitySlider;
    private Slider _sparkleIntensitySlider;
    private Slider _noiseScaleSlider; // [NEW]
    private Slider _moveSpeedSlider;  // [NEW]
    
    // Contextual Controls Groups (to show/hide)
    private GameObject _kioskOnlyGroup; // [NEW]
    private GameObject _simOnlyGroup;   // [NEW]
    private Image _modeBanner;          // [NEW]
    private Text _modeBannerText;       // [NEW]
    private GameObject _alignButton;    // [NEW]
    private GameObject _maskButton;     // [NEW]
    private GameObject _autoFloorButton; // [NEW]
    
    // Labels
    private Text _heightLabel;
    private Text _minDepthLabel;
    private Text _maxDepthLabel;
    private Text _tintStrengthLabel;
    private Text _sandScaleLabel;
    private Text _smoothingLabel;
    private Text _waterLevelLabel;
    private Text _colorShiftLabel;
    private Text _contourIntervalLabel;
    private Text _contourThicknessLabel;
    private Text _staticStabilityLabel;
    private Text _motionResponseLabel;
    private Text _handRejectionLabel;
    private Text _noiseScaleLabel;
    private Text _moveSpeedLabel;
    private Text _boundsSizeLabel;
    private Text _meshResLabel; // [NEW]
    private Text _lineSmoothLabel;
    private Text _causticIntensityLabel;
    private Text _causticScaleLabel;
    private Text _causticSpeedLabel;
    private Text _waterOpacityLabel;
    private Text _sparkleIntensityLabel;
    private Text _calibrationResultLabel;
    private Text _statusText; // [NEW] for general status messages

    // Optimization: Cached Strings
    private StringBuilder _sb = new StringBuilder();
    private float _lastMouseMoveTime;
    private const float UI_IDLE_TIME = 10f;
    private bool _isFaded = false;
    private float _secretGestureTimer = 0f; // [NEW] For Kiosk Admin Access

    // Camera Views
    // CamView moved to ARSandbox.Core
    private CamView _currentView = CamView.Top;
    private float _cameraZoomOffset = 0f; // Persists zoom across view changes

    void CycleCameraView() => CycleCameraView(1); // Default forward

    void CycleCameraView(int direction)
    {
        _currentView = CameraStateLogic.GetNextView(_currentView, direction);
        UpdateCameraTransform();
    }

    void UpdateCameraTransform()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // MVVM Access
        float size = 10f;
        if (ViewModel != null && ViewModel.Settings != null) size = ViewModel.Settings.MeshSize;

        // Ensure size is at least 10 to avoid clipping
        if (size < 10) size = 10;

        // Apply zoom offset (positive = zoomed in = closer)
        float effectiveSize = size - _cameraZoomOffset;
        effectiveSize = Mathf.Max(effectiveSize, 2f); // Prevent getting too close

        switch (_currentView)
        {
            case CamView.Top:
                // Standard: Look Down
                cam.transform.position = new Vector3(0, effectiveSize, 0);
                // FLIPPED: (90, 0, 180) rotates the view 180 degrees for inverted projectors
                cam.transform.rotation = Quaternion.Euler(90, 0, 180);
                break;
            case CamView.Perspective:
                // 45 Degree
                cam.transform.position = new Vector3(0, effectiveSize * 0.8f, -effectiveSize * 0.8f);
                cam.transform.LookAt(Vector3.zero);
                break;
            case CamView.Side:
                // Side View (Amplitude Check)
                cam.transform.position = new Vector3(0, effectiveSize * 0.2f, -effectiveSize);
                cam.transform.LookAt(Vector3.zero);
                break;
        }
        Debug.Log($"Switched View to: {_currentView} (Zoom Offset: {_cameraZoomOffset:F1})");
    }

    void Start()
    {
        Debug.Log("SandboxUI: Start() Called");
        
        // Auto-find ROI Editor if not assigned (Include Inactive)
        if (ROIEditor == null) ROIEditor = FindFirstObjectByType<ROIEditorView>(FindObjectsInactive.Include);
        if (ROIEditor != null && ViewModel != null && ViewModel.Settings != null)
             ROIEditor.Initialize(ViewModel.Settings, ViewModel);
        
        // "External" Initialization
        if (ViewModel == null)
            ViewModel = FindFirstObjectByType<SandboxViewModel>();

        if (ViewModel == null)
        {
            Debug.LogError("SandboxUI: No SandboxViewModel found!");
            return;
        }
        else if (ViewModel.Settings == null)
        {
            Debug.LogError("SandboxUI: ViewModel found but Settings are null! Initialization race condition?");
            return;
        }
        else 
        {
             Debug.Log("SandboxUI: ViewModel found.");
        }

        try {
            BuildUI();
            BuildCalibrationForlay();
        } catch (System.Exception e) {
            Debug.LogError($"SandboxUI: Error building UI: {e.Message}\n{e.StackTrace}");
        }

        ToggleUI(true); // Default to visible so user knows it's working
        ToggleCalibration(false);
        UpdateCameraTransform(); // Snap Camera to Top View on Launch
        Debug.Log("SandboxUI: Initialization complete. UI Visible.");
    }

    public void Update()
    {
        HandleInput();
        HandleAutoHide();
        HandleSecretGesture(); // [NEW]
        
        if (_isVisible)
        {
            UpdateLabels();
        }

        if (_isCalibrating)
        {
            UpdateCalibrationLogic();
        }
    }

    void HandleAutoHide()
    {
        if (!_isVisible) return;

        // Check for mouse movement or clicks
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude > 0.1f || Mouse.current.leftButton.isPressed)
            {
                _lastMouseMoveTime = Time.time;
                if (_isFaded) { SetUIFade(1f); _isFaded = false; }
            }
        }

        if (!_isFaded && Time.time - _lastMouseMoveTime > UI_IDLE_TIME)
        {
            SetUIFade(0.2f);
            _isFaded = true;
        }
    }

    void SetUIFade(float alpha)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }
    }

    void HandleSecretGesture()
    {
        // Top-Left Corner (100x100 pixels)
        // Screen (0,0) is usually Bottom-Left in Unity!
        // So Top-Left is (0, Screen.height)
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            if (pos.x < 100 && pos.y > (Screen.height - 100))
            {
                _secretGestureTimer += Time.deltaTime;
                if (_secretGestureTimer > 2.0f)
                {
                    if (!_isVisible) 
                    {
                        ToggleUI(true);
                        Debug.Log("Admin Access via Secret Gesture!");
                        // Feedback? Maybe play a sound or flash?
                    }
                    _secretGestureTimer = 0f; // Reset to prevent rapid toggling
                }
                return;
            }
        }
        
        _secretGestureTimer = 0f; // Reset if released or moved out
    }

    void HandleInput()
    {
        bool tab = false;
        bool rKey = false;
        bool esc = false;
        float heightInput = 0f;

#if ENABLE_INPUT_SYSTEM
        // New Input System Safety: Always null-check Keyboard.current
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.backquoteKey.wasPressedThisFrame) 
            { 
                tab = true; 
                Debug.Log("SandboxUI: Toggle key detected via New Input System"); 
            }
            if (Keyboard.current.rKey.wasPressedThisFrame) rKey = true;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) esc = true;
            // vKey removed
            
            // Continuous Height Control
            if (Keyboard.current.upArrowKey.isPressed) heightInput += 1f;
            if (Keyboard.current.downArrowKey.isPressed) heightInput -= 1f;
            
            if (Keyboard.current.eKey.wasPressedThisFrame) ToggleROIEditor();
        }
        else
        {
             // Optional: Log once if needed, or rely on Legacy fallback
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        // Legacy Input Logic: compiled out if Legacy Module is disabled
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.BackQuote)) 
        { 
            tab = true; 
            Debug.Log("SandboxUI: Toggle key detected via Legacy Input"); 
        }
        if (Input.GetKeyDown(KeyCode.R)) rKey = true;
        if (Input.GetKeyDown(KeyCode.Escape)) esc = true;
        // vKey removed
        
        if (Input.GetKey(KeyCode.UpArrow)) heightInput += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) heightInput -= 1f;
        
        // ROI Editor Shortcut
        if (Input.GetKeyDown(KeyCode.E))
        {
             ToggleROIEditor();
        }
#endif

        if (heightInput != 0)
        {
            AdjustCameraZoom(heightInput * 5.0f * Time.deltaTime);
        }

        // Camera Cycling (Left/Right)
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) CycleCameraView(-1);
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame) CycleCameraView(1);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleCameraView(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) CycleCameraView(1);
#endif
        
        // Action Logic
        if (tab)
        {
            ToggleUI(!_isVisible);
            if (!_isVisible) ToggleCalibration(false);
        }

        if (rKey)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        if (esc)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void ToggleROIEditor()
    {
        // Lazy Load
        if (ROIEditor == null) ROIEditor = FindFirstObjectByType<ROIEditorView>(FindObjectsInactive.Include);
        
        if(ROIEditor) {
            // Late Initialize if needed
            if(ViewModel != null && ViewModel.Settings != null) ROIEditor.Initialize(ViewModel.Settings, ViewModel);
            
            if (ROIEditor.IsActive)
            {
                ROIEditor.Hide();
                ToggleUI(true); // Restore Main UI
            }
            else
            {
                ROIEditor.Show();
                ToggleUI(false); // Hide Main UI
            }
        } else {
            Debug.LogError("ROIEditor reference missing! Please create a GameObject with 'ROIEditorView' component in the scene.");
        }
    }

    void AdjustCameraZoom(float delta)
    {
        // Update persistent zoom offset
        // Positive delta (Up Arrow) = zoom in = increase offset (decreases effectiveSize)
        _cameraZoomOffset += delta;
        
        // Clamp to reasonable range
        _cameraZoomOffset = Mathf.Clamp(_cameraZoomOffset, -20f, 15f);
        
        // Apply the new zoom to current view
        UpdateCameraTransform();
    }

    void UpdateCalibrationLogic()
    {
        // Continuously refresh texture for live feed
        if (ViewModel.Controller != null)
        {
            // Prefer Color if available (Kiosk Mode)
            Texture targetTex = ViewModel.Controller.GetColorTexture();
            if (targetTex == null) targetTex = ViewModel.Controller.GetRawDepthTexture();

            if (_calibrationOverlay != null)
            {
                if (_calibrationOverlay.texture != targetTex)
                {
                    _calibrationOverlay.texture = targetTex;
                    // FLIP: UV (0,1, 1, -1) handles the vertical flip often seen with Kinect textures
                    _calibrationOverlay.uvRect = new Rect(0, 1, 1, -1);
                }
            }
        }

        // Handle Dragging
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Check for hits
            float closestDist = 50f; // Pixel threshold
            _draggingHandleIndex = -1;

            for(int i=0; i<4; i++)
            {
                if (_handles[i] != null)
                {
                    float d = Vector2.Distance(mousePos, _handles[i].position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        _draggingHandleIndex = i;
                    }
                }
            }
        }

        if (_draggingHandleIndex != -1 && Mouse.current.leftButton.isPressed)
        {
            // Update Point
            // Screen (Pix) -> Normalized (0..1)
            float normX = Mathf.Clamp01(mousePos.x / Screen.width);
            float normY = Mathf.Clamp01(mousePos.y / Screen.height);
            
            // INVERT Y: Match the flipped camera feed AND flipped projector
            // Top of screen is now Bottom of projection
            normY = 1.0f - normY; 

            // Update Controller
            ViewModel.Settings.CalibrationPoints[_draggingHandleIndex] = new Vector2(normX, normY);
            
            // Update Visual
            _handles[_draggingHandleIndex].position = mousePos;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_draggingHandleIndex != -1)
            {
                _draggingHandleIndex = -1;
                ViewModel.SaveSettings(); // Save on release
            }
        }
    }

    void ToggleCalibration(bool state)
    {
        _isCalibrating = state;
        if (_calibrationOverlay != null)
        {
            _calibrationOverlay.gameObject.SetActive(state);
            // Refresh handle positions from controller
            if (state)
            {
                for(int i=0; i<4; i++)
                {
                    Vector2 norm = ViewModel.Settings.CalibrationPoints[i];
                    // INVERT Y: Restore visual position from inverted logic
                    float visualY = (1.0f - norm.y) * Screen.height;
                    _handles[i].position = new Vector2(norm.x * Screen.width, visualY);
                }
            }
        }
    }

    void BuildCalibrationForlay()
    {
        // 1. Raw Image fullscreen
        GameObject rawObj = new GameObject("CalibrationOverlay");
        rawObj.transform.SetParent(_uiRoot.transform.parent, false); // Parent to Canvas, not Panel
        rawObj.transform.SetAsFirstSibling(); // Behind UI

        _calibrationOverlay = rawObj.AddComponent<RawImage>();
        _calibrationOverlay.color = Color.white;
        _calibrationOverlay.raycastTarget = false; // Don't block UI clicks
        RectTransform rt = rawObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one; 
        rt.sizeDelta = Vector2.zero; // Full stretch

        // 2. Handles
        _handles = new RectTransform[4];
        _handleLabels = new Text[4];
        string[] names = { "BL", "TL", "TR", "BR" };
        Color[] colors = { Color.cyan, Color.green, Color.yellow, Color.red };

        for(int i=0; i<4; i++)
        {
            GameObject h = new GameObject($"Handle_{names[i]}");
            h.transform.SetParent(rawObj.transform, false);
            
            Image img = h.AddComponent<Image>();
            img.color = colors[i];
            
            RectTransform hRT = h.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(40, 40);
            hRT.anchorMin = Vector2.zero; 
            hRT.anchorMax = Vector2.zero; // Absolute positioning
            
            _handles[i] = hRT;

            // Label
            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(h.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.text = names[i];
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.black;
            tObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
        }

        // Border Guide
        GameObject borderObj = new GameObject("CalibrationBorder");
        borderObj.transform.SetParent(rawObj.transform, false);
        Image border = borderObj.AddComponent<Image>();
        border.color = Color.red;
        
        // Use a simple trick: stretch full then add negative padding? 
        // Or simpler: transparent center with outline script if available. 
        // Since we don't have outline script, we can make 4 skinny rectangles, or just a thick frame.
        // Let's do a semi-transparent red overlay with 5px padding to show "Edge".
        
        // Actually, let's just make it a hollow Frame using standard Unity technique:
        // A full rect with mask? No.
        // Let's just make 4 strips.
        CreateBorderStrip(borderObj.transform, "Top", _anchorTop); 
        CreateBorderStrip(borderObj.transform, "Bottom", _anchorBottom);
        CreateBorderStrip(borderObj.transform, "Left", _anchorLeft);
        CreateBorderStrip(borderObj.transform, "Right", _anchorRight);
        
        // Hide base image to keep just the strips? 
        DestroyImmediate(border); 

        rawObj.SetActive(false);
    }
    
    // Helpers for border
    private Vector2 _anchorTop = new Vector2(0.5f, 1);
    private Vector2 _anchorBottom = new Vector2(0.5f, 0);
    private Vector2 _anchorLeft = new Vector2(0, 0.5f);
    private Vector2 _anchorRight = new Vector2(1, 0.5f);

    void CreateBorderStrip(Transform parent, string name, Vector2 anchor)
    {
        GameObject s = new GameObject(name);
        s.transform.SetParent(parent, false);
        Image img = s.AddComponent<Image>();
        img.color = new Color(1, 0, 0, 0.8f); // Bright Red
        
        RectTransform rt = s.GetComponent<RectTransform>();
        // Top/Bottom strips
        if (anchor.y == 1 || anchor.y == 0)
        {
            rt.anchorMin = new Vector2(0, anchor.y);
            rt.anchorMax = new Vector2(1, anchor.y);
            rt.pivot = new Vector2(0.5f, anchor.y == 1 ? 1 : 0);
            rt.sizeDelta = new Vector2(0, 5); // 5px height
        }
        else
        {
            // Left/Right strips
            rt.anchorMin = new Vector2(anchor.x, 0);
            rt.anchorMax = new Vector2(anchor.x, 1);
            rt.pivot = new Vector2(anchor.x == 1 ? 1 : 0, 0.5f);
            rt.sizeDelta = new Vector2(5, 0); // 5px width
        }
    }

    // --- UI Structure ---

    GameObject CreateGroup(Transform parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        
        RectTransform rt = g.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        VerticalLayoutGroup layout = g.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        
        ContentSizeFitter csf = g.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        return g;
    }

    void UpdateVisibility()
    {
        if (_viewTab) _viewTab.SetActive(_activeTabIndex == 0);
        if (_setupTab) _setupTab.SetActive(_activeTabIndex == 1);
        if (_worldTab) _worldTab.SetActive(_activeTabIndex == 2);

        // --- CONTEXTUAL VISIBILITY ---
        bool isSim = ViewModel.Controller != null && ViewModel.Controller.IsSimulationEnabled;
        
        if (_kioskOnlyGroup) _kioskOnlyGroup.SetActive(!isSim);
        if (_simOnlyGroup) _simOnlyGroup.SetActive(isSim);

        // Specific Button Toggles (Safety)
        if (_alignButton) _alignButton.SetActive(!isSim);
        if (_maskButton) _maskButton.SetActive(!isSim);
        if (_autoFloorButton) _autoFloorButton.SetActive(!isSim);
        
        // Force layout rebuild
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_uiRoot.GetComponent<RectTransform>());
    }

    void SetActiveTab(int index)
    {
        _activeTabIndex = index;
        UpdateVisibility();
    }

    void BuildUI()
    {
        // 1. Create Host Canvas (Programmatic)
        GameObject canvasObj = new GameObject("AdminCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Topmost

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        else
        {
            var oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                DestroyImmediate(oldModule);
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        // 2. Root Panel
        _uiRoot = new GameObject("Panel");
        _uiRoot.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rt = _uiRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0.35f, 1); // Slightly wider (35%)
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;

        Image bg = _uiRoot.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f); // High opacity for readability

        _canvasGroup = _uiRoot.AddComponent<CanvasGroup>();

        VerticalLayoutGroup layout = _uiRoot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 15, 15); // Padding shifted to children
        layout.spacing = 10;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        // 2b. Mode Banner (Amber/Teal)
        GameObject bannerObj = new GameObject("ModeBanner");
        bannerObj.transform.SetParent(_uiRoot.transform, false);
        _modeBanner = bannerObj.AddComponent<Image>();
        _modeBanner.color = new Color(1f, 0.75f, 0f, 1f); // Deep Amber default
        RectTransform bannerRT = bannerObj.GetComponent<RectTransform>();
        bannerRT.sizeDelta = new Vector2(0, 40); // Fixed height
        
        GameObject bTextObj = new GameObject("Text");
        bTextObj.transform.SetParent(bannerObj.transform, false);
        _modeBannerText = bTextObj.AddComponent<Text>();
        _modeBannerText.text = "MODE: KIOSK";
        _modeBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _modeBannerText.color = Color.black;
        _modeBannerText.alignment = TextAnchor.MiddleCenter;
        _modeBannerText.fontStyle = FontStyle.Bold;
        _modeBannerText.fontSize = 20;
        bTextObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bTextObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bTextObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Space for header content
        GameObject innerContent = new GameObject("InnerContent");
        innerContent.transform.SetParent(_uiRoot.transform, false);
        VerticalLayoutGroup innerLayout = innerContent.AddComponent<VerticalLayoutGroup>();
        innerLayout.padding = new RectOffset(15, 15, 0, 0);
        innerLayout.spacing = 10;
        innerLayout.childControlHeight = true;
        innerLayout.childForceExpandHeight = false;

        // Ensure innerContent stretches to fill or fits children
        ContentSizeFitter innerCSF = innerContent.AddComponent<ContentSizeFitter>();
        innerCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        innerCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        // Add layout element to innerContent so _uiRoot's layout respects it
        LayoutElement innerLE = innerContent.AddComponent<LayoutElement>();
        innerLE.flexibleHeight = 1;

        // 3. Header & Tabs
        CreateLabel(innerContent.transform, "SANDBOX COMMAND", 24, Color.yellow);
        _statusText = CreateLabel(innerContent.transform, "", 14, Color.cyan);
        
        DefaultControls.Resources res = new DefaultControls.Resources();
        res.background = BackgroundSprite;
        res.knob = KnobSprite;
        res.standard = BackgroundSprite;

        BuildTabs(innerContent.transform, res);

        // 4. Content Groups
        _viewTab = CreateGroup(innerContent.transform, "View_Tab");
        _setupTab = CreateGroup(innerContent.transform, "Setup_Tab");
        _worldTab = CreateGroup(innerContent.transform, "World_Tab");

        BuildViewTab(res);
        BuildSetupTab(res);
        BuildWorldTab(res);

        UpdateVisibility();
    }

    void BuildTabs(Transform parent, DefaultControls.Resources res)
    {
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(parent, false);
        
        HorizontalLayoutGroup h = tabRow.AddComponent<HorizontalLayoutGroup>();
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        h.spacing = 5;

        RectTransform rt = tabRow.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        // Add LayoutElement so it's not squashed
        LayoutElement le = tabRow.AddComponent<LayoutElement>();
        le.minHeight = 40;
        le.preferredHeight = 40;

        string[] labels = { "VIEW", "SETUP", "WORLD" };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            CreateButton(tabRow.transform, res, labels[i], () => SetActiveTab(index));
        }
    }

    void ApplyPreset(string name)
    {
        ViewModel.ApplyPreset(name);
        
        // Refresh Sliders
        if (_heightSlider) _heightSlider.value = ViewModel.Settings.HeightScale;
        if (_tintStrengthSlider) _tintStrengthSlider.value = ViewModel.Settings.TintStrength;
        // Controller.UpdateMaterialProperties(); // Handled by VM
        // Controller.SaveSettings(); // Handled by VM
    }

    void BuildViewTab(DefaultControls.Resources res)
    {
        CreateHeader(_viewTab.transform, "SANDBOX VISUALS", Color.cyan);
        
        _heightSlider = CreateSliderRow(_viewTab.transform, res, "Hill Height", 0.0f, 10f, ViewModel.Settings.HeightScale, out _heightLabel, (v) => ViewModel.SetHeightScale(v));

        _tintStrengthSlider = CreateSliderRow(_viewTab.transform, res, "Color Intensity", 0.0f, 1.0f, ViewModel.Settings.TintStrength, out _tintStrengthLabel, (v) => ViewModel.SetTintStrength(v));

        _sandScaleSlider = CreateSliderRow(_viewTab.transform, res, "Texture Detail", 1f, 50f, ViewModel.Settings.SandScale, out _sandScaleLabel, (v) => ViewModel.SetSandScale(v));

        _waterLevelSlider = CreateSliderRow(_viewTab.transform, res, "Water Level", 0f, 10f, ViewModel.Settings.WaterLevel, out _waterLevelLabel, (v) => ViewModel.SetWaterLevel(v));

        _colorShiftSlider = CreateSliderRow(_viewTab.transform, res, "Color Spread", -1.0f, 1.0f, ViewModel.Settings.ColorShift, out _colorShiftLabel, (v) => ViewModel.SetColorShift(v));

        CreateHeader(_viewTab.transform, "WATER & CAUSTICS", Color.cyan);

        _waterOpacitySlider = CreateSliderRow(_viewTab.transform, res, "Water Opacity", 0f, 1f, ViewModel.Settings.WaterOpacity, out _waterOpacityLabel, (v) => ViewModel.SetWaterOpacity(v));

        _causticIntensitySlider = CreateSliderRow(_viewTab.transform, res, "Caustic Brightness", 0f, 2f, ViewModel.Settings.CausticIntensity, out _causticIntensityLabel, (v) => ViewModel.SetCausticIntensity(v));

        _causticScaleSlider = CreateSliderRow(_viewTab.transform, res, "Caustic Pattern Size", 0f, 0.15f, ViewModel.Settings.CausticScale, out _causticScaleLabel, (v) => ViewModel.SetCausticScale(v));

        _causticSpeedSlider = CreateSliderRow(_viewTab.transform, res, "Caustic Shimmer", 0f, 2f, ViewModel.Settings.CausticSpeed, out _causticSpeedLabel, (v) => ViewModel.SetCausticSpeed(v));

        _sparkleIntensitySlider = CreateSliderRow(_viewTab.transform, res, "Sand Sparkle", 0f, 5f, ViewModel.Settings.SparkleIntensity, out _sparkleIntensityLabel, (v) => ViewModel.SetSparkleIntensity(v));

        _contourIntervalSlider = CreateSliderRow(_viewTab.transform, res, "Contour Gap", 0.1f, 2.0f, ViewModel.Settings.ContourInterval, out _contourIntervalLabel, (v) => ViewModel.SetContourInterval(v));

        _contourThicknessSlider = CreateSliderRow(_viewTab.transform, res, "Line Thickness", 0.1f, 5.0f, ViewModel.Settings.ContourThickness, out _contourThicknessLabel, (v) => ViewModel.SetContourThickness(v));

        CreateHeader(_viewTab.transform, "THEME PRESETS", Color.yellow);
        GameObject row = new GameObject("PresetRow");
        row.transform.SetParent(_viewTab.transform, false);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 5; h.childControlWidth = true;
        
        CreateButton(row.transform, res, "Volcano", () => ApplyPreset("Volcano"));
        CreateButton(row.transform, res, "Ocean", () => ApplyPreset("Ocean"));
        CreateButton(row.transform, res, "Oasis", () => ApplyPreset("Oasis"));

        CreateHeader(_viewTab.transform, "STYLE", Color.white);
        
        CreateButton(_viewTab.transform, res, "Cycle Color Scheme", () => {
            ViewModel.CycleColorScheme();
            UpdateLabels();
        });

        CreateToggle(_viewTab.transform, res, "Discrete Bands (Topographic)", ViewModel.Settings.UseDiscreteBands, (v) => ViewModel.SetDiscreteBands(v));
    }

    void BuildSetupTab(DefaultControls.Resources res)
    {
        CreateHeader(_setupTab.transform, "SENSOR & STABILITY", Color.green);

        _staticStabilitySlider = CreateSliderRow(_setupTab.transform, res, "Anti-Shake", 0.1f, 5.0f, ViewModel.Settings.MinCutoff, out _staticStabilityLabel, (v) => ViewModel.SetMinCutoff(v));
        _motionResponseSlider = CreateSliderRow(_setupTab.transform, res, "Follow Speed", 0.001f, 0.2f, ViewModel.Settings.Beta, out _motionResponseLabel, (v) => ViewModel.SetBeta(v));
        _handRejectionSlider = CreateSliderRow(_setupTab.transform, res, "Hand Rejection", 10f, 300f, ViewModel.Settings.HandThreshold, out _handRejectionLabel, (v) => ViewModel.SetHandThreshold(v));
        _lineSmoothSlider = CreateSliderRow(_setupTab.transform, res, "Line Smoothness", 0f, 5f, ViewModel.Settings.SpatialBlur, out _lineSmoothLabel, (v) => ViewModel.SetSpatialBlur(v));

        // --- KIOSK ONLY SECTION ---
        _kioskOnlyGroup = CreateGroup(_setupTab.transform, "Kiosk_Only_Setup");
        
        CreateHeader(_kioskOnlyGroup.transform, "CALIBRATION", Color.white);
        _autoFloorButton = CreateButton(_kioskOnlyGroup.transform, res, "Auto-Calibrate Floor (Zero)", () => {
            ViewModel.CalibrateFloor();
            if (_calibrationResultLabel != null) 
                _calibrationResultLabel.text = $"Floor: {ViewModel.Settings.MaxDepth:F0}mm | Peak: {ViewModel.Settings.MinDepth:F0}mm";
            UpdateLabels();
        });
        
        _calibrationResultLabel = CreateLabel(_kioskOnlyGroup.transform, "Ready", 12, Color.gray);

        CreateHeader(_kioskOnlyGroup.transform, "MANUAL OVERRIDE", Color.white);
        
        _alignButton = CreateButton(_kioskOnlyGroup.transform, res, "Align Projector (Keystone)", () => {
             ToggleCalibration(true);
             ToggleUI(false);
        });
        
        _maskButton = CreateButton(_kioskOnlyGroup.transform, res, "Define Active Area (Mask)", () => {
            if (ROIEditor == null) ROIEditor = FindFirstObjectByType<ROIEditorView>(FindObjectsInactive.Include);
            if(ROIEditor) {
                if(ViewModel != null && ViewModel.Settings != null) ROIEditor.Initialize(ViewModel.Settings, ViewModel);
                ROIEditor.Show();
                ToggleUI(false);
            }
        });

        _minDepthSlider = CreateSliderRow(_setupTab.transform, res, "Top (Peak) mm", 0f, 2000f, ViewModel.Settings.MinDepth, out _minDepthLabel, (v) => ViewModel.SetMinDepth(v));
        _maxDepthSlider = CreateSliderRow(_setupTab.transform, res, "Bottom (Floor) mm", 500f, 3000f, ViewModel.Settings.MaxDepth, out _maxDepthLabel, (v) => ViewModel.SetMaxDepth(v));
    }

    void BuildWorldTab(DefaultControls.Resources res)
    {
        CreateHeader(_worldTab.transform, "WORLD SETTINGS", Color.white);
        _boundsSizeSlider = CreateSliderRow(_worldTab.transform, res, "Sandbox Size [m]", 5f, 30f, ViewModel.Settings.MeshSize, out _boundsSizeLabel, (v) => ViewModel.SetMeshSize(v));
        _meshResSlider = CreateSliderRow(_worldTab.transform, res, "Surface Detail", 100f, 500f, ViewModel.Settings.MeshResolution, out _meshResLabel, (v) => ViewModel.SetMeshResolution(v));
        CreateToggle(_worldTab.transform, res, "Solid Side Walls", ViewModel.Settings.ShowWalls, (v) => ViewModel.SetShowWalls(v));
        CreateToggle(_worldTab.transform, res, "Flat Mapping (2D)", ViewModel.Settings.FlatMode, (v) => ViewModel.SetFlatMode(v));

        // --- SIM ONLY SECTION ---
        _simOnlyGroup = CreateGroup(_worldTab.transform, "Sim_Only_Settings");
        CreateHeader(_simOnlyGroup.transform, "SIMULATION CONTROLS", Color.yellow);
        _noiseScaleSlider = CreateSliderRow(_simOnlyGroup.transform, res, "Terrain Chaos", 0f, 0.15f, ViewModel.Settings.NoiseScale, out _noiseScaleLabel, (v) => ViewModel.SetNoiseScale(v));
        _moveSpeedSlider = CreateSliderRow(_simOnlyGroup.transform, res, "Slide Speed", 0f, 3f, ViewModel.Settings.MoveSpeed, out _moveSpeedLabel, (v) => ViewModel.SetMoveSpeed(v));

        CreateHeader(_worldTab.transform, "SYSTEM", Color.white);
        CreateToggle(_worldTab.transform, res, "Override Sensor (Simulated)", ViewModel.Controller != null && ViewModel.Controller.IsSimulationEnabled, (v) => OnToggleSimulation(v));
        CreateButton(_worldTab.transform, res, "Cycle Monitor Camera", () => CycleCameraView());
    }

    private void OnToggleSimulation(bool useSim)
    {
        if (useSim)
        {
            // Simple: Just switch to simulation
            ViewModel.SetEnableSimulation(true);
            UpdateLabels();
            UpdateVisibility();
        }
        else
        {
            // Complex: Attempt to re-enable hardware with 3s watchdog
            StartCoroutine(HandshakeHardwareCoroutine());
        }
    }

    private System.Collections.IEnumerator HandshakeHardwareCoroutine()
    {
        SetStatusText("SEARCHING FOR SENSOR...", Color.cyan);
        
        // 1. Trigger Hardware Activation
        ViewModel.SetEnableSimulation(false); // This starts the provider
        
        // 2. 3-Second Watchdog
        float timer = 0f;
        while (timer < 3.0f)
        {
            timer += Time.deltaTime;
            
            // Success Check (Does the controller have a valid running provider?)
            if (ViewModel.Controller != null && 
                ViewModel.Controller.IsSimulationEnabled == false && 
                ViewModel.Controller.GetRawDepthTexture() != null) // Simple heuristic for "it's working"
            {
                SetStatusText("SENSOR CONNECTED", Color.green);
                UpdateLabels();
                UpdateVisibility();
                yield break;
            }
            yield return null;
        }

        // 3. Timeout Failure
        Debug.LogWarning("SandboxUI: Hardware Handshake Timeout (3s). Reverting to Simulation.");
        ViewModel.SetEnableSimulation(true); // REVERT
        SetStatusText("HARDWARE TIMEOUT: RETURNING TO SIM", Color.red);
        UpdateLabels();
        UpdateVisibility();
        
        yield return new WaitForSeconds(2f);
        SetStatusText("", Color.white);
    }

    private void SetStatusText(string msg, Color col)
    {
        if (_statusText)
        {
            _statusText.text = msg;
            _statusText.color = col;
        }
        Debug.Log($"[STATUS] {msg}");
    }

    // --- Component Factories ---

    Slider CreateSlider(Transform parent, DefaultControls.Resources res, float min, float max, float current, UnityEngine.Events.UnityAction<float> onVal)
    {
        GameObject sliderObj = DefaultControls.CreateSlider(res);
        sliderObj.transform.SetParent(parent, false);
        
        Slider s = sliderObj.GetComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.value = current;
        s.onValueChanged.AddListener(onVal);
        
        // Setup Save on release if possible (Slider doesn't have onPointerUp by default, 
        // but we can use an EventTrigger or just keep it as onValueChanged for now)

        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30); 
        
        return s;
    }

    // New Smart Factory: Label + Slider + Value in one row
    Slider CreateSliderRow(Transform parent, DefaultControls.Resources res, string label, float min, float max, float current, out Text valueLabel, UnityEngine.Events.UnityAction<float> onVal)
    {
        GameObject row = new GameObject("SliderRow_" + label);
        row.transform.SetParent(parent, false);
        
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.childControlWidth = true;
        h.childForceExpandWidth = false;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = 10;
        
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 35);

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 35;
        le.preferredHeight = 35;

        // 1. Label (Fixed Width)
        Text t = CreateLabel(row.transform, label, 14, Color.white);
        LayoutElement leLabel = t.gameObject.AddComponent<LayoutElement>();
        leLabel.preferredWidth = 120; // Enough for "Gradient Intensity"

        // 2. Slider (Stretch)
        Slider s = CreateSlider(row.transform, res, min, max, current, onVal);
        LayoutElement leSlider = s.gameObject.AddComponent<LayoutElement>();
        leSlider.flexibleWidth = 1;

        // 3. Value Output (Fixed Width)
        valueLabel = CreateLabel(row.transform, "0.00", 14, Color.cyan);
        valueLabel.alignment = TextAnchor.MiddleRight;
        LayoutElement leValue = valueLabel.gameObject.AddComponent<LayoutElement>();
        leValue.preferredWidth = 60;

        return s;
    }

    Text CreateHeader(Transform parent, string content, Color col)
    {
        Text t = CreateLabel(parent, content, 18, col);
        t.fontStyle = FontStyle.Bold;
        
        // Add a bit more padding above headers
        LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 35; 
        
        return t;
    }

    Text CreateLabel(Transform parent, string content, int size, Color col)
    {
        GameObject tObj = new GameObject("Label");
        tObj.transform.SetParent(parent, false);
        
        Text t = tObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        t.text = content;
        t.fontSize = size;
        t.color = col;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt = tObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, size + 5);

        // Add LayoutElement for VerticalLayoutGroups
        LayoutElement le = tObj.AddComponent<LayoutElement>();
        le.minHeight = size + 5;
        le.preferredHeight = size + 5;

        return t;
    }

    void CreateToggle(Transform parent, DefaultControls.Resources res, string label, bool isOn, UnityEngine.Events.UnityAction<bool> onVal)
    {
        GameObject tObj = DefaultControls.CreateToggle(res);
        tObj.transform.SetParent(parent, false);
        
        Toggle t = tObj.GetComponent<Toggle>();
        t.isOn = isOn;
        t.onValueChanged.AddListener(onVal);

        // Styling: Find the Background and Checkmark
        // Default hierarchy: Toggle -> Background -> Checkmark
        Image bg = tObj.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Darker background

        Image checkmark = tObj.transform.Find("Background/Checkmark")?.GetComponent<Image>();
        if (checkmark != null) {
            checkmark.color = Color.yellow; // Distinctive check color
        }

        Text txt = tObj.GetComponentInChildren<Text>();
        if (txt) {
            txt.text = label;
            txt.color = Color.white;
            txt.fontSize = 18;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle = FontStyle.Bold;
        }

        RectTransform rt = tObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30);

        // Add LayoutElement to prevent squashing
        LayoutElement le = tObj.AddComponent<LayoutElement>();
        le.minHeight = 30;
        le.preferredHeight = 30;

        // Ensure the Label child stretches to fit
        Transform labelX = tObj.transform.Find("Label");
        if (labelX)
        {
            RectTransform labelRT = labelX.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0);
            labelRT.anchorMax = new Vector2(1, 1);
            labelRT.offsetMin = new Vector2(35, 0); // Room for checkmark
            labelRT.offsetMax = new Vector2(0, 0);
        }
    }
    
    GameObject CreateButton(Transform parent, DefaultControls.Resources res, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject bObj = DefaultControls.CreateButton(res);
        bObj.transform.SetParent(parent, false);
        
        Button b = bObj.GetComponent<Button>();
        b.onClick.AddListener(onClick);
        
        Text t = bObj.GetComponentInChildren<Text>();
        if (t) {
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold;
        }
        
        RectTransform rt = bObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);

        LayoutElement le = bObj.AddComponent<LayoutElement>();
        le.minHeight = 40;
        le.preferredHeight = 40;

        return bObj;
    }

    void ToggleUI(bool show)
    {
        _isVisible = show;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = show ? 1f : 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;
        }
        
        // Safety: If UI is being shown, ensure ROI Editor is closed
        if (show && ROIEditor != null && ROIEditor.IsActive)
        {
            ROIEditor.Hide();
            Debug.Log("SandboxUI: Force-closed ROI Editor to show Main UI.");
        }
    }

    void UpdateLabels()
    {
        if (_heightLabel == null || ViewModel?.Settings == null) return;

        // Use StringBuilder to avoid GC
        _sb.Clear(); _sb.Append("Hz: ").Append(ViewModel.Settings.HeightScale.ToString("F2"));
        // Add current preset name for feedback
        _sb.Append(" (").Append(ViewModel.Settings.ColorScheme.ToString()).Append(")");
        _heightLabel.text = _sb.ToString();

        if (_minDepthLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(ViewModel.Settings.MinDepth.ToString("F0"));
            _minDepthLabel.text = _sb.ToString();
        }

        if (_maxDepthLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(ViewModel.Settings.MaxDepth.ToString("F0"));
            _maxDepthLabel.text = _sb.ToString();
        }

        if (_tintStrengthLabel) {
            _sb.Clear(); _sb.Append("%: ").Append((ViewModel.Settings.TintStrength * 100).ToString("F0"));
            _tintStrengthLabel.text = _sb.ToString();
        }

        if (_sandScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.SandScale.ToString("F1"));
            _sandScaleLabel.text = _sb.ToString();
        }

        if (_waterLevelLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(ViewModel.Settings.WaterLevel.ToString("F2"));
            _waterLevelLabel.text = _sb.ToString();
        }

        if (_colorShiftLabel) {
            _sb.Clear(); _sb.Append("s: ").Append(ViewModel.Settings.ColorShift.ToString("F2"));
            _colorShiftLabel.text = _sb.ToString();
        }

        if (_contourIntervalLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(ViewModel.Settings.ContourInterval.ToString("F2"));
            _contourIntervalLabel.text = _sb.ToString();
        }

        if (_contourThicknessLabel) {
            _sb.Clear(); _sb.Append("px: ").Append(ViewModel.Settings.ContourThickness.ToString("F1"));
            _contourThicknessLabel.text = _sb.ToString();
        }

        if (_staticStabilityLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(ViewModel.Settings.MinCutoff.ToString("F2"));
            _staticStabilityLabel.text = _sb.ToString();
        }

        if (_motionResponseLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(ViewModel.Settings.Beta.ToString("F3"));
            _motionResponseLabel.text = _sb.ToString();
        }

        if (_handRejectionLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(ViewModel.Settings.HandThreshold.ToString("F0"));
            _handRejectionLabel.text = _sb.ToString();
        }

        if (_lineSmoothLabel) {
            _sb.Clear(); _sb.Append("Iter: ").Append(ViewModel.Settings.SpatialBlur);
            _lineSmoothLabel.text = _sb.ToString();
        }

        if (_noiseScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.NoiseScale.ToString("F3"));
            _noiseScaleLabel.text = _sb.ToString();
        }

        if (_moveSpeedLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(ViewModel.Settings.MoveSpeed.ToString("F2"));
            _moveSpeedLabel.text = _sb.ToString();
        }

        if (_causticIntensityLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.CausticIntensity.ToString("F2"));
            _causticIntensityLabel.text = _sb.ToString();
        }

        if (_causticScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.CausticScale.ToString("F3"));
            _causticScaleLabel.text = _sb.ToString();
        }

        if (_causticSpeedLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(ViewModel.Settings.CausticSpeed.ToString("F2"));
            _causticSpeedLabel.text = _sb.ToString();
        }

        if (_waterOpacityLabel) {
            _sb.Clear(); _sb.Append("%: ").Append((ViewModel.Settings.WaterOpacity * 100).ToString("F0"));
            _waterOpacityLabel.text = _sb.ToString();
        }

        if (_sparkleIntensityLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.SparkleIntensity.ToString("F2"));
            _sparkleIntensityLabel.text = _sb.ToString();
        }

        if (_boundsSizeLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(ViewModel.Settings.MeshSize.ToString("F1"));
            _boundsSizeLabel.text = _sb.ToString();
        }

        if (_meshResLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(ViewModel.Settings.MeshResolution);
            _meshResLabel.text = _sb.ToString();
        }

        // --- MODE BANNER UPDATE ---
        bool isSim = ViewModel.Controller != null && ViewModel.Controller.IsSimulationEnabled;
        if (_modeBanner && _modeBannerText)
        {
            if (isSim)
            {
                _modeBanner.color = new Color(0f, 0.5f, 0.5f, 1f); // Muted Teal
                _modeBannerText.text = "MODE: SIMULATION (VIRTUAL)";
                _modeBannerText.color = Color.white;
            }
            else
            {
                _modeBanner.color = new Color(1f, 0.75f, 0f, 1f); // Deep Amber
                _modeBannerText.text = "MODE: KIOSK (HARDWARE)";
                _modeBannerText.color = Color.black; 
            }
        }

        // --- SLIDER SYNC ---
        if (_noiseScaleSlider) _noiseScaleSlider.value = ViewModel.Settings.NoiseScale;
        if (_moveSpeedSlider) _moveSpeedSlider.value = ViewModel.Settings.MoveSpeed;
    }
    }
}
