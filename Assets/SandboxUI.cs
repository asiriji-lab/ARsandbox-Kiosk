using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Ruggedized Admin UI for Museum AR Sandbox.
/// Adheres to "Start-Phase" initialization and Programmatic UI standards.
/// Updated: Supports 4-Point Calibration and Auto-Floor.
/// </summary>
public class SandboxUI : MonoBehaviour
{
    [Header("Target Controller")]
    [FormerlySerializedAs("controller")]
    public ARSandboxController Controller;

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
    private Slider _waterOpacitySlider;
    private Slider _sparkleIntensitySlider;
    
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
    private Text _lineSmoothLabel;
    private Text _causticIntensityLabel;
    private Text _causticScaleLabel;
    private Text _causticSpeedLabel;
    private Text _waterOpacityLabel;
    private Text _sparkleIntensityLabel;
    private Text _calibrationResultLabel;

    // Optimization: Cached Strings
    private StringBuilder _sb = new StringBuilder();
    private float _lastMouseMoveTime;
    private const float UI_IDLE_TIME = 10f;
    private bool _isFaded = false;

    // Camera Views
    public enum CamView { Top, Perspective, Side }
    private CamView _currentView = CamView.Top;

    void CycleCameraView() => CycleCameraView(1); // Default forward

    void CycleCameraView(int direction)
    {
        int next = (int)_currentView + direction;
        if (next < 0) next = 2; // Wrap around for -1
        else if (next > 2) next = 0; // Wrap around for +1
        _currentView = (CamView)next;
        UpdateCameraTransform();
    }

    void UpdateCameraTransform()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float size = Controller.Width; // Assume square
        // Ensure size is at least 10 to avoid clipping
        if (size < 10) size = 10;

