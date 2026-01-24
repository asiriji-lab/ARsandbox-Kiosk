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

    // Internal
    private Device _kinectDevice;
    private ushort[] _depthData;
    private ushort[] _latestDepthFrame;
    private int _depthWidth;
    private int _depthHeight;
    private bool _isRunning = false;
    private bool _newFrameAvailable = false;
    private object _lockObj = new object();

    public bool IsRunning => _isRunning;
    public int Width => _depthWidth;
    public int Height => _depthHeight;

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
                    ColorResolution = ColorResolution.Off,
                    DepthMode = DepthMode,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    CameraFPS = CameraFPS
                });

                // Get initial calibration to know resolution
                var cal = _kinectDevice.GetCalibration();
                _depthWidth = cal.DepthCameraCalibration.ResolutionWidth;
                _depthHeight = cal.DepthCameraCalibration.ResolutionHeight;
                
                _depthData = new ushort[_depthWidth * _depthHeight];
                _latestDepthFrame = new ushort[_depthWidth * _depthHeight];

                _isRunning = true;
                // Start polling thread
                Task.Run(() => KinectLoop());
                
                Debug.Log($"Kinect Initialized. Res: {_depthWidth}x{_depthHeight}");
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
            using (Capture capture = _kinectDevice.GetCapture())
            {
                if (capture != null && capture.Depth != null)
                {
                    lock (_lockObj)
                    {
                        // Copy data to buffer
                        capture.Depth.CopyTo(_latestDepthFrame, 0, 0, _latestDepthFrame.Length);
                        _newFrameAvailable = true;
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        Shutdown();
    }
}
