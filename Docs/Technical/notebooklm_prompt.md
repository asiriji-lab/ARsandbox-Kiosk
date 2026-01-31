# NotebookLM Research Prompt: World 3 - Calibration & Real-World Alignment

**Instructions for User**: 
1. Open [NotebookLM](https://notebooklm.google.com/).
2. Ensure you have the **UC Davis SARndbox documentation** (PDFs/Webpages) loaded as sources.
3. Paste the following prompt into the chat to get deep technical insights for our Unity implementation.

---

### **Research Prompt**

"I am building a Unity-based implementation of the AR Sandbox (similar to the UC Davis SARndbox) using an **Azure Kinect DK** and a projector. I need to replicate the professional calibration workflow to ensure perfect alignment between the physical sand, the 3D depth data, and the projected topography.

Please research the following technical topics based on the SARndbox documentation:

#### **1. Projector-Camera Calibration (Extrinsic Alignment)**
- How exactly does SARndbox align the Projector's 2D image space with the Kinect's 3D camera space?
- The user mentioned setting **'4 corners to define a square'**. Is this a Homography transform? An Affine transform? 
- What is the mathematical relationship used (e.g., `ProjectorMatrix * ViewMatrix * WorldPoint`)?
- Does it use a 'Calibration Disk' method? If so, what is the step-by-step logic for collecting those points?

#### **2. Base Plane & Region of Interest (ROI)**
- How does the system define the 'Zero Level' (Sea Level)? Is it a fixed height value or a fitted 3D plane equation (`Ax + By + Cz + D = 0`)?
- How does it mask out the floor/room (Region of Interest) so the simulation only runs within the sandbox walls?
- Specifically, how are the corners of the sandbox defined in the software?

#### **3. Azure Kinect DK Specifics**
- The Azure Kinect has intrinsic parameters (Camera Matrix/Distortion). How should these be combined with the Projector calibration?
- Should I perform 'undistortion' on the depth map *before* or *after* aligning it to the projector?

#### **4. Interactive Tools**
- What visual guides (crosshairs, circles, grids) does the original software project during calibration to help the user? 
- What acts as the confirmation input (Keyboard? Mouse? A specific depth gesture?) when capturing a calibration point?

**Output Goal**:
Provide a step-by-step algorithm or pseudocode for implementing the **'4-Corner Calibration'** and **'Base Plane Capture'** in Unity/C#."
