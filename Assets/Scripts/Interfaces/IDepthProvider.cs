using UnityEngine;

public interface IDepthProvider
{
    // Lifecycle
    void Initialize();
    void Shutdown();

    // Depth Data Access
    bool IsRunning { get; }
    int Width { get; }
    int Height { get; }
    ushort[] GetDepthData();

    // Color Data Access (for Calibration)
    int ColorWidth { get; }
    int ColorHeight { get; }
    Texture2D GetColorTexture();

    // Metadata
    string GetDeviceName();
}
