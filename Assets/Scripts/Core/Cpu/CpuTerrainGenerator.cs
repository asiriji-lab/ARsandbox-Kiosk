using UnityEngine;
using ARSandbox.Core;
using System.Collections.Generic;

namespace ARSandbox.Core.Cpu
{
    public class CpuTerrainGenerator : ITerrainGenerator
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private Vector2[] _uv2s; // Store height for shader gradients
        private int[] _triangles;
        
        private int _currentRes = -1;

        public void Initialize(GameObject root, int resolution, float size)
        {
            _meshFilter = root.GetComponent<MeshFilter>();
            _meshRenderer = root.GetComponent<MeshRenderer>();
            
            // Ensure renderer is ENABLED for CPU mode
            if (_meshRenderer != null) _meshRenderer.enabled = true;

            if (_mesh == null) {
                _mesh = new Mesh();
                _mesh.name = "CpuTerrain";
                _mesh.MarkDynamic(); // Optimize for frequent updates
                _meshFilter.mesh = _mesh;
            }
            
            InitBuffers(resolution, size);
        }

        private void InitBuffers(int res, float size)
        {
            if (_currentRes == res) return;
            _currentRes = res;

            int vertCount = res * res;
            _vertices = new Vector3[vertCount];
            _uvs = new Vector2[vertCount];
            _uv2s = new Vector2[vertCount];
            
            // Grid Setup (Static X/Z)
            for(int z=0; z<res; z++) {
                for(int x=0; x<res; x++) {
                    int i = z*res + x;
                    float u = (float)x / (res-1);
                    float v = (float)z / (res-1);
                    _uvs[i] = new Vector2(u, v);
                    _vertices[i] = new Vector3((u-0.5f)*size, 0, (v-0.5f)*size);
                }
            }
            
            // Triangles
            int tris = (res-1) * (res-1) * 6;
            _triangles = new int[tris];
            int t = 0;
            for(int z=0; z<res-1; z++) {
                for(int x=0; x<res-1; x++) {
                    int v0 = z*res + x;
                    int v1 = v0 + 1;
                    int v2 = (z+1)*res + x;
                    int v3 = v2 + 1;
                    
                    _triangles[t++] = v0; _triangles[t++] = v2; _triangles[t++] = v1;
                    _triangles[t++] = v1; _triangles[t++] = v2; _triangles[t++] = v3;
                }
            }
            
            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.uv = _uvs;
            _mesh.uv2 = _uv2s;
            _mesh.triangles = _triangles;
        }

        public void UpdateMesh(TerrainFrameData frameData)
        {
            // CPU Generator pulls local array
            ushort[] depthData = frameData.Source.GetResultBuffer();
            if (depthData == null) return;

            int depthW = frameData.DepthWidth;
            int depthH = frameData.DepthHeight;
            SandboxSettingsSO settings = frameData.Settings;

            // Resample depth to mesh resolution
            int res = settings.MeshResolution;
            if (res != _currentRes) InitBuffers(res, settings.MeshSize);
            
            float minD = settings.MinDepth;
            float maxD = settings.MaxDepth;
            float heightScale = settings.HeightScale;
            float range = Mathf.Max(1, maxD - minD);
            
            // Naive nearest neighbor or bilinear scaling
            for(int i=0; i<_vertices.Length; i++)
            {
                int x = i % res;
                int z = i / res;
                
                // Map mesh UV to depth coords
                float u = (float)x / (res-1);
                float v = (float)z / (res-1);
                
                int dx = Mathf.Clamp((int)(u * depthW), 0, depthW-1);
                int dy = Mathf.Clamp((int)(v * depthH), 0, depthH-1);
                
                ushort dVal = depthData[dy * depthW + dx];
                
                float y = 0;
                float realElevation = 0;
                
                if (dVal > 0)
                {
                   float norm = (maxD - dVal) / range;
                   y = Mathf.Clamp(norm, -0.2f, 1.2f) * heightScale;
                   
                   // UV2.x needs PHYSICAL elevation (Meters) for Shader Gradient & Water
                   // dVal, maxD are in mm.
                   realElevation = (maxD - dVal) * 0.001f * heightScale;
                }
                
                _vertices[i].y = y;
                _uv2s[i].x = realElevation; 
            }
            
            _mesh.vertices = _vertices;
            _mesh.uv2 = _uv2s;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        public void SetMaterial(Material mat)
        {
            if (_meshRenderer != null) _meshRenderer.sharedMaterial = mat;
        }

        public Material GetMaterial()
        {
            return _meshRenderer != null ? _meshRenderer.material : null;
        }

        public void Draw(SandboxSettingsSO settings, Transform root) {
            // Standard renderer handles this automatically
        }

        public void Dispose()
        {
            if (_mesh != null) Object.Destroy(_mesh);
        }
    }
}
