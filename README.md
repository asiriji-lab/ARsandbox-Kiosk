# AR Sandbox Kiosk

A high-performance Augmented Reality Sandbox built in Unity. Real-time topography from a depth sensor, projected onto physical sand.

---

## Quick Start

### Prerequisites
| Requirement | Version |
|---|---|
| Unity | **2022.3 LTS** |
| Depth Sensor | Azure Kinect DK, Orbbec Femto Bolt, or **None** (Simulation mode) |
| Sensor SDK | [Azure Kinect SDK 1.4.1](https://learn.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download) or Orbbec K4A Wrapper |
| OS | Windows 10/11 |

### First Run
1. Clone this repo and open in Unity.
2. Press **Play** — the app auto-detects your sensor (or falls back to simulation).
3. Press **Tab** to open the Admin panel.
4. Go to **Setup → Edit Boundary (ROI)** and click the 4 corners of your sandbox.
5. Go to **Setup → Auto-Calibrate Floor** to zero the height.

### Controls
| Key | Action |
|---|---|
| `Tab` / `` ` `` | Toggle Admin UI |
| `←` `→` | Cycle camera view (Top / Perspective / Side) |
| `↑` `↓` | Zoom In / Out |
| `R` | Reload Scene |
| `Esc` | Exit |

---

## Features

- **Zero-Copy GPU Pipeline** — Depth filtering, mesh generation, and rendering all on GPU. Stable 60 FPS.
- **Keystone Calibration** — Align projector and sensor with 4-corner handle dragging.
- **ROI Masking** — Define the active sandbox area. Everything outside is cropped.
- **Adaptive De-noising** — 1-Euro filter: steady when still, responsive when moving.
- **Bicubic Terrain** — 16-tap sampling eliminates stair-stepping.
- **Elevation Palettes** — UC Davis, Desert, Natural, Heat, Topographic, Grayscale.
- **Water & Caustics** — Configurable water level with animated caustic overlay.
- **Contour Lines** — Screen-space stable, adjustable interval and thickness.
- **Auto-Hide UI** — Admin panel fades during inactivity for exhibit use.
- **Settings Persistence** — All calibration and visual settings saved to JSON.

---

## Documentation

| Document | Description |
|---|---|
| [System Architecture](./Docs/Technical/System_Architecture.md) | GPU pipeline, module responsibilities, keystone math |
| [Setup Guide](./Docs/Guides/Setup_Guide.md) | Detailed hardware setup |
| [Deployment Guide](./Docs/Guides/Deployment_Guide.md) | Kiosk installation instructions |
| [Calibration Workflow](./Docs/Guides/Calibration_UX_Workflow.md) | Step-by-step calibration process |
| [Coding Standards](./Docs/Standards/Coding_Standard.md) | Naming conventions and performance rules |
| [Hardware Troubleshooting](./Docs/Technical/Hardware_Troubleshooting_Guide.md) | Common sensor issues and fixes |
| [Contributing](./CONTRIBUTING.md) | How to contribute |

---

## License

This project is licensed under the [MIT License](./LICENSE).
