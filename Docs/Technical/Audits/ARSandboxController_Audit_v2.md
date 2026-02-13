# Clean Code Audit: ARSandboxController.cs (v2)

**Target**: `Assets/Scripts/Core/ARSandboxController.cs`
**Date**: 2026-02-09

## Summary
The `ARSandboxController` remains a "God Class", acting as a central hub for mismatched responsibilities. While some logic has been offloaded to `DepthProcessor` and `TerrainMeshGenerator`, the controller still retains too much "Glue Code" and "View Logic".

## Critical Violations

### 1. Single Responsibility Principle (SRP)
*   **Gradient Generation**: The controller contains 50+ lines of code dedicated to generating texture ramps from gradients (`ApplyGradientPreset`, `RefreshColorRamp`, `InitColorRamp`). This is purely **Visualization Domain** logic and should be in a separate service.
*   **Settings Proxying**: Lines 44-86 define over 20 properties that simply get/set values in `Settings`. This adds 40 lines of noise and violates the "Single Source of Truth" by encouraging access via the Controller instead of the Settings object.

### 2. Initialization Abstraction
*   **Mixed Levels of Abstraction**: `Start()` mixes high-level logic (checking for Kinect) with low-level details (searching asset database for textures).
*   **Legacy Code**: `AutoAssignTextures` is empty/harmless but adds noise.

### 3. Dependency Management
*   **Hidden Dependencies**: The controller manually searches for `WaterCaustics` using `AssetDatabase` in `Start()`. This makes testing difficult and relies on specific project folder structures.

## Refactoring Plan

### Step 1: Extract Gradient Logic
*   **Action**: Move `ApplyGradientPreset`, `RefreshColorRamp`, `InitColorRamp`, and `_colorRampTexture` to a new component `TerrainColorizer` or `VisualizationManager`.
*   **Benefit**: Removes ~50 lines of unrelated logic from the controller.

### Step 2: Remove Settings Proxies
*   **Action**: Delete lines 44-86.
*   **Impact**: Classes accessing `Controller.MinDepthMM` must change to `Controller.Settings.MinDepth`. This is a *Good Thing* as it makes the dependency on Settings explicit.

### Step 3: Simplify Initialization
*   **Action**: Move the `CausticTexture` loading logic to `SandboxSettingsSO` or a dedicated `ResourceLoader`.
*   **Action**: Remove `AutoAssignTextures`.

## Score
*   **Readability**: C-
*   **Maintainability**: D
*   **SRP Adherence**: F
