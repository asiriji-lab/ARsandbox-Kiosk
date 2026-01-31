using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public class SimulatedDepthProvider : MonoBehaviour, IDepthProvider
{
    [Header("Simulation Settings")]
    public int Resolution = 512;
    [Range(0.01f, 0.5f)]
    public float NoiseScale = 0.1f;
    [Range(0f, 2f)]
    public float MoveSpeed = 0.5f;
    [Range(0f, 1f)]
    public float DetailAmount = 0.5f;
    [Range(0.1f, 4f)]
    public float Steepness = 1.0f;
    [Range(0.1f, 3f)]
    public float Amplitude = 1.0f;
    [Range(-2f, 2f)]
    public float HeightOffset = 0.0f;

    [Header("Output Range (mm)")]
    public float MinDepthMM = 500f;
    public float MaxDepthMM = 1500f;

    [Header("Debug")]
    public bool SimulateHang = false;

    private NativeArray<ushort> _depthBuffer;
    private ushort[] _depthDataManaged; 
    private float _noiseOffsetX;
    private float _noiseOffsetZ;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Width => Resolution;
    public int Height => Resolution;

    public void Initialize()
    {
        if (_depthBuffer.IsCreated) _depthBuffer.Dispose();
        _depthBuffer = new NativeArray<ushort>(Resolution * Resolution, Allocator.Persistent);
        _depthDataManaged = new ushort[Resolution * Resolution];
        _isRunning = true;
    }

    public void Shutdown()
    {
        _isRunning = false;
        if (_depthBuffer.IsCreated) _depthBuffer.Dispose();
    }

    public ushort[] GetDepthData()
    {
        if (!_isRunning || SimulateHang) return null;
        GenerateFrame();
        _depthBuffer.CopyTo(_depthDataManaged);
        return _depthDataManaged;
    }

    public string GetDeviceName() => "Simulated (Burst Optimized)";

    [BurstCompile]
    struct SimNoiseJob : IJobParallelFor
    {
        public int Resolution;
        public float NoiseScale;
        public float OffsetX, OffsetZ;
        public float DetailAmount, Steepness, Amplitude, HeightOffset;
        public float MinDepth, MaxDepth;
        
        [WriteOnly] public NativeArray<ushort> Output;

        public void Execute(int i)
        {
            int x = i % Resolution;
            int z = i / Resolution;

            float xCoord = (float)x * NoiseScale + OffsetX;
            float zCoord = (float)z * NoiseScale + OffsetZ;

            // Simplex noise is much faster than Mathf.PerlinNoise
            float noise1 = (noise.snoise(new float2(xCoord, zCoord)) + 1.0f) * 0.5f;
            float noise2 = (noise.snoise(new float2(xCoord * 2.0f, zCoord * 2.0f)) + 1.0f) * 0.5f;
            
            float finalNoise = math.lerp(noise1, (noise1 + noise2) * 0.66f, DetailAmount);
            finalNoise = math.pow(finalNoise, Steepness) * Amplitude + HeightOffset;
            finalNoise = math.saturate(finalNoise);

            // HARD-SHELL CLAMP: Prevent simulation from ever outputting 0 (shadow)
            float depthVal = math.lerp(MaxDepth, MinDepth, finalNoise);
            Output[i] = (ushort)(math.clamp(depthVal, 1.0f, 65000.0f) + 0.5f);
        }
    }

    void GenerateFrame()
    {
        _noiseOffsetX = (_noiseOffsetX + Time.deltaTime * MoveSpeed) % 1000f;
        _noiseOffsetZ = (_noiseOffsetZ + Time.deltaTime * MoveSpeed * 0.5f) % 1000f;

        var job = new SimNoiseJob
        {
            Resolution = Resolution,
            NoiseScale = NoiseScale,
            OffsetX = _noiseOffsetX, OffsetZ = _noiseOffsetZ,
            DetailAmount = DetailAmount, Steepness = Steepness,
            Amplitude = Amplitude, HeightOffset = HeightOffset,
            MinDepth = MinDepthMM, MaxDepth = MaxDepthMM,
            Output = _depthBuffer
        };

        job.Schedule(Resolution * Resolution, 64).Complete();
    }

    void OnDestroy() => Shutdown();
    void OnDisable() => Shutdown();
}
