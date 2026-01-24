# AR Sandbox Coding Standard & Technical Guide

This document establishes the technical standards for the AR Sandbox project to ensure high performance, maintainability, and stability across hardware and software updates.

## 1. Core Principles
- **Performance First**: Since the sandbox requires high-frequency mesh updates, always prioritize Burst-compiled Jobs over Main Thread loops.
- **Data-Visual Decoupling**: Separate raw depth data processing from mesh displacement and shader rendering.
- **Hardware Safety**: Always validate sensor data (depth and width/height) before processing to prevent `NullReference` and `IndexOutOfRange` errors.

---

## 2. Naming Conventions (Unity C#)

### 2.1 Private Fields
Use **PascalCase** with an underscore prefix.
- ✅ `private float _heightMin;`
- ✅ `private Vector3[] _vertices;`

### 2.2 Public Properties & Variables
Use **PascalCase**.
- ✅ `public float HeightMax { get; set; }`
- ✅ `public bool IsRunning;`

### 2.3 Constants & Enums
Use **PascalCase**. Avoid `SCREAMING_CAPS` unless it's a literal mathematical constant (e.g., `PI`).
- ✅ `public enum ProviderType { Kinect, Simulator }`

---

## 3. Unity Inspector Standards
Make the editor user-friendly for non-technical users.
- **Headers**: Group related fields using `[Header("Section Name")]`.
- **Tooltips**: Explain complex variables with `[Tooltip("Description here")]`.
- **Ranges**: Use `[Range(min, max)]` for sliders to prevent invalid data input.

```csharp
[Header("Height Calibration")]
[Tooltip("Maximum distance from sensor to floor in MM")]
[Range(1000f, 3000f)]
public float MaxDepthMM = 1500f;
```

---

## 4. Performance Standards (Jobs & Burst)

### 4.1 Struct Memory Layout
Any struct passed to a Job or the GPU must have a sequential layout.
```csharp
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct MyData { ... }
```

### 4.2 Job Handling
- Always mark input data as `[ReadOnly]`.
- Use `NativeArray` for data that needs to live between frames.
- Call `.Complete()` on jobs only when necessary to maximize multi-threading.

---

## 5. Shader Standards
- **Standardized Properties**: Use consistent property names (e.g., `_HeightMin`, `_HeightMax`) so scripts can sync with multiple shaders interchangeably.
- **Saturate**: Aggressively use `saturate()` in HLSL for data normalization (0-1) to avoid brightness/color blowouts.

---

## 6. Git & File Management
- **Git LFS**: All `.dll`, `.png`, and `.fbx` files **must** be tracked via Git LFS.
- **Placeholder Check**: If a DLL fails to load, check its file size. If it's ~130 bytes, run `git lfs pull`.

---

## 7. Verification Checklist for New Features
- [ ] Does it run without dropping below 60 FPS?
- [ ] Does it handle "Invalid/Zero" depth data gracefully?
- [ ] Is it calibrated to real-world MM measurements?
