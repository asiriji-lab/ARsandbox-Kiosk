# AR Sandbox Documentation

This folder contains all project documentation, organized by category.

---

## 📁 Folder Structure

### [Standards/](./Standards/)
Coding conventions and architectural guidelines for the project.

| File | Description |
|------|-------------|
| [Coding_Standard.md](./Standards/Coding_Standard.md) | Unity 6 project configuration, Azure Kinect DLL management, naming conventions, and performance standards |
| [Unity_Architecture_Standards.md](./Standards/Unity_Architecture_Standards.md) | Script lifecycle, Input System patterns, UI framework selection, and programmatic UI construction |

---

### [Technical/](./Technical/)
In-depth technical references for specific systems.

| File | Description |
|------|-------------|
| [Shader_Architecture.md](./Technical/Shader_Architecture.md) | Color correction science, Lerp-based tinting, desert color palettes, and PBR integration for sand shaders |
| [Triplanar_Sand_Rendering.md](./Technical/Triplanar_Sand_Rendering.md) | Triplanar mapping implementation, UV-free texture projection, and performance optimization for dynamic meshes |

---

### [Guides/](./Guides/)
Step-by-step setup and usage instructions.

| File | Description |
|------|-------------|
| [Setup_Guide.md](./Guides/Setup_Guide.md) | Complete setup walkthrough: software installation, project setup, Kinect integration, and troubleshooting |
| [Calibration_UX_Workflow.md](./Guides/Calibration_UX_Workflow.md) | Ideal staff workflow for physical alignment, depth range setting, and system verification |

---

### [History/](./History/)
Project status reports and refactoring notes.

| File | Description |
|------|-------------|
| [Project_Status_Report.md](./History/Project_Status_Report.md) | "Ferrari Engine" refactor summary: MeshData API, Input System safety, URP settings, camera controls |
| [Refactoring_Analysis.md](./History/Refactoring_Analysis.md) | Naming convention violations analysis and refactoring plan for `ARSandboxController.cs` |

---

## Quick Reference

| Need to... | Go to... |
|------------|----------|
| Set up the project from scratch | [Setup_Guide.md](./Guides/Setup_Guide.md) |
| Calibrate the system (Staff) | [Calibration_UX_Workflow.md](./Guides/Calibration_UX_Workflow.md) |
| Understand coding conventions | [Coding_Standard.md](./Standards/Coding_Standard.md) |
| Fix shader color issues | [Shader_Architecture.md](./Technical/Shader_Architecture.md) |
| Understand what's been done | [Project_Status_Report.md](./History/Project_Status_Report.md) |
