using UnityEngine;
using System.Collections.Generic;
using ARSandbox.Core.Math;
using ARSandbox.Core.Vision;

namespace ARSandbox.Core
{
    public class AlignmentSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera ProjectorCamera;
        public SandboxSettingsSO Settings; // Reference to save calibration results
        public ComputeShader TerrainShader; // For readback if needed, or we assume KinectProvider handles data
        
        [Header("Calibration State")]
        public bool IsCalibrating = false;
        public int CurrentPointIndex = 0;
        public List<Vector2> TargetUVs = new List<Vector2>(); // Defined 2D points (Grid)
        
        private List<Vector3> _collectedWorldPoints = new List<Vector3>();
        private List<Vector2> _collectedScreenPoints = new List<Vector2>();
        
        private CalibrationDiskScanner _scanner;
        
        void Awake()
        {
            _scanner = new CalibrationDiskScanner();
            InitializeGrid(3, 3); // 3x3 Grid = 9 points
        }
        
        private void InitializeGrid(int cols, int rows)
        {
            TargetUVs.Clear();
            float xStep = 1.0f / (cols + 1);
            float yStep = 1.0f / (rows + 1);
            
            for(int y=1; y<=rows; y++) {
                for(int x=1; x<=cols; x++) {
                    TargetUVs.Add(new Vector2(x * xStep, y * yStep));
                }
            }
        }
        
        public void StartCalibration()
        {
            IsCalibrating = true;
            CurrentPointIndex = 0;
            _collectedWorldPoints.Clear();
            _collectedScreenPoints.Clear();
            Debug.Log("[Alignment] Starting Calibration Sequence...");
        }
        
        // Named 'Capture' to avoid confusion with Update
        public void CaptureCurrentPoint(Vector3[] depthData, int width, int height)
        {
            if (!IsCalibrating || CurrentPointIndex >= TargetUVs.Count) return;
            
            Vector2 targetUV = TargetUVs[CurrentPointIndex];
            
            // Scan for disk
            Vector3? diskPos = _scanner.ScanForDisk(depthData, width, height, targetUV);
            
            if (diskPos.HasValue)
            {
                _collectedWorldPoints.Add(diskPos.Value);
                _collectedScreenPoints.Add(targetUV);
                Debug.Log($"[Alignment] Point {CurrentPointIndex} Captured: 3D{diskPos.Value} -> 2D{targetUV}");
                
                CurrentPointIndex++;
                if (CurrentPointIndex >= TargetUVs.Count)
                {
                    FinalizeCalibration();
                }
            }
            else
            {
                Debug.LogWarning("[Alignment] Disk not found! Ensure disk is raised >5mm above sand.");
            }
        }
        
        private void FinalizeCalibration()
        {
            Debug.Log("[Alignment] Solving DLT Matrix...");
            
            // 1. Construct A Matrix for DLT
            int N = _collectedWorldPoints.Count;
            float[,] A = new float[2 * N, 12];
            
            for(int i=0; i<N; i++)
            {
                Vector3 M = _collectedWorldPoints[i]; // World
                Vector2 m = _collectedScreenPoints[i]; // Screen
                
                // DLT Equations (Row 1)
                A[2*i, 0] = M.x; A[2*i, 1] = M.y; A[2*i, 2] = M.z; A[2*i, 3] = 1;
                A[2*i, 4] = 0;   A[2*i, 5] = 0;   A[2*i, 6] = 0;   A[2*i, 7] = 0;
                A[2*i, 8] = -m.x * M.x; A[2*i, 9] = -m.x * M.y; A[2*i, 10] = -m.x * M.z; A[2*i, 11] = -m.x;
                
                // DLT Equations (Row 2)
                A[2*i+1, 0] = 0;   A[2*i+1, 1] = 0;   A[2*i+1, 2] = 0;   A[2*i+1, 3] = 0;
                A[2*i+1, 4] = M.x; A[2*i+1, 5] = M.y; A[2*i+1, 6] = M.z; A[2*i+1, 7] = 1;
                A[2*i+1, 8] = -m.y * M.x; A[2*i+1, 9] = -m.y * M.y; A[2*i+1, 10] = -m.y * M.z; A[2*i+1, 11] = -m.y;
            }
            
            // 2. Solve SVD
            float[] h = NativeLinearAlgebra.SolveDLT(A);
            
            // 3. Convert to Matrix4x4
            Matrix4x4 projMat = NativeLinearAlgebra.VectorToMatrix(h);
            
            // 4. Apply to Camera
            if (ProjectorCamera != null)
            {
                ProjectorCamera.projectionMatrix = projMat;
                // We keep View Matrix identity if the P matrix incorporates extrinsic, 
                // OR we have to separate them. DLT gives P = K * [R|t].
                // So setting projectionMatrix = P and worldToCamera = Identity is valid if P covers everything.
                // However, Unity's Camera also uses position for Culling.
                // For a fixed projector, this is usually fine if we don't move the "transform".
                ProjectorCamera.worldToCameraMatrix = Matrix4x4.identity; 
            }
            
            IsCalibrating = false;
            Debug.Log("[Alignment] Calibration Complete! Matrix Applied.");
        }
    }
}
