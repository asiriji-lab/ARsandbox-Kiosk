# AR Sandbox Documentation

This folder contains all project documentation, organized by category.

---

## 📁 Folder Structure

### [Standards/](./Standards/)
Coding conventions and architectural guidelines.

| File | Description |
|------|-------------|
| [Coding_Standard.md](./Standards/Coding_Standard.md) | **Strict Standard**: Naming conventions, grammar, defensive patterns, and API documentation. |
| [Unity_Architecture_Standards.md](./Standards/Unity_Architecture_Standards.md) | Script lifecycle, Input System patterns, and programmatic UI construction. |

---

### [Technical/](./Technical/)
In-depth technical references for specific systems.

| File | Description |
|------|-------------|
| [Implementation_Manual.md](./Technical/Implementation_Manual.md) | **Technical Depth**: MeshData API, Job scheduling, DLL management, and URP config. |
| [Shader_Architecture.md](./Technical/Shader_Architecture.md) | Color correction, Lerp-based tinting, and PBR integration for sand shaders. |
| [Triplanar_Sand_Rendering.md](./Technical/Triplanar_Sand_Rendering.md) | Triplanar mapping implementation and performance optimization for dynamic meshes. |
| [Reference/](./Technical/Reference/) | Raw research and property notes (UC Davis, Water, URP). |

---

### [Guides/](./Guides/)
Step-by-step setup and usage instructions.

| File | Description |
|------|-------------|
| [Setup_Guide.md](./Guides/Setup_Guide.md) | Complete setup walkthrough: software, project, and Kinect integration. |
| [Calibration_UX_Workflow.md](./Guides/Calibration_UX_Workflow.md) | Staff workflow for physical alignment and system verification. |
| [Deployment_Guide.md](./Guides/Deployment_Guide.md) | Build and deployment notes. |

---

### [History/](./History/)
Project status reports and refactoring notes.

| File | Description |
|------|-------------|
| [Project_Status_Report.md](./History/Project_Status_Report.md) | "Ferrari Engine" refactor summary (MeshData, Input System, URP). |
| [Bug_Fix_Log.md](./History/Bug_Fix_Log.md) | Granular log of specific bug fixes and issue resolutions ("When, What, How"). |
| [Refactoring_Analysis.md](./History/Refactoring_Analysis.md) | Naming convention analysis for `ARSandboxController.cs`. |

---

## Quick Reference

| Need to... | Go to... |
|------------|----------|
| Set up the project from scratch | [Setup_Guide.md](./Guides/Setup_Guide.md) |
| Understand coding standards or implementation details | For strict coding standards and naming conventions, see [Coding_Standard.md](./Standards/Coding_Standard.md).<br>For deep-dive implementation details (MeshData, DLLs, URP), see the [Implementation_Manual.md](./Technical/Implementation_Manual.md). |
| Fix shader color or rendering issues | [Shader_Architecture.md](./Technical/Shader_Architecture.md) |
| Check research or raw properties | [Reference/](./Technical/Reference/) |
| Understand recent engine changes | [Project_Status_Report.md](./History/Project_Status_Report.md) |
