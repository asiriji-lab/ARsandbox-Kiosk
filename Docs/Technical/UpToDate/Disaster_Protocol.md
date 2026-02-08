# Disaster Protocol: Graceful Failure & Immersion Safety

## Protocol Definition
" Immersion is the currency of the exhibit. If we break it, we are bankrupt."
In the event of hardware or performance failure, the system must prioritize **Communication** (to the Admin) and **Stability** (to the User).

---

## Scenario A: Sensor Disconnect (Mid-Operation)
*Kid kicks the Kinect cable or USB power surges.*

| Frame | System State | Visual Output (Projection) | Admin Notification |
| :--- | :--- | :--- | :--- |
| **0** | Kinect API returns `Error: Device Lost` | Last valid depth frame sticks. | Log error internally. |
| **1-60** | Watchdog confirms loss. | **Fade to Gray**: The colorful terrain slow-fades to a monochromatic gray texture. | Admin Panel: Header flashes **RED**. |
| **60-120** | Controller kills Kinect thread. | **Maintenance Overlay**: A subtle "Technical Difficulties" icon appears in the corner. | Notification: "SENSOR DISCONNECTED. Auto-retry in 10s." |
| **180+** | Logic check. | **Auto-Simulation**: If retry fails twice, switch to Simulation (Noise) to avoid a static screen. | Popup: "Switching to Sim Mode to maintain uptime." |

---

## Scenario B: Thermal Throttling / FPS Drop
*GPU overheats, FPS drops below 20fps.*

| Frame | System State | Visual Output | Admin Notification |
| :--- | :--- | :--- | :--- |
| **Trigger** | `PerformanceMonitor` detects < 20fps for 5s. | No immediate change. | Warning: "GPU THROTTLING DETECTED." |
| **Mitigation 1** | Reduce Shadow Resolution / Blur. | Slight drop in visual quality. | "Mitigation: Reducing rendering quality." |
| **Mitigation 2** | Reduce Compute Resolution (1024 -> 512). | Terrain becomes noticeably blockier but smooth. | "Mitigation: Resolution Half-Scale Applied." |
| **Recovery** | GPU cools down, FPS > 50fps. | Slow-restore resolution to original. | "System Healthy. Quality Restored." |

---

## Scenario C: Critical Shader Crash (TDR)
*GPU driver resets due to heavy compute load.*

1.  **Detection**: Unity catches `Graphics Device Lost`.
2.  **Visual**: Screen goes Black immediately (better than a frozen glitched frame).
3.  **Action**: System attempts to reload Compute Shaders and Re-bind GraphicsBuffers.
4.  **Standby**: If reload fails, show a static high-res "Under Maintenance" map image (built-in fallback).

---

## 3-Second Handshake Rule
When an admin tries to "Enable Sensor":
*   **0.0s**: User clicks "Enable". UI shows "Handshaking...".
*   **1.5s**: Thread waits for Kinect API `OpenDevice()`.
*   **3.0s**: If no successful frame received, **KILL THREAD**. 
*   **3.1s**: UI shows "Sensor Timeout: Check Cables". Mode remains in SIMULATION.
*   **Never** block the Main UI thread while waiting.

---

## Hand Simulation Radius
Simulation mode uses a **Radius-based "Brush"** for hand interaction (100mm default).
*   **Input**: Mouse Click.
*   **Effect**: Pushes sand down/up in a Gaussian circle rather than a single vertex point.
*   **Rationale**: Ensures calibration of "Hand Rejection" logic is tested against a realistic surface area.
