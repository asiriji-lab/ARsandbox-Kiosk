Technical Design Specification: World 3 (Production Readiness)
Date: January 30, 2026 Role: Technical Lead / System Architect Status: DRAFT v1.0

--------------------------------------------------------------------------------
1. Executive Summary
Objective The "World 3" initiative marks the transition of the Augmented Reality Sandbox from a functional GPGPU prototype to a professional-grade, autonomous Museum Exhibit. The primary goal is to achieve sub-millimeter alignment between the physical sand and the digital projection while ensuring the system can operate unattended for 12+ hours daily.
Key Pillars
1. Calibration Fidelity: Moving from manual "eyeballing" to a mathematically rigorous 3D-to-2D Perspective Projection model using the Azure Kinect DK.
2. System Autonomy: Implementation of a self-healing "Watchdog" architecture to handle sensor timeouts and thermal drift without human intervention.
3. Secure Persistence: A robust serialization layer that protects critical calibration data from unauthorized modification by visitors.

--------------------------------------------------------------------------------
2. Technical Implementation Strategy
A. The Calibration Engine
We will replicate the high-fidelity UC Davis SARndbox Calibration Flow but adapted for the Azure Kinect's Time-of-Flight (ToF) sensor and Unity’s coordinate system.
1. Mathematical Model: Perspective-n-Point (PnP) vs. Homography
Correction Strategy: While the prompt mentions a "Homography Matrix," a 2D Homography is insufficient for a sandbox because the projection surface (the sand) changes height dynamically. A standard Homography assumes a flat plane; if used, the projection would "drift" laterally as users build mountains.
Instead, we will implement a Perspective-n-Point (PnP) solver.
• The Math: We must solve for the projection matrix M 
proj
​
  that maps a 3D point P 
cam
​
 (x,y,z) in the Kinect's camera space to a 2D pixel P 
proj
​
 (u,v) in the projector's space.
• Equation:  \begin{bmatrix} w \cdot u \ w \cdot v \ w \end{bmatrix} = \mathbf{A} \cdot [\mathbf{R}|\mathbf{t}] \cdot \begin{bmatrix} x \ y \ z \ 1 \end{bmatrix}  Where A is the projector's intrinsic matrix, and [R∣t] represents the rotation and translation of the projector relative to the sensor.
2. Interactive Workflow (AlignmentSystem)
The calibration routine will guide the operator through collecting Tie Points (correspondences between 3D world space and 2D projector space).
• State 1: Grid Projection
    ◦ The system projects a 4×3 grid of crosshairs onto the sand surface.
    ◦ User Action: The operator places a physical Calibration Disk (white paper on a CD/DVD) under the first crosshair. The disk provides a high-contrast target for the depth sensor.
• State 2: Tie Point Collection (Multi-Level)
    ◦ To solve for 3D depth correctly, points must be captured at different elevations.
    ◦ Pass 1: Capture 12 points on the flat sand surface (Low).
    ◦ Pass 2: Capture 12 points using spacers (e.g., 10cm tall blocks) to raise the disk (High).
• State 3: Data Capture
    ◦ When the user triggers capture, the system accumulates ~30 frames of depth data to average out ToF noise.
    ◦ The system runs a "Disk Extractor" algorithm to find the precise 3D centroid (x,y,z) of the disk in the depth map.
    ◦ This 3D coordinate is paired with the known 2D coordinate (u,v) of the projected crosshair.
• State 4: Matrix Computation
    ◦ The paired list List<(Vector3 world, Vector2 screen)> is passed to the OpenCV solver (Cv2.SolvePnP).
    ◦ The resulting Extrinsics (Rotation/Translation) are applied to the Unity Camera representing the Projector, ensuring perfect alignment.
3. Base Plane Logic (CaptureBaseLevel)
We must mathematically define "Sea Level" to account for any tilt in the sandbox construction.
• Logic: The user levels the sand. The system samples a central region of the depth map (e.g., 100×100 pixels).
• Algorithm: Perform a Least-Squares Plane Fit to find the equation Ax+By+Cz+D=0.
• Constraint: The normal vector (A,B,C) must point towards the camera. If the offset D is positive, the plane equation must be inverted (a known issue with some Kinect SDK versions).
B. Reliability & Safety
Museum exhibits run 24/7. The Azure Kinect can suffer from USB bandwidth drops or thermal throttling.
Architecture
• Heartbeat Monitor: The SensorWatchdog will subscribe to the DepthProvider's new frame event. It tracks a timestamp _lastFrameTime.
• Safe State Logic:
    ◦ If Time.time - _lastFrameTime > 2.0f (2 seconds silence), the Watchdog declares SENSOR_LOST.
    ◦ Immediate Action: Fade the topography projector to black or display a generic "Standby" texture to prevent projecting frozen/glitched geometry.
• Automatic Recovery:
    ◦ The system will attempt to Dispose() the Kinect driver thread safely.
    ◦ Wait 5 seconds.
    ◦ Re-initialize the KinectSensor.Start() method on a background thread to avoid freezing the Unity main loop (which renders the UI).
