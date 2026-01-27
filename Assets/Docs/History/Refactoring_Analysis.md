# Refactoring Analysis: Coding Standard Violations 🔍

This document tracks identified violations against [Coding_Standard.md](../Standards/Coding_Standard.md) and provides a safe implementation roadmap.

## 🚨 Identified Violations (ARSandboxController.cs)

### 1. Naming Conventions (Section 2)
| Category | Current Name | Proposed Name | Risk | Mitigation |
| :--- | :--- | :--- | :--- | :--- |
| **Struct Field** | `SandboxSettings.minDepth` | `MinDepth` | 🔴 High (JSON Break) | Use `[JsonProperty]` or manual mapping. |
| **Struct Field** | `SandboxSettings.maxDepth` | `MaxDepth` | 🔴 High (JSON Break) | Use `[JsonProperty]` or manual mapping. |
| **Private Prop** | `SettingsPath` | `_settingsPath` | 🟢 Low | Internal reference update only. |

### 2. Documentation Standards (Section 4.1)
The following public methods are missing mandatory XML `<summary>` tags:
- `RefreshCausticTexture()`
- `UpdateMeshDimensions(float size)`
- `SaveSettings()`
- `LoadSettings()`
- `CalibrateFloor()`

### 3. Performance Standards (Section 4.2)
- **Log Stripping**: Multiple `Debug.Log` calls in `Start()`, `SwitchProvider()`, and `LoadSettings()` must be wrapped in a `[Conditional("DEVELOPMENT_BUILD")]` method.
- **MeshData API**: The current implementation (Line 543) is already using MeshData (Correct), but needs clearer documentation.

## 🚨 Identified Violations (SandboxUI.cs)

### 1. Naming Conventions
- **Correct**: Most private fields already use the `_camelCase` prefix.
- **Violation**: `public ARSandboxController Controller;` (Correct) vs any legacy `controller` references.

### 2. UI Performance (Section 7)
- **StringBuilder**: `_sb` is used correctly (Correct).
- **Visibility**: Toggle uses `CanvasGroup.alpha` correctly (Correct).

---

## 🛠️ Refactoring Roadmap (Safety First)

### Phase 1: Preparation (No Code Change)
- [x] Identify all `SandboxUI` references to fields in `ARSandboxController`.
- [x] Create this analysis report.

### Phase 2: Metadata Layer
- [ ] Add `[FormerlySerializedAs]` to all public field renames in the Controller.
- [ ] Add `/// <summary>` tags to all public methods.

### Phase 3: Field Renaming (Automation)
- [ ] Mass-rename private fields to `_camelCase` where missing.
- [ ] Mass-rename local variables to `camelCase` where missing.

### Phase 4: Structural Fixes
- [ ] Implement the `LogDebug` wrapper and replace all `Debug.Log` calls.
- [ ] Update `SandboxSettings` JSON mapping to be case-insensitive or use explicit attributes.
