using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Handles depth filtering (1-Euro) using Compute Shaders.
/// </summary>
public class DepthProcessor : System.IDisposable
{
    private ComputeShader _shader;
    private int _kernelIndex;
    
    // Buffers
    private ComputeBuffer _rawDepthInput;
    private ComputeBuffer _filterState;
    private ComputeBuffer _filteredDepthOutput; // Consumed by MeshGenerator

    // CPU Shadow buffer for legacy Debug Texture
    private uint[] _cpuDebugBuffer; 
    private ushort[] _resultBuffer; // Legacy ushort return
    
    private int _resolution = -1;
    private int _width, _height;

    public ComputeBuffer GetOutputBuffer() => _filteredDepthOutput;

    // Legacy support for ARSandboxController debug texture
    public ushort[] GetResultBuffer() => _resultBuffer;

    public DepthProcessor(ComputeShader shader)
    {
        _shader = shader;
        _kernelIndex = _shader.FindKernel("FilterDepth");
    }

    public void Process(ushort[] inputData, int width, int height, SandboxSettingsSO settings, bool isSimulation)
    {
        if (_shader == null) return;
        
        int totalPix = width * height;
        EnsureBuffers(totalPix, width, height);

        // 1. Upload Data
        // Optimization: We could use a compute shader to unpack ushort[] if we uploaded it as ByteAddressBuffer,
        // but SetData with an int[] or mapped buffer is standard. 
        // Since input is ushort[], we need to promote to int/uint for SetData or use a native array alias?
        // ComputeBuffer.SetData supports Array. 
        // Problem: SetData<ushort> is not directly supported for StructuredBuffer<uint> layout unless we pack.
        // Simplest valid path: Convert to int[] or uint[] on CPU or use ByteAddressBuffer. 
        // For now, let's map the ushort[] to a temp int[] array or just loop copy.
        // ACTUALLY: Let's use a temporary NativeArray<int> or just use SetData with the ushort array 
        // and change the shader to accept StructuredBuffer<half> maybe? No.
        // Fast path: reuse a persistent int[] buffer.
        
        // Convert ushort -> uint for upload (Unity ComputeBuffer doesn't like ushort direct upload to uint buffer usually, stride mismatch)
        // Stride of uint is 4 bytes. ushort is 2.
        // We will create a robust int array.
        if (_cpuDebugBuffer == null || _cpuDebugBuffer.Length != totalPix) _cpuDebugBuffer = new uint[totalPix];
        
        // TODO: Optimize this CPU loop (Burst it?) -> For now, simple loop.
        for(int i=0; i<totalPix; i++) _cpuDebugBuffer[i] = inputData[i];
        
        _rawDepthInput.SetData(_cpuDebugBuffer);

        // 2. Set Params
        _shader.SetFloat("_DeltaTime", Time.deltaTime);
        _shader.SetFloat("_MinCutoff", settings.MinCutoff);
        _shader.SetFloat("_Beta", settings.Beta);
        _shader.SetFloat("_HandThreshold", settings.HandThreshold);
        _shader.SetFloat("_MaxDepth", settings.MaxDepth);
        _shader.SetInt("_Width", width);
        _shader.SetInt("_Height", height);

        // 3. Dispatch
        _shader.SetBuffer(_kernelIndex, "_RawDepthInput", _rawDepthInput);
        _shader.SetBuffer(_kernelIndex, "_FilterState", _filterState);
        _shader.SetBuffer(_kernelIndex, "_FilteredDepthOutput", _filteredDepthOutput);

        int groups = Mathf.CeilToInt(totalPix / 64.0f);
        _shader.Dispatch(_kernelIndex, groups, 1, 1);

        // 4. Readback for Legacy Debug (Optional: Toggle this?)
        // Retrieve data for the Debug Texture and legacy consumption
        // In GPGPU phase, we ideally STOP doing this, but the Controller expects it for `GetRawDepthTexture()`.
        _filteredDepthOutput.GetData(_cpuDebugBuffer);
        
        // Convert back to ushort for legacy compatibility
        for(int i=0; i<totalPix; i++) _resultBuffer[i] = (ushort)_cpuDebugBuffer[i];
    }

    private void EnsureBuffers(int totalPix, int w, int h)
    {
        if (_resolution == totalPix && _filteredDepthOutput != null && _filteredDepthOutput.IsValid()) return;

        Dispose();

        _width = w; _height = h;
        _resolution = totalPix;

        // 4 bytes per int
        _rawDepthInput = new ComputeBuffer(totalPix, 4); 
        
        // State: 2 floats per pixel (Filtered, PrevRaw) -> 8 bytes/pixel
        _filterState = new ComputeBuffer(totalPix * 2, 4); 
        
        // Output: 1 uint per pixel (4 bytes)
        _filteredDepthOutput = new ComputeBuffer(totalPix, 4);

        _resultBuffer = new ushort[totalPix];
    }

    public void Dispose()
    {
        _rawDepthInput?.Release();
        _filterState?.Release();
        _filteredDepthOutput?.Release();
    }
}
