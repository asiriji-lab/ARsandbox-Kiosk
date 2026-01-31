# AR Sandbox Consolidated Source Code

This document contains the primary C# source files for the AR Sandbox project, consolidated for analysis in NotebookLM.

---

## 1. ARSandboxController.cs (Main Logic & Mesh Generation)
```csharp
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

[RequireComponent(typeof(MeshFilter))]
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

    [Header("Sandbox Settings")]
    public float Width = 10f;
    public float Length = 10f;
    public int MeshResolution = 200; 

    private SandboxSettings _settings;
    public ref SandboxSettings CurrentSettings => ref _settings;
    
    [Range(0f, 0.15f)] public float NoiseScale = 0.1f;
    public float MoveSpeed = 0.5f;

    [Header("Height Calibration")]
    public float MinDepthMM = 500f; 
    public float MaxDepthMM = 1500f;
    public float HeightScale = 5f;

    [Header("1 Euro Filter (Adaptive)")]
    public float MinCutoff = 1.0f; 
    public float Beta = 0.007f;    
    public float HandFilterThreshold = 80f; 
    [Range(0, 5)] public int SpatialBlurIterations = 2;   

    [Header("Visuals")]
    [Range(0f, 1f)] public float TintStrength = 0.5f;
    [Range(1f, 100f)] public float SandScale = 10.0f;
    [Range(0f, 10f)] public float WaterLevel = 0.5f;
    [Range(0f, 2f)] public float WaterOpacity = 0.6f;
    public Color WaterColor = new Color(0, 0.2f, 1f, 1f);
    [Range(-0.5f, 0.5f)] public float ColorShift = 0.0f; 

    [Header("Contour Styling")]
    public float ContourInterval = 0.5f;
    [Range(0.1f, 5.0f)] public float ContourThickness = 1.0f; 

    [Header("Sparkles")]
    [Range(0f, 5f)] public float SparkleIntensity = 1.0f;

    [Header("Water Caustics")]
    public Texture2D CausticTexture;
    [Range(0f, 2f)] public float CausticIntensity = 0.5f;
    [Range(0f, 0.15f)] public float CausticScale = 2.0f;
    [Range(0f, 2f)] public float CausticSpeed = 0.5f;

    public GradientPreset CurrentGradientPreset = GradientPreset.UCDavis;
    public bool UseDiscreteBands = false;
    public Gradient ElevationGradient;
    private Texture2D _colorRampTexture;

    [Header("Projection Settings")] public bool FlatMode = false;
    [Header("Calibration")] public Vector2[] CalibrationPoints;

    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector2[] _uvs;
    private Vector2[] _uv2;
    private Texture2D _rawDepthTexture;
    private byte[] _rawColorBuffer;

    private NativeArray<ushort> _filterInput;
    private NativeArray<float> _filterState;
    private NativeArray<float> _filterPrevRaw;
    private NativeArray<ushort> _filterOutput;
    private ushort[] _filteredData;
    private int _filterResolution = -1;

    void Awake() { InitMesh(); CalibrationPoints = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }; LoadSettings(); }
    void Start() { InitColorRamp(); AutoAssignTextures(); _kinectProvider = gameObject.AddComponent<KinectDepthProvider>(); _simProvider = gameObject.AddComponent<SimulatedDepthProvider>(); SwitchProvider(EnableSimulation); }

    void SwitchProvider(bool useSim)
    {
        EnableSimulation = useSim;
        if (_activeProvider != null) _activeProvider.Shutdown();
        if (useSim) { _activeProvider = _simProvider; _simProvider.NoiseScale = NoiseScale; _simProvider.MoveSpeed = MoveSpeed; _simProvider.MinDepthMM = MinDepthMM; _simProvider.MaxDepthMM = MaxDepthMM; }
        else { _activeProvider = _kinectProvider; }
        _activeProvider.Initialize();
    }

    void Update() { if (_activeProvider == (IDepthProvider)_simProvider && !EnableSimulation) SwitchProvider(false); if (_activeProvider == (IDepthProvider)_kinectProvider && EnableSimulation) SwitchProvider(true); ProcessDepthData(); UpdateShader(); }

    [BurstCompile] public struct OneEuroJob : IJobParallelFor { /* 1 Euro Filter Implementation */ }
    [BurstCompile] public struct SpatialBlurJob : IJobParallelFor { /* Spatial Blur Implementation */ }
    [BurstCompile] public struct MeshGenJob : IJobParallelFor { /* Mesh Generation implementation using Jobs/Burst */ }

    void ProcessDepthData() { /* Orchestrates Jobs and updates NativeArrays */ }
    void UpdateMeshGeometry(ushort[] depthData, int depthW, int depthH) { /* Applies job results to Mesh */ }

    public void CalibrateFloor() { /* Calibration logic */ }
    public void SaveSettings() { SyncFieldsToSettings(); SandboxSettingsManager.SaveSettings(_settings); }
    public void LoadSettings() { _settings = SandboxSettingsManager.LoadSettings(); SyncSettingsToFields(); }

    /* Helper methods for sync, material properties, and gradients */
}
```