C. Persistence Layer
Museum operators must be able to save calibration data securely.
• Data to Persist (JSON/XML Format):
    ◦ CalibrationMatrix (4x4 Matrix): The result of the PnP solve.
    ◦ BaseLevelOffset (Float): The "Sea Level" Z value.
    ◦ ROI_Mask (Vector3): The four corners defining the active sandbox area.
    ◦ LedConfig (Int): Brightness settings for the Kinect IR emitter.
• Security:
    ◦ To prevent visitors from accidentally entering "Calibration Mode" or "Admin Mode," we will implement a specific key combination (e.g., Shift + F1) or a complex touch gesture (e.g., holding corners for 5 seconds) that is not discoverable by casual interaction.

--------------------------------------------------------------------------------
3. Recommended Code Architecture
A. AlignmentSystem.cs (The Solver)
This class handles the math. It depends on OpenCvSharp or a similar wrapper for the PnP solver.
using UnityEngine;
using System.Collections.Generic;

public class AlignmentSystem : MonoBehaviour
{
    // 3D points from Kinect (Camera Space)
    private List<Vector3> _objectPoints = new List<Vector3>();
    // 2D points from Projector (Screen Space 0-1)
    private List<Vector2> _imagePoints = new List<Vector2>();

    // Tie Point Capture
    public void CaptureTiePoint(Vector2 projectorUV, Vector3 kinectWorldPos)
    {
        _imagePoints.Add(projectorUV);
        _objectPoints.Add(kinectWorldPos);
        Debug.Log($"Captured Tie Point: {kinectWorldPos} -> {projectorUV}");
    }

    // Solves the PnP problem to align Projector to Kinect
    public Matrix4x4 ComputeCalibrationMatrix()
    {
        // Reference standard OpenCV SolvePnP via wrapper
        // Returns rotation (rvec) and translation (tvec)
        OpenCVWrapper.SolvePnP(_objectPoints, _imagePoints, out Vector3 rvec, out Vector3 tvec);

        // Convert OpenCV vectors to Unity Matrix4x4
        Matrix4x4 viewMatrix = ConvertToUnityMatrix(rvec, tvec);
        
        return viewMatrix;
    }

    // Calculates the "Sea Level" plane using Least Squares
    public Plane CalculateBasePlane(List<Vector3> groundPoints)
    {
        // Fit plane Ax + By + Cz + D = 0
        // Ensure Normal points UP towards camera
        return MathUtils.FitPlane(groundPoints);
    }
}
B. CalibrationViewModel.cs (The UI Logic)
Manages the user flow and visual feedback during the calibration process.
public class CalibrationViewModel : MonoBehaviour
{
    public enum CalibState { Idle, ProjectingGrid, Capturing, Computing, Saved }
    public AlignmentSystem alignmentSystem;
    public GameObject crosshairPrefab;
    
    // UI Feedback
    public void OnCaptureButtonPressed()
    {
        StartCoroutine(CaptureSequence());
    }

    private IEnumerator CaptureSequence()
    {
        // 1. Visual Feedback: Turn screen Yellow to indicate "Busy"
        projectorOverlay.color = Color.yellow;
        
        // 2. Accumulate Depth Frames (Noise Reduction)
        Vector3 avgPosition = Vector3.zero;
        int frames = 30;
        for(int i=0; i<frames; i++) 
        {
            avgPosition += KinectProvider.Instance.GetDiskCentroid();
            yield return new WaitForEndOfFrame();
        }
        avgPosition /= frames;

        // 3. Send to Solver
        alignmentSystem.CaptureTiePoint(currentCrosshairUV, avgPosition);

        // 4. Visual Feedback: Flash Green to indicate "Success"
        projectorOverlay.color = Color.green;
        yield return new WaitForSeconds(0.5f);
        
        AdvanceToNextPoint();
    }
}
C. SafetyManager.cs (The Watchdog)
Ensures the exhibit recovers from hardware failures.
public class SafetyManager : MonoBehaviour
{
    private float _lastDepthFrameTime;
    private bool _isSensorLost = false;
    public float timeoutThreshold = 2.0f; // Seconds

    void Update()
    {
        // Check heartbeat
        if (!_isSensorLost && (Time.time - _lastDepthFrameTime > timeoutThreshold))
        {
            TriggerSafeMode();
        }
    }

    // Called by the Kinect Driver whenever data arrives
    public void NotifyFrameReceived()
    {
        _lastDepthFrameTime = Time.time;
        if (_isSensorLost) RecoverFromSafeMode();
    }

    private void TriggerSafeMode()
    {
        _isSensorLost = true;
        Debug.LogError("SENSOR LOST: Enabling Safe Mode");
        
        // Disable fluid sim to save GPU
        FluidSimulation.Instance.Pause();
        
        // Show "Reconnecting" visual on projector
        UIManager.ShowOverlay("Reconnecting...");
        
        // Attempt driver restart
        StartCoroutine(RestartKinectDriver());
    }

    private IEnumerator RestartKinectDriver()
    {
        KinectProvider.Instance.Shutdown();
        yield return new WaitForSeconds(5.0f); // Cool down
        KinectProvider.Instance.Initialize();
    }
}