using UnityEngine;

public class SimulatedDepthProvider : MonoBehaviour, IDepthProvider
{
    [Header("Simulation Settings")]
    [Tooltip("Resolution of the simulation grid")]
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

    private ushort[] _depthData;
    private float _noiseOffsetX;
    private float _noiseOffsetZ;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Width => Resolution;
    public int Height => Resolution;

    public void Initialize()
    {
        _depthData = new ushort[Resolution * Resolution];
        _isRunning = true;
    }

    public void Shutdown()
    {
        _isRunning = false;
    }

    public ushort[] GetDepthData()
    {
        if (!_isRunning) return null;

        GenerateFrame();
        return _depthData;
    }

    public string GetDeviceName()
    {
        return "Simulated Perlin Noise";
    }

    void GenerateFrame()
    {
        // Update offsets
        _noiseOffsetX += Time.deltaTime * MoveSpeed;
        _noiseOffsetZ += Time.deltaTime * MoveSpeed * 0.5f;

        for (int z = 0; z < Resolution; z++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                int i = z * Resolution + x;

                // Base Coordinates
                float xCoord = (float)x * NoiseScale + _noiseOffsetX;
                float zCoord = (float)z * NoiseScale + _noiseOffsetZ;

                // Layer 1: Big Hills
                float noise1 = Mathf.PerlinNoise(xCoord, zCoord);
                
                // Layer 2: Smaller Detail
                float noise2 = Mathf.PerlinNoise(xCoord * 2f, zCoord * 2f) * 0.5f;
                
                // Combine
                float finalNoise = Mathf.Lerp(noise1, (noise1 + noise2) * 0.66f, DetailAmount);
                
                // Modifiers
                finalNoise = Mathf.Pow(finalNoise, Steepness);
                finalNoise *= Amplitude;
                
                float normalizedHeight = finalNoise + HeightOffset;
                normalizedHeight = Mathf.Clamp01(normalizedHeight);

                // Convert 0..1 height back to Depth (mm)
                // Note: Depth is "Distance from sensor", so Higher Ground = Lower Depth Value
                // Height 1.0 (Peak) -> MinDepthMM
                // Height 0.0 (Floor) -> MaxDepthMM
                
                float depthVal = Mathf.Lerp(MaxDepthMM, MinDepthMM, normalizedHeight);
                _depthData[i] = (ushort)depthVal;
            }
        }
    }

    void OnDestroy()
    {
        Shutdown();
    }
}
