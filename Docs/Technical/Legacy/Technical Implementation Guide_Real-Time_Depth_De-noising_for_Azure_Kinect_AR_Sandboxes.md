Technical Implementation Guide: Real-Time Depth De-noising for Azure Kinect AR Sandboxes

1. Executive Summary: The Efficiency-Stability Frontier

Time-of-Flight (ToF) depth sensing, as utilized in the Azure Kinect, provides high-resolution 3D point clouds at interactive rates. However, the technology is plagued by specific physical noise profiles—including low signal-to-noise ratios in dark regions and "flying pixel" artifacts at depth discontinuities. In an interactive AR sandbox, maintaining user immersion requires a delicate balance: the "Stability-Latency Frontier." Excessive filtering introduces lag that makes the sand feel "soupy," while insufficient filtering results in shimmering geometry that destroys the illusion of a solid surface.

For professional-grade sandboxes, the recommendation is a bifurcated pipeline: a Burst-optimized CPU Job System when depth data is required for Unity Physics/Collisions, and a GPGPU Compute pipeline for purely visual feedback. While general-purpose vectorized grayscale conversions have been benchmarked to drop from 18ms to 3ms (zyl910331), a full depth de-noising pipeline targeting a 512x512 grid should aim for sub-2ms per-frame overhead to remain within the 60 FPS frame budget. This guide details the implementation of a "Best Bang for Buck" system that achieves rock-solid topography without perceptible trail-lag.

Configuration Comparison

Configuration	Latency Target	Stability Profile	Core Heuristic	Recommended Use Case
Low-Latency	< 2ms	Moderate	Minimal Spatial + High-Sampling	High-speed hand tracking; Nyquist-compliant sampling
High-Stability	4ms - 6ms	Rock-Solid	Adaptive 1€ + Signal-Weighted Bilateral	Static sand calibration; high-precision topography


--------------------------------------------------------------------------------


2. System Architecture: The Hybrid De-noising Pipeline

A single filter is insufficient for AR sandboxes because ToF noise is not uniform. We categorize noise into Axial Noise (fluctuations along the Z-axis) and Lateral Noise (jitter in the XY plane). Critically, axial noise increases proportionally with distance from the sensor, whereas lateral noise remains relatively constant across the depth range (Atif Anwer). A robust system must address these distinct behaviors via a three-stage hybrid flow.

Architectural Blueprint

The "Gold Standard" architecture consists of:

1. Temporal Stage (Adaptive 1€ Filter): A single-pole lowpass filter that modulates its cutoff frequency based on pixel velocity. This handles axial fluctuations without ghosting.
2. Spatial Stage (Bilateral Filter): An edge-preserving pass that smooths flat sand surfaces while maintaining sharp boundaries for hands and tools.
3. Logical/Confidence Stage: A Hysteresis pass using Signal Amplitude and a Laplacian kernel to identify and discard "flying pixels"—erroneous readings occurring where the sensor collects modulated light from both foreground and background.

Data Flow Diagram

1. Raw Ingestion: Capture 16-bit raw depth and 8-bit signal amplitude.
2. Temporal Smoothing: Apply 1€ logic to each pixel, calculating dx/dt to adapt smoothing factors.
3. Spatial Aggregation: Execute a bilateral pass using both range (sigmaR) and spatial (sigmaS) weights.
4. Artifact Rejection: Compare the Laplacian of the depth image against signal amplitude; discard pixels below the confidence threshold.
5. Alignment & Output: Vector deinterleaving to align data to 32-byte boundaries for rendering or physics.


--------------------------------------------------------------------------------


3. The 1€ Filter Module: Velocity-Aware Temporal Stability

The primary challenge in interactive depth sensing is the "Lag vs. Jitter" trade-off. Standard Exponential Moving Averages (EMA) fail because human hand movements are high-frequency, while sand is largely static. The 1€ Filter treats the signal as a single-pole lowpass filter where the alpha value (\alpha) is modulated by the signal’s derivative.

Math for Adaptive Cutoff

The smoothing factor \alpha is derived from the time constant \tau and the sampling interval \Delta t: \alpha = 1 - e^{-\Delta t/\tau} To minimize lag, we modulate \tau based on the rate of change (dx/dt). When movement is fast, we decrease \tau (increasing \alpha) to allow the filter to follow the input. When movement is slow, we increase \tau to eliminate jitter. To prevent quantization errors when \alpha is very small, we maintain the internal state variable S with extra bits of precision (Source: Jason S).

C# Implementation (Unity.Mathematics)

[BurstCompile]
public struct OneEuroJob : IJobParallelFor {
    public float DeltaTime;
    public float Beta; // Velocity coefficient
    public float MinCutoff;
    [ReadOnly] public NativeArray<float> RawInput;
    public NativeArray<float> FilteredState;
    public NativeArray<float> PrevRaw;

