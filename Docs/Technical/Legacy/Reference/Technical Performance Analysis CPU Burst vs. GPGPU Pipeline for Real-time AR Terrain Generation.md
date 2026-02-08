Technical Performance Analysis: CPU Burst vs. GPGPU Pipeline for Real-time AR Terrain Generation

1. Executive Summary

In high-resolution interactive systems such as Augmented Reality (AR) sandboxes, the architectural choice between CPU-side processing and GPGPU (General-Purpose GPU) pipelines determines the threshold between seamless immersion and disruptive latency. Strategic architectural decisions must prioritize minimizing data movement over raw compute speed. As we scale resolution targets toward high-fidelity 512x512 grids, the overhead of synchronizing data between the processor and the graphics card has surpassed actual calculation time as our primary bottleneck.

Current synchronization overhead, specifically the "Memory Sync" headache caused by ComputeBuffer.GetData readbacks, renders the current C# Job System/Burst architecture non-viable for the 512x512 target resolution. While the Burst compiler optimizes mathematical operations exceptionally well, it cannot bypass the physical "Round Trip" latency required to bring generated mesh data back to the CPU for standard rendering. We must commit to a GPU-Resident pipeline using the Graphics.RenderPrimitives API to reclaim the 30ms lost to PCIe bus contention and eliminate the CPU-to-GPU synchronization bottleneck entirely.

2. Current State Analysis: Unity C# Job System & Burst Compiler

The Unity C# Job System remains the current industry standard for manageable complexity and "threading simplicity." By utilizing the Burst Compiler, we achieve near-native performance for math-heavy tasks; however, Burst has little to no effect on memory access patterns or data layouts, which are currently sabotaging our throughput.

At a standard 200x200 grid resolution (40,000 vertices), the current system maintains acceptable performance. Scaling to 512x512 (262,144 vertices) exposes the critical failure of standard mesh constructors, which incur update lags as high as 55ms—well beyond the 16.6ms budget for 60 FPS. While pivoting to the Advanced Mesh API (MeshData) reduces this lag to roughly 4ms, the granular cost of SetVertexBufferData (~0.61ms) and SetIndexBufferData (~0.45ms) contributes to a total update time of 1.1ms per chunk. While seemingly fast, this is an order of magnitude slower than the actual data generation in Burst (~0.04ms).

Furthermore, the CPU implementation of the 1 Euro Filter and Spatial Smoothing is hindered by our current NodeStruct data layout. At 84 bytes, this struct exceeds the standard 64-byte CPU cache line. This mismatch guarantees that every single node access spans two cache lines or triggers a cache miss, effectively halving the CPU's theoretical throughput during smoothing operations. This cache-miss risk, combined with main-thread overhead, defines the hard threshold where CPU processing becomes the bottleneck for real-time AR.

3. Proposed Architecture: GPGPU & Procedural Rendering

To eliminate the "Round Trip" latency that plagues CPU-based solutions, we propose a GPGPU refactoring to a "GPU-Resident" pipeline. This architecture treats the GPU as the primary site for both data generation and visualization, ensuring that vertex data never leaves VRAM.

In this pipeline, the traditional reliance on the now-obsolete Graphics.DrawProcedural is bypassed in favor of Graphics.RenderPrimitives (or RenderPrimitivesIndexed for optimized index buffer support). We eliminate the ComputeBuffer.GetData readback entirely, allowing the vertex shader to read directly from the ComputeBuffer where the terrain was generated. This approach leverages ComputeBuffer.CopyCount and IndirectArguments to manage dynamic vertex counts, enabling the GPU to tell itself how many instances to draw without CPU intervention.

By keeping depth textures and vertex buffers entirely on the GPU, we expect performance gains of at least 2x in-editor. More importantly, it shifts the system's limitation from processor cache-misses to the physical limits of the PCIe bus throughput.

4. The PCIe Bottleneck: Analyzing "Round Trip" Latency

In real-time sensor-driven applications, PCIe bandwidth saturation is a strategic risk that introduces a hard floor on latency. Every "Memory Sync" operation consumes limited bandwidth on the PCIe bus, causing significant stalls regardless of GPU compute power.

