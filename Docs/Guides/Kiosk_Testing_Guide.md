# AR Sandbox Kiosk - Staff Testing Guide

**Purpose:** This guide ensures that staff can reliably start, calibrate, and verify the AR Sandbox Kiosk before public use.

---

## 1. Hardware Startup
**Before launching the software:**
1.  **Projector:** Turn ON. Ensure it covers the sand box area.
2.  **Kinect Sensor:** Ensure it is plugged into a USB 3.0 port (Blue/SS).
    *   *Check:* The white LED on the Kinect should be ON.
3.  **Sand:** Smooth out the sand to be relatively flat level.

## 2. Launching the App
1.  Double-click the **"ARSandbox"** icon.
2.  **Wait 10 seconds** for the system to initialize.
    *   *Self-Check:* If the screen is **Blue** or **Black**, wait at least 15s for the "Healer" system to auto-correct settings.

## 3. Mode Verification (Kiosk vs. Simulation)
The system auto-detects the Hardware.
*   **Correct State:** You should see a **Live Camera Feed** of the sand.
*   **Incorrect State:** If you see a digital terrain (Simulated Mode), the Kinect is **not detected**.
    *   *Fix:* Close App -> Replug Kinect -> Restart App.

## 4. Calibration (Essential Step)
*Perform this if the projection does not line up with the box edges.*

1.  Press **`F1`** to toggle the **Control Panel**.
2.  Click **"View Tab"** -> **"Calibration"** section.
3.  **4-Point Alignment:**
    *   Click the **"Calibrate Corners"** (or Keystone) button.
    *   Drag the **4 Red/Green Circles** on screen to strictly match the **4 Corners of the Sandbox**.
    *   *Tip:* The camera view might be rotated 180°. Moving your mouse Left might move the circle Right. This is normal behavior for this version.
4.  **Height Calibration:**
    *   Flatten the sand completely.
    *   Click **"Auto-Calibrate Floor"**.
    *   Wait 2 seconds. The colors should settle (Blue = Low, Red = High).

## 5. Functional Test
**The "Hand Wave" Test:**
1.  Stand next to the box.
2.  Hold your hand above the sand (about 1 foot high).
3.  **Result:** You should see a "Rain" or "Water" effect projected exactly on your hand (or a shadow/highlight depending on the mode).
4.  **Latency Check:** Move your hand quickly. The projection should follow with minimal delay (<0.5s).

## 6. Common Issues & Quick Fixes

| Symptom | Cause | Quick Fix |
| :--- | :--- | :--- |
| **Flat Blue Screen** | Corrupted Settings | Restart the App. The "Self-Healer" will reset depths to default. |
| **Projection is Mirrored** | Projector Settings | Use the Projector Remote -> Menu -> Projection -> Front/Ceiling. |
| **Input is Inverted** | Calibration | Drag the corners to the physically opposite side if the image is flipped. |
| **"No Sensor Found"** | Loose Cable | Check USB connection. Ensure it's not in a USB 2.0 (Black) port. |

---
*If problems persist without resolution, contact the Technical Lead.*
