# UX Design: AR Sandbox Calibration Workflow

**Target Audience**: Museum Staff / Educators (Non-technical).
**Goal**: Configure the physical-to-digital alignment in under 2 minutes.

## 1. Pre-Calibration State
*   **Physical**: The sandbox is powered on, projector is active, and Kinect is connected.
*   **System**: The application launches into "Play Mode" automatically.
*   **Initial Feedback**: If uncalibrated, the projection might be misaligned (off the sand) or colors might be "muddy" (incorrect depth range).

## 2. Entering "Admin Mode"
*   **Trigger**: Press `[TAB]` on the wireless keyboard.
*   **Visual Response**:
    *   The "Ruggedized Admin UI" slides in from the left (covering 35% of screen).
    *   The main projection **dims** slightly to improve UI readability, or the UI is high-contrast (Black/Yellow).
    *   **Dashboard**: Shows system health (FPS, Sensor Status: "Connected").

## 3. Step 1: Physical Alignment (Keystone/Mapping)
**Objective**: Tell the software where the sandbox corners are in the camera's view.

1.  **User Action**: Click the **[CALIB]** tab -> Toggle **[Edit Corner Mapping]**.
2.  **System Response (The "Calibration Pattern")**:
    *   The projector displays a bright **Red Frame** outlining the current logical area.
    *   On the Monitor/UI: A generic grayscale depth feed appears as an overlay.
    *   **Four Colored Handles** (Cyan, Green, Yellow, Red) appear on the monitor corners.
3.  **Instruction**: "Drag the colored handles on the screen until the Red Frame on the sand perfectly aligns with the physical wooden walls."
4.  **Feedback Loop**:
    *   As the user drags a handle on the monitor `(Mouse Drag)`, the projected Red Frame moves in real-time on the sand.
    *   *Success Indicator*: When the projected light hits exactly the edge of the sand (not spilling onto the floor or walls), alignment is complete.

## 4. Step 2: Depth Range (The "Floor & Peak" Set)
**Objective**: Define the "Zero" height (Bottom) and "Max" height (Mountain Peak) for color mapping.

### Auto-Calibration (Recommended)
1.  **User Action**: Flatten the sand roughly to the bottom level or ensure a specific "Low Point" is visible.
2.  **User Action**: Click **[Auto-Calibrate Floor (Zero)]**.
3.  **System Feedback**:
    *   Status Text: "Measuring... (3s)"
    *   The system averages 30 frames of depth data to ignore noise.
    *   Result: "Floor Set to 1450mm".
    *   **Visual Confirmation**: The "Water" layer immediately snaps to this new bottom level.

### Manual Fine-Tuning
*   **User Action**: Adjust **[Floor Depth]** slider.
    *   *Visual*: Watch the blue "Water" plane rise/fall. Stop when it just touches the sand surface.
*   **User Action**: Adjust **[Peak Depth]** slider.
    *   *Visual*: Watch the color gradient. Pile up sand to the highest expected point. Adjust slider until that peak turns "White/Red" (Max Color).

## 5. Step 3: Visual Verification
1.  **User Action**: Toggle **[Edit Corner Mapping]** OFF.
2.  **User Action**: Press `[TAB]` to hide UI.
3.  **User Test**:
    *   Dig a hole: Does water appear? (Yes/No)
    *   Build a hill: Does it turn green/yellow/red? (Yes/No)
    *   Touch the wall: Does the projection stop at the wall? (Yes/No)

## 6. Error Handling & Edge Cases

| Issue | User Feedback | Recovery Action |
| :--- | :--- | :--- |
| **Sensor Disconnected** | UI Header turns RED. Text: "SENSOR ERROR: Check Cables". | Retry connection loop automatically every 5s. |
| **Too Dark/Noise** | "Signal Quality: Low". | Suggest closing blinds or checking IR interference. |
| **Inverted Depth** | "Floor < Peak?" Warning if Min > Max. | Auto-swap values or show Alert icon. |

## 7. Wishlist / "North Star" Features
*   **Projector-Based UI**: Instead of looking at a monitor, project the "Click Here" buttons onto the sand itself (requires hand tracking).
*   **Auto-Corner Detection**: Computer Vision detects the square wooden box frame and snaps the corners automatically.
