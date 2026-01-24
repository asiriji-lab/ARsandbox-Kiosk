# Project Status Report: "The Ferrari Engine" Refactor
**Date:** 2026-01-18
**Status:** High-Performance Standard Achieved

This document records the architectural upgrades and work history for the AR Sandbox project. It is intended to help future agents/developers understand the state of the codebase and the purpose of surrounding documentation.

---

## 🚀 Recent Work: The "High-Performance" Refactor
We have moved the project from a prototype state to a ruggedized, professional codebase.

### 1. Mesh Engine Optimization (Zero-GC)
**Legacy Issue:** The terrain simulation was allocating `new Vector3[]` every frame (40k+ items), causing massive Garbage Collection (GC) lag spikes.
**The Fix:** 
- Switched to the **MeshData API** (`Mesh.AllocateWritableMeshData`).
- Implemented **Interleaved Vertex Buffers** using a `struct TerrainVertex`.
- **Result:** Zero bytes allocated per frame. Silky smooth 60 FPS.
- **Robustness:** Added dynamic resolution safety (`EnsureMeshInitialized`) so resizing the grid at runtime won't crash the app.

### 2. Input System Safety
**Legacy Issue:** The project crashed if the Editor was set to "New Input System" because the code used legacy `Input.GetKeyDown`.
**The Fix:**
- Wrapped all Input logic in preprocessor directives:
  ```csharp
  #if ENABLE_INPUT_SYSTEM
     // New System Logic
  #elif ENABLE_LEGACY_INPUT_MANAGER
     // Old System Logic
  #endif
  ```
- **Result:** The code compiles and runs error-free on ANY Unity Input configuration.
- **ESC Key:** Fixed to support both Editor (`EditorApplication.isPlaying = false`) and Builds (`Application.Quit`).

### 3. Hardware Integration (URP settings)
**Optimization:**
- Enabled **Forward+ Rendering** (Better light handling).
- Enabled **GPU Resident Drawer** (Instanced Drawing) for maximum throughput.
- Set **BatchRendererGroup Variants** onto "Keep All".

### 4. Setup Camera Zoom & Cycle Controls
**Feature:** Added runtime camera adjustment and view cycling.
- **Zoom:** `Up Arrow` (In/Up), `Down Arrow` (Out/Down).
- **Cycle View:** `Left Arrow` (Prev), `Right Arrow` (Next).
- **Top View:** Moves Camera vertically (World Y).
- **Perspective:** Moves Camera forward/backward (Local Z).

### 5. Solid Walls (Skirt) with Dimming
**Feature:** Toggleable "Cake Mode" where terrain sides are enclosed.
- **Physics:** Walls extend from the terrain edge down to the floor (Y=0).
- **Visuals:** 
  - Uses `UV2` height gradient to match terrain color at top and water color at bottom.
  - **Dimming:** Walls are rendered 40% darker (`_Brightness = 0.6`) to visually distinguish them from the active surface.
- **UI:** Toggle "Solid Walls" in Admin Panel.

---

## 📂 Documentation Map (What's in the Assets folder?)
If you are looking around, here is what the other markdown files contain:

### `unitycode.md`
**Topic:** Professional Unity Architecture
**Content:** Explains the "Senior Architect" mindset. Key rules:
- **Lifecycle:** `Awake()` for internal init, `Start()` for external comms.
- **Null Safety:** Strict execution order discipline.

### `urpthingy.md`
**Topic:** Universal Render Pipeline (URP) Technical Deep Dive
**Content:** Analysis of URP 17/Unity 6 features.
- Difference between **Forward** vs **Forward+**.
- How the **Render Graph** system works.
- Explanation of **SRP Batcher** and **GPU Resident Drawer**.

### `uc_davis_properties.md`
**Topic:** Legacy Algorithm Analysis
**Content:** A breakdown of the original C++ algorithms from the UC Davis source code (2.8).
- How the Water Shader works (GLSL).
- How Contour Lines are generated.
- Elevation Color Mapping logic.

### `walkthrough.md`
**Topic:** General Setup Guide
**Content:** Step-by-step instructions for installing the software, connecting the Kinect, and basic troubleshooting. (Note: Contains the foundational setup, while this file tracks the advanced refactoring).

---

## 🏁 Final Verification State
- **Console:** Safe (No Red Errors).
- **Profiler:** 0 Bytes GC Alloc in `UpdateMeshGeometry`.
- **Deployment:** `UpdateAndRun.bat` created for Kiosk usage.