        switch (_currentView)
        {
            case CamView.Top:
                // Standard: Look Down
                cam.transform.position = new Vector3(0, size, 0);
                cam.transform.rotation = Quaternion.Euler(90, 0, 0);
                break;
            case CamView.Perspective:
                // 45 Degree
                cam.transform.position = new Vector3(0, size * 0.8f, -size * 0.8f);
                cam.transform.LookAt(Vector3.zero);
                break;
            case CamView.Side:
                // Side View (Amplitude Check)
                cam.transform.position = new Vector3(0, size * 0.2f, -size);
                cam.transform.LookAt(Vector3.zero);
                break;
        }
        Debug.Log($"Switched View to: {_currentView}");
    }

    void Start()
    {
        Debug.Log("SandboxUI: Start() Called");
        // "External" Initialization
        if (Controller == null)
            Controller = FindFirstObjectByType<ARSandboxController>();

        if (Controller == null)
        {
            Debug.LogError("SandboxUI: No ARSandboxController found!");
            return;
        }
        else 
        {
             Debug.Log("SandboxUI: Controller found.");
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

    void Update()
    {
        HandleInput();
        HandleAutoHide();
        
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

    void AdjustCameraZoom(float delta)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (_currentView == CamView.Top)
        {
            // Top View: Move Up/Down (World Y)
            // Up Arrow (+delta) = Move Up; Down Arrow (-delta) = Move Down
            cam.transform.Translate(0, delta, 0, Space.World);
        }
        else
        {
            // Perspective: Move Forward/Back (Local Z)
            // Up Arrow (+delta) = Move Forward (Zoom In); Down Arrow (-delta) = Move Back
            cam.transform.Translate(0, 0, delta, Space.Self);
        }
    }

    void UpdateCalibrationLogic()
    {
        // Ensure Texture is up to date
        if (_calibrationOverlay.texture == null)
        {
            _calibrationOverlay.texture = Controller.GetRawDepthTexture();
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
            // Note: RawImage fills screen, so Screen = Texture Coords roughly
            float normX = Mathf.Clamp01(mousePos.x / Screen.width);
            float normY = Mathf.Clamp01(mousePos.y / Screen.height);

            // Update Controller
            Controller.CalibrationPoints[_draggingHandleIndex] = new Vector2(normX, normY);
            
            // Update Visual
            _handles[_draggingHandleIndex].position = mousePos;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_draggingHandleIndex != -1)
            {
                _draggingHandleIndex = -1;
                Controller.SaveSettings(); // Save on release
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
                    Vector2 norm = Controller.CalibrationPoints[i];
                    _handles[i].position = new Vector2(norm.x * Screen.width, norm.y * Screen.height);
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
        // rt.sizeDelta = Vector2.zero; // Let layout handle it

        VerticalLayoutGroup layout = g.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 5;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        
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
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 10;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        // 3. Header & Tabs
        CreateLabel(_uiRoot.transform, "SANDBOX COMMAND", 24, Color.yellow);
        
        DefaultControls.Resources res = new DefaultControls.Resources();
        res.background = BackgroundSprite;
        res.knob = KnobSprite;
        res.standard = BackgroundSprite;

        BuildTabs(_uiRoot.transform, res);

        // 4. Content Groups
        _viewTab = CreateGroup(_uiRoot.transform, "View_Tab");
        _setupTab = CreateGroup(_uiRoot.transform, "Setup_Tab");
        _worldTab = CreateGroup(_uiRoot.transform, "World_Tab");

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

        string[] labels = { "VIEW", "SETUP", "WORLD" };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            CreateButton(tabRow.transform, res, labels[i], () => SetActiveTab(index));
        }
    }

    void ApplyPreset(string name)
    {
        if (name == "Volcano")
        {
            Controller.HeightScale = 8.0f;
            Controller.TintStrength = 0.8f;
            Controller.UseDiscreteBands = true;
            Controller.ApplyGradientPreset(ARSandboxController.GradientPreset.Desert);
        }
        else if (name == "Ocean")
        {
            Controller.HeightScale = 3.0f;
            Controller.TintStrength = 0.4f;
            Controller.UseDiscreteBands = false;
            Controller.ApplyGradientPreset(ARSandboxController.GradientPreset.UCDavis);
        }
        else // Oasis / Default
        {
            Controller.HeightScale = 5.0f;
            Controller.TintStrength = 0.5f;
            Controller.UseDiscreteBands = false;
            Controller.ApplyGradientPreset(ARSandboxController.GradientPreset.UCDavis);
        }
        
        // Refresh Sliders
        if (_heightSlider) _heightSlider.value = Controller.HeightScale;
        if (_tintStrengthSlider) _tintStrengthSlider.value = Controller.TintStrength;
        Controller.UpdateMaterialProperties();
        Controller.SaveSettings();
    }

    void BuildViewTab(DefaultControls.Resources res)
    {
        CreateHeader(_viewTab.transform, "SANDBOX VISUALS", Color.cyan);
        
        _heightSlider = CreateSliderRow(_viewTab.transform, res, "Hill Height", 0.0f, 10f, Controller.HeightScale, out _heightLabel, (v) => {
            Controller.HeightScale = v;
            Controller.SaveSettings();
        });

        _tintStrengthSlider = CreateSliderRow(_viewTab.transform, res, "Color Intensity", 0.0f, 1.0f, Controller.TintStrength, out _tintStrengthLabel, (v) => {
            Controller.TintStrength = v;
            Controller.SaveSettings();
        });

        _sandScaleSlider = CreateSliderRow(_viewTab.transform, res, "Texture Detail", 1f, 50f, Controller.SandScale, out _sandScaleLabel, (v) => {
            Controller.SandScale = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _waterLevelSlider = CreateSliderRow(_viewTab.transform, res, "Water Level", 0f, 10f, Controller.WaterLevel, out _waterLevelLabel, (v) => {
            Controller.WaterLevel = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _colorShiftSlider = CreateSliderRow(_viewTab.transform, res, "Color Spread", -0.5f, 0.5f, Controller.ColorShift, out _colorShiftLabel, (v) => {
            Controller.ColorShift = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        CreateHeader(_viewTab.transform, "WATER & CAUSTICS", Color.cyan);

        _waterOpacitySlider = CreateSliderRow(_viewTab.transform, res, "Water Opacity", 0f, 1f, Controller.WaterOpacity, out _waterOpacityLabel, (v) => {
            Controller.WaterOpacity = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _causticIntensitySlider = CreateSliderRow(_viewTab.transform, res, "Caustic Brightness", 0f, 2f, Controller.CausticIntensity, out _causticIntensityLabel, (v) => {
            Controller.CausticIntensity = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _causticScaleSlider = CreateSliderRow(_viewTab.transform, res, "Caustic Pattern Size", 0f, 0.15f, Controller.CausticScale, out _causticScaleLabel, (v) => {
            Controller.CausticScale = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _causticSpeedSlider = CreateSliderRow(_viewTab.transform, res, "Caustic Shimmer", 0f, 2f, Controller.CausticSpeed, out _causticSpeedLabel, (v) => {
            Controller.CausticSpeed = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _sparkleIntensitySlider = CreateSliderRow(_viewTab.transform, res, "Sand Sparkle", 0f, 5f, Controller.SparkleIntensity, out _sparkleIntensityLabel, (v) => {
            Controller.SparkleIntensity = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _contourIntervalSlider = CreateSliderRow(_viewTab.transform, res, "Contour Gap", 0.1f, 2.0f, Controller.ContourInterval, out _contourIntervalLabel, (v) => {
            Controller.ContourInterval = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

        _contourThicknessSlider = CreateSliderRow(_viewTab.transform, res, "Line Thickness", 0.1f, 5.0f, Controller.ContourThickness, out _contourThicknessLabel, (v) => {
            Controller.ContourThickness = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });

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
            int next = (int)Controller.CurrentGradientPreset + 1;
            if (next >= System.Enum.GetValues(typeof(ARSandboxController.GradientPreset)).Length) next = 0;
            Controller.ApplyGradientPreset((ARSandboxController.GradientPreset)next);
            Controller.SaveSettings();
            UpdateLabels();
        });

        CreateToggle(_viewTab.transform, res, "Discrete Bands (Topographic)", Controller.UseDiscreteBands, (v) => {
            Controller.UseDiscreteBands = v;
            Controller.UpdateMaterialProperties();
            Controller.SaveSettings();
        });
    }

    void BuildSetupTab(DefaultControls.Resources res)
    {
        CreateHeader(_setupTab.transform, "SENSOR & STABILITY", Color.green);

        _staticStabilitySlider = CreateSliderRow(_setupTab.transform, res, "Anti-Shake", 0.1f, 5.0f, Controller.MinCutoff, out _staticStabilityLabel, (v) => {
            Controller.MinCutoff = v;
            Controller.SaveSettings();
        });

        _motionResponseSlider = CreateSliderRow(_setupTab.transform, res, "Follow Speed", 0.001f, 0.2f, Controller.Beta, out _motionResponseLabel, (v) => {
            Controller.Beta = v;
            Controller.SaveSettings();
        });

        _handRejectionSlider = CreateSliderRow(_setupTab.transform, res, "Hand Rejection", 10f, 300f, Controller.HandFilterThreshold, out _handRejectionLabel, (v) => {
            Controller.HandFilterThreshold = v;
            Controller.SaveSettings();
        });

        _lineSmoothSlider = CreateSliderRow(_setupTab.transform, res, "Line Smoothness", 0f, 5f, Controller.SpatialBlurIterations, out _lineSmoothLabel, (v) => {
            Controller.SpatialBlurIterations = (int)v;
            Controller.SaveSettings();
        });

        CreateHeader(_setupTab.transform, "CALIBRATION", Color.white);
        CreateButton(_setupTab.transform, res, "Auto-Calibrate Floor (Zero)", () => {
            Controller.CalibrateFloor();
            if (_calibrationResultLabel != null) 
                _calibrationResultLabel.text = $"Floor: {Controller.MaxDepthMM:F0}mm | Peak: {Controller.MinDepthMM:F0}mm";
            UpdateLabels();
        });
        _calibrationResultLabel = CreateLabel(_setupTab.transform, "Place sensor at 1.5m-2m height", 14, Color.gray);

        CreateToggle(_setupTab.transform, res, "Edit Corner Mapping", false, (v) => ToggleCalibration(v));

        _minDepthSlider = CreateSliderRow(_setupTab.transform, res, "Peak Depth [mm]", 0f, 3000f, Controller.MinDepthMM, out _minDepthLabel, (v) => {
            Controller.MinDepthMM = v;
            Controller.SaveSettings();
        });

        _maxDepthSlider = CreateSliderRow(_setupTab.transform, res, "Floor Depth [mm]", 0f, 3000f, Controller.MaxDepthMM, out _maxDepthLabel, (v) => {
            Controller.MaxDepthMM = v;
            Controller.SaveSettings();
        });
    }

    void BuildWorldTab(DefaultControls.Resources res)
    {
        CreateHeader(_worldTab.transform, "WORLD SETTINGS", Color.white);

        _boundsSizeSlider = CreateSliderRow(_worldTab.transform, res, "Sandbox Size [m]", 5f, 30f, Controller.Width, out _boundsSizeLabel, (v) => {
            Controller.UpdateMeshDimensions(v);
            Controller.SaveSettings();
        });

        CreateToggle(_worldTab.transform, res, "Solid Side Walls", Controller.ShowWalls, (v) => Controller.ShowWalls = v);

        CreateToggle(_worldTab.transform, res, "Flat Mapping (2D)", Controller.FlatMode, (v) => {
            Controller.FlatMode = v;
            Controller.SaveSettings();
        });

        CreateHeader(_worldTab.transform, "SIMULATION MODE", Color.yellow);
        CreateToggle(_worldTab.transform, res, "Enable Virtual Sand", Controller.EnableSimulation, (v) => {
            Controller.EnableSimulation = v;
        });

        CreateSliderRow(_worldTab.transform, res, "Terrain Chaos", 0f, 0.15f, Controller.NoiseScale, out _noiseScaleLabel, (v) => {
            Controller.NoiseScale = v;
            Controller.SaveSettings();
        });

        CreateSliderRow(_worldTab.transform, res, "Slide Speed", 0f, 3f, Controller.MoveSpeed, out _moveSpeedLabel, (v) => {
            Controller.MoveSpeed = v;
            Controller.SaveSettings();
        });
        
        CreateHeader(_worldTab.transform, "ADMIN", Color.gray);
        CreateButton(_worldTab.transform, res, "Cycle Monitor Camera", () => CycleCameraView());
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
    }
    
    void CreateButton(Transform parent, DefaultControls.Resources res, string label, UnityEngine.Events.UnityAction onClick)
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
    }

    void UpdateLabels()
    {
        if (_heightLabel == null) return;

        // Use StringBuilder to avoid GC
        _sb.Clear(); _sb.Append("Hz: ").Append(Controller.HeightScale.ToString("F2"));
        // Add current preset name for feedback
        _sb.Append(" (").Append(Controller.CurrentGradientPreset.ToString()).Append(")");
        _heightLabel.text = _sb.ToString();

        if (_minDepthLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(Controller.MinDepthMM.ToString("F0"));
            _minDepthLabel.text = _sb.ToString();
        }

        if (_maxDepthLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(Controller.MaxDepthMM.ToString("F0"));
            _maxDepthLabel.text = _sb.ToString();
        }

        if (_tintStrengthLabel) {
            _sb.Clear(); _sb.Append("%: ").Append((Controller.TintStrength * 100).ToString("F0"));
            _tintStrengthLabel.text = _sb.ToString();
        }

        if (_sandScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(Controller.SandScale.ToString("F1"));
            _sandScaleLabel.text = _sb.ToString();
        }

        if (_waterLevelLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(Controller.WaterLevel.ToString("F2"));
            _waterLevelLabel.text = _sb.ToString();
        }

        if (_colorShiftLabel) {
            _sb.Clear(); _sb.Append("s: ").Append(Controller.ColorShift.ToString("F2"));
            _colorShiftLabel.text = _sb.ToString();
        }

        if (_contourIntervalLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(Controller.ContourInterval.ToString("F2"));
            _contourIntervalLabel.text = _sb.ToString();
        }

        if (_contourThicknessLabel) {
            _sb.Clear(); _sb.Append("px: ").Append(Controller.ContourThickness.ToString("F1"));
            _contourThicknessLabel.text = _sb.ToString();
        }

        if (_staticStabilityLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(Controller.MinCutoff.ToString("F2"));
            _staticStabilityLabel.text = _sb.ToString();
        }

        if (_motionResponseLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(Controller.Beta.ToString("F3"));
            _motionResponseLabel.text = _sb.ToString();
        }

        if (_handRejectionLabel) {
            _sb.Clear(); _sb.Append("mm: ").Append(Controller.HandFilterThreshold.ToString("F0"));
            _handRejectionLabel.text = _sb.ToString();
        }

        if (_lineSmoothLabel) {
            _sb.Clear(); _sb.Append("Iter: ").Append(Controller.SpatialBlurIterations);
            _lineSmoothLabel.text = _sb.ToString();
        }

        if (_noiseScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(Controller.NoiseScale.ToString("F3"));
            _noiseScaleLabel.text = _sb.ToString();
        }

        if (_moveSpeedLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(Controller.MoveSpeed.ToString("F2"));
            _moveSpeedLabel.text = _sb.ToString();
        }

        if (_causticIntensityLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(Controller.CausticIntensity.ToString("F2"));
            _causticIntensityLabel.text = _sb.ToString();
        }

        if (_causticScaleLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(Controller.CausticScale.ToString("F3"));
            _causticScaleLabel.text = _sb.ToString();
        }

        if (_causticSpeedLabel) {
            _sb.Clear(); _sb.Append("v: ").Append(Controller.CausticSpeed.ToString("F2"));
            _causticSpeedLabel.text = _sb.ToString();
        }

        if (_waterOpacityLabel) {
            _sb.Clear(); _sb.Append("%: ").Append((Controller.WaterOpacity * 100).ToString("F0"));
            _waterOpacityLabel.text = _sb.ToString();
        }

        if (_sparkleIntensityLabel) {
            _sb.Clear(); _sb.Append("x: ").Append(Controller.SparkleIntensity.ToString("F2"));
            _sparkleIntensityLabel.text = _sb.ToString();
        }

        if (_boundsSizeLabel) {
            _sb.Clear(); _sb.Append("m: ").Append(Controller.Width.ToString("F1"));
            _boundsSizeLabel.text = _sb.ToString();
        }
    }
}
