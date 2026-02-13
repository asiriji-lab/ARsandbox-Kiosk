# Hardware Troubleshooting Guide: Azure Kinect

> [!IMPORTANT]
> This system uses a **Hardware Sentinel** (Watchdog) that will automatically restart the sensor if it freezes for more than 2 seconds. However, if restarts happen frequently, follow this guide.

## Common Failure Modes

### 1. "Device Keeps Dying" (Freeze / Restart Loop)
**Symptoms:** The depth stream freezes, then the screen goes black or re-initializes every few seconds.
**Cause:** USB Bandwidth Saturation or Power Drop.
**Fixes:**
-   **USB Controller**: Ensure the Kinect is on its **own** USB 3.0 controller. Do not share it with webcams or Arduino serial ports.
-   **Power**: The Azure Kinect **MUST** use the external power adapter. USB power is not sufficient for the depth camera in NFOV modes.
-   **Cable**: Use the official cable or a high-quality active USB 3.0 extension. Passive extensions > 1.5m will cause data drops.

### 2. "Sensor Not Found"
**Cause:** The device is in a confused state or another app has claimed it.
**Fixes:**
-   Close **Azure Kinect Viewer** before running Unity.
-   Unplug USB and Power, wait 10 seconds, reconnect.
-   Check Device Manager for "Azure Kinect 4K Camera" and "Azure Kinect Depth Camera".

### 3. Thermal Shutdown
**Symptoms:** The device shuts down after 10-20 minutes.
**Cause:** Overheating.
**Fix:** Ensure the fan exhaust on the back is not blocked. The device creates significant heat during depth processing.

## Sentinel Logic (How the Fix Works)
-   **Monitoring**: The software checks for new frames every `Update()`.
-   **Detection**: If no new frame arrives for **2.0 seconds** (configurable in `SandboxSettings`), the Watchdog triggers.
-   **Recovery**: The system calls `Shutdown()`, waits, and `Initialize()` to reset the USB driver stack.

## FAQ: Deep Dives

### Why can't two programs access the same camera?
Most operating systems (Windows, Linux, macOS) enforce **Exclusive Access** for high-bandwidth peripherals like the Azure Kinect.
-   **Driver Limitation**: The OS driver opens a single data pipe. Once claimed, any other request gets `Access Denied`.
-   **Bandwidth**: The Azure Kinect consumes ~300MB/s of USB 3.0 bandwidth. Two apps accessing the stream simultaneously would exceed the bus limit instantly.
-   **Solution**: Close all other apps (OBS, Viewer, Chrome) before launching the Sandbox.