    public void Execute(int i) {
        // 1. Calculate Velocity (Derivative)
        float dx = (RawInput[i] - PrevRaw[i]) / DeltaTime;
        float edx = math.lerp(0, dx, 0.1f); // Smooth the derivative

        // 2. Adaptive Cutoff
        float cutoff = MinCutoff + Beta * math.abs(edx);
        float tau = 1.0f / (2.0f * math.PI * cutoff);
        float alpha = 1.0f - math.exp(-DeltaTime / tau);

        // 3. Update State with Quantization Protection
        FilteredState[i] = math.lerp(FilteredState[i], RawInput[i], alpha);
        PrevRaw[i] = RawInput[i];
    }
}



--------------------------------------------------------------------------------


4. Spatial Filtering: Bilateral Logic and Flying Pixel Removal

Spatial filtering is required to eliminate Flying Pixels—artifacts where a depth reading "floats" between a foreground object (a hand) and the background (the sand). Research from UCL confirms that thresholding amplitude alone is insufficient; a combination of geometric and signal-based heuristics is required.

Bilateral Filter Implementation

To prevent the "blurring" of sand edges, the filter must weight neighbors by both spatial distance and depth similarity (range).

// HLSL snippet for GPU implementation
float4 BilateralFilter(float2 uv, float sigmaR, float sigmaS) {
    float centerDepth = _DepthTex.Sample(point_clamp_sampler, uv).r;
    float sumWeights = 0;
    float sumDepth = 0;

    for(int x = -2; x <= 2; x++) {
        for(int y = -2; y <= 2; y++) {
            float2 offset = float2(x, y) * _TexelSize;
            float neighborDepth = _DepthTex.Sample(point_clamp_sampler, uv + offset).r;
            
            float distS = length(float2(x, y));
            float distR = abs(centerDepth - neighborDepth);
            
            // Spatial * Range Weights
            float w = exp(-(distS*distS)/(2*sigmaS*sigmaS) - (distR*distR)/(2*sigmaR*sigmaR));
            sumDepth += neighborDepth * w;
            sumWeights += w;
        }
    }
    return sumDepth / sumWeights;
}


Flying Pixel Logic and NIR Saturation

Flying pixels are identified via a Laplacian of the depth image. However, a "Principal" implementation also accounts for NIR Saturation. Highly specular reflections (e.g., from a monitor or glossy sand) can cause pixels to "fly towards the camera" (UCL Fig 1). We discard pixels if:

1. The Laplacian response exceeds a geometric discontinuity threshold.
2. The Signal Amplitude is too low (insufficient light) OR exceeds a saturation threshold (specular interference).


--------------------------------------------------------------------------------


5. Production Optimization Strategy: SIMD and Burst Efficiency

Processing a 512x512 grid involves >260k per-pixel operations. To avoid bottlenecks, we must respect the CPU's power-of-2 register constraints (16, 32, or 64 bytes).

Vectorization and Memory Alignment

Standard RGB or 16-bit depth data often lacks the alignment required for AVX2/SSE instructions. Loading unaligned memory into registers can cause significant performance penalties or General Protection Faults (GPF).

* The YGroup3Unzip Strategy: For non-power-of-2 data, use a "deinterleaving" approach (zyl910331) to shuffle and unzip data into aligned float8 or float4 vectors.
* 32-Byte Boundaries: Ensure all NativeArray allocations are aligned to 32-byte boundaries to allow for _mm256_load_si256 intrinsics.
* Branching: Avoid if-statements in the kernel. Use math.select to handle depth clamping. This ensures LLVM can auto-vectorize the loop without breaking the instruction pipeline.

Architectural Decision Matrix

* GPU RenderTextures: Mandatory for visual effects (e.g., height-based color mapping, contours).
* CPU Job System: Mandatory if the stabilized depth is consumed by Unity Physics (e.g., a virtual ball rolling on sand).


--------------------------------------------------------------------------------


6. Tuning and Maintenance Manual

Environmental factors—specifically sunlight and sand reflectivity—will necessitate onsite calibration. Sunlight introduces significant axial noise through NIR saturation, while very dark sand reduces signal amplitude.

Exposed Variables Table

Variable	Starting Value	"So What?" Impact
Beta	0.007	Velocity sensitivity. High = less lag, but more "jitter" during fast hand movement.
MinCutoff	1.0 Hz	Static stability. Lower values make the sand feel "heavy" and rock-solid.
RangeSigma	0.05	Edge preservation. Higher values smooth the sand but blur hand silhouettes.
Hysteresis	0.1	Temporal logic. Prevents flickering between valid/invalid states at edges.

Calibration Protocol

1. NIR Baseline: Check for "pixels flying toward camera." This indicates specular reflections. Adjust the Amplitude Saturation threshold to kill these points.
2. Static Pass: Observe the sand without movement. If the topography "shimmers," decrease MinCutoff until the shimmering ceases.
3. Dynamic Pass: Move a hand quickly over the sand. If a "depth trail" or ghosting follows the hand, increase Beta to allow the filter to adapt to high-velocity changes.

By strictly following this adaptive, multi-stage pipeline, developers can transform raw, noisy ToF data into a stable, production-ready AR environment that maintains the physical integrity of the interactive experience.
