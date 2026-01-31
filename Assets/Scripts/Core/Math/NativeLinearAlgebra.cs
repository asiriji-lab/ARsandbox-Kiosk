using UnityEngine;
using System;

namespace ARSandbox.Core.Math
{
    public static class NativeLinearAlgebra
    {
        public static float[] SolveDLT(float[,] A)
        {
            // Solve Ah = 0.
            // Equivalent to finding eigenvector of A^T*A corresponding to smallest eigenvalue.
            float[,] AtA = Multiply(Transpose(A), A);
            
            // Jacobi SVD on Square Symmetric Matrix (Eigen Decomposition)
            // For symmetric real matrix, SVD V equals Eigenvectors.
            // We want the eigenvector for the smallest eigenvalue.
            
            float[] eigenvalues;
            float[,] eigenvectors;
            JacobiEigen(AtA, out eigenvalues, out eigenvectors);
            
            // Find index of smallest eigenvalue
            int minIdx = 0;
            float minVal = float.MaxValue;
            for(int i=0; i<eigenvalues.Length; i++)
            {
                if (Mathf.Abs(eigenvalues[i]) < minVal)
                {
                    minVal = Mathf.Abs(eigenvalues[i]);
                    minIdx = i;
                }
            }
            
            // Extract that column from eigenvectors
            float[] h = new float[12];
            for(int i=0; i<12; i++) h[i] = eigenvectors[i, minIdx];
            
            return h;
        }

        // Robust Cyclic Jacobi for Symmetric Matrix Eigen Decomposition
        private static void JacobiEigen(float[,] A, out float[] d, out float[,] V)
        {
            int n = A.GetLength(0);
            d = new float[n]; // Eigenvalues
            V = Identity(n);  // Eigenvectors
            
            // Copy Diagonal to d
            for(int i=0; i<n; i++) d[i] = A[i, i];
            
            float[] b = new float[n];
            float[] z = new float[n];
            Array.Copy(d, b, n);
            
            for (int i = 0; i < 50; i++) // 50 iterations usually enough for 12x12
            {
                float sm = 0.0f;
                for (int ip = 0; ip < n - 1; ip++)
                {
                    for (int iq = ip + 1; iq < n; iq++)
                        sm += Mathf.Abs(A[ip, iq]);
                }

                if (sm == 0.0f) return; // Converged

                float tresh = (i < 4) ? 0.2f * sm / (n * n) : 0.0f;

                for (int ip = 0; ip < n - 1; ip++)
                {
                    for (int iq = ip + 1; iq < n; iq++)
                    {
                        float g = 100.0f * Mathf.Abs(A[ip, iq]);
                        if (i > 4 && (float)(Mathf.Abs(d[ip]) + g) == Mathf.Abs(d[ip])
                                  && (float)(Mathf.Abs(d[iq]) + g) == Mathf.Abs(d[iq]))
                        {
                            A[ip, iq] = 0.0f;
                        }
                        else if (Mathf.Abs(A[ip, iq]) > tresh)
                        {
                            float h = d[iq] - d[ip];
                            float t;
                            if ((float)(Mathf.Abs(h) + g) == Mathf.Abs(h))
                            {
                                t = (A[ip, iq]) / h;
                            }
                            else
                            {
                                float theta = 0.5f * h / (A[ip, iq]);
                                t = 1.0f / (Mathf.Abs(theta) + Mathf.Sqrt(1.0f + theta * theta));
                                if (theta < 0.0f) t = -t;
                            }
                            
                            float c = 1.0f / Mathf.Sqrt(1 + t * t);
                            float s = t * c;
                            float tau = s / (1.0f + c);
                            float h_val = t * A[ip, iq];
                            z[ip] -= h_val;
                            z[iq] += h_val;
                            d[ip] -= h_val;
                            d[iq] += h_val;
                            A[ip, iq] = 0.0f;
                            
                            // Rotate pairs
                            for (int j = 0; j < ip; j++) Rotate(A, s, tau, j, ip, j, iq);
                            for (int j = ip + 1; j < iq; j++) Rotate(A, s, tau, ip, j, j, iq);
                            for (int j = iq + 1; j < n; j++) Rotate(A, s, tau, ip, j, iq, j);
                            
                            // Update Eigenvectors
                            for (int j = 0; j < n; j++) Rotate(V, s, tau, j, ip, j, iq);
                        }
                    }
                }
                
                for(int ip=0; ip<n; ip++) {
                    b[ip] += z[ip];
                    d[ip] = b[ip];
                    z[ip] = 0.0f;
                }
            }
        }
        
        private static void Rotate(float[,] M, float s, float tau, int i, int j, int k, int l)
        {
            float g = M[i, j];
            float h = M[k, l];
            M[i, j] = g - s * (h + g * tau);
            M[k, l] = h + s * (g - h * tau);
        }

        public static Matrix4x4 VectorToMatrix(float[] h)
        {
            Matrix4x4 m = new Matrix4x4();
            // DLT Output h corresponds to 3x4 P matrix:
            // Row 0: u equation
            // Row 1: v equation
            // Row 2: w (normalization) equation
            
            // Unity Matrix4x4 expects:
            // Row 0: x clip
            // Row 1: y clip
            // Row 2: z clip (depth)
            // Row 3: w clip (perspective divide)
            
            // So we map DLT Row 2 -> Unity Row 3.
            // And we can set Unity Row 2 (Z) to some default (e.g., Z maps to Z).
            
            m.m00 = h[0]; m.m01 = h[1]; m.m02 = h[2]; m.m03 = h[3];
            m.m10 = h[4]; m.m11 = h[5]; m.m12 = h[6]; m.m13 = h[7];
            
            // Z-buffer row (DLT doesn't solve for Z, so we pass Z through or normalize it)
            // Since we project to 2D screen, Z is technically lost, but for Unity to verify, 
            // we can set it to 0 0 1 0 (Z_out = Z_in).
            m.m20 = 0;    m.m21 = 0;    m.m22 = 1;    m.m23 = 0;
            
            // W-divide row (Crucial!)
            m.m30 = h[8]; m.m31 = h[9]; m.m32 = h[10]; m.m33 = h[11];
            
            return m;
        }

        private static float[,] Multiply(float[,] A, float[,] B)
        {
            int rA = A.GetLength(0); int cA = A.GetLength(1);
            int cB = B.GetLength(1);
            float[,] C = new float[rA, cB]; 
            // Naive O(N^3) fine for N=12
            for (int i = 0; i < rA; i++)
                for (int j = 0; j < cB; j++)
                    for (int k = 0; k < cA; k++)
                        C[i, j] += A[i, k] * B[k, j];
            return C;
        }

        private static float[,] Transpose(float[,] A)
        {
            int r = A.GetLength(0); int c = A.GetLength(1);
            float[,] T = new float[c, r];
            for(int i=0; i<r; i++)
                for(int j=0; j<c; j++)
                    T[j, i] = A[i, j];
            return T;
        }

        private static float[,] Identity(int n)
        {
            float[,] I = new float[n, n];
            for(int i=0; i<n; i++) I[i, i] = 1f;
            return I;
        }
    }
}
