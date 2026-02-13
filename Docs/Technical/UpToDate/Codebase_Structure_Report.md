# Codebase Structure Report

**Generated**: 2026-02-13
**Scope**: `Assets/Scripts/Core/`

## Key Differences (Job System vs Compute Shader)

| Feature | Legacy Implementation | Current Implementation | Verdict |
|---|---|---|---|
| **Depth Processing** | Uses C# Job System and Burst Compiler | Uses **Compute Shaders** (`TerrainSimulation.compute`) | **Trust Code (Compute)** |
| **Mesh Generation** | Uses NativeArray and Jobs | Uses **ComputeBuffer** directly on GPU | **Trust Code (Compute)** |

## Core Components
- **`ARSandboxController.cs`**: Orchestrator. Owns the Compute Shader pipeline.
- **`DepthProcessor.cs`**: Dispatches `FilterDepth` kernel.
- **`TerrainMeshGenerator.cs`**: Dispatches `GenerateMesh` kernel.
