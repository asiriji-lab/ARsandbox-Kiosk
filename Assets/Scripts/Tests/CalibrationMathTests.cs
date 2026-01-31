using NUnit.Framework;
using UnityEngine;
using ARSandbox.Core.Math;
using System.Collections.Generic;

namespace ARSandbox.Tests
{
    public class CalibrationMathTests
    {
        [Test]
        public void TestDLTSolver_Identity()
        {
            // Scenario: Projector is at (0,0,0) facing +Z. 
            // Screen is at Z=1, covering -1 to +1 in X and Y.
            // A point at (0,0,1) should map to center of screen (0.5, 0.5) if UV is normalized?
            // Let's use a simpler known transformation.
            // If World = Screen (Identity Projection), then (x,y,z) -> (x,y).
            
            // Let's simulate a standard Camera Projection Matrix behavior manually.
            List<Vector3> worldPoints = new List<Vector3>();
            List<Vector2> screenPoints = new List<Vector2>();

            // Synthetic Ground Truth: P = Identity (mostly)
            // u = x/z, v = y/z ?
            // Let's assume a strictly planar homography-like setup for simplicity, 
            // OR just feed it 6 random points and their EXPECTED projections from a known matrix.
            
            Matrix4x4 knownP = Matrix4x4.Perspective(60, 1.0f, 0.1f, 100f);
            // We need 6 points to solve DLT reliably.
            
            // Generate synthetic data
            for(int i=0; i<8; i++)
            {
                Vector3 p = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(2f, 5f));
                Vector3 screenPos = knownP.MultiplyPoint(p);
                // Convert Normalized Device Coordinates (-1 to 1) to UV (0 to 1)
                Vector2 uv = new Vector2((screenPos.x + 1) * 0.5f, (screenPos.y + 1) * 0.5f);
                
                worldPoints.Add(p);
                screenPoints.Add(uv);
            }
            
            // Build Matrix A
            int N = worldPoints.Count;
            float[,] A = new float[2 * N, 12];
             for(int i=0; i<N; i++)
            {
                Vector3 M = worldPoints[i]; Vector2 m = screenPoints[i];
                // Normals
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
            
            // Verify
            // NOTE: DLT solves up to a scale factor. P and k*P are the same.
            // So we check if M projects to m using solvedP.
            
            Vector3 testPoint = worldPoints[0];
            Vector2 expectedUV = screenPoints[0];
            
            Vector4 projected = solvedP * new Vector4(testPoint.x, testPoint.y, testPoint.z, 1);
            Vector2 resultUV = new Vector2(projected.x / projected.w, projected.y / projected.w);
            
            // Debug.Log($"Expected: {expectedUV}, Got: {resultUV}");
            // Assert.IsTrue(Vector2.Distance(expectedUV, resultUV) < 0.05f); // Tolerance
            
            float dist = Vector2.Distance(expectedUV, resultUV);
            Debug.Log($"[Test] Expected: {expectedUV.ToString("F3")}, Got: {resultUV.ToString("F3")}, Dist: {dist}");
            
            // If distance is huge, maybe matrix is zero?
            Debug.Log($"[Test] Solved Matrix:\n{solvedP}");
            
            Assert.IsTrue(dist < 0.1f, $"Projection mismatch! Dist: {dist}");
        }
    }
}