Quantifying the bottleneck is critical: PCIe 3.0 provides roughly 16 GB/s of unidirectional bandwidth on a x16 slot. At 60 FPS, this translates to only ~266MB per frame. When processing high-resolution meshes alongside sensor data and textures, we operate near the saturation point. Evidence from BlazeFace/Barracuda implementations shows that in complex scenes with 1 million vertices, these readback spikes can jump from 2ms to 30ms. Such spikes are unacceptable for AR, as they exceed the entire frame budget and cause severe tracking misalignment.

A Compute Shader refactor bypasses the need for the CPU to ever "see" the high-resolution mesh data. The high-bandwidth internal memory of the GPU handles all generation and smoothing, leaving the PCIe bus free for critical sensor command dispatches.

5. Processing Neighbor-Aware Kernels & Vertex Logic

Spatial smoothing and temporal stability are essential for cleaning noise-heavy sensor data, but implementing "neighbor-aware" kernels presents a divergence in architectural efficiency.

While the CPU handles complex branching logic with better branch prediction, the GPU suffers from SIMD divergence, where threads in a warp are disabled while others execute divergent branches. However, for spatial blurs and 1 Euro Filters, the raw parallel throughput of the GPU outweighs these costs. On a 512x512 grid, the GPU can process neighbor lookups across thousands of threads simultaneously, whereas the CPU version struggles with the aforementioned 84-byte struct cache misses.

When scaling to 512x512 or higher, the generation of UVs and Normals frequently causes "Stutter" (frametime spikes) on the CPU due to the main-thread cost of applying mesh updates or triggering Garbage Collection (GC). The GPGPU architecture handles these effectively by utilizing InterlockedAdd in the shader to prevent race conditions during vertex increments, ensuring a stable framerate that is isolated from CPU-side logic stalls.

6. Architectural Comparison Matrix

The following matrix summarizes the multi-dimensional trade-offs necessary for the final architectural decision:

Category	CPU Burst/Jobs	GPU Compute Shaders
Throughput	High at 40k; fails at 260k+ (55ms)	Massive (Scales to 10M+ vertices)
Latency	High (Readback spikes to 30ms)	Ultra-Low (GPU-Resident)
Complexity	Manageable C# debugging	Higher; SIMD divergence risks
Stability	Risk of GC spikes & cache misses	Stable; eliminates PCIe Round Trip
Memory Layout	Chunky Structs (Cache Misses)	Parallel Buffers (SoA Optimized)

Strategic Comparison of Trade-offs

CPU Burst/Jobs

* Pros: Better for complex branching; lower entry barrier for standard C# developers.
* Cons: 1.1ms update cost per chunk is deceptive; throughput is halved by 84-byte struct cache misses; restricted by the 16 GB/s PCIe 3.0 bottleneck during mesh upload.

GPU Compute Shaders

* Pros: Eliminates the "Memory Sync" readback; Graphics.RenderPrimitives is roughly 2x faster; enables the GPU to manage its own vertex counts via IndirectArguments.
* Cons: Requires SoA (Structure of Arrays) data management; debugging is significantly more difficult than C#; requires careful kernel sizing to maximize occupancy.

7. Phase 2 Recommendations: Optimization Roadmap

To ensure long-term scalability and stabilize the 512x512 resolution target, we will execute the following three-step roadmap:

1. Immediate Buffer Optimization: Switch the current pipeline to the Advanced Mesh API (MeshData) and increase the asyncUploadBufferSize to 16MB. We must carefully manage the asyncUploadPersistentBuffer to avoid memory fragmentation within the ring buffer.
  * So What? This prevents the main thread from stalling during mesh hand-offs and minimizes the risk of unpredictable memory allocations during runtime.
2. GPGPU Migration: Refactor terrain generation and the 1 Euro Filter into Compute Shader kernels, implementing Graphics.RenderPrimitivesIndexed to eliminate the GetData readback entirely.
  * So What? This removes the 30ms latency spikes observed in million-vertex scenarios, ensuring the 512x512 grid remains well within the 16.6ms frame budget.
3. Unity 6 Integration: Implement the Batch Renderer Group (BRG) technology and the new "GPU Resident Drawer" system for secondary terrain features (foliage, instanced rocks).
  * So What? This leverages GPU-driven culling and occlusion culling, potentially boosting visualization performance from 30 FPS to over 200 FPS by reducing the CPU cost of collecting and batching renderers.

Technical Path Forward: The transition to a GPGPU-resident pipeline is the only viable path to achieve the sub-millisecond frametimes required for professional AR terrain generation. Minimizing PCIe bus contention through the elimination of the "Round Trip" is now our highest architectural priority.
