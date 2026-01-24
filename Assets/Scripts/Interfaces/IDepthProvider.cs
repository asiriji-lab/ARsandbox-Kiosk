using UnityEngine;

public interface IDepthProvider
{
    // Lifecycle
    void Initialize();
    void Shutdown();

    // Data Access
    bool IsRunning { get; }
    int Width { get; }
    int Height { get; }
    ushort[] GetDepthData();

    // Metadata
    string GetDeviceName();
}
