# AR Sandbox Codebase Structure Report

## 1. Project Overview
**Project**: Motionsix AR Sandbox (Kiosk Edition)
**Engine**: Unity 6 (URP Forward+)
**Core Tech**: Azure Kinect DK, Compute Shaders, Unity Job System (Legacy/Hybrid)
**Goal**: High-performance (60 FPS+) real-time topographic mapping of sand using depth sensors.

> **⚠️ Important Architectural Note**:
> The codebase has evolved towards a **GPU-Keep-Alive** architecture. While some older documentation mentions "C# Jobs" and "Burst" for validation, the core depth processing pipeline (Filtering & Mesh Generation) is now fully implemented in **Compute Shaders** (`TerrainSimulation.compute`) to minimize CPU-GPU memory bandwidth overhead.

---

## 2. Directory Structure & Key Locations

### Root Directory: `d:\motionsix\Sandbox(new)`

| Directory | Description |
| :--- | :--- |
| **`.agent/`** | Agent memory, workflows, and task tracking (AI Assistant context). |
| **`Assets/`** | Valid Unity Assets (Source Code, Scenes, Resources). |
| **`Docs/`** | **MASTER Documentation**. Implementation guides, architecture specs. |
| **`Packages/`** | Unity Package Manager dependencies. |
| **`ProjectSettings/`** | Unity project configuration (Input, Tags, Graphics). |

### Assets Directory: `Assets/`

| Path | Purpose |
| :--- | :--- |
| **`Scripts/Core/`** | **The Implementation Heart**. `ARSandboxController`, `DepthProcessor`. |
| **`Resources/Compute/`** | Compute Shaders (`TerrainSimulation.compute`) for GPU processing. |
| **`Scenes/`** | Unity Scenes. Likely `MainScene` or similar is the entry point. |
| **`Shaders/`** | Visual shaders, specifically `Topography.shader` (Rendering). |
| **`Plugins/`** | Azure Kinect SDK DLLs (`k4a.dll`, `depthengine_2_0.dll`). |
| **`Settings/`** | `SandboxSettings` ScriptableObjects (Persisted config). |

---

## 3. Architecture & Data Flow

### The "GPU-Keep-Alive" Pipeline
The system is designed to keep heavy data on the GPU as much as possible.

1.  **Input (Hardware)**
    *   **Source**: `KinectDepthProvider` (Hardware) or `SimulatedDepthProvider` (Dev Mode).
    *   **Data**: Raw 16-bit depth map (millimeters).

2.  **Processing (Compute Layer)**
    *   **Controller**: `ARSandboxController.cs` orchestrates the loop.
    *   **Depth Filtering**: `DepthProcessor.cs` dispatches `TerrainSimulation.compute` (Kernel: `FilterDepth`).
        *   Implements **1-Euro Filter** (Jitter reduction) on GPU.
    *   **Validation**: Small buffer readback to CPU for logic checks (Legacy/Watchdog).

3.  **Visualization (Render Layer)**
    *   **Mesh Generation**: `TerrainMeshGenerator.cs` dispatches `TerrainSimulation.compute` (Kernel: `GenerateMesh`).
        *   Converts filtered depth maps directly to a 3D Mesh in a `RWStructuredBuffer`.
    *   **Rendering**: `Topography.shader`.
        *   Uses **Triplanar Mapping** for sand texture.
        *   Uses a **1D Color Ramp** (Texture) for elevation coloring.

### Class Responsibility Map

| Class | Location | Role |
| :--- | :--- | :--- |
| `ARSandboxController` | `Scripts/Core` | **God Class**. Manages providers, settings, and the update loop. |
| `DepthProcessor` | `Scripts/Core` | Wraps the Compute Shader "Filter" kernel. |
| `TerrainMeshGenerator` | `Scripts/Core` | Wraps the Compute Shader "GenerateMesh" kernel. |
| `SandboxUI` | `Scripts/Core/UI` | Programmatic UI Manager (Tabs, Sliders). |
| `SandboxSettingsSO` | `Scripts/Core/Settings` | Data container for all tunable parameters (Water Level, Filter Beta). |

---

## 4. Documentation Guide

> **Note on Documentation Integrity**: The documentation in `Docs/` is the master source. **Ignor**e `Assets/Docs/` if it exists, as it is deprecated (See `.agent/File_Structure_Audit.md`).

