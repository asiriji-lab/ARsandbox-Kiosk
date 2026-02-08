using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor; // Requires Azure Kinect Sensor SDK
using System.Threading.Tasks;
using System;

public class KinectDepthProvider : MonoBehaviour, IDepthProvider
{
    [Header("Kinect Settings")]
    public int DeviceIndex = 0;
    public DepthMode DepthMode = DepthMode.NFOV_2x2Binned; 
    public FPS CameraFPS = FPS.FPS30;

    // Internal - Depth
    private Device _kinectDevice;
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

    public void Initialize()
    {
        InitKinect();
    }

    public void Shutdown()
    {
        _isRunning = false;
        if (_kinectDevice != null)
        {
            _kinectDevice.StopCameras();
            _kinectDevice.Dispose();
            _kinectDevice = null;
        }
    }

    public string GetDeviceName()
    {
        return $"Azure Kinect (Index {DeviceIndex})";
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
        
        // Return cached data if no new frame
        return _depthData;
    }

    public Texture2D GetColorTexture()
    {
        if (!_isRunning || _colorTexture == null) return null;

        lock (_lockObj)
        {
            if (_newColorFrameAvailable)
            {
                // BGRA32 format: each pixel is 4 bytes
                _colorTexture.LoadRawTextureData(_latestColorFrame);
                _colorTexture.Apply();
                _newColorFrameAvailable = false;
            }
        }

        return _colorTexture;
    }

    void InitKinect()
    {
        try
        {
            if (Device.GetInstalledCount() > 0)
            {
                _kinectDevice = Device.Open(DeviceIndex);
                _kinectDevice.StartCameras(new DeviceConfiguration
                {
                    ColorFormat = ImageFormat.ColorBGRA32,
                    ColorResolution = ColorResolution.R720p, // 1280x720
                    DepthMode = DepthMode,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    CameraFPS = CameraFPS
                });

                // Get initial calibration to know resolution
                var cal = _kinectDevice.GetCalibration();
                _depthWidth = cal.DepthCameraCalibration.ResolutionWidth;
                _depthHeight = cal.DepthCameraCalibration.ResolutionHeight;
                _colorWidth = cal.ColorCameraCalibration.ResolutionWidth;
                _colorHeight = cal.ColorCameraCalibration.ResolutionHeight;
                
                _depthData = new ushort[_depthWidth * _depthHeight];
                _latestDepthFrame = new ushort[_depthWidth * _depthHeight];
                
                // Color: BGRA32 = 4 bytes per pixel
                _colorData = new byte[_colorWidth * _colorHeight * 4];
                _latestColorFrame = new byte[_colorWidth * _colorHeight * 4];
                _colorTexture = new Texture2D(_colorWidth, _colorHeight, TextureFormat.BGRA32, false);

                _isRunning = true;
                // Start polling thread
                Task.Run(() => KinectLoop());
                
                Debug.Log($"[Kinect] Initialized. Depth: {_depthWidth}x{_depthHeight}, Color: {_colorWidth}x{_colorHeight}");
                _stopwatch.Start();
                _lastLogTime = _stopwatch.ElapsedMilliseconds;
            }
            else
            {
                Debug.LogError("No Azure Kinect device found!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start Kinect: {e.Message}");
        }
    }

    void KinectLoop()
    {
        while (_isRunning && _kinectDevice != null)
        {
            try 
            {
                using (Capture capture = _kinectDevice.GetCapture())
                {
                    if (capture != null)
                    {
                        lock (_lockObj)
                        {
                            // Depth
                            if (capture.Depth != null)
                            {
                                capture.Depth.CopyTo(_latestDepthFrame, 0, 0, _latestDepthFrame.Length);
                                _newFrameAvailable = true;
                                _depthFrameCount++;
                            }
                            
                            // Color
                            if (capture.Color != null)
                            {
                                capture.Color.CopyTo(_latestColorFrame, 0, 0, _latestColorFrame.Length);
                                _newColorFrameAvailable = true;
                                _colorFrameCount++;
                            }
                        }

                        // Periodically log heartbeat (every 5 seconds)
                        long now = _stopwatch.ElapsedMilliseconds;
                        if (now - _lastLogTime > 5000)
                        {
                            Debug.Log($"[Kinect] Heartbeat - Recv: {_depthFrameCount} depth, {_colorFrameCount} color frames.");
                            _lastLogTime = now;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kinect] CRITICIAL LOOP ERROR: {e.Message}");
                // Don't break, try to recover next frame? Or break if fatal?
                // If GetCapture failed, maybe device is lost.
                // For now, allow retry but log furiously.
            }
        }
    }

    void OnDestroy()
    {
        Shutdown();
    }
}
