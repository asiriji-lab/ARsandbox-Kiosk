using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor; // Orbbec K4A Wrapper (binary compatible)
using System.Threading.Tasks;
using System;

/// <summary>
/// Depth provider for Orbbec Femto via K4A Wrapper.
/// Uses NFOV_Unbinned (1024x1024) by default.
/// </summary>
public class FemtoDepthProvider : MonoBehaviour, IDepthProvider
{
    [Header("Femto Settings")]
    public int DeviceIndex = 0;
    public DepthMode DepthMode = DepthMode.NFOV_Unbinned; // 1024x1024
    public FPS CameraFPS = FPS.FPS30;

    // Internal - Depth
    private Device _femtoDevice;
    private ushort[] _depthData;
    private ushort[] _latestDepthFrame;
    private int _depthWidth;
    private int _depthHeight;
    private bool _isRunning = false;
    private bool _newFrameAvailable = false;
    private object _lockObj = new object();

    // Internal - Color
    private byte[] _colorData;
    private byte[] _latestColorFrame;
    private int _colorWidth;
    private int _colorHeight;
    private Texture2D _colorTexture;
    private bool _newColorFrameAvailable = false;

    // Diagnostics
    private int _depthFrameCount = 0;
    private int _colorFrameCount = 0;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
    private long _lastLogTime = 0;

    public bool IsRunning => _isRunning;
    public int Width => _depthWidth;
    public int Height => _depthHeight;
    public int ColorWidth => _colorWidth;
    public int ColorHeight => _colorHeight;

    public bool IsSensorAvailable()
    {
        try
        {
            int count = Device.GetInstalledCount();
            Debug.Log($"[FemtoDepthProvider] IsSensorAvailable: Device.GetInstalledCount() = {count}");
            return count > 0;
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError($"[FemtoDepthProvider] K4A wrapper DLL not found: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FemtoDepthProvider] IsSensorAvailable error: {e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    public void Initialize()
    {
        InitFemto();
    }

    public void Shutdown()
    {
        _isRunning = false;
        if (_femtoDevice != null)
        {
            try
            {
                _femtoDevice.StopCameras();
                _femtoDevice.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Femto] Shutdown error: {e.Message}");
            }
            _femtoDevice = null;
        }
    }

    public string GetDeviceName()
    {
        return $"Orbbec Femto (Index {DeviceIndex})";
    }

    public ushort[] GetDepthData()
    {
        if (!_isRunning) return null;

        lock (_lockObj)
        {
            if (_newFrameAvailable)
            {
                Array.Copy(_latestDepthFrame, _depthData, _latestDepthFrame.Length);
                _newFrameAvailable = false;
                return _depthData;
            }
        }

        return _depthData;
    }

    public Texture2D GetColorTexture()
    {
        if (!_isRunning || _colorTexture == null) return null;

        lock (_lockObj)
        {
            if (_newColorFrameAvailable)
            {
                _colorTexture.LoadRawTextureData(_latestColorFrame);
                _colorTexture.Apply();
                _newColorFrameAvailable = false;
            }
        }

        return _colorTexture;
    }

    void InitFemto()
    {
        try
        {
            int deviceCount = Device.GetInstalledCount();
            Debug.Log($"[Femto] Device.GetInstalledCount() = {deviceCount}");
            
            if (deviceCount > 0)
            {
                Debug.Log($"[Femto] Opening device at index {DeviceIndex}...");
                _femtoDevice = Device.Open(DeviceIndex);
                
                var config = new DeviceConfiguration
                {
                    ColorFormat = ImageFormat.ColorBGRA32,
                    ColorResolution = ColorResolution.R720p,
                    DepthMode = DepthMode,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    CameraFPS = CameraFPS
                };
                
                Debug.Log($"[Femto] Starting cameras with DepthMode={DepthMode}, FPS={CameraFPS}...");
                _femtoDevice.StartCameras(config);

                var cal = _femtoDevice.GetCalibration();
                _depthWidth = cal.DepthCameraCalibration.ResolutionWidth;
                _depthHeight = cal.DepthCameraCalibration.ResolutionHeight;
                _colorWidth = cal.ColorCameraCalibration.ResolutionWidth;
                _colorHeight = cal.ColorCameraCalibration.ResolutionHeight;

                _depthData = new ushort[_depthWidth * _depthHeight];
                _latestDepthFrame = new ushort[_depthWidth * _depthHeight];

                _colorData = new byte[_colorWidth * _colorHeight * 4];
                _latestColorFrame = new byte[_colorWidth * _colorHeight * 4];
                _colorTexture = new Texture2D(_colorWidth, _colorHeight, TextureFormat.BGRA32, false);

                _isRunning = true;
                Task.Run(() => FemtoLoop());

                Debug.Log($"[Femto] Initialized OK. Depth: {_depthWidth}x{_depthHeight}, Color: {_colorWidth}x{_colorHeight}");
                _stopwatch.Start();
                _lastLogTime = _stopwatch.ElapsedMilliseconds;
            }
            else
            {
                Debug.LogError("[Femto] No Orbbec Femto device found! (Device.GetInstalledCount() == 0)");
            }
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError($"[Femto] CRITICAL: K4A wrapper DLL missing: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Femto] Init FAILED: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }

    void FemtoLoop()
    {
        while (_isRunning && _femtoDevice != null)
        {
            try
            {
                using (Capture capture = _femtoDevice.GetCapture())
                {
                    if (capture != null)
                    {
                        lock (_lockObj)
                        {
                            if (capture.Depth != null)
                            {
                                capture.Depth.CopyTo(_latestDepthFrame, 0, 0, _latestDepthFrame.Length);
                                _newFrameAvailable = true;
                                _depthFrameCount++;
                            }

                            if (capture.Color != null)
                            {
                                capture.Color.CopyTo(_latestColorFrame, 0, 0, _latestColorFrame.Length);
                                _newColorFrameAvailable = true;
                                _colorFrameCount++;
                            }
                        }

                        long now = _stopwatch.ElapsedMilliseconds;
                        if (now - _lastLogTime > 5000)
                        {
                            Debug.Log($"[Femto] Heartbeat - Recv: {_depthFrameCount} depth, {_colorFrameCount} color frames.");
                            _lastLogTime = now;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Femto] CRITICAL LOOP ERROR: {e.GetType().Name}: {e.Message}");
            }
        }
    }

    void OnDestroy()
    {
        Shutdown();
    }
}
