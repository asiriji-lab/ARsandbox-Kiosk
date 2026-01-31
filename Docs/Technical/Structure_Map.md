# Project Structure & Architecture Map

## Core Logic
| Component | File Path | Responsibility |
|-----------|-----------|----------------|
| **Controller** | `Assets/ARSandboxController.cs` | The "Brain". Manages the K2 depth sensor, simulation loop, mesh generation (Burst/Jobs), and settings persistence. |
| **UI Manager** | `Assets/SandboxUI.cs` | The "Face". Programmatically builds the UI (No Prefabs). Handles input, tabs, and calls Controller methods. |
| **Interfaces** | `Assets/Scripts/Interfaces/IDepthProvider.cs` | Contract for depth sources (Sim vs Kinect). |

## Data Flow
1. **Input**: `KinectDepthProvider` or `SimulatedDepthProvider` produce `ushort[]` depth data.
2. **Processing**: `ARSandboxController` runs `OneEuroFilter` (smoothing) and `SpatialBlur` (noise reduction) via Jobs.
3. **Visualization**: 
    - **Mesh**: `MeshGenJob` converts processed depth to a 3D Mesh.
    - **Shader**: `Topography.shader` colors the mesh based on height (using `ElevationGradient`).

## Key Locations
- **Settings**: Saved to `Application.persistentDataPath/sandbox_settings.json`.
- **Docs**: `Assets/Docs/Standards/` contains Coding Standards.
- **Agent Memory**: `.agent/` contains the Roadmap (`Chunks_Overview.md`) and Active Task (`progress.txt`).

## "Where do I fix...?"
- **"The sand is jittery"**: Tweak `MinCutoff` / `Beta` in `ARSandboxController` or UI "Filtering" tab.
- **"The colors are wrong"**: Check `Topography.shader` or `UpdateMaterialProperties` in Controller.
- **"I can't see the UI"**: Check `BuildUI()` in `SandboxUI.cs`.
- **"Performance is bad"**: Look at `MeshGenJob` or `SpatialBlurJob` in `ARSandboxController`.
