# AR Sandbox Kiosk (Operational Refactor)

A high-performance, studio-grade Augmented Reality (AR) Sandbox built in Unity. This project features advanced de-noising, real-time bicubic topography generation, and an exhibit-centric user interface designed for public installations.

---

## � Project Navigation
To maintain a clean Unity workspace, all project documentation has been moved to the root `/Docs` directory.

- **[Coding Standards](./Docs/Standards/Coding_Standard.md)**: Naming conventions and performance rules.
- **[Implementation Manual](./Docs/Technical/Implementation_Manual.md)**: Deep dive into the architecture and de-noising math.
- **[Deployment Guide](./Docs/Guides/Deployment_Guide.md)**: Instructions for setting up the kiosk hardware.
- **[Bug Fix Log](./Docs/History/Bug_Fix_Log.md)**: Historical record of optimizations and stabilization.

---

## 🚀 Key High-End Features

### 🛡️ 1€ Adaptive Filter (De-noising)
- Core compute logic offloaded to **Unity Burst & Job System** for near-zero latency.
- Dynamic adaptation: Rock-steady stability when still (**Anti-Shake**) and high-speed responsiveness when moving (**Follow Speed**).

### �️ Precision Topography
- **16-Tap Bicubic Filtering**: Custom sampling kernel in `MeshGenJob` eliminates "stair-stepping" artifacts on inland peaks.
- **Slope-Based Masking**: Advanced shader logic in `Topography.shader` prevents "black blob" singularities on flat plateaus by hiding converged contour lines.
- **Pixel-Perfect Contours**: Screen-space stable lines that remain razor-sharp regardless of zoom or steepness.

### ✋ Exhibit-Grade UX
- **Hand Rejection**: Intelligent height-velocity filtering protects the topography from hands and tools.
- **Auto-Hide Admin UI**: The command panel fades during inactivity to provide an immersive visitor experience.
- **Atmospheric Presets**: Instant environment shifts (Volcano, Ocean, Oasis) with custom height scaling and tinting.

---

## 🛠️ Technical Overview
- **Hardware**: Compatible with Azure Kinect, Kinect v2, and Simulated data.
- **Architecture**: Programmatic UI (No Prefab dependencies) and Multi-threaded Mesh generation.
- **Persistence**: Safe JSON-based serialization with robust fallback defaults for museum settings.
- **Standards**: 100% compliant with the `PascalCase` (Public) and `_camelCase` (Private) naming standards.

---

## 🕹️ Controls & Setup

### **Getting Started**
1. Ensure your Kinect sensor is connected.
2. Run `UpdateAndRun.bat` or open the project in Unity.
3. Open the **ADMIN** panel (`Tab`).
4. Go to **SETUP** -> **Auto-Calibrate Floor** to zero the sandbox.

### **Key Bindings**
- **Toggle UI**: `Tab` or `` ` `` (Tilde)
- **Cycle Monitor View**: `Left/Right Arrows` (Top, Perspective, Side)
- **Camera Zoom**: `Up/Down Arrows`
- **Reload Scene**: `R`
- **Exit application**: `Esc`

---
**Maintained by**: Ralph Workspace Workflow 🚀
