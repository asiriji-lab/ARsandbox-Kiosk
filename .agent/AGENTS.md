# Ralph Memory: AR Sandbox Project

## Architectural Patterns
- **Provider Pattern**: Decoupling depth data sourcing from the mesh rendering.
- **Interface Driven**: Using `IDepthProvider` to allow seamless switching between "Reality" (Kinect) and "Simulation" (Perlin).
- **Shader-First Visualization**: Using URP shaders locally for contours, water, and colormapping instead of slow CPU-bound mesh updates or Matplotlib style rendering.

## Codebase Patterns
- **Start-Phase Initialisation**: UI and Controller components should self-init or ruggedly handle missing references in `Start()`.
- **Programmatic UI**: UI is built from code using factory patterns for consistency and ease of expansion.
- **Interleaved MeshData API**: Using high-performance MeshData API for zero-allocation terrain deformation.

## Core Rules for Agents
1. Always maintain the separation between `IDepthProvider` and `ARSandboxController`.
2. Do not merge simulation logic back into the main controller.
3. Every refactor chunk must end with a progress update to `progress.txt`.
