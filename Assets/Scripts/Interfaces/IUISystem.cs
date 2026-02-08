using UnityEngine;

public interface IUISystem
{
    // Data Access
    SandboxSettingsSO CurrentSettings { get; }
    
    // Core Actions
    bool IsSimulationEnabled { get; }
    Texture GetRawDepthTexture();
    Texture2D GetColorTexture();
    void SetMode(bool simMode);
    void SaveSettings();
    void UpdateMeshDimensions(float sizeInMeters);
    void UpdateMeshResolution(int resolution);
    void CalibrateFloor();
    
    // Visualization Actions
    string GetActiveProviderName();
    void ApplyGradientPreset(GradientPreset preset);
    void UpdateMaterialProperties();
}
