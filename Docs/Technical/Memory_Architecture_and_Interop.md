# The Story of Memory: AR Sandbox Architecture

## 1. The Philosophy: "Zero-Copy"
In high-performance graphics applications like this AR Sandbox, **Memory Bandwidth** is the scarcest resource. Every time we move data between the CPU (Processor) and the GPU (Graphics Card), we pay a heavy tax.

### The Old Way (The Tax)
1.  CPU reads depth data from Kinect.
2.  CPU loops through 217,000 pixels (512x424) to create a mesh.
3.  CPU uploads that mesh to the GPU.
4.  GPU renders it.
*Result: Laptop fans spin up, framerate drops, and garbage collection spikes.*

### The New Way (Zero-Copy)
1.  CPU uploads the distinct depth frame **once** to a Compute Buffer.
2.  **GPU** Compute Shader reads that buffer and generates the mesh vertices directly in VRAM.
3.  **GPU** Pixel Shader reads those same vertices to render pixels.
*Result: The data never leaves the GPU. The CPU is free to handle UI and Logic.*

---

## 2. The Danger: "The Stride"
When C# talks to HLSL (Shader Language), they don't exchange objects; they exchange **raw bytes**. They must agree exactly on the shape of the data. This shape is called the **Stride**.

### The "Black Crosses" Incident
We recently encountered a bug where the terrain had a grid of black crosses.
*   **C# Struct**: 40 bytes (Pos + Normal + UV + UV2).
*   **HLSL Struct**: 48 bytes (Pos + Normal + UV + UV2 + **Implicit Padding**).
*   **The Mismatch**: C# sent 40 bytes. The GPU read 48.
    *   Vertex 0: Read bytes 0-47. (Correct)
    *   Vertex 1: Read bytes 48-95. (Actually started at byte 40 of C# data).
    *   Result: Data "drifted" by 8 bytes every vertex. Every few vertices, the UV coordinates (used for color) would drift into the `0.0` range, turning the pixel black.

## 3. The Rules of Allocation

### Rule #1: Respect the 16-Byte Boundary
GPUs start reading data much faster if it aligns to 16 bytes (float4).
*   **Bad**: 40 bytes (10 floats). Not divisible by 16.
*   **Good**: 48 bytes (12 floats). Divisible by 16.

### Rule #2: Explicit Padding
Don't let the compiler guess. If you need 40 bytes of data, add 8 bytes of "Padding" to reach 48.
```csharp
struct TerrainVertex {
    Vector3 pos;    // 12 bytes
    Vector3 norm;   // 12 bytes
    Vector2 uv;     // 8 bytes
    Vector2 uv2;    // 8 bytes
    Vector2 padding;// 8 bytes (Explicit!)
} // Total: 48 bytes
```

### Rule #3: Document the Math
Always write the stride calculation in comments near the buffer creation.
`// Stride: 12 floats * 4 bytes = 48 bytes`

---

## 4. Memory Lifecycle (The Sentinel)
We use `GraphicsBuffer` and `ComputeBuffer` which are **Unmanaged Memory**.
*   **Creation**: We allocate VRAM in `Start()`.
*   **Destruction**: We **MUST** call `.Release()` or `.Dispose()` in `OnDestroy()`.
*   **Leak**: If we forget, that VRAM is gone until Unity closes. The `unity-memory-sentinel` skill watches for this.
