Seamless Sand Rendering in Unity URP: A Technical Guide for Dynamic Curved Meshes
1. The UV Dilemma in Dynamic Terrain Rendering
In high-fidelity real-time environments, particularly those utilizing dynamic geometry from Azure Kinect sensors or procedural heightmaps, a strategic conflict exists between traditional UV mapping and the fluid nature of generated meshes. Traditional UV unwrapping is designed for static assets with predictable topology; when applied to sand dunes or sensor-driven surfaces that deform at runtime, this paradigm collapses. Steep curves and real-time mesh updates create a critical failure point for immersion, as the texture loses its relationship with the surface's physical area.
This failure is defined by "UV Stretching," a technical artifact where the mismatch between 3D surface area and 2D UV representation results in massive texel density loss. On steep inclines, a minimal sliver of UV space is elongated across a large vertical area, causing textures to appear blurry and distorted. Furthermore, real-time updated meshes present a prohibitive computational bottleneck; CPU-based UV unwrapping is an O(N) operation that cannot be performed at frame rates required for interactive applications. To maintain visual integrity, technical artists must move away from pre-authored coordinates and standardize on procedural spatial projections.
2. Triplanar Mapping: The Procedural Solution
Triplanar mapping provides the necessary paradigm shift to maintain uniform texel density across arbitrary geometry. By generating texture coordinates procedurally within the fragment shader based on world-space orientation, it ensures that regardless of mesh deformation, the texture remains consistent and undistorted.
Mechanics of the Three-Axis Projection
The logic requires sampling the texture three separate times—once for each orthogonal direction—using the following world-space planes:
• YZ Plane (X-Axis): Projects along the side faces.
• XZ Plane (Y-Axis): Projects from top-down (standard terrain mapping).
• XY Plane (Z-Axis): Projects along the front/back faces.
The Linear vs. Non-Linear Weighting Debate
Standard linear blending typically results in "ghosting" artifacts and blurry overlaps at 45-degree angles. Standardize instead on Casey Primozic’s non-linear power-function approach. By raising weights to a high power (e.g., pow(weights, 8)), the shader sharpens the transition, driving small weights toward zero and the dominant weight toward one.
Strategic Optimization: This non-linear scaling enables conditional branching. Command the use of HLSL-style logic to skip texture samples if a weight is below a negligible threshold (e.g., if (weight.x > 0.01)). This significantly reduces the fragment shader's execution cost in areas where only one or two axes are visible.
Weight Normalization Steps
To ensure consistent surface brightness, normalize weights so they sum to 1.0:
1. Initialize Absolute Normals: Take the abs() of the world-space normal to treat all directions equally.
2. Apply Sharpness: Raise each component to the chosen power (default 8.0).
3. Sum Components: Calculate the sum (Sum=x 
p
 +y 
p
 +z 
p
 ).
