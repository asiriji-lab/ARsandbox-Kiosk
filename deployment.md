# Museum Deployment Guide

## 1. Setup & Installation
1. Clone the repository to the museum PC: `d:\motionsix\Sandbox(new)`
2. Build the Unity Project:
   - **Scene**: Ensure the main scene is `Scenes/Sandbox` (or current active scene).
   - **Output Name**: Must be **`ARSandbox.exe`** (or update the `.bat` file to match).
   - **Folder**: build it directly into `d:\motionsix\Sandbox(new)` or a `Build` folder (adjust path in .bat if needed).

## 2. Kiosk Mode (Auto-Launcher)
We have added a `UpdateAndRun.bat` script.
- **What it does**: 
    1. Tries to `git pull` the latest code from GitHub.
    2. Launches the App.
    3. If the App crashes or closes, it waits 5 seconds and re-launches it.
- **Installation**:
    1. Right-click `UpdateAndRun.bat` -> **Create Shortcut**.
    2. Press `Win+R`, type `shell:startup`.
    3. Drag the shortcut into that folder.
    4. *Result*: The Sandbox will now auto-boot when the PC turns on.

## 3. Calibration (Staff Manual)
The `Tab` key (or hidden button) toggles the **Admin Panel**.

### A. Daily "Zeroing" (Morning Routine)
If the sand height looks wrong (colors don't match mountains):
1. Flatten the sand roughly with your hands.
2. Open Admin Panel.
3. Click **[Auto-Calibrate Floor (Zero)]**.
4. *Done!* (This measures the average sand height and resets the software).

### B. Projector Alignment (Installation Only)
If the projection is crooked or doesn't line up with the box:
1. Open Admin Panel.
2. Check **[Edit Mapping (Corners)]**.
3. You will see 4 squares on the screen.
4. Drag each square to the corresponding **physical corner** of the sandbox.
5. Uncheck the box to save.
