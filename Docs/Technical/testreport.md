# Technical Report: Architectural Mitigation of Topological Webbing via Geometry Shader Culling

## 1. Executive Summary
The Motionsix AR Sandbox utilizes a "GPU-Keep-Alive" pipeline where depth data from a Kinect sensor is transformed into a terrain mesh via `TerrainSimulation.compute`. Currently, the system exhibits "webbing" artifacts—vertical geometric walls connecting valid terrain points to zero-height points at the sensor's invalid data boundaries. This report proposes a solution using a Geometry Shader to explicitly cull triangles containing invalid vertices. This approach maintains the fixed grid topology while dynamically discarding noise at the primitive assembly stage, ensuring cleaner visuals and correct lighting compared to fragment-based clipping.

---

## 2. Root Cause Analysis

### 2.1 The Decoupling of Topology and Data
The fundamental cause of the webbing artifacts is the decoupling of vertex data (height) from index data (topology). The system utilizes a fixed grid topology, meaning the index buffer defines a static list of triangles (e.g., connecting index `i` to `i+1` and `i+width`) that never changes, regardless of the input data.

### 2.2 The "Zero-Height" Artifact
When the depth camera returns an invalid signal (value 0), the current Compute Shader logic sets the vertex position to a default ground plane height (`y=0`).

*   **Scenario:** Vertex A has valid data (Height = 100mm). Neighbor Vertex B has invalid data (Height = 0mm).
*   **Result:** Because the index buffer statically connects A and B, the GPU renders a valid triangle spanning this vertical drop. To the rasterizer, this is valid geometry; to the user, it appears as an unsightly "web" or "curtain" connecting the sand to the floor.

---

## 3. Proposed Solution: Geometry Shader Culling
To resolve this without expensive CPU-side mesh regeneration, we will implement an "Invalid Vertex Masking" strategy using the GPU pipeline.

### 3.1 Algorithm Description
The logic moves the culling decision from the Fragment Shader (per-pixel) to the Geometry Shader (per-primitive), effectively preventing invalid triangles from ever reaching the rasterizer.

*   **Step 1: Flagging (Compute Shader)**
    *   In `TerrainSimulation.compute`, during the depth processing kernel, we evaluate the raw depth value.
    *   If the depth is 0 (invalid), we write a flag value (e.g., `1.0`) to a secondary channel of the vertex struct, such as `uv2.y`.
    *   If valid, we write `0.0`.

*   **Step 2: Analysis (Geometry Shader)**
    *   The Rendering Pipeline passes primitives (triangles) to a new Geometry Shader stage.
    *   The shader accepts an input array of 3 vertices: `triangle v2g IN`.
    *   It inspects the `uv2.y` flag of all three vertices.

*   **Step 3: Culling (Primitive Discard)**
    *   Logic: `bool isInvalid = (IN[0].uv2.y > 0 || IN[1].uv2.y > 0 || IN[2].uv2.y > 0);`
    *   If `isInvalid` is true, the shader returns immediately without appending vertices to the `TriangleStream`. This effectively deletes the triangle.
    *   If `isInvalid` is false, the shader loops through the 3 vertices and appends them to the stream, rendering the triangle normally.

## 4. Benefits over Pixel Shader Clipping
*   **Performance:** Geometry shader culling removes the primitive before rasterization. Pixel shader clipping (`clip()` or `discard`) forces the GPU to process the geometry and run the fragment shader for every pixel on the wall before killing it, which wastes fill rate.
*   **Visual Fidelity:** Discarding pixels can leave jagged edges and does not fix lighting errors. Geometry shaders allow for the recalculation of face normals for the remaining valid triangles, preventing lighting artifacts where the terrain abruptly ends.

---

## 5. Implementation Plan

### 5.1 Compute Shader (`TerrainSimulation.compute`)
We must modify the `MeshVertex` struct and the `GenerateMesh` kernel.
*   **Action:** Ensure the struct alignment allows for the flag. If using `float3` position, padding may be required to align to 16 bytes.
*   **Code Change:** Update usage of `uv2`.

### 5.2 Rendering Pipeline (`Topography.shader`)
We need to introduce a Geometry pass to the existing shader.
*   **Action:** Add `#pragma geometry geom` to the shader pass.
*   **Code Structure:** Implement the Geometry program block.

### 5.3 C# Integration (`TerrainMeshGenerator.cs`)
*   **Action:** Update the `GraphicsBuffer` or `ComputeBuffer` stride definition to match the new HLSL struct size.
*   **Verification:** Ensure `System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshVertex))` matches the HLSL stride exactly (e.g., growing from 32 bytes to 40 bytes if adding UV2).

---

## 6. Risk & Mitigation

*   **Risk: Geometry Shader Performance Overhead**
    *   **Context:** Geometry shaders can introduce performance penalties on certain hardware due to the interruption of the fixed-function pipeline.
    *   **Mitigation:** Use `[maxvertexcount(3)]` strictly to minimize buffer allocation. If performance drops below 60 FPS target, fallback to a "Degenerate Triangle" approach (setting vertex position to NaN in the Vertex Shader) which relies on hardware clipping.

*   **Risk: Platform Compatibility (Metal/iOS)**
    *   **Context:** Geometry shaders are not supported on Apple Metal APIs (macOS/iOS).
    *   **Mitigation:** Since this is a "Kiosk Edition" likely running on Windows (given the Kinect requirement), this is acceptable. If porting to Mac, use the Vertex Shader NaN technique described regarding degenerate triangles.

*   **Risk: Debugging Visibility**
    *   **Context:** Invisible triangles are hard to debug.
    *   **Mitigation:** Implement a Debug Mode boolean in the shader. When enabled, instead of discarding invalid triangles, render them in bright red. Use RenderDoc or Unity's Frame Debugger to inspect the `uv2` outputs of the Compute Shader before enabling the cull logic.