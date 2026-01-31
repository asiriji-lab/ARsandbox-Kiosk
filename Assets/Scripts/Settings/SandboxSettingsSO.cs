using UnityEngine;
using UnityEngine.Serialization;

public enum GradientPreset { UCDavis, Desert, Natural, Heat, Grayscale, Topography }

[CreateAssetMenu(fileName = "NewSandboxSettings", menuName = "AR Sandbox/Settings")]
public class SandboxSettingsSO : ScriptableObject
{
    [Header("Depth Range")]
    public float MinDepth = 500f;
    public float MaxDepth = 1500f;

    [Header("Projection")]
    public float HeightScale = 5f;
    public float MeshSize = 10f;
    public int MeshResolution = 200;
    public float MinHeight = 0f;          // Added for absolute mapping
    public float ColorHeightMax = 5f;    // Added for absolute mapping
    public bool FlatMode = false;
    public Vector2[] CalibrationPoints = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
    [HideInInspector] public Matrix4x4 ProjectionMatrix = Matrix4x4.identity;
    public Vector2[] BoundaryPoints = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };

    [Header("Visuals - Basic")]
    [Range(0f, 1f)] public float TintStrength = 0.5f;
    [Range(1f, 100f)] public float SandScale = 10.0f;
    public GradientPreset ColorScheme = GradientPreset.UCDavis;
    public bool UseDiscreteBands = false;
    public bool ShowWalls = false;

    [Header("Visuals - Water")]
    [Range(0f, 10f)] public float WaterLevel = 0.5f;
    [Range(0f, 1f)] public float WaterOpacity = 0.6f;
    public Color WaterColor = new Color(0, 0.2f, 1f, 1f);
    [Range(-0.5f, 0.5f)] public float ColorShift = 0.0f;

    [Header("Visuals - Contours")]
    public float ContourInterval = 0.5f;
    public float ContourThickness = 1.0f;

    [Header("Visuals - Effects")]
    public float SparkleIntensity = 1.0f;
    public float CausticIntensity = 0.5f;
    public float CausticScale = 2.0f;
    public float CausticSpeed = 0.5f;

    [Header("Reliability")]
    public float WatchdogTimeout = 5.0f;
    public bool WatchdogAutoRetry = true;

    [Header("Simulation")]
    public float NoiseScale = 0.5f;
    public float MoveSpeed = 0.5f;

    [Header("Filtering")]
    public float SmoothingFactor = 0.5f;
    public float MinCutoff = 1.0f;
    public float Beta = 0.007f;
    public float HandThreshold = 80f;
    public int SpatialBlur = 2;

    // Reset to defaults context menu
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        MinDepth = 500f;
        MaxDepth = 1500f;
        HeightScale = 5f;
        MeshSize = 10f;
        MeshResolution = 200;
        MinHeight = 0f;
        ColorHeightMax = 5f;
        FlatMode = false;
        CalibrationPoints = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
        TintStrength = 0.5f;
        SandScale = 10.0f;
        ColorScheme = GradientPreset.UCDavis;
        UseDiscreteBands = false;
        ShowWalls = false;
        WaterLevel = 0.5f;
        WaterOpacity = 0.6f;
        WaterColor = new Color(0, 0.2f, 1f, 1f);
        ContourInterval = 0.5f;
        ContourThickness = 1.0f;
        SparkleIntensity = 1.0f;
        CausticIntensity = 0.5f;
        CausticScale = 2.0f;
        CausticSpeed = 0.5f;
        WatchdogTimeout = 5.0f;
        WatchdogAutoRetry = true;
        NoiseScale = 0.5f;
        MoveSpeed = 0.5f;
        SmoothingFactor = 0.5f;
        MinCutoff = 1.0f;
        Beta = 0.007f;
        HandThreshold = 80f;
        SpatialBlur = 2;
    }
}
