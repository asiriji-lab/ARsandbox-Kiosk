# AR Sandbox Kiosk (Operational Refactor)

A high-performance Augmented Reality (AR) Sandbox built in Unity, featuring professional-grade de-noising, real-time topography, and an exhibit-centric user interface.

## 🚀 Key Features

### 🛡️ 1€ Adaptive Filter (De-noising)
- Implemented the **1€ Filter** using **Unity Burst & Job System** for near-zero latency.
- Dynamically adapts smoothing: Rock-steady stability when still (**Anti-Shake**), and high-speed responsiveness when moving (**Follow Speed**).

### 🎨 Pro-Visuals & Topography
- **Pixel-Perfect Contours**: Screenspace-stable contour lines that remain sharp regardless of slope.
- **Dynamic Color Schemes**: Five professional presets including *Natural (Snowy Peaks)*, *Heat*, and *UCDavis*.
- **Water Levels**: Real-time adjustable sea level with specular highlights.
- **Hole Filling**: Automatic suppression of sensor "black holes" or shadows.

### ✋ Exhibit-Grade UX
- **Hand Rejection**: Intelligent height-velocity filtering to ignore hands and tools while moving sand.
- **Auto-Hide Admin UI**: Interface fades out during inactivity to keep the projection clear.
- **Atmospheric Presets**: One-tap environment transformations (Volcano, Ocean, Oasis).

## 🛠️ Technical Details
- **Hardware**: Compatible with Azure Kinect / Kinect v2.
- **Optimization**: Core compute logic offloaded to Unity Jobs (Multi-threaded).
- **Settings**: Persistent JSON-based save system.

## 🕹️ Controls
- **Toggle UI**: `Tab` or `` ` `` (Tilde)
- **Calibration**: **SETUP** Tab -> **Auto-Calibrate Floor**