| Document | Path | Status | Relevance |
| :--- | :--- | :--- | :--- |
| **Architecture Overview** | `Docs/Technical/Architecture_Overview.md` | **High** | Explains the conceptual model (Kinect -> Job/Compute -> Mesh). *Note: Mentions Jobs, but code uses Compute.* |
| **Structure Map** | `Docs/Technical/Structure_Map.md` | **High** | Quick lookup for "Where is X?". |
| **Implementation Manual** | `Docs/Technical/Implementation_Manual.md` | **Critical** | Low-level details on DLLs, Memory Safety, and Unity 6 configuration. |
| **Shader Architecture** | `Docs/Technical/Shader_Architecture.md` | **Medium** | Explanation of Triplanar and properties. |
| **Setup Guide** | `Docs/Guides/Setup_Guide.md` | **User** | Connectivity and installation instructions. |

---

## 5. Workflows & Agent Context

The `.agent` directory contains specific workflows for maintenance and development.

*   **Chunks Overview** (`.agent/Chunks_Overview.md`): Roadmap of development tasks (Chunks).
*   **Workflows** (`.agent/workflows/`): Automated or guided checklists.
    *   `verify_logs.md`: Steps to check Unity logs.
*   **Worlds** (`.agent/worlds/`): Likely milestone reports (e.g., `World_04_ROI_Definition.md`).

---

## 6. Detailed File Tree (Context Map)

```text
d:/motionsix/Sandbox(new)/
├── Assets/
│   ├── Resources/Compute/
│   │   └── TerrainSimulation.compute  [CRITICAL: GPU Kernels for Filter/Mesh]
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── ARSandboxController.cs [CRITICAL: Main Application Loop]
│   │   │   ├── DepthProcessor.cs      [CRITICAL: GPU Dispatcher for Filtering]
│   │   │   ├── TerrainMeshGenerator.cs[CRITICAL: GPU Dispatcher for Mesh]
│   │   │   ├── Settings/
│   │   │   │   └── SandboxSettingsSO.cs [Config Data Structure]
│   │   │   └── UI/
│   │   │       └── SandboxUI.cs       [User Interface Logic]
│   │   └── Providers/
│   │       ├── KinectDepthProvider.cs [Hardware Integration]
│   │       └── SimulatedDepthProvider.cs [Dev Mode Simulation]
│   └── Shaders/
│       └── Topography.shader          [CRITICAL: Visualization Logic]
├── Docs/
│   ├── Technical/
│   │   ├── UpToDate/                  [TRUST THESE]
│   │   │   ├── Architecture_Overview.md
│   │   │   └── Codebase_Structure_Report.md
│   │   └── Legacy/                    [DO NOT TRUST]
│   │       └── Implementation_Manual.md
│   └── Guides/
│       └── Setup_Guide.md             [Physical Setup]
└── .agent/
    ├── Chunks_Overview.md             [Project Roadmap]
    └── rules.md                       [AI Persona Rules]
```

---

## 7. "Source of Truth" Guide (Resolving Conflicts)

NotebookLM may find contradictions between older docs and newer code. Use this table to decide which is correct.

| Topic | Documentation Says... | Code Actually Does... | **Verdict** |
| :--- | :--- | :--- | :--- |
| **Depth Processing** | "Uses C# Job System and Burst Compiler" | Uses **Compute Shaders** (`TerrainSimulation.compute`) | **Trust Code (Compute)** |
| **Mesh Generation** | "Uses NativeArray and Jobs" | Uses **ComputeBuffer** directly on GPU | **Trust Code (Compute)** |
| **Texture Names** | Mentions `_MainTex` | Uses `_ColorRamp` and `_MainTex` (Triplanar) | **Trust Code (Shaders)** |
| **Settings** | "Saved in PlayerPrefs" (Old) | Saved in `sandbox_settings.json` | **Trust Code (JSON)** |

---

## 8. Glossary & Concepts

*   **1-Euro Filter**: A specific algorithm used to smooth noisy signal data (jitter) from the Kinect sensor while minimizing lag.
*   **Triplanar Mapping**: A texturing technique that projects textures from 3 directions (X, Y, Z) to prevent stretching on steep terrain slopes.
*   **Compute Shader**: A program running on the GPU that handles general math (filtering, geometry) much faster than the CPU.
*   **URP (Universal Render Pipeline)**: The modern Unity rendering engine this project uses.
*   **Idempotency**: The design principle where re-running a setup script or function multiple times doesn't break things (used in the UI/Settings).

---

## 9. Suggested Prompts for NotebookLM

To get the most out of this codebase, try asking NotebookLM these questions:

1.  *"Explain the data flow from the hardware sensor to the final mesh on screen, highlighting where the GPU takes over."*
2.  *"Identify any potential performance bottlenecks in the `ARSandboxController` specifically related to main-thread blocking."*
3.  *"Compare the 'Simulated' provider features against the real 'Kinect' provider based on the script analysis."*
4.  *"Generate a user guide for the Kiosk Operator based on the Settings variables found in `SandboxSettingsSO`."*

