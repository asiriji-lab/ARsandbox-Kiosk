# Clean Code Audit: ARSandboxController.cs

**Target**: `Assets/Scripts/Core/ARSandboxController.cs`
**Date**: 2026-02-09

## Summary
The `ARSandboxController` exhibits classic "God Class" symptoms. It acts as a central hub for Settings, Hardware, Rendering, and even UI logic (Gradient generation). This violates the **Single Responsibility Principle** and the **Composition Root** architectural rule.

## Violations

### 1. Single Responsibility & Architecture
-   **Violation**: The class manages too many distinct domains:
    -   **Life-Cycle**: `Awake`/`Start` (Initialization).
    -   **Persistence**: `LoadSettings`, `SaveSettings` (should be `SandboxSettingsManager`).
    -   **Visualization Logic**: `ApplyGradientPreset`, `InitColorRamp`, `RefreshColorRamp`, `HexToColor`. This is purely "View" logic or "Theme" logic.
    -   **Parameter Proxies**: Lines 43-85 define dozens of properties that just passthrough to `Settings`. This creates noise and violates the **Single Source of Truth** pattern (clients should access `Settings` directly).
-   **Recommendation**:
    -   Extract Gradient logic to a `GradientGenerator` or `ThemeService`.
    -   Remove Proxy Properties (unless required for Unity Inspector modification, but strictly they hide the `Settings` object).

### 2. Functions (Complexity & Size)
-   **Violation**: `Awake` (50+ lines) and `Start` (40+ lines) mix abstraction levels.
    -   *Example*: `Awake` loads settings, checks shaders, initializes subsystems, and sets up watchdogs.
-   **Recommendation**: Extract methods like `InitializeShader()`, `InitializeProviders()`, `ValidateDependencies()`.

### 3. Arguments
-   **Violation**: `_meshGenerator.UpdateMesh(...)` takes **6 arguments**.
    -   `_depthProcessor.Process(...)` takes **5 arguments**.
-   **Recommendation**: Wrap these in a context object (e.g., `RenderContext` or just pass `SandboxSettings`).

### 4. Naming & Noise
-   **Violation**: `CurrentSettings` (Line 40) is a redundant alias for `Settings`.
-   **Violation**: `UI` (Line 90) is too generic.

## Proposed Refactoring (Safe First Steps)
1.  **Extract Gradient Logic**: Move `ApplyGradientPreset`, `RefreshColorRamp`, etc., into a static helper or a dedicated `TerrainColorizer` component.
2.  **Remove Dead Code**: Delete commented out legacy code (Lines 315).
3.  **Group Initialization**: Refactor `Awake` into `InitSettings()`, `InitGraphics()`, `InitHardware()`.
