using UnityEngine;
using ARSandbox;

public class SandboxViewModel : MonoBehaviour
{
    // Settings Reference (Single Source of Truth)
    public SandboxSettingsSO Settings;
    
    // Command Interface (Actions)
    public IUISystem Controller;

    // Reactive Properties (UI State)
    public ReactiveProperty<float> HeightScale = new ReactiveProperty<float>();
    public ReactiveProperty<float> MinDepth = new ReactiveProperty<float>();
    public ReactiveProperty<float> MaxDepth = new ReactiveProperty<float>();
    
    public ReactiveProperty<float> TintStrength = new ReactiveProperty<float>();
    public ReactiveProperty<float> SandScale = new ReactiveProperty<float>();
    public ReactiveProperty<float> WaterLevel = new ReactiveProperty<float>();
    public ReactiveProperty<float> WaterOpacity = new ReactiveProperty<float>();
    public ReactiveProperty<float> ColorShift = new ReactiveProperty<float>();
    
    public ReactiveProperty<float> ContourInterval = new ReactiveProperty<float>();
    public ReactiveProperty<float> ContourThickness = new ReactiveProperty<float>();
    
    public ReactiveProperty<float> MinCutoff = new ReactiveProperty<float>();
    public ReactiveProperty<float> Beta = new ReactiveProperty<float>();
    public ReactiveProperty<float> HandThreshold = new ReactiveProperty<float>();
    public ReactiveProperty<float> SpatialBlur = new ReactiveProperty<float>();

    public ReactiveProperty<float> SparkleIntensity = new ReactiveProperty<float>();
    public ReactiveProperty<float> CausticIntensity = new ReactiveProperty<float>();
    public ReactiveProperty<float> CausticScale = new ReactiveProperty<float>();
    public ReactiveProperty<float> CausticSpeed = new ReactiveProperty<float>();

    public ReactiveProperty<bool> UseDiscreteBands = new ReactiveProperty<bool>();

    private void Awake()
    {
        // Try to find dependencies if not assigned
        if (Controller == null) Controller = GetComponent<IUISystem>() ?? FindFirstObjectByType<ARSandboxController>();
        if (Settings == null && Controller != null) Settings = Controller.CurrentSettings;
        
        if (Settings != null)
        {
            InitializeFromSettings();
        }
    }

    // Initialize View Model from Data Model
    public void InitializeFromSettings()
    {
        HeightScale.Value = Settings.HeightScale;
        MinDepth.Value = Settings.MinDepth;
        MaxDepth.Value = Settings.MaxDepth;
        
        TintStrength.Value = Settings.TintStrength;
        SandScale.Value = Settings.SandScale;
        WaterLevel.Value = Settings.WaterLevel;
        WaterOpacity.Value = Settings.WaterOpacity;
        ColorShift.Value = Settings.ColorShift;
        
        ContourInterval.Value = Settings.ContourInterval;
        ContourThickness.Value = Settings.ContourThickness;
        
        MinCutoff.Value = Settings.MinCutoff;
        Beta.Value = Settings.Beta;
        HandThreshold.Value = Settings.HandThreshold;
        SpatialBlur.Value = Settings.SpatialBlur;
        
        SparkleIntensity.Value = Settings.SparkleIntensity;
        CausticIntensity.Value = Settings.CausticIntensity;
        CausticScale.Value = Settings.CausticScale;
        CausticSpeed.Value = Settings.CausticSpeed;
        
        UseDiscreteBands.Value = Settings.UseDiscreteBands;
    }

    // Public Actions (Called by UI)
    // These update the ViewModel -> Update Settings -> Trigger Controller
    
    public void SetHeightScale(float v) { HeightScale.Value = v; Settings.HeightScale = v; UpdateVisuals(); Save(); }
    public void SetMinDepth(float v) { MinDepth.Value = v; Settings.MinDepth = v; Save(); }
    public void SetMaxDepth(float v) { MaxDepth.Value = v; Settings.MaxDepth = v; Save(); } // Might need controller update if used for projection
    
    public void SetTintStrength(float v) { TintStrength.Value = v; Settings.TintStrength = v; UpdateVisuals(); Save(); }
    public void SetSandScale(float v) { SandScale.Value = v; Settings.SandScale = v; UpdateVisuals(); Save(); }
    
