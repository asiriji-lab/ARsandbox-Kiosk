# UC Davis AR Sandbox Properties

This document outlines the technical properties and implementation details of the [UC Davis AR Sandbox](https://arsandbox.ucdavis.edu/) software (version 2.8), specifically focusing on its Water Shader, Contour Lines, and Color Mapping.

These details are based on an analysis of the source code located in `SARndbox-2.8`.

## 1. Water Shader

**Implementation File:** `share/SARndbox-2.8/Shaders/SurfaceAddWaterColor.fs`

The water effect is applied as a post-processing step in the fragment shader. It modifies the existing base color of the terrain if the calculated water level is above the surface.

### Properties:
*   **Shader Type:** GLSL Fragment Shader.
*   **Logic:**
    *   **Water Level Check:** Calculates the height of the water column above the fragment. If `waterLevel > 0.0`, the water effect is applied.
    *   **Surface Normal (`wn`):** Calculates a surface normal dynamically based on the gradient of the water level texture (`quantitySampler`). This allows the water surface to look curved/wavy rather than flat.
    *   **Specular Highlight:** Implements a strong specular highlight to simulate wetness/glossiness.
        *   Equation: `pow(dot(wn, lightDir), 100.0)`
        *   `lightDir` appears to be a fixed vector approx `(0.075, 0.075, 1.0)`.
        *   The high exponent (`100.0`) creates a sharp, shiny highlight.
    *   **Blending:** The water color is mixed with the terrain base color based on opacity.
        *   `baseColor = mix(baseColor, waterColor, min(waterLevel * waterOpacity, 1.0))`
*   **Visual Style:**
    *   The code includes commented-out sections for **Perlin Noise** and **Turbulence**, which suggests capabilities for more complex animations (like lava or turbulent water), but the active default is a cleaner, specular-based "still" or "flowing" water look.

## 2. Contour Lines

**Implementation File:** `share/SARndbox-2.8/Shaders/SurfaceAddContourLines.fs`

Contour lines are procedurally generated in the fragment shader rather than being pre-calculated geometry. This ensures they are view-dependent and crisp.

### Properties:
*   **Shader Type:** GLSL Fragment Shader.
*   **Algorithm:**
    *   Uses a "half-pixel offset" elevation texture sample.
    *   **Edge Detection:** Calculates the elevation integer interval for each of the four corners of a pixel (using `floor(elevation * contourLineFactor)`).
    *   **Crossing Check:** Determines if the pixel edges cross a contour threshold (i.e., if corners lie in different elevation intervals).
    *   **Thin Lines:** The active algorithm is optimized to draw the "thinnest possible contour lines" by removing redundant pixels in 4-connected lines, preventing thick or double-width lines.
*   **Visuals:**
    *   Renders strict **Black** lines (`vec4(0.0, 0.0, 0.0, 1.0)`).
    *   Line thickness is virtually 1 pixel wide due to the shader logic.

## 3. Color Mapping (Elevation)

**Implementation File:** `ElevationColorMap.cpp` & `SurfaceGlobalAmbientHeightMapShader.fs`

The coloration of the sand/terrain is handled via a 1D texture lookup, which maps elevation values to specific colors.

### Properties:
*   **Technique:** 1D Texture Lookup.
*   **Mapping:**
    *   The Vertex Shader calculates a texture coordinate (`heightColorMapTexCoord`) based on the vertex's vertical distance from a base plane.
    *   The Fragment Shader samples a 1D texture using this coordinate.
*   **Formats:**
    *   Supports loading color maps from `.cpt` files (GMT Color Palette Table) or simple text files.
    *   The map interpolates between defined color keys (RGB values at specific elevations).
*   **Customization:**
    *   Users can define custom color ramps by providing different color map files.
    *   The system scales the map to fit the calibrated elevation range.
