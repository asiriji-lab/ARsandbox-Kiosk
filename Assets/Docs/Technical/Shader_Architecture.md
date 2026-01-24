Sand Shader Architecture & Color Correction

1. Executive Summary and Technical Objectives

In the competitive landscape of Augmented Reality (AR) development, visual fidelity in terrain environments is a primary driver of user presence. As the digital and physical worlds converge, the transition from a procedural "rainbow" height gradient to a geologically accurate desert shader is critical for maintaining immersion. Low-fidelity color ramps often fail at the fragment level, resulting in muddy intersections that shatter the illusion of a cohesive environment. This document outlines the architectural shift from crude multiplicative blending to a sophisticated physically based rendering (PBR) model utilizing triplanar mapping and perceptual color interpolation.

Current Implementation Shortcomings	Proposed Architectural Improvements
Muddy Green Artifacts: Caused by subtractive color mixing in non-linear spaces.	Lerp-Based Soft Tinting: Preserves albedo luminance and warm chromaticity.
Texture Stretching: UV-based mapping fails on steep dune inclines and canyon walls.	Triplanar Mapping: Projects textures in World Space to eliminate UV distortion.
Zombie Gradients: Desaturated "dead zones" in color ramps due to sRGB interpolation.	Perceptual Blending: Implementation of okLAB-based interpolation for vibrant ramps.
High-Frequency Tiling: Obvious repeating patterns on large-scale AR terrains.	Stochastic Sampling: Procedural tiling mitigation via hex-grid or noise-based offset.

The following sections provide a technical diagnosis of current color blending failures and the mathematical logic required for a high-performance solution.

2. Diagnosis: The "Green Sand" Phenomenon

The most prevalent visual failure in desert shaders is the appearance of "muddy" or desaturated green artifacts in transition zones. This phenomenon is rooted in the physics of subtractive color mixing when using standard Multiply operations. When a warm base texture—specifically a beige desert sand like #EDC9AF—is multiplied by a gradient containing cool blue or green height data, the result is a catastrophic loss of warm luminance.

