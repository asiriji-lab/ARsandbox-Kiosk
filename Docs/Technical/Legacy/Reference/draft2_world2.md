--------------------------------------------------------------------------------
Technical Design Specification: World 3 (Native C# Implementation)
Date: January 30, 2026 Role: Technical Lead / System Architect Reference: "World 3" Production Phase Constraint Checklist: No OpenCV, No EmguCV, Pure UnityEngine & System.Math.

--------------------------------------------------------------------------------
1. Executive Summary
Objective Transition the AR Sandbox from a prototype to a standalone, museum-grade exhibit capable of sub-millimeter alignment accuracy. This phase ("World 3") removes dependencies on managed wrappers (OpenCvSharp) to simplify deployment and licensing. We will implement a custom "Lightweight Solver" for calibration and a native depth-thresholding computer vision system.
Key Pillars
1. Dependency-Free Calibration: Implementation of a Direct Linear Transform (DLT) solver in pure C# to calculate the Projector-View Matrix.
2. Native Computer Vision: Raw depth buffer processing to detect calibration artifacts (disks) using geometric thresholding rather than image recognition.
3. Hardware Abstraction: Direct interfacing with the Azure Kinect Sensor SDK to leverage hardware-accelerated undistortion before data reaches the C# layer.

--------------------------------------------------------------------------------
2. Technical Implementation Strategy
A. Data Acquisition: The "Undistorted" Pipeline
Instead of manually implementing Brown-Conrady distortion models in C#, we will leverage the Azure Kinect SDK’s internal Transformation class. The SDK internally handles the complex lens distortion corrections (Brown-Conrady) on the GPU/CPU before we access the data.
• Step 1: Access raw DepthFrame (16-bit ushort).
• Step 2: Use Device.GetCalibration() to retrieve factory intrinsics.
• Step 3: Use Transformation.DepthImageToPointCloud() to convert the depth map into a linear array of Vector3 (Camera Space) points.
    ◦ Result: We receive a Vector3[] array where every point is already physically accurate (in meters) and corrected for lens distortion. This simplifies our custom solver to a linear problem.
B. The Calibration Engine: "Native DLT"
Since we cannot use cv::solvePnP, we will implement a Direct Linear Transform (DLT). This algorithm solves for the 3×4 Projection Matrix (P) that maps 3D world points (Sand) to 2D screen coordinates (Projector).
1. Mathematical Model
We seek the matrix P such that for every tie-point i: 
w 
i
​
  

​
  
u 
i
​
 
v 
i
​
 
1
​
  

​
 =P 

​
  
X 
i
​
 
Y 
i
​
 
Z 
i
​
 
1
​
  

​
 
 Where (u,v) are projector pixels and (X,Y,Z) are Kinect camera-space coordinates.
2. The Algorithm (Pure C#)
We will construct a system of linear equations Ah=0, where h is the vectorized form of matrix P.
For each of the N≥6 calibration points, we add two rows to Matrix A:  \begin{bmatrix} -X_i & -Y_i & -Z_i & -1 & 0 & 0 & 0 & 0 & u_i X_i & u_i Y_i & u_i Z_i & u_i \ 0 & 0 & 0 & 0 & -X_i & -Y_i & -Z_i & -1 & v_i X_i & v_i Y_i & v_i Z_i & v_i \end{bmatrix} \begin{bmatrix} h_1 \ \vdots \ h_{12} \end{bmatrix} = 0 
• Solver Logic: We perform Singular Value Decomposition (SVD) on Matrix A (2N×12). The solution h is the last column of V (corresponding to the smallest singular value).
• Note: Since C# System.Math lacks SVD, we will include a lightweight single-file SVD implementation (e.g., derived from the Golub-Reinsch algorithm or similar open-source C# numerical recipes).
C. Feature Extraction: "Height-Thresholding"
We replace OpenCV blob detection with a geometric approach tailored to the Azure Kinect's high-fidelity depth data.
1. Region of Interest (ROI): The user places the disk under a projected crosshair. We define a 20x20 pixel ROI in the depth map around this expected location.
2. Planar Segmentation: We calculate the average height (Z 
sand
​
 ) of the pixels at the perimeter of the ROI.
3. Thresholding: We iterate through the internal pixels. If Z 
pixel
​
 <(Z 
sand
​
 −5mm), the pixel belongs to the calibration disk (since it is physically higher/closer to the camera).
4. Centroiding: We compute the arithmetic mean of the (X,Y,Z) coordinates of all disk pixels to filter out sensor noise (random error σ≈1−2mm).

--------------------------------------------------------------------------------
3. Recommended Code Architecture
A. NativeLinearAlgebra.cs (The Math Core)
Encapsulates the SVD logic required for DLT.
public static class NativeLinearAlgebra
{
    /// <summary>
    /// Solves Ah = 0 using Singular Value Decomposition.
    /// Returns the 12-element vector h representing the Projection Matrix.
    /// </summary>
    public static float[] SolveDLT(float[,] A)
    {
        // 1. Compute A^T * A to form a square symmetric matrix (12x12).
        float[,] AtA = Multiply(Transpose(A), A);
        
        // 2. Perform Jacobi SVD or Golub-Reinsch SVD on 12x12 matrix.
        // Since matrix is small (12x12), iterative Jacobi is fast and stable.
        // Ref: [9] "New Fast and Accurate Jacobi SVD Algorithm"
        SingularValueDecomposition svd = new JacobiSVD(AtA);
        
        // 3. The solution corresponds to the eigenvector of the smallest eigenvalue.
        return svd.RightSingularVectors.GetLastColumn();
    }
    
    // Helper: Convert 12-element vector to 4x4 Unity Matrix
    public static Matrix4x4 VectorToMatrix(float[] h)
    {
        Matrix4x4 m = new Matrix4x4();
        m.m00 = h; m.m01 = h[10]; m.m02 = h[11]; m.m03 = h[12];
        // ... map remaining elements ...
        // Note: Projector Z is 'forward', Unity Z is 'forward'. 
        // Handedness conversion (Y-flip) may be required here.
        return m;
    }
}
B. CalibrationDiskScanner.cs (The Vision System)
Handles the detection of the physical disk without CV libraries.
public class CalibrationDiskScanner
{
    private const float DISK_HEIGHT_THRESHOLD = 0.005f; // 5mm threshold [7]
    
    public Vector3? ScanForDisk(Vector3[] pointCloud, int width, int height, Vector2 expectedUV)
    {
        // 1. Define Search Window (e.g. 40x40 pixels) around projected point
        int startX = (int)expectedUV.x - 20;
        int startY = (int)expectedUV.y - 20;
        
        List<Vector3> sandSamples = new List<Vector3>();
        List<Vector3> diskSamples = new List<Vector3>();

        // 2. First Pass: Sample the perimeter to find "Sea Level"
        for (int x = startX; x < startX + 40; x++)
        {
            sandSamples.Add(GetPoint(pointCloud, width, x, startY));      // Top edge
            sandSamples.Add(GetPoint(pointCloud, width, x, startY + 40)); // Bottom edge
        }
        float averageSandZ = CalculateAverageZ(sandSamples);

        // 3. Second Pass: Scan interior for "floating" pixels
        for (int y = startY + 5; y < startY + 35; y++)
        {
            for (int x = startX + 5; x < startX + 35; x++)
            {
                Vector3 pt = GetPoint(pointCloud, width, x, y);
                
                // If point is > 5mm closer to camera than sand
                if (pt.z < (averageSandZ - DISK_HEIGHT_THRESHOLD) && pt.z > 0.1f)
                {
                    diskSamples.Add(pt);
                }
            }
        }

        // 4. Return Centroid
        if (diskSamples.Count < 50) return null; // Noise filter
        return CalculateCentroid(diskSamples);
    }
}
C. ProjectorCameraAligner.cs (The Unity Component)
Orchestrates the calibration and applies the result to the Camera.
public class ProjectorCameraAligner : MonoBehaviour
{
    public Camera projectorCam; // The Unity Camera acting as the Projector
    
    // Lists to hold the tie-points
    private List<Vector3> _worldPoints = new List<Vector3>(); // From Kinect
    private List<Vector2> _projectorPixels = new List<Vector2>(); // From Screen UV
    
    public void FinalizeCalibration()
    {
        // 1. Construct the A matrix (2N x 12)
        float[,] A = ConstructAMatrix(_worldPoints, _projectorPixels);
        
        // 2. Solve DLT
        float[] h = NativeLinearAlgebra.SolveDLT(A);
        Matrix4x4 projectionMatrix = NativeLinearAlgebra.VectorToMatrix(h);
        
        // 3. Apply to Unity Camera
        // Crucial: Unity uses a specific projection matrix format. 
        // We override the standard Frustum with our calculated matrix.
        projectorCam.projectionMatrix = projectionMatrix;
        
        // 4. Save to disk (Persistence)
        CalibrationStorage.Save(projectionMatrix);
    }
}

--------------------------------------------------------------------------------
4. Calibration Workflow (User Experience)
1. Hardware Alignment: User launches "Align Mode." Projector displays a grid. User physically adjusts the Azure Kinect mount to cover the sand area.
2. Base Plane Capture: User places a flat board on the sandbox rails. System averages 100 frames to determine the "Zero Plane" equation (Ax+By+Cz+D=0).
3. Tie-Point Collection:
    ◦ Projector displays a crosshair.
    ◦ User places the physical disk.
    ◦ User presses [Space] or holds hand over "Virtual Button."
    ◦ CalibrationDiskScanner extracts the centroid (3D) and pairs it with the crosshair (2D).
    ◦ Repeat 12-20 times at various heights (Sand Level, Mid Level, High Level).
4. Solve: System runs SolveDLT. The projection snaps into alignment.
5. Risk Assessment & Mitigations
• Flying Pixels: Azure Kinect suffers from "flying pixels" (mixed depth values) at object edges.
    ◦ Mitigation: The CalibrationDiskScanner must ignore the outer 2-3 pixels of the detected disk blob to ensure we only average the reliable center surface.
• Thermal Drift: Azure Kinect depth values can drift by ~1-2mm during warm-up.
    ◦ Mitigation: Enforce a 45-minute warm-up period in the software via a "System Health" dashboard before allowing calibration.
• Coordinate Handedness: Unity (Left-Handed) vs. Kinect (Right-Handed).
    ◦ Mitigation: In VectorToMatrix, invert the Z-axis column of the calculated matrix. P 
Unity
​
 =P 
DLT
​
 ×Scale(1,1,−1).