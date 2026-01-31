using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using System;

/// <summary>
/// Handles generating the Grid Mesh and Side Walls for the AR Sandbox using Compute Shaders.
/// </summary>
public class TerrainMeshGenerator : System.IDisposable
{
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh; // Keep for legacy/UI bounds if needed, or remove later
    
    // Grid Data
    private GraphicsBuffer _vertexBuffer;
    private GraphicsBuffer _indexBuffer; // Added for Zero-Copy Rendering
    private ComputeShader _shader;
    private int _kernelIndex;
    private int _width, _height;

    // Procedural Rendering State
    private RenderParams _renderParams;
    private Material _material;
    private Material _wallMaterial;

    // Walls (Legacy CPU for now, or TODO: Port to GPU)
    private GameObject _wallObj;
    private Mesh _wallMesh;

    public TerrainMeshGenerator(ComputeShader shader, int width, int height)
    {
        _shader = shader;
        _width = width;
        _height = height;
        _kernelIndex = _shader.FindKernel("GenerateMesh");

        // Initialize Vertex Buffer
        // Stride: Pos(3) + Normal(3) + UV(2) + UV2(2) = 10 floats = 40 bytes
        _vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, width * height, 10 * sizeof(float));
        
        // Initialize Index Buffer for Grid
        InitIndexBuffer(width, height);
        
