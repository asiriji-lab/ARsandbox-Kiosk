# Kiosk Site Data Sheet (KSDS)

This document defines the critical data points required for a professional-grade installation of the AR Sandbox Kiosk. Accurate measurement and recording of these values ensures reproducible setups, easier troubleshooting, and optimal calibration performance.

---

## 1. Site Identity
**Kiosk ID:** `[UNIQUE_ID_001]`  
**Location:** `[Site Name / Room Number]`  
**Install Date:** `[YYYY-MM-DD]`  
**Technician:** `[Name]`

---

## 2. Physical Dimensions (Millimeters)
Measure these values with a laser tape or precision ruler. These are the "Ground Truth" for calibration.

| Parameter | Value (mm) | Description |
| :--- | :--- | :--- |
| **Box Inner Width (X)** | `[ 3200 ]` | The exact width of the sand containment area (short visible side). |
| **Box Inner Length (Y)** | `[ 4200 ]` | The exact length of the sand containment area (long visible side). |
| **Sand Depth Max** | `[ ____ ]` | Maximum fill depth of sand before spilling. |
| **Sand Depth Min** | `[ ____ ]` | Minimum sand required to cover the bottom glass/wood. |
| **Sensor Height (Z)** | `[ ____ ]` | Distance from the Sensor Lens to the **Sand Floor** (0mm plane). |
| **Projector Height** | `[ ____ ]` | Distance from the Projector Lens to the Sand Floor. |
| **Throw Distance** | `[ ____ ]` | Horizontal distance from Projector Lens center to screen center (if offset). |

> **Note:** Ideally, Sensor interactions should be centered. Report any X/Y offset of the sensor relative to the box center:
> *   **Sensor Offset X:** `[ +/- ____ ]`
> *   **Sensor Offset Y:** `[ +/- ____ ]`

---

## 3. Hardware Inventory

### Compute Unit
*   **Model:** `[e.g. Dell Precision 3660, Custom Build]`
*   **CPU:** `[e.g. i7-12700K]`
*   **GPU:** `[e.g. RTX 3070]`
*   **RAM:** `[e.g. 32GB DDR5]`
*   **OS Build:** `Windows 10 / 11 Pro [Version]`

### Projection
*   **Make/Model:** `[e.g. BenQ TK850i]`
*   **Native Resolution:** `[1920x1080 / 3840x2160]`
*   **Lumens:** `[e.g. 3000]`
*   **Keystone Setting:** `[Hardware 0 / Software Corrected]` (Prefer 0 hardware keystone, fix in software).
*   **Focus Check:** `[Pass/Fail]` (Is the pixel grid sharp at sand level?)

### Sensor
*   **Device Type:** `[Kinect v2 / Azure Kinect DK / Orbbec Femto]`
*   **Serial Number:** `[S/N]`
*   **USB Port:** `[USB 3.0 / 3.1 Gen 2]` (Ensure it's on a dedicated controller).
*   **Firmware Version:** `[v1.x.x]`

---

## 4. Software Configuration Profile

### Display Settings
*   **Desktop Resolution:** `[Width] x [Height]` (Target: 1920x1080 @ 60Hz)
*   **Scaling:** `[100% / 125%]` (MUST be 100% for pixel-perfect mapping).
*   **Orientation:** `[Landscape / Portrait]` (Usually Landscape).
*   **Multiple Displays:** `[Extend / Duplicate]` (Extend if admin monitor is present).

### Application Constants (SandboxSettings.cs)
Record the tuned values here for backup.

| Setting | Value (mm) | Description |
| :--- | :--- | :--- |
| **MinDepth** | `[ 500 ]` | Sensor cutoff for ceiling/camera body. |
| **MaxDepth** | `[ 1200 ]`| Sensor cutoff for floor/table. |
| **Field of View (FOV)** | `[ ____ ]` | Vertical FOV of the Projector (if known). |
| **Box Aspect Ratio** | `[ 3.2:4.2 ]` | Matches physical box dimensions (Width/Length). |

---

## 5. Environmental Factors
*   **Ambient Light (Lux):** `[ ____ ]` (Measure at sand surface. Should be < 50 Lux for best contrast).
*   **Temperature:** `[ __ °C ]` (Ensure adequate airflow for Projector/PC).
*   **Power:** `[Dedicated Circuit / Shared]` (Dedicated preferred to avoid GPU brownouts).

---

## 6. Calibration Checklist (Post-Install)
- [ ] **Physical Leveling:** Box is perfectly level (use spirit level).
- [ ] **Sand Prep:** Sand is evenly distributed and flat (use a rake/board for baseline).
- [ ] **Keystone Alignment:** Interaction corners match projected corners perfectly.
- [ ] **Depth Floor Zero:** "Digging to bottom" shows the lowest color (Blue).
- [ ] **Depth Peak:** "Piling to top" shows the highest color (Red/White).
- [ ] **Jitter Check:** Place a static object (box) on sand. Edges should be stable, not flickering.

---

## 7. Sign-Off
**QA Passed:** `[Yes/No]`
**Date:** `[YYYY-MM-DD]`
