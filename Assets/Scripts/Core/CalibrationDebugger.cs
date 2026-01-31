using UnityEngine;
using ARSandbox.Core.Math;
using System.Collections.Generic;

public class CalibrationDebugger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Starting DLT Verification ===");
        
        List<Vector3> worldPoints = new List<Vector3>();
        List<Vector2> screenPoints = new List<Vector2>();

        // Scenario: Input coords in range [-1, 1], Output [-1, 1].
        // Identity mapping: x->u, y->v. (Simulating Planar or Ortho for simplicity first to debug Solver)
        // If Solver can't solve Identity, it's broken.
        
        // P = [1 0 0 0; 0 1 0 0; 0 0 1 0]
        // u = x/z, v = y/z. If z=1, u=x, v=y.
        
        // Point 1
        worldPoints.Add(new Vector3(-0.5f, -0.5f, 1f)); screenPoints.Add(new Vector2(-0.5f, -0.5f));
        worldPoints.Add(new Vector3(0.5f, -0.5f, 1f)); screenPoints.Add(new Vector2(0.5f, -0.5f));
        worldPoints.Add(new Vector3(0.5f, 0.5f, 1f)); screenPoints.Add(new Vector2(0.5f, 0.5f));
        worldPoints.Add(new Vector3(-0.5f, 0.5f, 1f)); screenPoints.Add(new Vector2(-0.5f, 0.5f));
        // Add varied Z just in case
        worldPoints.Add(new Vector3(0f, 0f, 2f)); screenPoints.Add(new Vector2(0f, 0f)); 
        worldPoints.Add(new Vector3(0.2f, 0.2f, 2f)); screenPoints.Add(new Vector2(0.1f, 0.1f)); // z=2 -> half size

        // Build Matrix A
        int N = worldPoints.Count;
        float[,] A = new float[2 * N, 12];
        for(int i=0; i<N; i++)
        {
            Vector3 M = worldPoints[i]; Vector2 m = screenPoints[i];
            A[2*i, 0] = M.x; A[2*i, 1] = M.y; A[2*i, 2] = M.z; A[2*i, 3] = 1;
            A[2*i, 4] = 0;   A[2*i, 5] = 0;   A[2*i, 6] = 0;   A[2*i, 7] = 0;
            A[2*i, 8] = -m.x * M.x; A[2*i, 9] = -m.x * M.y; A[2*i, 10] = -m.x * M.z; A[2*i, 11] = -m.x;
            
            A[2*i+1, 0] = 0;   A[2*i+1, 1] = 0;   A[2*i+1, 2] = 0;   A[2*i+1, 3] = 0;
            A[2*i+1, 4] = M.x; A[2*i+1, 5] = M.y; A[2*i+1, 6] = M.z; A[2*i+1, 7] = 1;
            A[2*i+1, 8] = -m.y * M.x; A[2*i+1, 9] = -m.y * M.y; A[2*i+1, 10] = -m.y * M.z; A[2*i+1, 11] = -m.y;
        }
        
        // Solve
        float[] h = NativeLinearAlgebra.SolveDLT(A);
        Matrix4x4 solvedP = NativeLinearAlgebra.VectorToMatrix(h);
        
        Debug.Log($"Solved Matrix:\n{solvedP}");
        
        // Verify Point 0
        Vector3 tp = worldPoints[0];
        Vector4 proj = solvedP * new Vector4(tp.x, tp.y, tp.z, 1);
        Vector2 res = new Vector2(proj.x / proj.w, proj.y / proj.w);
        Vector2 exp = screenPoints[0];
        
        Debug.Log($"Test Point 0: Expected {exp}, Got {res}, Diff: {Vector2.Distance(exp, res)}");
        
        // Verify Point 5 (Z=2)
        tp = worldPoints[5];
        proj = solvedP * new Vector4(tp.x, tp.y, tp.z, 1);
        res = new Vector2(proj.x / proj.w, proj.y / proj.w);
        exp = screenPoints[5];
        Debug.Log($"Test Point 5: Expected {exp}, Got {res}, Diff: {Vector2.Distance(exp, res)}");
    }
}
