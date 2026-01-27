# AR Sandbox Setup Guide

This file contains the complete step-by-step instructions to create your AR Sandbox in Unity.

## 1. Install Software
Before starting, ensure you have these installed:
1.  **Unity 2020.3 LTS or newer** (Recommended: Unity 2021.3 or 2022.3).
    - **IMPORTANT:** Install the **Universal Render Pipeline (URP)** template when creating the project. This project is optimized for URP.
2.  **Azure Kinect Sensor SDK** (v1.4.1 installed on Windows).
3.  **Azure Kinect DK** hardware.

## 2. Setting Up the Project

1.  Open **Unity Hub**.
2.  Click **New Project**.
3.  Select **3D (URP)** or **3D Sample Scene (URP)**.
    *   *Note: Do not select "3D Core" or "HDRP".* **If you are already in a project**, ensure URP is installed via Package Manager.
4.  Name it `ARSandbox` and create it.

## 3. Install Kinect Packages
1.  Inside Unity, check if you have the Azure Kinect package. 
2.  **Recommended**: Download the [Azure Kinect Examples for Unity](https://github.com/microsoft/Azure-Kinect-Samples/tree/master/body-tracking-samples/sample_unity_bodytracking/Assets/AzureKinectExamples) from GitHub.
3.  **Alternative**: Keep it simple. Ensure `Microsoft.Azure.Kinect.Sensor.dll` is in your project `Assets/Plugins` folder. If you don't know how to get this, the easiest way is to find a "Unity Azure Kinect" package on the Asset Store or GitHub.

## 4. Add the Scripts (Copy-Paste)
You need to create two files in your project.

### Step 4.1: The Controller Script
1.  In Unity, look at the **Project** window (usually at the bottom).
2.  Right-click -> **Create** -> **C# Script**.
3.  Name it exactly: `ARSandboxController`.
4.  Double-click it to open.
5.  Delete **everything** inside and paste the code from `ARSandboxController.cs` (provided below/attached).
6.  Save the file (Ctrl+S).

### Step 4.2: The Shader
1.  In Unity **Project** window.
2.  Right-click -> **Create** -> **Shader** -> **Standard Surface Shader**.
3.  Name it exactly: `Topography`. (If it asks, name the file `Topography`).
4.  Double-click it to open.
5.  Delete **everything** inside and paste the code from `Topography.shader` (provided below/attached).
6.  Save the file.

## 5. Set Up the Scene
1.  **Create the Sandbox Object**:
    *   In the **Hierarchy** (left side), Right-click -> **Create Empty**.
    *   Rename it to `Sandbox`.
    *   Set its **Position** to (0, 0, 0).
2.  **Add the Script**:
    *   Click the `Sandbox` object.
    *   Drag the `ARSandboxController` script onto it.
3.  **Create the Material**:
    *   In **Project** window, Right-click -> **Create** -> **Material**.
    *   Name it `Mat_Sand`.
    *   With `Mat_Sand` selected, look at the Inspector (top right).
    *   Find the **Shader** dropdown. It usually says "Standard". Change it to `Custom` -> `Topography`.
    *   You should see colors (Water, Sand, Grass) appear in the inspector.
4.  **Apply Material**:
    *   Click the `Sandbox` object again.
    *   Drag `Mat_Sand` into the **Element 0** slot of the `Mesh Renderer` component (the script should have added this component automatically. If not, ignore this step until you press Play, or manually add a Mesh Renderer).
    *   *Note*: The script adds MeshRenderer at runtime usually, but you can add it manually: Add Component -> `Mesh Renderer` -> Set Material to `Mat_Sand`.

## 6. Calibration (Important!)
Select the `Sandbox` object and look at the `ARSandboxController` settings:
*   **Min Depth MM**: How close (in millimeters) is the highest sand mountain to the camera? (e.g., 500mm = 0.5 meters).
*   **Max Depth MM**: How far is the bottom of the box? (e.g., 1500mm = 1.5 meters).
*   **Water Level (in Material)**: Open the `Mat_Sand` material. Adjust `Water Level` slider until the "Water" appears at the bottom of your mesh.

## 7. Play!
1.  Connect your Azure Kinect.
2.  Press the **Play** button in Unity.
3.  The mesh should appear and ripple like water at the bottom!

## 9. Testing without Hardware (Simulation Mode)

If you don't have the Kinect sensor connected yet, you can still test the visuals:
1.  Select the **Sandbox** object.
2.  In the **ARSandboxController**, check the box **Enable Simulation**.
3.  Press **Play`.
4.  **Simulation Controls:** Press `Tab` to open the Admin UI. You'll see:
    
    **Common Controls (Always Visible):**
    *   **Enable Simulation:** Toggle between Kinect data and procedural terrain.
    *   **Flat Mode (Projection):** Projects terrain from top-down view.
    *   **Solid Walls:** Shows/hides boundary walls.
    *   **Desert Gradient:** Switches between UC Davis (rainbow) and Desert (warm earth tones) color schemes.
    *   **Discrete Bands:** Creates flat color zones like a topographic map instead of smooth gradients.
    *   **Height Scale:** Exaggerates or flattens terrain elevation.
    *   **Gradient Intensity:** Controls how strongly the elevation colors overlay the sand texture.
    *   **Sand Scale:** Adjusts the size/tiling of sand texture details.
    *   **Boundary Size:** Changes the physical size of the terrain mesh.
    
    **Simulation-Specific Controls (When Simulation is ON):**
    *   **Noise Scale:** Controls terrain detail (Low = Big Hills, High = Fine Detail).
    *   **Move Speed:** Controls how fast the terrain animates/flows.
    *   **Cycle View Camera:** Switches camera between Top, Perspective, and Side views.
    
    **Real-Time Controls (When Simulation is OFF, Kinect connected):**
    *   **Auto-Calibrate Floor:** Automatically detects floor and peak heights.
    *   **Edit Mapping (Corners):** 4-point calibration for sensor alignment.
    *   **Min/Max Depth:** Manual depth range adjustment in millimeters.
    *   **Terrain Change Rate:** Smoothing factor for Kinect data (0=instant, 0.95=very smooth).

5.  **What you can do:**
    *   **Test Gradient Presets:** Toggle "Desert Gradient" to see warm vs cool colors.
    *   **Try Discrete Bands:** Enable "Discrete Bands" for a classic topographic map look with 5 flat color zones.
    *   **Combine Features:** Try Desert Gradient + Discrete Bands for a warm-toned topo map.
    *   **Calibrate Colors:** Adjust `Gradient Intensity` slider to control elevation color strength.
    *   **Test Performance:** Ensure the game runs smoothly (60+ FPS) with the mesh deformation.
    *   **Verify Shader:** Confirm the water animation and height blending work correctly.
    *   **Polish Water:** Adjust `Water Glossiness` (Shininess) and `Fresnel Power` (Edge lighting) to make the water look wet and realistic.

## 10. Troubleshooting
- **Pink Material:** You are likely in a URP project using the old Standard shader. Use the updated URP-compatible shader provided.
- **Kinect Error:** Ensure the `k4a.dll` and `depthengine_2_0.dll` are in your Project Root folder.
- **Checkerboard Pattern:** The shader was trying to load a missing texture. The fixed shader uses pure code-based colors.
