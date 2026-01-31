# Refactoring Report Generation Prompt

**Instructions**:
1. Upload the `consolidated_source.md` and the `Technical Performance Analysis CPU Burst vs. GPGPU.md` file (if available) to your LLM (NotebookLM, ChatGPT, Claude).
2. Paste the following prompt to generate a comprehensive refactoring report.

---

### **Prompt**

"I need a formal **Refactoring Report and Implementation Plan** for the AR Sandbox Unity project.

**Context**:
The current codebase suffers from:
1.  **God Classes**: `ARSandboxController` mixes simulation logic, mesh generation, sensor input, and UI data.
2.  **Tight Coupling**: `SandboxUI` directly modifies fields in `ARSandboxController`, making it impossible to change the simulation without breaking the UI.
3.  **Performance Bottleneck**: High-resolution meshes (512x512) cause PCIe bus saturation due to CPU<->GPU data transfer of large arrays.

**Required Report Sections**:

#### **1. Executive Summary**
- Briefly state the current health of the codebase and the critical need for refactoring.

#### **2. Architectural Strategy (The Guidelines)**
- Define the new architecture. We are moving towards:
    - **MVVM for UI**: Use `SandboxSettings` (ScriptableObject) as the Model, `SandboxViewModel` as the connector, and `SandboxUI` (View) as purely visual.
    - **Service-Based Logic**: Extract `IDepthProvider` management and `SimulationLoop` into separate services or managers.
    - **Hybrid Compute Pipeline**: Acknowledge the need to eventually move mesh generation to GPGPU (Compute Shaders) to solve the PCIe bottleneck.

#### **3. Phase 1: The "Clean-Up" (Immediate Actions)**
- **Goal**: Decouple UI and Logic *before* optimizing performance.
- **Specific Tasks**:
    - Convert `SandboxSettings` struct to a `ScriptableObject` that resides in the Project view.
    - Create a `SandboxViewModel` that exposes `ReactiveProperty<T>` (or Unity Events) for UI binding.
    - Refactor `ARSandboxController` to *only* handle the simulation loop and hardware interfaces, removing all UI-related code.

#### **4. Phase 2: The "Optimization" (Performance)**
- **Goal**: Solve the 512x512 resolution lag.
- **Specific Tasks**:
    - Implement the **GPGPU Pipeline** described in the uploaded technical analysis.
    - Replace `MeshGenJob` (Burst) with a `TerrainSimulation.compute` shader.
    - Use `Graphics.RenderPrimitives` to draw the terrain directly from GPU memory, bypassing the CPU.

#### **5. Implementation Roadmap**
- Create a step-by-step checklist for the developer to follow, starting from safely extracting the Settings into a ScriptableObject.

**Output Format**:
Please provide this as a structured Markdown report suitable for a technical lead or senior developer."