* Luminance Crushing: The Multiply node functions as a chromatic filter (Out = Base \times Blend). Because values are normalized (0.0–1.0), the operation inherently reduces energy. Multiplying warm yellow-orange sand by cool-toned segments "crushes" the high-frequency highlight details required for a realistic silicate surface.
* Chromatic Shifts: Sand (#EDC9AF) has high reflectance in Red/Green but lower in Blue. Multiplying this by cool-toned ramps cancels out the vibrancy of both, resulting in an "undead" olive sludge rather than a geological shadow.
* Zombie Gradients: Interpolating between critical desert hues—such as #F37031 (Vivid Tangelo) and #EFDE63 (Naples Yellow)—in standard sRGB space often creates a "dead zone." This desaturated midpoint is a failure of linear interpolation in non-perceptual spaces. To rejuvenate these transitions, we must move toward perceptual color spaces like okLAB, which maintains consistent lightness and saturation through the gradient ramp.

To resolve these "dead-zone" artifacts, the shader must move away from simple multiplication toward a Linear Interpolate (Lerp) logic.

3. Logic Fix: Soft Tint Implementation via Lerp

Implementing a decoupled "Tint Strength" provides artists with strategic control over procedural variation without overriding base texture fidelity. This preserves the high-frequency detail of the sand albedo while allowing for elevation-based chromatic shifts.

Shader Graph Node Structure

1. Input A: Raw Sand Albedo texture.
2. Input B: Albedo texture multiplied by the Gradient Color (sampled from height data).
3. Input T (Alpha): TintStrength property (Float Slider, 0.0–1.0, Default 0.2).
4. Note: Ensure the World Position node is set to Absolute World to maintain consistent coloring even if the AR world-origin or terrain anchor shifts.

Mathematical Superiority

The Soft Tint approach uses the standard Lerp formula: FinalColor = A \times (1 - T) + B \times T By using a default T value of 0.2, the fragment is composed of 80% original high-fidelity albedo and 20% tinted version. This prevents the darkened multiplier from overwhelming the silicate highlights.

Pro-Tip: Contrast-Preserving Blend Modes For high-contrast dunes, implement Soft Light or Overlay blend modes using the Pegtop formula to preserve grains: (1 - 2 \times Blend) \times Base^2 + 2 \times Blend \times Base. This approach acts as a "diffused spotlight," preventing the gradient from ever fully crushing the base texture to absolute black.

4. The "Desert-Safe" Chromatic Palette

Geological accuracy prevents chromatic interference by ensuring the height gradient remains within the warm, iron-oxide spectrum. By moving from deep reds to bleached silicates, we eliminate the complementary color conflict.

Elevation Zone	Color Name	Hex Code	Geological Purpose
Low (Floor)	Citrine Brown	#912C0C	Iron-rich deposits and deep, damp crevices.
Mid-Low	Vivid Tangelo	#F37031	Oxidized sandstone layers and dune transitions.
Mid-High	Naples Yellow	#EFDE63	Sun-bleached ridges and wind-scoured silt.
Peak	Desert Sun	#FEF4E7	High-intensity silicate glare and salt crusts.

Gradient Implementation Data (JSON)

{
  "gradient": {
    "name": "Geological_Desert_Ramp",
    "type": "Linear",
    "stops": [
      {"position": 0.0, "color": "#912C0C", "label": "Canyon Floor"},
      {"position": 0.25, "color": "#F37031", "label": "Mid-Dune"},
      {"position": 0.75, "color": "#EFDE63", "label": "Sandstone Ridge"},
      {"position": 1.0, "color": "#FEF4E7", "label": "Dune Peak"}
    ]
  }
}


Note: In the Shader Graph, place a Saturate Node after the Height Remap but before the Gradient input to clamp all fragments into the valid 0.0–1.0 range.

5. Geometric Integrity: Triplanar Mapping Logic

To eliminate UV stretching on steep AR dunes, the shader must employ World Space Triplanar Mapping. This ensures the sand ripples maintain their scale across vertical canyon walls and horizontal plains.

1. Sampling: Sample the albedo/normal textures three times (on the XY, ZY, and XZ planes).
2. Blend Weights: Use the Absolute value of the World Normal to calculate axis-specific weights.
3. Safety Normalization: Divide individual weights by the sum of all components: Weights / (Weights.x + Weights.y + Weights.z + 0.001f). The epsilon (+ 0.001f) is critical to avoid division-by-zero errors on complex meshes where normals might nullify.

Tiling Mitigation

Senior-level AR terrains require Stochastic Sampling or Hex-Grid Tiling to break up micro-tiling grain patterns. By sampling the texture with a randomized offset or procedural rotation based on a low-frequency noise mask, you can ensure that sand grains appear unique across vast procedural terrains.

6. PBR Integration: Roughness and Ambient Occlusion

Final surface realism under direct sunlight is achieved by properly defining the sand as a dielectric material within the PBR stack.

* Smoothness Logic: Sand is highly diffuse. Calculate as Smoothness = 1 - RoughnessMap.
* Micro-Depth: Place the Ambient Occlusion (AO) map into the designated AO slot to provide micro-depth for dune ripples, preventing the surface from appearing like flat plastic.

Node Property	Standard Default Value	Purpose
Smoothness	0.1 – 0.2	Prevents unrealistic mirror-reflections on grains.
Metallic	0.0	Correctly identifies the material as a dielectric.
Ambient Occlusion	1.0 (Texture driven)	Adds micro-shadowing to dune ripples.
Normal Strength	0.5 – 1.0	Enhances perceived depth of wind-blown patterns.

This architectural framework provides the definitive guide for engineers to resolve muddy artifacts and achieve high-fidelity, geologically accurate sand shaders in modern AR terrain systems.
