Technical Specification: Implementing High-Fidelity Wetness and Water Effects in Unity 6 (URP)

1. Architectural Overview of Modern Unity Graphics

The evolution of Unity’s rendering architecture has culminated in a transition from a monolithic, "black box" legacy pipeline to the modular Scriptable Render Pipeline (SRP). This shift represents a strategic move toward "performance by default," moving away from imperative rendering to declarative resource management. To leverage Unity 6's most advanced features—including the Render Graph API—hardware must support modern APIs like Vulkan or DirectX 12, which provide the granular control over GPU memory and synchronization necessary for high-fidelity environment effects.

A critical hurdle for engineers transitioning to the Universal Render Pipeline (URP) is the fundamental incompatibility of legacy shading languages. Surface Shaders (#pragma surface) are no longer supported and require a complete structural rewrite into pure HLSL or Shader Graph. This transition is essential for utilizing modern batching technology and reducing CPU overhead through persisted GPU data.

Feature	Legacy Built-in Pipeline	Universal Render Pipeline (URP)
Pipeline Architecture	Hard-coded / Fixed-function	Scriptable (C#) / Modular
Shading Language	Cg / HLSL (Surface Shaders)	Pure HLSL / Shader Graph
Batching Technology	Static & Dynamic Batching	SRP Batcher / GPU Resident Drawer
Transformation Logic	UnityObjectToClipPos	TransformObjectToHClip
Resource Management	Imperative / Manual	Declarative (Render Graph)

This architecture provides the declarative scheduling required to implement complex wetness systems that scale across platforms without the "code spaghettification" found in legacy implementations.


--------------------------------------------------------------------------------


2. The Physics and Math of "Looking Wet": Fresnel and Specularity

Believable water effects rely on the accurate simulation of light-surface interaction. The foundation of surface tangibility is the dot product, which determines how much a surface normal aligns with the viewer’s direction. This value allows us to mathematically define "edge" lighting, which is vital for simulating the reflective nature of wet surfaces without the cost of high-resolution reflection probes.

For unlit shaders, a lead-level implementation requires calculating the viewDir manually by subtracting the world position from the built-in _WorldSpaceCameraPos variable.

Fresnel Calculation Logic (HLSL):

// Calculate view direction for unlit or custom lighting models
float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS.xyz);

// Dot product between world normal and view direction
float fresnel = dot(input.normalWS, viewDir);

// Invert and saturate to move highlight to the edges
fresnel = saturate(1.0 - fresnel);

// Tighten the effect using an exponent property
fresnel = pow(fresnel, _FresnelExponent);


To transition a surface from dry to wet, "jacking up specularity" is insufficient. A lead engineer must balance three core properties:

1. Smoothness: Drastically increased to sharpen specular highlights.
2. Specular Color: Increased to enhance reflectivity.
3. Diffuse Color (Albedo): Darkened to simulate light absorption in damp materials.

Technical Note: In the Deferred pipeline, ambient light is calculated during the geometry pass. Therefore, achieving visual parity often requires "fiddling" with ambient values to prevent wet surfaces from appearing unnaturally bright in shadowed areas.


--------------------------------------------------------------------------------


3. Shader Construction: HLSL and SRP Batcher Compatibility

Maximizing throughput in Unity 6 requires strict adherence to SRP Batcher compatibility. This optimization persists material data in GPU memory, allowing the engine to skip expensive set-pass calls between materials sharing the same shader.

For compatibility, all material properties must be wrapped in a UnityPerMaterial constant buffer. Critically, to leverage the SRP Batcher for built-in engine properties (like transformation matrices), you must also declare the UnityPerDraw CBUFFER, which contains variables like unity_ObjectToWorld.

Standardized HLSL Structure:

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Mandatory CBUFFER for Material Properties
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float _Smoothness;
    float _WetnessAmount;
CBUFFER_END

// Mandatory CBUFFER for Built-in Draw Properties
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    // ... additional required engine variables
CBUFFER_END


Standardized Coordinate Spaces: Always use the modern library functions to ensure cross-API compatibility.

Suffix	Space	Transformation Function
OS	Object Space	TransformObjectToWorld
WS	World Space	TransformWorldToView
VS	View Space	TransformViewToProjection
CS	Clip Space	TransformObjectToHClip


--------------------------------------------------------------------------------


4. Volumetric Wetness: Decals and G-Buffer Modification

Strategic Constraint: The implementation of volumetric wetness via G-buffer modification is a technique exclusive to the Deferred Rendering Path. Attempting this in Forward Rendering would require a fundamentally different approach, likely involving complex multi-pass shaders or global texture arrays.

Wetness volumes allow for modular environmental interaction. These volumes project into the scene and modify the G-buffer (smoothness, albedo, and normals) before the lighting pass.

Implementation Strategies:

* Irregular Puddle Formation: Use mask textures, specifically thresholded Perlin Noise. By animating the threshold via script, you can simulate puddles growing or drying dynamically.
* Particle Integration: Decal volumes can be attached to particle systems, enabling effects like "wet trails" from water pistols or leaking pipes.
* Sheltered Areas: To protect indoor environments from global rain effects, "negative volumes" are the preferred architectural solution. These volumes explicitly exclude geometry from the wetness calculation.
* Technical Approximation: Because the G-buffer does not store all PBR data, engineers must often estimate metallic or dielectric properties based on specular highlights when applying volume modifications.


--------------------------------------------------------------------------------


5. Advanced Implementation: The Render Graph API in Unity 6

Unity 6 mandates the use of the Render Graph API for custom rendering features. This declarative system moves away from the imperative ScriptableRenderPass, offering Native Pass Merging for mobile (TBDR) platforms and automatic resource lifetime management.

The Five-Step Render Graph Node Process:

1. Create the Node: Use renderGraph.AddRasterRenderPass.
2. Define Inputs: Request resources from the ContextContainer. Use frameData.Get<UniversalResourceData>() specifically to retrieve the activeColorTexture.
3. Define Outputs: Use rasterPassBuilder.SetRenderAttachment to define where the node writes data.
4. Implement the Render Function: Execute logic via a static function using RasterCommandBuffer.
5. Connect and Dispose: Set the RenderPassEvent (e.g., AfterRenderingPostProcessing). Critically, you must call rasterPassBuilder.Dispose() to finish the node creation.

This system ensures that high-overhead water effects are culled automatically if they do not contribute to the final frame, saving vital GPU cycles on mobile and VR hardware.


--------------------------------------------------------------------------------


6. Dynamic Effects and Performance Optimization

Production-ready wetness must scale from mobile to high-end hardware while maintaining visual dynamism.

* Weather Timelines: Link shader properties to global weather controllers. This allows surfaces to transition from dry to "saturated" by gradually darkening the diffuse map and sharpening specular highlights in sync with the skybox.
* Moving Vehicles: For raindrops on moving windshields, use UV shifting on decal textures. Animating these parameters creates the appearance of "tails" or trailing water streaks behind moving objects.
* GPU Resident Drawer & Occlusion Culling: These Unity 6 features offload draw-call management and the exclusion of hidden objects from the CPU to the GPU. This is essential for rendering dense, rain-slicked environments with thousands of unique props.
* Approximation Caveat: Lead engineers should note that G-buffer modification is an approximation. Since metallic/dielectric values aren't always available in the buffer, the shader must assume these values based on existing specular properties to calculate light absorption correctly.

This specification serves as the architectural roadmap for implementing environmental wetness that is both physically grounded and optimized for the next generation of Unity rendering.
