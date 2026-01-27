# AR Sandbox Technical Implementation Manual

This document provides in-depth technical specifications and architectural mandates for the AR Sandbox project. For coding style and naming rules, refer to [Coding_Standard.md](../Standards/Coding_Standard.md).

---

## 1. Infrastructure & Hardware (Azure Kinect)

A standardized approach to native binary management is required for Unity 6 integration.

### 1.1 DLL Management
The Azure Kinect SDK (1.4.1) files must be placed in `Assets/Plugins/`.
- **Core**: `k4a.dll`, `k4abt.dll`, `onnxruntime.dll`, `depthengine_2_0.dll`
- **ML**: `dnn_model_2_0.onnx`, `DirectML.dll`
- **Managed**: `Microsoft.Azure.Kinect.Sensor.dll`, `Microsoft.Azure.Kinect.BodyTracking.dll`

### 1.2 DirectML Conflict
Manually copy `DirectML.dll` from the Kinect SDK installation folder into the Unity Editor installation directory (e.g., `.../Editor/6000.0.60f1/Editor`) to prevent Play Mode crashes.

---

## 2. Unity 6 Project Configuration

- **Graphics**: Use **URP Forward+** rendering path.
- **Performance**: Enable **GPU Resident Drawer** (Instanced Drawing) in the URP Asset.
- **Strip Shaders**: Set BatchRendererGroup Variants to `Keep All`.
- **Static Batching**: **Disable** Static Batching; it conflicts with the GPU Resident Drawer for dynamic terrain.

---

## 3. High-Performance C# Architecture

### 3.1 Memory Management (MeshData API)
Mandatory for per-frame mesh updates. Use `Mesh.AllocateWritableMeshData(1)` to eliminate managed heap allocations. The `MeshData` pointer access bypasses the deep-copy overhead of the legacy `Mesh.vertices` API.

### 3.2 Job System & Burst
- **Temporal Filtering**: Use the `OneEuroJob` to eliminate sensor jitter while maintaining responsiveness.
- **Spatial Smoothing**: Use the `SpatialBlurJob` to interpolate missing sensor data and smooth depth "staircasing."
- **Data Safety**: Always mark input data as `[ReadOnly]` and ensure structs have `LayoutKind.Sequential`.

---

## 4. Shader & Rendering Internals

- **SRP Batcher Compatibility**: All per-material interactive variables MUST be in a single `CBUFFER_START(UnityPerMaterial)` block.
- **1D LUT Elevation**: We map depth directly to a 1D Look-Up Table (Color Ramp) on the GPU. Refer to `ARSandboxController.RefreshColorRamp` for the CPU-side generation logic.
- **Triplanar Sand**: To prevent texture stretching on vertical dunes, we use 3-axis world-space projection with `pow(weights, 8)` blending.

---

## 5. UI Architecture & Optimization

- **Visibility Management**: Always prefer `CanvasGroup.alpha` over `GameObject.SetActive()`. Firing `SetActive` causes a full "Dirty" state on the Canvas, forcing mesh regeneration.
- **Hot-Path String Formatting**: Within `Update()` or frequently changed labels, use a pre-allocated `StringBuilder` or the `TMP_Text.SetText("{0}", val)` zero-allocation API.
- **Input Raycasting**: Disable "Raycast Target" on non-interactive UI elements (Labels, Icons) to minimize the overhead of the Graphic Raycaster.