    public void SetWaterLevel(float v) { WaterLevel.Value = v; Settings.WaterLevel = v; UpdateVisuals(); Save(); }
    public void SetWaterOpacity(float v) { WaterOpacity.Value = v; Settings.WaterOpacity = v; UpdateVisuals(); Save(); }
    public void SetColorShift(float v) { ColorShift.Value = v; Settings.ColorShift = v; UpdateVisuals(); Save(); }
    
    public void SetContourInterval(float v) { ContourInterval.Value = v; Settings.ContourInterval = v; UpdateVisuals(); Save(); }
    public void SetContourThickness(float v) { ContourThickness.Value = v; Settings.ContourThickness = v; UpdateVisuals(); Save(); }
    
    public void SetMinCutoff(float v) { MinCutoff.Value = v; Settings.MinCutoff = v; Save(); }
    public void SetBeta(float v) { Beta.Value = v; Settings.Beta = v; Save(); }
    public void SetHandThreshold(float v) { HandThreshold.Value = v; Settings.HandThreshold = v; Save(); }
    public void SetSpatialBlur(float v) { SpatialBlur.Value = v; Settings.SpatialBlur = (int)v; Save(); }
    
    public void SetSparkleIntensity(float v) { SparkleIntensity.Value = v; Settings.SparkleIntensity = v; UpdateVisuals(); Save(); }
    public void SetCausticIntensity(float v) { CausticIntensity.Value = v; Settings.CausticIntensity = v; UpdateVisuals(); Save(); }
    public void SetCausticScale(float v) { CausticScale.Value = v; Settings.CausticScale = v; UpdateVisuals(); Save(); }
    public void SetCausticSpeed(float v) { CausticSpeed.Value = v; Settings.CausticSpeed = v; UpdateVisuals(); Save(); }

    public void SetDiscreteBands(bool v) { UseDiscreteBands.Value = v; Settings.UseDiscreteBands = v; UpdateVisuals(); Save(); }
    
    // World & Sim Settings
    public void SetMeshSize(float v) { Settings.MeshSize = v; Controller.UpdateMeshDimensions(v); Save(); }
    public void SetShowWalls(bool v) { Settings.ShowWalls = v; UpdateVisuals(); Save(); }
    public void SetFlatMode(bool v) { Settings.FlatMode = v; Save(); } // Controller reads Settings directly in Update/Shader
    public void SetEnableSimulation(bool v) { Controller.SetMode(v); }
    public void SetNoiseScale(float v) { Settings.NoiseScale = v; Save(); }
    public void SetMoveSpeed(float v) { Settings.MoveSpeed = v; Save(); }
    
    public void CalibrateFloor()
    {
        Controller.CalibrateFloor();
        // Update VM from new settings (as calibration changes min/max)
        MinDepth.Value = Settings.MinDepth; 
        MaxDepth.Value = Settings.MaxDepth;
    }

    public void ApplyPreset(string name)
    {
         if (name == "Volcano") {
            Settings.HeightScale = 8.0f; Settings.TintStrength = 0.8f; Settings.UseDiscreteBands = true;
            Controller.ApplyGradientPreset(GradientPreset.Desert);
        } else if (name == "Ocean") {
            Settings.HeightScale = 3.0f; Settings.TintStrength = 0.4f; Settings.UseDiscreteBands = false;
            Controller.ApplyGradientPreset(GradientPreset.UCDavis);
        } else {
            Settings.HeightScale = 5.0f; Settings.TintStrength = 0.5f; Settings.UseDiscreteBands = false;
            Controller.ApplyGradientPreset(GradientPreset.UCDavis);
        }
        InitializeFromSettings(); // Refresh all properties
        UpdateVisuals();
        Save();
    }
    
    public void CycleColorScheme()
    {
        // This Logic might belong in Controller or just simple rotation here
        // For now, simpler to call controller if it exposes cycle logic, or implement here using SO enums
         int next = (int)Settings.ColorScheme + 1;
         if (next >= System.Enum.GetValues(typeof(GradientPreset)).Length) next = 0;
         Settings.ColorScheme = (GradientPreset)next;
         Controller.ApplyGradientPreset(Settings.ColorScheme);
         Save();
    }

    private void UpdateVisuals() => Controller?.UpdateMaterialProperties();
    public void SaveSettings() => Controller?.SaveSettings();
    private void Save() => SaveSettings();
}