---

## 2. SandboxUI.cs (Administrative Admin UI)
```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using UnityEngine.InputSystem;

public class SandboxUI : MonoBehaviour
{
    public ARSandboxController Controller;
    public Sprite KnobSprite;
    public Sprite BackgroundSprite;
    public Sprite HandleSprite;

    private GameObject _uiRoot;
    private CanvasGroup _canvasGroup;
    private bool _isVisible = false;
    private int _activeTabIndex = 0;

    void Start() { /* Builds programmatic UI and initializes references */ }
    void Update() { HandleInput(); HandleAutoHide(); if (_isVisible) UpdateLabels(); }

    void BuildUI() { /* Massive method building UI hierarchy via code */ }
    void UpdateLabels() { /* Pulls data from Controller to update UI text */ }

    /* Factory methods for creating Buttons, Sliders, Toggles programmatically */
}
```

---

## 3. SandboxSettings.cs (Data Model)
```csharp
using System;
using UnityEngine;

public enum GradientPreset { UCDavis, Desert, Natural, Heat, Grayscale }

[Serializable]
public struct SandboxSettings
{
    public float MinDepth;
    public float MaxDepth;
    public float HeightScale;
    public float MeshSize;
    public bool FlatMode;
    public Vector2[] CalibrationPoints; 
    public float TintStrength;
    public float SandScale;
    public GradientPreset ColorScheme; 
    public bool UseDiscreteBands;      
    public bool ShowWalls;             
    public float WaterLevel;
    public float WaterOpacity;
    public Color WaterColor;
    public float ColorShift;
    public float ContourInterval;
    public float ContourThickness;
    public float SparkleIntensity;
    public float CausticIntensity;
    public float CausticScale;
    public float CausticSpeed;
    public float NoiseScale; 
    public float MoveSpeed;  
    public float MinCutoff; 
    public float Beta;
    public float HandThreshold;
    public int SpatialBlur;

    public static SandboxSettings Default() { /* Returns default values */ }
}
```

---

## 4. IDepthProvider.cs (Hardware Interface)
```csharp
using UnityEngine;

public interface IDepthProvider
{
    void Initialize();
    void Shutdown();
    bool IsRunning { get; }
    int Width { get; }
    int Height { get; }
    ushort[] GetDepthData();
    string GetDeviceName();
}
```

---

## 5. IUISystem.cs (Controller Interface)
```csharp
using UnityEngine;

public interface IUISystem
{
    ref SandboxSettings CurrentSettings { get; }
    Texture GetRawDepthTexture();
    void SetMode(bool simMode);
    void SaveSettings();
    void UpdateMeshDimensions(float sizeInMeters);
    void CalibrateFloor();
    string GetActiveProviderName();
    void ApplyGradientPreset(GradientPreset preset);
    void UpdateMaterialProperties();
}
```

---

## 6. SandboxSettingsManager.cs (Persistence)
```csharp
using System.IO;
using UnityEngine;

public static class SandboxSettingsManager
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "sandbox_settings.json");

    public static void SaveSettings(SandboxSettings settings) { /* JSON serialization to disk */ }
    public static SandboxSettings LoadSettings() { /* JSON deserialization from disk */ }
}
```