        _mesh = new Mesh { name = "TerrainMesh" };
        _mesh.SetIndexBufferParams((width - 1) * (height - 1) * 6, IndexFormat.UInt32); // For bounds/compatibility
    }

    public void Initialize(GameObject root, int resolution, float size, ComputeShader shader)
    {
        _meshFilter = root.GetComponent<MeshFilter>();
        _meshRenderer = root.GetComponent<MeshRenderer>();
        
        // Disable renderer component to prevent double-rendering (we render procedurally)
        if (_meshRenderer != null) 
        {
            _meshRenderer.enabled = false;
            _renderParams.layer = root.layer;
        }
    }

    public void UpdateMesh(ComputeBuffer filteredDepthBuffer, int depthW, int depthH, SandboxSettingsSO settings, Transform rootTransform)
    {
        // EnsureMeshInitialized(settings.MeshResolution, settings.MeshSize); // No longer needed with constructor init

        // 1. Dispatch Compute Shader to generate vertices
        _shader.SetBuffer(_kernelIndex, "_FilteredDepthOutput", filteredDepthBuffer);
        _shader.SetBuffer(_kernelIndex, "_VertexBuffer", _vertexBuffer);
        
        _shader.SetFloat("_MeshSize", settings.MeshSize);
        _shader.SetInt("_MeshResolution", settings.MeshResolution);
        _shader.SetFloat("_MinDepthMM", settings.MinDepth);
        _shader.SetFloat("_MaxDepthMM", settings.MaxDepth);
        _shader.SetFloat("_HeightScale", settings.HeightScale);
        _shader.SetBool("_FlatMode", settings.FlatMode);
        
        // Debug FlatMode
        if (settings.FlatMode && Time.frameCount % 120 == 0) // Log every 2 seconds at 60fps
        {
            Debug.Log($"[TerrainMeshGenerator] FlatMode is ENABLED - terrain should be flat at Y=0");
        }
        
        // Calibration
        _shader.SetVector("_pBL", settings.CalibrationPoints[0]);
        _shader.SetVector("_pTL", settings.CalibrationPoints[1]);
        _shader.SetVector("_pTR", settings.CalibrationPoints[2]);
        _shader.SetVector("_pBR", settings.CalibrationPoints[3]);
        
        // ROI Boundary
        if (settings.BoundaryPoints != null && settings.BoundaryPoints.Length == 4)
        {
            // Convert Vector2[] to Vector4[] for Shader (Unity requires Vector4[] for SetVectorArray)
            Vector4[] boundaryV4 = new Vector4[4];
            boundaryV4[0] = settings.BoundaryPoints[0];
            boundaryV4[1] = settings.BoundaryPoints[1];
            boundaryV4[2] = settings.BoundaryPoints[2];
            boundaryV4[3] = settings.BoundaryPoints[3];
            _shader.SetVectorArray("_BoundaryPoints", boundaryV4);
        }
        
        int totalVerts = settings.MeshResolution * settings.MeshResolution;
        int groups = Mathf.CeilToInt(totalVerts / 64.0f);
        _shader.Dispatch(_kernelIndex, groups, 1, 1);

        // 2. ZERO-COPY RENDERING (Rule 2.D)
        if (_material != null)
        {
            _material.SetBuffer("_VertexBuffer", _vertexBuffer);
            _renderParams.worldBounds = new Bounds(rootTransform.position, new Vector3(settings.MeshSize, settings.HeightScale, settings.MeshSize));
            _renderParams.material = _material; // Ensure correct instance
            // Note: MaterialPropertyBlock for terrain is handled via 'RegisterMaterialProperties' updating a cached block if we wanted,
            // but for RenderPrimitives we usually pass it in RenderParams.
            // For now, let's assume the Global properties or Material properties are enough.
            // If we need dynamic props (like offsets), we should add a 'SetTerrainProps' method.
            
            Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, _indexBuffer, _indexBuffer.count, 0);
        }

        // 3. Walls - STILL NEED READBACK if we want CPU walls (secondary pass)
        // This is a small transfer and only happens if walls are enabled.
        if (settings.ShowWalls)
        {
            float[] vertexData = new float[totalVerts * 10];
            _vertexBuffer.GetData(vertexData);
            UpdateWalls(vertexData, settings.MeshResolution, settings.MeshSize, rootTransform);
        }
        else if (_wallObj != null && _wallObj.activeSelf)
        {
            _wallObj.SetActive(false);
        }
    }

    void UpdateWalls(float[] vertexData, int resolution, float meshSize, Transform parent)
    {
        // Create wall container if needed
        if (_wallObj == null)
        {
            _wallObj = new GameObject("TerrainWalls");
            _wallObj.transform.SetParent(parent);
            _wallObj.transform.localPosition = Vector3.zero;
            _wallObj.transform.localRotation = Quaternion.identity;
            
            var mf = _wallObj.AddComponent<MeshFilter>();
            var mr = _wallObj.AddComponent<MeshRenderer>();
            
            // Copy material from terrain or use cached instance
            if (_wallMaterial != null)
            {
                mr.material = _wallMaterial;
            }
            else if (_meshRenderer != null && _meshRenderer.sharedMaterial != null)
            {
                mr.sharedMaterial = _meshRenderer.sharedMaterial;
            }
            
            _wallMesh = new Mesh();
            _wallMesh.name = "WallMesh";
            mf.mesh = _wallMesh;
        }
        
        _wallObj.SetActive(true);
        
        // Stride: 10 floats per vertex (pos3, normal3, uv2, uv2_2)
        const int stride = 10;
        
        // Each edge has (resolution - 1) quads
        // Each quad needs 4 vertices (not shared for proper normals)
        // 4 edges * (resolution - 1) quads * 4 vertices = totalWallVerts
        int quadsPerEdge = resolution - 1;
        int totalQuads = quadsPerEdge * 4; // 4 edges
        int totalWallVerts = totalQuads * 4; // 4 verts per quad
        int totalWallTris = totalQuads * 6; // 6 indices per quad (2 triangles)
        
        var vertices = new Vector3[totalWallVerts];
        var normals = new Vector3[totalWallVerts];
        var uvs = new Vector2[totalWallVerts];
        var uv2s = new Vector2[totalWallVerts]; // Height data for shader coloring
        var indices = new int[totalWallTris];
        
        int vi = 0; // Vertex index
        int ti = 0; // Triangle index
        
        // Helper: Get vertex position from flat array
        Vector3 GetPos(int x, int z)
        {
            int idx = (z * resolution + x) * stride;
            return new Vector3(vertexData[idx], vertexData[idx + 1], vertexData[idx + 2]);
        }
        
        // Helper: Get UV2 height data from flat array (stored at offset 8-9)
        float GetUV2Height(int x, int z)
        {
            int idx = (z * resolution + x) * stride;
            return vertexData[idx + 8]; // UV2.x contains calculated height
        }
        
        // Generate walls for each edge
        // Bottom edge (z = 0)
        for (int x = 0; x < resolution - 1; x++)
        {
            var topLeft = GetPos(x, 0);
            var topRight = GetPos(x + 1, 0);
            var bottomLeft = new Vector3(topLeft.x, 0, topLeft.z);
            var bottomRight = new Vector3(topRight.x, 0, topRight.z);
            float hL = GetUV2Height(x, 0);
            float hR = GetUV2Height(x + 1, 0);
            
            int baseVert = vi;
            vertices[vi] = bottomLeft; normals[vi] = Vector3.back; uvs[vi] = new Vector2(0, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = bottomRight; normals[vi] = Vector3.back; uvs[vi] = new Vector2(1, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = topLeft; normals[vi] = Vector3.back; uvs[vi] = new Vector2(0, 1); uv2s[vi] = new Vector2(hL, 0); vi++;
            vertices[vi] = topRight; normals[vi] = Vector3.back; uvs[vi] = new Vector2(1, 1); uv2s[vi] = new Vector2(hR, 0); vi++;
            
            indices[ti++] = baseVert; indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 1;
            indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 3;
        }
        
        // Top edge (z = resolution - 1)
        for (int x = 0; x < resolution - 1; x++)
        {
            var topLeft = GetPos(x, resolution - 1);
            var topRight = GetPos(x + 1, resolution - 1);
            var bottomLeft = new Vector3(topLeft.x, 0, topLeft.z);
            var bottomRight = new Vector3(topRight.x, 0, topRight.z);
            float hL = GetUV2Height(x, resolution - 1);
            float hR = GetUV2Height(x + 1, resolution - 1);
            
            int baseVert = vi;
            vertices[vi] = bottomLeft; normals[vi] = Vector3.forward; uvs[vi] = new Vector2(0, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = bottomRight; normals[vi] = Vector3.forward; uvs[vi] = new Vector2(1, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = topLeft; normals[vi] = Vector3.forward; uvs[vi] = new Vector2(0, 1); uv2s[vi] = new Vector2(hL, 0); vi++;
            vertices[vi] = topRight; normals[vi] = Vector3.forward; uvs[vi] = new Vector2(1, 1); uv2s[vi] = new Vector2(hR, 0); vi++;
            
            indices[ti++] = baseVert; indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 2;
            indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 3;
        }
        
        // Left edge (x = 0)
        for (int z = 0; z < resolution - 1; z++)
        {
            var topLeft = GetPos(0, z);
            var topRight = GetPos(0, z + 1);
            var bottomLeft = new Vector3(topLeft.x, 0, topLeft.z);
            var bottomRight = new Vector3(topRight.x, 0, topRight.z);
            float hL = GetUV2Height(0, z);
            float hR = GetUV2Height(0, z + 1);
            
            int baseVert = vi;
            vertices[vi] = bottomLeft; normals[vi] = Vector3.left; uvs[vi] = new Vector2(0, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = bottomRight; normals[vi] = Vector3.left; uvs[vi] = new Vector2(1, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = topLeft; normals[vi] = Vector3.left; uvs[vi] = new Vector2(0, 1); uv2s[vi] = new Vector2(hL, 0); vi++;
            vertices[vi] = topRight; normals[vi] = Vector3.left; uvs[vi] = new Vector2(1, 1); uv2s[vi] = new Vector2(hR, 0); vi++;
            
            indices[ti++] = baseVert; indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 2;
            indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 3;
        }
        
        // Right edge (x = resolution - 1)
        for (int z = 0; z < resolution - 1; z++)
        {
            var topLeft = GetPos(resolution - 1, z);
            var topRight = GetPos(resolution - 1, z + 1);
            var bottomLeft = new Vector3(topLeft.x, 0, topLeft.z);
            var bottomRight = new Vector3(topRight.x, 0, topRight.z);
            float hL = GetUV2Height(resolution - 1, z);
            float hR = GetUV2Height(resolution - 1, z + 1);
            
            int baseVert = vi;
            vertices[vi] = bottomLeft; normals[vi] = Vector3.right; uvs[vi] = new Vector2(0, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = bottomRight; normals[vi] = Vector3.right; uvs[vi] = new Vector2(1, 0); uv2s[vi] = new Vector2(0, 0); vi++;
            vertices[vi] = topLeft; normals[vi] = Vector3.right; uvs[vi] = new Vector2(0, 1); uv2s[vi] = new Vector2(hL, 0); vi++;
            vertices[vi] = topRight; normals[vi] = Vector3.right; uvs[vi] = new Vector2(1, 1); uv2s[vi] = new Vector2(hR, 0); vi++;
            
            indices[ti++] = baseVert; indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 1;
            indices[ti++] = baseVert + 1; indices[ti++] = baseVert + 2; indices[ti++] = baseVert + 3;
        }
        
        // Apply to mesh
        _wallMesh.Clear();
        _wallMesh.vertices = vertices;
        _wallMesh.normals = normals;
        _wallMesh.uv = uvs;
        _wallMesh.uv2 = uv2s; // Height data for shader coloring
        _wallMesh.triangles = indices;
        _wallMesh.RecalculateBounds();
    }

    void InitIndexBuffer(int width, int height)
    {
        int numTilesX = width - 1;
        int numTilesZ = height - 1;
        int numTris = numTilesX * numTilesZ * 2;
        int[] indices = new int[numTris * 3];

        int i = 0;
        for (int z = 0; z < numTilesZ; z++)
        {
            for (int x = 0; x < numTilesX; x++)
            {
                int v0 = z * width + x;
                int v1 = z * width + (x + 1);
                int v2 = (z + 1) * width + x;
                int v3 = (z + 1) * width + (x + 1);

                indices[i++] = v0; indices[i++] = v2; indices[i++] = v1;
                indices[i++] = v1; indices[i++] = v2; indices[i++] = v3;
            }
        }

        _indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, sizeof(int));
        _indexBuffer.SetData(indices);
    }

    public void RegisterMaterialProperties(MaterialPropertyBlock props, bool forWalls)
    {
       if (forWalls && _wallObj != null) 
       {
            var r = _wallObj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in r) renderer.SetPropertyBlock(props);
            return;
       }
       // For GPGPU, we store the material and setup RenderParams
       if (!forWalls)
       {
           _renderParams.matProps = props;
       }
    }

    public void SetMaterial(Material sharedMat)
    {
        if (sharedMat == null) return;

        // 1. Terrain Material (Zero-Copy GPGPU)
        _material = new Material(sharedMat);
        _material.EnableKeyword("_PROCEDURAL_ON");
        _material.name = "Terrain_Procedural_Instance";
        
        // 2. RenderParams for Graphics.RenderPrimitives
        _renderParams = new RenderParams(_material);
        _renderParams.worldBounds = new Bounds(Vector3.zero, new Vector3(10, 5, 10));

        // 3. Wall Material (Standard Pipeline)
        _wallMaterial = new Material(sharedMat);
        _wallMaterial.DisableKeyword("_PROCEDURAL_ON");
        _wallMaterial.name = "Walls_Standard_Instance";

        // Assign to Wall GameObject (if it exists yet)
        if (_wallObj != null && _wallObj.TryGetComponent<MeshRenderer>(out var wr)) wr.material = _wallMaterial;
    }

    // --- INTERNAL INIT ---

    public void Dispose()
    {
        _vertexBuffer?.Release();
        _indexBuffer?.Release();
        if (_wallObj != null) UnityEngine.Object.Destroy(_wallObj);
    }
}
