using UnityEngine;

public interface IUISystem
{
    // Data Access
    // Data Access
    SandboxSettingsSO CurrentSettings { get; }
    
    // Core Actions
    bool IsSimulationEnabled { get; }
    Texture GetRawDepthTexture();
    void SetMode(bool simMode);
    void SaveSettings();
    void UpdateMeshDimensions(float sizeInMeters);
    void CalibrateFloor();
    
    // Visualization Actions
    string GetActiveProviderName();
    void ApplyGradientPreset(GradientPreset preset);
    void UpdateMaterialProperties();
}
