using UnityEngine;
using System;

/// <summary>
/// Monitors a depth provider for hangs and triggers a reset if no data is received within a timeout period.
/// </summary>
public class SensorWatchdog
{
    private float _lastHeartbeatTime;
    private Action _onTimeout;
    
    public bool IsHangDetected { get; private set; }
    public float TimeSinceLastFrame => Time.time - _lastHeartbeatTime;

    public SensorWatchdog(Action onTimeout)
    {
        _onTimeout = onTimeout;
        ResetHeartbeat();
    }

    /// <summary>
    /// Call this whenever a valid data frame is received.
    /// </summary>
    public void ResetHeartbeat()
    {
        _lastHeartbeatTime = Time.time;
        IsHangDetected = false;
    }

    public void Update(float timeoutThreshold, bool autoRetry)
    {
        if (!autoRetry) return;

        if (TimeSinceLastFrame > timeoutThreshold && !IsHangDetected)
        {
            IsHangDetected = true;
            Debug.LogWarning($"[SensorWatchdog] SENSOR HANG DETECTED! No data for {TimeSinceLastFrame:F1}s. Triggering reset...");
            _onTimeout?.Invoke();
        }
    }
}
