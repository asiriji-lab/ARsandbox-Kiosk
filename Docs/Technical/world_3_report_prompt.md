# NotebookLM Prompt: World 3 Technical Strategy (Real-World Alignment)

**Goal**: Act as a **Technical Lead/Architect**. Based on the uploaded project sources (Architecture Overview, Codebase, UC Davis Docs), generate a comprehensive **Technical Design Specification (TDS)** for the next phase of development: **World 3**.

---

## 1. Executive Summary
Provide a high-level overview of "World 3".
- **Objective**: Transition from a GPGPU prototype to a professional-grade, calibrated Museum Exhibit.
- **Key Pillars**: Calibration Accuracy, Hardware Reliability, and Exhibit Persistence.

## 2. Technical Implementation Strategy

### A. The Calibration Engine (Chunk 9)
Replicate the **UC Davis SARndbox Calibration Flow** using our modern Azure Kinect setup.
- **Problem**: The projector (2D) and Kinect (3D) are in different coordinate spaces.
- **Solution Design**:
    1.  **Mathematical Model**: Explain the **Homography Matrix** approach for 4-point calibration.
    2.  **Interactive Workflow**: Step-by-step design for the `AlignmentSystem` class.
        - *State 1*: Projector displays 4 corners.
        - *State 2*: User centers "Calibration Disk" on projected points.
        - *State 3*: System captures `(ProjectorUV, KinectWorldPos)`.
        - *State 4*: System computes the transformation matrix.
    3.  **Base Plane Logic**: How to implement `CaptureBaseLevel` to define "Sea Level" ($$Z_{zero}$$) from a noisy depth map.

### B. Reliability & Safety (Chunk 8)
Design a robust "Watchdog" system for a 24/7 unattended exhibit.
- **Architecture**:
    - **Heartbeat**: How `SensorWatchdog` should monitor the `DepthProvider`.
    - **Safe State**: Behavior when data is lost (e.g., freeze frame, black screen, "Reconnect" overlay?).
    - **Automatic Recovery**: Logic for restarting the Kinect driver without crashing Unity.

### C. Persistence Layer (Chunk 10)
Design a secure saving system for museum operators.
- **Data to Persist**: `CalibrationMatrix`, `BaseLevelOffset`, `ROI_Mask`, `LedConfig`.
- **Security**: How to prevent visitors from accidentally resetting calibration (e.g., Hidden "Admin Mode" gesture or key combo).

## 3. Recommended Code Architecture
Provide C# pseudocode or Class Structure for the core systems:
- `AlignmentSystem.cs` (The Solver)
- `CalibrationViewModel.cs` (The UI Logic)
- `SafetyManager.cs` (The Watchdog Coordinator)

---

**Output Format**: Professional Markdown Report.
