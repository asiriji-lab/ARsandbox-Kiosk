# Refactoring Analysis: Naming Convention Violations 🔍

**Standard:** 
1. **Public/Inspector Fields:** MUST be `PascalCase`.
2. **Private Fields:** SHOULD be `_camelCase` (underscored).
3. **Methods:** MUST be `PascalCase`.

## 🚨 Identified Violations (ARSandboxController.cs)

| Line | Type | Current Name (Violation) | Proposed Name (PascalCase) | Risk Level |
| :--- | :--- | :--- | :--- | :--- |
| 226 | `GradientPreset` | `currentGradientPreset` | `CurrentGradientPreset` | 🟡 Medium (Inspector Reset) |
| 227 | `bool` | `useDiscreteBands` | `UseDiscreteBands` | 🟡 Medium (Inspector Reset) |
| 228 | `Gradient` | `elevationGradient` | `ElevationGradient` | 🟡 Medium (Inspector Reset) |

*(Note: `DeviceIndex`, `Width`, `Length`, `MinDepthMM`, `EnableSimulation`, `NoiseScale`, `MoveSpeed` are already Correct)*

## 🚨 Identified Violations (Private Fields - Consistency)
Both `ARSandboxController.cs` and `SandboxUI.cs` should follow the `_camelCase` standard for private fields to avoid confusion with local variables.

- **ARSandboxController:** `mesh` -> `_mesh`, `isRunning` -> `_isRunning`, `latestDepthFrame` -> `_latestDepthFrame`, etc.
- **SandboxUI:** `uiRoot` -> `_uiRoot`, `isVisible` -> `_isVisible`, `heightSlider` -> `_heightSlider`, etc.

## ⚠️ Ripple Effects & Risks

### 1. Inspector Data Loss 📉
Renaming `currentGradientPreset`, `useDiscreteBands`, and `elevationGradient` will cause Unity to reset their values. 
**Mitigation:** Use `[FormerlySerializedAs("oldName")]`.

### 2. JSON Serialization Mapping 📄
The `SandboxSettings` struct (Lines 30-44) currently uses `camelCase` (e.g., `noiseScale`).
**Decision:** We will keep the Struct as `camelCase` (Legacy Data Standard) but rename the Controller fields to `PascalCase` (Code Standard). We must ensure `SaveSettings()` and `LoadSettings()` map them correctly.

### 3. SandboxUI References 💥
`SandboxUI.cs` references several of these fields. They must be updated simultaneously.

## ☑️ Next Steps to Implement
1. [ ] Apply `PascalCase` to remaining public fields in `ARSandboxController.cs` using `[FormerlySerializedAs]`.
2. [ ] Refactor all private fields in `ARSandboxController.cs` and `SandboxUI.cs` to use the `_camelCase` prefix.
3. [ ] Bulk replace references in `SandboxUI.cs`.
4. [ ] Verify `SaveSettings()`/`LoadSettings()` logic remains functional.
