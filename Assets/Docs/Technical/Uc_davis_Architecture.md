# Technical Implementation Specification: Open AR Sandbox

This specification provides a senior-level technical breakdown of the Open AR Sandbox architecture. It defines the logical operation, data contracts, and algorithmic requirements for each core module.

---

## 1. System Orchestration: `MainThread` & `Sandbox`
The system is built on a "Parametric Pipeline" where a shared state dictionary (`sb_params`) is passed through a sequence of processing modules.

### Shared State: `sb_params`
| Property | Type | Description |
| :--- | :--- | :--- |
| `frame` | `float32[h, w]` | The current normalized heightmap. |
| `ax` | `Matplotlib.Axis` | The render target (mapped to the projector). |
| `extent` | `[minX, maxX, minY, maxY, minZ, maxZ]` | Physical and logical bounds. |
| `marker` | `DataFrame` | Detected markers and their coordinates. |
| `active_cmap` | `bool` | Toggle for colormapping. |
| `active_contours` | `bool` | Toggle for contour lines. |

### The Execution Loop
1.  **Poll Sensor**: Fetch raw depth data.
2.  **Filter/Normalize**: Basic cleanup (Gaussian blur, temporal averaging).
3.  **Update Loop**: Iterate through all registered modules.
    - Each module: `sb_params = module.update(sb_params)`
4.  **Trigger Render**: Refresh the projector display.

---

## 2. Sensor Module: Data Pipeline
**Purpose**: Convert raw distance (mm) from hardware into a normalized heightmap grid.

### Key Logic: `get_frame()`
```python
# 1. Temporal Averaging (Noise Reduction)
stacked_frames = [sensor.get_raw() for _ in range(n_frames)]
mean_depth = mean(stacked_frames, ignore_zeros=True)

# 2. Spatial Filtering
smoothed_depth = GaussianBlur(mean_depth, sigma=3)

# 3. Normalization (Depth to Height)
# Concept: Height = HeightOfSensor - MeasuredDistance
# s_max = table surface, s_min = max sand height
inverted_frame = s_max - smoothed_depth
```

---

## 3. Visualization: `CmapModule` (Colormapping & Shading)
**Purpose**: High-fidelity terrain rendering with dynamic relief shading.

### Inputs
*   `frame`: Height buffer.
*   `LightSource`: Altitude, Azimuth, Vertical Exaggeration.

### Workflow: `update()`
1.  **Relief Shading**: If active, calculate the normal vectors of the heightmap.
2.  **Light Calculation**: Multiply the colorized pixels by the dot product of the surface normal and light direction.
3.  **Matplotlib Mapping**:
    - Project height value `[0, 1]` to a chosen `Colormap` (e.g., `gist_earth`).
    - Use `imshow` with `zorder=-500` to act as the base "background" layer.

---

## 4. Visualization: `ContourLinesModule`
**Purpose**: Mathematical extraction of topographic lines.

### Key Parameters
*   `contours_step`: Interval between lines (e.g., every 50mm).
*   `threshold`: Max lines allowed before disabling to prevent GPU/CPU lock (Matplotlib specific).

### Algorithmic Detail
The module uses a marching squares (or similar) algorithm via `ax.contour`.
1.  **Generate Levels**: `numpy.arange(minHeight, maxHeight, step)`.
2.  **Extract Paths**: Computes the geometry for major (thick) and minor (thin) lines.
3.  **Text Overlay**: Calculates the optimal position for elevation labels along the generated paths.

---

## 5. Feature Module: `TopoModule` (Sea Level & Simulation)
**Purpose**: Active simulation layers (water, lava) using masking logic.

### Logic: `create_paths()`
1.  **Thresholding**: Isolate areas where `height < SeaLevel`.
2.  **Contour Infilling**:
    - Use `skimage.measure.find_contours` on the binary mask.
    - Create a closed Polygon (Path).
3.  **Texture Overlay**:
    - Sample a fluid texture (water image).
    - Apply the Path as a stencil.
    - **Animation**: Shift the texture UVs per frame to simulate moving waves.

---

## 6. Marker Module: `MarkerDetection`
**Purpose**: Bridge the gap between physical objects and the digital simulation.

### Coordinate Transformation
Detecting markers happens in **Color Space** (RGB), but simulations happen in **Depth Space**.
*   **CoordinateMap**: A look-up table (or matrix) mapping `RGB(x,y)` to `Depth(x,y)`.
*   **Update Loop**:
    1.  `cv2.aruco.detectMarkers` (RGB frame).
    2.  `convert_color_to_depth()`: Translate screen pixels to 3D cloud coordinates.
    3.  `transform_to_box_coordinates()`: Offset by the sandbox "margins" to get local coordinates for simulation.

---

## 7. Porting Checklist (for Coding Agents)

1.  [ ] **Depth Texture Filter**: Recreate the Gaussian/Bilateral smoothing.
2.  [ ] **Shader - Heightmap**: Map height `Z` to UV `U` on a 1D gradient.
3.  [ ] **Shader - Contours**: Use `frac(height / spacing)` in the fragment shader to draw lines without heavy geometry calculations.
4.  [ ] **Calibration UI**: Implement a 4-point corner drag system to define the `s_left`, `s_right`, etc., margins.
5.  [ ] **Water Layer**: Implement a simple `Plane` that moves on the Y-axis and uses a depth-comparison shader for "infilling" valleys.
