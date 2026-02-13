# Clean Code Audit: SandboxUI.cs

**Target**: `Assets/Scripts/UI/SandboxUI.cs`
**Date**: 2026-02-09

## Summary
`SandboxUI.cs` has grown into a "Monolithic View" (1300+ lines). While it successfully implements the "Programmatic UI" pattern, it fails to separate **View Construction** from **Interaction Logic**.

## Critical Violations

### 1. Single Responsibility Principle (SRP)
*   **View vs Logic**: The class manually constructs UI elements (View) AND handles their events (Controller) AND executes business logic (e.g., `HandshakeHardwareCoroutine`, `UpdateCalibrationLogic`).
*   **Input Handling**: It contains raw Input polling (`Keyboard.current`, `Mouse.current`) mixed with UI layout code.

### 2. High Coupling
*   **Direct Dependency**: It binds directly to `ARSandboxController` in some places (e.g., `ViewModel.Controller.GetColorTexture()`), bypassing the ViewModel abstraction layer.
*   **Circular Reference Risk**: The `ROIEditor` lazy-loading pattern is fragile (`FindObjectsInactive.Include`).

### 3. Magic Numbers & Strings
*   **Layout**: Hardcoded pixel values everywhere (`new Vector2(0, 40)`, `padding = 15`). These should be in a `UIStyle` or `LayoutConstants` struct.
*   **Logic**: `closestDist = 50f` (Hit test threshold), `timer < 3.0f` (Watchdog).

## Refactoring Plan

### Step 1: Extract Input Logic
*   **Action**: Move `HandleInput()`, `HandleSecretGesture()`, `HandleAutoHide()` to a dedicated `SandboxInputController` or `UIInputManager`.
*   **Benefit**: Reduces `SandboxUI` complexity by ~200 lines.

### Step 2: Extract Styles
*   **Action**: Move all `Color`, `Font`, `padding`, and `sizeDelta` values to a static `UIStyle` class.
*   **Benefit**: Centralized styling, easier to theme, removes magic numbers.

### Step 3: Specific View Components
*   **Action**: Extract the "Calibration Overlay" logic to `CalibrationOverlayView.cs`.
*   **Action**: Extract the "Tab Building" logic to `SandboxUITabs.cs` (partial class or helper).

## Score
*   **Readability**: C
*   **Maintainability**: D+
*   **SRP Adherence**: D