4. Normalize: Divide the powered vector by the sum to ensure x+y+z=1.0.
3. Strategic Analysis of Coordinate Spaces
The choice of coordinate space determines how textures react to mesh deformation and translation.
Feature
World Space
Object Space
Alignment
Global Origin; fixed to the world.
Local Pivot; fixed to the mesh.
Movement
Causes "sliding" or "swimming" artifacts.
Texture moves with the mesh pivot.
Continuity
Perfect seams between adjacent objects.
Seams appear at object boundaries.
For meshes that deform via Kinect but remain static in the world, World Space is the superior choice. It provides a strategic advantage by hiding "vertex jitter" common in sensor-driven data. Because the texture is tied to the world, the mesh "vibrates" through the static texture, which is far less perceptible than the texture vibrating with the mesh.
If a mesh must move and deform, utilize a Local World Space compromise: calculate UVs as WorldPosition - ObjectPosition. This maintains world-axis alignment while effectively "parenting" the projection to the object's transform.
4. Logic Guide for Shader Graph Construction
Implementation must prioritize modular control over built-in Unity nodes to allow for advanced blending and optimization.
Weight Generation Logic
• Normal Vector Initialization: Use a Normal Vector node in World Space.
• Absolute Orientation: Pass to an Absolute node to remove sign data.
• Sharpening: Use a Power node with a property-driven exponent (8.0).
• Normalization: Divide the powered result by the sum of its components.
Texture Sampling & Swizzling
Command precise swizzle pairs to prevent the 90-degree rotation of textures on side planes. Use the world-space position (multiplied by tiling) as follows:
• X-Axis (YZ): Swizzle ZY for the UV input.
• Y-Axis (XZ): Swizzle XZ for the UV input.
• Z-Axis (XY): Swizzle XY for the UV input.
Reoriented Normal Mapping (RNM)
Linear blending of normal maps results in flat lighting. Standardize on Reoriented Normal Mapping (RNM). Unlike "Whiteout" blending, which simply adds derivatives, RNM rotates the high-frequency grain-detail normal to follow the base curvature of the dune. This preserves lighting integrity and specular highlights on steep slopes where standard blending typically fails.
5. Sand Aesthetics: Achieving High-Fidelity Realism
Realism in sand requires a multi-layered approach to simulate mineral grains and wind patterns.
Dual-Normal Layering
Implement a two-tier normal system combined via RNM:
1. Macro Ripples: Large-scale wind patterns that define the broad "wave" of the desert.
2. Micro Grains: High-frequency noise that provides the tactile, matte quality of sand grains.
The "Journey" Aesthetic Modules
• Ocean Specular: Utilize a Blinn-Phong model with high power and a subtle blueish tint. Apply the Fresnel Effect to define sharp highlights along the crests of the dunes.
• Dynamic Sparkles: Simulate quartz reflections using the dot product of the View Direction and a high-resolution Blue Noise map (to prevent visible tiling).
Emission Integration for Sparkles
1. Sample Blue Noise using triplanar UVs.
2. Use a Step node to isolate peak values into bright "points."
3. Multiply by an HDR Color property.
4. Connect to the Emission slot to trigger Post-Processing Bloom, allowing sparkles to shimmer as the camera moves.
6. Performance Optimization for Mobile and AR
On tile-based GPUs (Meta Quest, HoloLens), tripling texture samples is a primary bottleneck.
Biplanar Mapping Optimization
Biplanar mapping reduces fetch costs by 33% by discarding the weakest axis. Using the constant 0.5773 (1/ 
3

​
 ), the weights are re-normalized across the two dominant planes: w = clamp((w - 0.5773) / (1.0 - 0.5773), 0.0, 1.0). This maintains visual quality while saving a full texture sample.
Vertex Stage Offloading
Standardize on moving UV calculations to the vertex stage using Custom Interpolators. Calculating UV = WorldPos * Tiling in the fragment shader 3×2 times is a waste of cycles. Perform these linear operations in the vertex shader and interpolate the results to the fragment stage.
Sampler Sharing and Channel Packing
• Sampler Sharing: Use one Sampler State for all texture samples.
• MOHS Map: Create a packed texture: Metallic (R), Occlusion (G), Height/Sparkle Noise (B), Smoothness (A). This retrieves all PBR and aesthetic data in a single triplanar pass.
URP System-Level Settings for AR
Setting
Recommendation
Rationale
Rendering Path
Forward
Avoids slow GMEM loads on Tile-Based GPUs.
MSAA
2x
Minimum required to smooth high-contrast sand edges.
SRP Batcher
Enabled
Optimizes draw calls for complex Kinect meshes.
Soft Shadows
Disabled
Minimizes fragment shader instruction count and branching.
7. Conclusion: Synthesis for Dynamic Environments
High-quality sand rendering in Unity URP requires the synthesis of procedural triplanar mapping and specialized lighting modules. World-space stability is critical for sensor-driven meshes, providing a stable visual anchor against vertex jitter. By offloading calculations to the vertex stage and utilizing biplanar logic, technical artists can achieve the "Journey" aesthetic even within the tight constraints of mobile AR.
Technical Checklist:
• [ ] Weight Normalization: Verify weights sum to 1.0; check for divide-by-zero protection.
• [ ] Primozic Optimization: Implement if (weight > 0.01) branching for texture samples.
• [ ] Swizzle Verification: Ensure side-plane UVs use ZY and XZ to prevent 90-degree rotation.
• [ ] RNM Blending: Verify normal map blending is set to Reoriented, not Linear.
• [ ] Vertex Offloading: Move WorldPos * Tiling to Custom Interpolators.
• [ ] Biplanar Constant: Ensure weight re-normalization uses the 0.5773 constant.
• [ ] Sampler State: Check that all nodes share a single Sampler State.
• [ ] Local World Space: If using moving meshes, ensure UV logic is WorldPos - ObjectPos.