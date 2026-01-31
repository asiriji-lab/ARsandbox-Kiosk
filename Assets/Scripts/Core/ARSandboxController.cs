using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using UnityEngine.Serialization;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System;
using ARSandbox.UI;
using ARSandbox.Core;

namespace ARSandbox
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ARSandboxController : MonoBehaviour, IUISystem
{
    // Vertex Struct for Interleaved MeshData (High Performance)
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Pos;
        public Vector2 Uv;
        public Vector2 Uv2;
    }

    [Header("Data Source")]
    public bool EnableSimulation = true;
    private IDepthProvider _activeProvider;
    private KinectDepthProvider _kinectProvider;
    private SimulatedDepthProvider _simProvider;

    // Data Source - Settings
    public SandboxSettingsSO Settings;
    public SandboxSettingsSO CurrentSettings => Settings;

    // Legacy Fields Proxied to SO
    public float Width { get => Settings.MeshSize; set => Settings.MeshSize = value; }
    public float Length { get => Settings.MeshSize; set => Settings.MeshSize = value; }
    public int MeshResolution { get => Settings.MeshResolution; set => Settings.MeshResolution = value; }

    public float NoiseScale { get => Settings.NoiseScale; set => Settings.NoiseScale = value; }
    public float MoveSpeed { get => Settings.MoveSpeed; set => Settings.MoveSpeed = value; }

    [Header("Height Calibration")]
    public float MinDepthMM { get => Settings.MinDepth; set => Settings.MinDepth = value; }
    public float MaxDepthMM { get => Settings.MaxDepth; set => Settings.MaxDepth = value; }
    public float HeightScale { get => Settings.HeightScale; set => Settings.HeightScale = value; }

    [Header("1 Euro Filter (Adaptive)")]
    public float MinCutoff { get => Settings.MinCutoff; set => Settings.MinCutoff = value; }
    public float Beta { get => Settings.Beta; set => Settings.Beta = value; }
    public float HandFilterThreshold { get => Settings.HandThreshold; set => Settings.HandThreshold = value; }
    public int SpatialBlurIterations { get => Settings.SpatialBlur; set => Settings.SpatialBlur = value; }

    [Header("Visuals")]
    public float TintStrength { get => Settings.TintStrength; set => Settings.TintStrength = value; }
    public float SandScale { get => Settings.SandScale; set => Settings.SandScale = value; }
    public float WaterLevel { get => Settings.WaterLevel; set => Settings.WaterLevel = value; }
    public float WaterOpacity { get => Settings.WaterOpacity; set => Settings.WaterOpacity = value; }
    public Color WaterColor { get => Settings.WaterColor; set => Settings.WaterColor = value; }
    public float ColorShift { get => Settings.ColorShift; set => Settings.ColorShift = value; }

    [Header("Contour Styling")]
    public float ContourInterval { get => Settings.ContourInterval; set => Settings.ContourInterval = value; }
    public float ContourThickness { get => Settings.ContourThickness; set => Settings.ContourThickness = value; }
    public bool ShowWalls { get => Settings.ShowWalls; set => Settings.ShowWalls = value; }

    [Header("Sparkles")]
    public float SparkleIntensity { get => Settings.SparkleIntensity; set => Settings.SparkleIntensity = value; }

    [Header("Water Caustics")]
    public Texture2D CausticTexture;
    public float CausticIntensity { get => Settings.CausticIntensity; set => Settings.CausticIntensity = value; }
    public float CausticScale { get => Settings.CausticScale; set => Settings.CausticScale = value; }
    public float CausticSpeed { get => Settings.CausticSpeed; set => Settings.CausticSpeed = value; }

    public GradientPreset CurrentGradientPreset { get => Settings.ColorScheme; set => Settings.ColorScheme = value; }
    public bool UseDiscreteBands { get => Settings.UseDiscreteBands; set => Settings.UseDiscreteBands = value; }
    public Gradient ElevationGradient;
    private Texture2D _colorRampTexture;
    public ComputeShader TerrainSimulationShader;
    
    [Header("UI Reference")]
    public SandboxUI UI; // Now visible via import

    // Logic Components
    private DepthProcessor _depthProcessor;
    private TerrainMeshGenerator _meshGenerator;
    private SensorWatchdog _watchdog;

    // Debug Texture (Now retrieved from DepthProcessor)
    private Texture2D _rawDepthTexture;
    private byte[] _rawColorBuffer;

    private string _settingsPath => System.IO.Path.Combine(Application.persistentDataPath, "sandbox_settings.json");

    void Awake()
    {
        if (Settings == null) Settings = ScriptableObject.CreateInstance<SandboxSettingsSO>();
        
        // Load persisted settings from JSON
        SandboxSettingsManager.Load(Settings);
        
        if (GetComponent<SandboxViewModel>() == null) gameObject.AddComponent<SandboxViewModel>();
        
        if (TerrainSimulationShader == null)
        {
            Debug.LogWarning("TerrainSimulationShader is not assigned in ARSandboxController. Attempting to load from Resources...");
            TerrainSimulationShader = Resources.Load<ComputeShader>("Compute/TerrainSimulation");
            
            if (TerrainSimulationShader == null)
            {
                Debug.LogError("CRITICAL: Could not load TerrainSimulation.compute from Resources! Please assign it manually in the Inspector.");
                return; // Prevent null reference exceptions
            }
            else
            {
                Debug.Log("Successfully loaded TerrainSimulation.compute from Resources.");
            }
        }

        // CRITICAL: Validate Settings Reference matches ViewModel
        var viewModel = FindFirstObjectByType<SandboxViewModel>();
        if (viewModel != null && viewModel.Settings != Settings)
        {
            Debug.LogError($"SETTINGS MISMATCH! ARSandboxController.Settings and SandboxViewModel.Settings MUST reference the SAME ScriptableObject!\n" +
                          $"Controller: {(Settings != null ? Settings.name : "NULL")}\n" +
                          $"ViewModel: {(viewModel.Settings != null ? viewModel.Settings.name : "NULL")}\n" +
                          $"FIX: In Unity Inspector, assign the SAME Settings asset to both components!");
        }

        _depthProcessor = new DepthProcessor(TerrainSimulationShader);
        _meshGenerator = new TerrainMeshGenerator(TerrainSimulationShader, Settings.MeshResolution, Settings.MeshResolution);
        _meshGenerator.Initialize(gameObject, Settings.MeshResolution, Settings.MeshSize, TerrainSimulationShader);
        
        // Assign material to generator for Zero-Copy rendering
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) _meshGenerator.SetMaterial(mr.sharedMaterial);

        _watchdog = new SensorWatchdog(ResetSensor);
    }

    void Start()
    {
        InitColorRamp();
        AutoAssignTextures();

        _kinectProvider = gameObject.AddComponent<KinectDepthProvider>();
        _simProvider = gameObject.AddComponent<SimulatedDepthProvider>();
        
        SwitchProvider(EnableSimulation);

        if (FindFirstObjectByType<SandboxUI>() == null)
            new GameObject("SandboxUI").AddComponent<SandboxUI>();

        if (CausticTexture == null)
        {
            CausticTexture = Resources.Load<Texture2D>("WaterCaustics"); 
            #if UNITY_EDITOR
            if (CausticTexture == null) {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("WaterCaustics t:Texture2D");
                if (guids.Length > 0) {
                    CausticTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            #endif
        }
    }

    [ContextMenu("Refresh Caustic Texture")]
    public void RefreshCausticTexture() {
        CausticTexture = null; Start();
    }

    void SwitchProvider(bool useSim)
    {
        EnableSimulation = useSim;
        if (_activeProvider != null) _activeProvider.Shutdown();

        if (useSim)
        {
            _activeProvider = _simProvider;
            _simProvider.NoiseScale = NoiseScale;
            _simProvider.MoveSpeed = MoveSpeed;
            _simProvider.MinDepthMM = MinDepthMM;
            _simProvider.MaxDepthMM = MaxDepthMM;
        }
        else
        {
            _activeProvider = _kinectProvider;
        }

        _activeProvider.Initialize();
        _watchdog?.ResetHeartbeat();
        Debug.Log($"Switched to Provider: {_activeProvider.GetDeviceName()}");
    }

    public void ResetSensor()
    {
        if (_activeProvider == null) return;
        Debug.Log($"[ARSandboxController] Resetting Sensor: {_activeProvider.GetDeviceName()}");
        
        _activeProvider.Shutdown();
        _activeProvider.Initialize();
        _watchdog?.ResetHeartbeat();
    }

    void OnDestroy()
    {
        _depthProcessor?.Dispose();
        _meshGenerator?.Dispose();
    }

    void Update()
    {
        if (_activeProvider == (IDepthProvider)_simProvider && !EnableSimulation) SwitchProvider(false);
        if (_activeProvider == (IDepthProvider)_kinectProvider && EnableSimulation) SwitchProvider(true);

        if (_activeProvider == (IDepthProvider)_simProvider)
        {
             _simProvider.MinDepthMM = MinDepthMM;
             _simProvider.MaxDepthMM = MaxDepthMM;
             _simProvider.NoiseScale = NoiseScale;
             _simProvider.MoveSpeed = MoveSpeed;
        }

        ProcessDepthData();
        UpdateShader();

        _watchdog?.Update(Settings.WatchdogTimeout, Settings.WatchdogAutoRetry);
    }

    void ProcessDepthData()
    {
        if (_activeProvider == null || !_activeProvider.IsRunning) return;

        ushort[] depthData = _activeProvider.GetDepthData();
        if (depthData == null) return;

        _watchdog?.ResetHeartbeat();

        int depthW = _activeProvider.Width;
        int depthH = _activeProvider.Height;

        // Delegate to DepthProcessor (Compute Shader)
        _depthProcessor.Process(depthData, depthW, depthH, Settings, EnableSimulation);
        
        // Lazy Init Debug Texture
        if (_rawDepthTexture == null || _rawDepthTexture.width != depthW)
        {
            _rawDepthTexture = new Texture2D(depthW, depthH, TextureFormat.R8, false);
            _rawColorBuffer = new byte[depthW * depthH];
        }

        // Delegate to MeshGenerator (Compute Shader)
        // Pass the ComputeBuffer directly!
        _meshGenerator.UpdateMesh(_depthProcessor.GetOutputBuffer(), depthW, depthH, Settings, transform);
        
        // Update Debug Texture (Optional: Disable this for max perf)
        UpdateDebugTexture(_depthProcessor.GetResultBuffer());
    }

    // Deprecated: UpdateMeshGeometry removed.

    // --- Visualization & Utils ---

    void UpdateDebugTexture(ushort[] depthData)
    {
        for(int i=0; i<depthData.Length; i++)
        {
            _rawColorBuffer[i] = (byte)(depthData[i] / 10); 
        }
        _rawDepthTexture.LoadRawTextureData(_rawColorBuffer);
        _rawDepthTexture.Apply();
    }

    /// <summary>
    /// Returns the raw depth texture used for debugging and calibration overlays.
    /// </summary>
    /// <returns>The generated R8 depth texture.</returns>
    public Texture GetRawDepthTexture() => _rawDepthTexture;

    // --- Legacy / Shared Systems (Walls, Colors, etc) ---

    public void CalibrateFloor()
    {
        if (_activeProvider == null || !_activeProvider.IsRunning) return;
        ushort[] data = _activeProvider.GetDepthData();
        if (data == null) return;
        
        long sum = 0;
        int count = 0;
        for(int i=0; i<data.Length; i++)
        {
            if (data[i] > 200 && data[i] < 3500) { sum += data[i]; count++; }
        }

        if (count > 0)
        {
            float avg = (float)sum / count;
            MaxDepthMM = avg; 
            MinDepthMM = avg - 450f;
            SaveSettings();
            Debug.Log($"Auto-Calibrated: Floor={MaxDepthMM}, Peak={MinDepthMM}");
        }
    }

    public void UpdateMeshDimensions(float size) {
        // Handled by Settings and MeshGenerator Update loop now, 
        // but if external scripts call this, we update the setting.
        Settings.MeshSize = size;
    }

    public void SaveSettings() {
        // TODO: Implement JSON persistence for SO or rely on Editor serialization
    }
    public void LoadSettings() {
        // TODO: Load from JSON if needed
    }


    // INTERFACE IMPL
    public bool IsSimulationEnabled => EnableSimulation;
    // GetRawDepthTexture is already defined
    public void SetMode(bool simMode) => SwitchProvider(simMode);
    public string GetActiveProviderName() => _activeProvider != null ? _activeProvider.GetDeviceName() : "None";
    public void UpdateMaterialProperties() => UpdateShader();

    // --- Color/Shader Logic ---
    void UpdateShader() {
        if (_meshGenerator == null) return;

        // 1. Terrain Properties (Zero-Copy)
        MaterialPropertyBlock terrainProps = GetMaterialProperties();
        terrainProps.SetFloat("_Procedural", 1.0f); // Signal for logic that still uses float
        _meshGenerator.RegisterMaterialProperties(terrainProps, false);
        
        // 2. Wall Properties (Standard Mesh)
        // Walls need the same color/height props to render correctly.
        if (ShowWalls)
        {
            MaterialPropertyBlock wallProps = GetMaterialProperties();
            wallProps.SetFloat("_Procedural", 0.0f); // Disable procedural
            _meshGenerator.RegisterMaterialProperties(wallProps, true);
        }
    }


    MaterialPropertyBlock GetMaterialProperties() {
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        var r = GetComponent<MeshRenderer>();
        if(r) r.GetPropertyBlock(props);
        
        props.SetTexture("_ColorRamp", _colorRampTexture);
        props.SetFloat("_HeightMin", Settings.MinHeight);
        props.SetFloat("_HeightMax", HeightScale);
        props.SetFloat("_ColorHeightMax", Settings.ColorHeightMax);
        props.SetFloat("_TintStrength", TintStrength);
        props.SetFloat("_SparkleIntensity", SparkleIntensity);
        props.SetFloat("_SandScale", SandScale);
        props.SetFloat("_WaterLevel", WaterLevel);
        props.SetFloat("_WaterOpacity", WaterOpacity);
        props.SetColor("_WaterColor", WaterColor);
        props.SetFloat("_ColorShift", ColorShift);
        props.SetFloat("_ContourThickness", ContourThickness);
        props.SetFloat("_ContourInterval", ContourInterval);
        props.SetFloat("_DiscreteBands", UseDiscreteBands ? 1.0f : 0.0f);

        if (CausticTexture == null) CausticTexture = Resources.Load<Texture2D>("WaterCaustics");
        if (CausticTexture != null) props.SetTexture("_CausticTex", CausticTexture);
        
        props.SetFloat("_CausticIntensity", CausticIntensity);
        props.SetFloat("_CausticScale", CausticScale);
        props.SetFloat("_CausticSpeed", CausticSpeed);
        
        return props;
    }

    // Keeping Color Gradient Logic as it is View-Independent Controller Logic (for now)
    
    Color HexToColor(string hex) {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }
    public void ApplyGradientPreset(GradientPreset preset) {
        CurrentGradientPreset = preset; ElevationGradient = new Gradient();
        GradientColorKey[] colorKeys;
        
        switch(preset) {
            case GradientPreset.UCDavis:
                colorKeys = new GradientColorKey[5];
                colorKeys[0] = new GradientColorKey(Color.blue, 0.0f); 
                colorKeys[1] = new GradientColorKey(Color.cyan, 0.4f);
                colorKeys[2] = new GradientColorKey(Color.green, 0.475f); 
                colorKeys[3] = new GradientColorKey(Color.yellow, 0.65f);
                colorKeys[4] = new GradientColorKey(Color.red, 1.0f);
                break;
            case GradientPreset.Desert:
                colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(HexToColor("912C0C"), 0.0f); 
                colorKeys[1] = new GradientColorKey(HexToColor("F37031"), 0.25f);
                colorKeys[2] = new GradientColorKey(HexToColor("EFDE63"), 0.75f); 
                colorKeys[3] = new GradientColorKey(HexToColor("FEF4E7"), 1.0f);
                break;
            case GradientPreset.Natural:
                colorKeys = new GradientColorKey[5];
                colorKeys[0] = new GradientColorKey(new Color(0, 0.1f, 0.5f), 0.0f); 
                colorKeys[1] = new GradientColorKey(new Color(0.1f, 0.6f, 0.1f), 0.2f); 
                colorKeys[2] = new GradientColorKey(new Color(1f, 0.8f, 0f), 0.45f);    
                colorKeys[3] = new GradientColorKey(new Color(0.8f, 0f, 0f), 0.65f);    
                colorKeys[4] = new GradientColorKey(Color.white, 0.85f);                
                break;
            case GradientPreset.Heat:
                colorKeys = new GradientColorKey[5];
                colorKeys[0] = new GradientColorKey(Color.black, 0.0f); 
                colorKeys[1] = new GradientColorKey(Color.blue, 0.25f);
                colorKeys[2] = new GradientColorKey(Color.red, 0.5f); 
                colorKeys[3] = new GradientColorKey(Color.yellow, 0.75f);
                colorKeys[4] = new GradientColorKey(Color.white, 1.0f);
                break;
            case GradientPreset.Topography:
                colorKeys = new GradientColorKey[8];
                colorKeys[0] = new GradientColorKey(HexToColor("000080"), 0.0f);   // Deep Blue
                colorKeys[1] = new GradientColorKey(HexToColor("0080FF"), 0.15f);  // Light Blue
                colorKeys[2] = new GradientColorKey(HexToColor("008000"), 0.30f);  // Dark Green
                colorKeys[3] = new GradientColorKey(HexToColor("ADFF2F"), 0.45f);  // Green-Yellow
                colorKeys[4] = new GradientColorKey(HexToColor("FFFF00"), 0.60f);  // Yellow
                colorKeys[5] = new GradientColorKey(HexToColor("D2B48C"), 0.75f);  // Tan
                colorKeys[6] = new GradientColorKey(HexToColor("8B4513"), 0.90f);  // Brown
                colorKeys[7] = new GradientColorKey(HexToColor("FFFFFF"), 1.00f);  // White
                break;
            case GradientPreset.Grayscale:
                colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.black, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.white, 1.0f);
                break;
            default:
                colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.black, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.white, 1.0f);
                break;
        }
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f); alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        ElevationGradient.SetKeys(colorKeys, alphaKeys); RefreshColorRamp();
    }
    void RefreshColorRamp() {
        if (_colorRampTexture == null) _colorRampTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        _colorRampTexture.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < 256; i++) {
             float t = (float)i / 255f; _colorRampTexture.SetPixel(i, 0, ElevationGradient.Evaluate(t));
        }
        _colorRampTexture.Apply();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null) { renderer.material.SetTexture("_ColorRamp", _colorRampTexture); }
    }
    void InitColorRamp() {
        if (ElevationGradient == null || ElevationGradient.colorKeys.Length <= 2) { ApplyGradientPreset(CurrentGradientPreset); } 
        else { RefreshColorRamp(); }
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null) {
            renderer.material.SetFloat("_HeightMin", 0); renderer.material.SetFloat("_HeightMax", HeightScale);
            renderer.material.SetColor("_SpecularColor", new Color(0.2f, 0.2f, 0.2f, 1f));
        }
    }
    void AutoAssignTextures() { /* Simplified */ }
}
}
