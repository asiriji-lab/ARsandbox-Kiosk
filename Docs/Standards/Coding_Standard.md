# AR Sandbox Coding Standard (Strict)

This document establishes the mandatory "Style & Grammar" rules for the AR Sandbox project. These rules are designed to prevent AI hallucinations and maintain immediate readability in the Unity Inspector.

---

## 1. Core Principles
- **Clarity > Cleverness**: Prefer verbose, readable code over complex "clever" one-liners.
- **Inspector-First**: Variables meant for calibration must be exposed clearly to non-technical users.
- **Safety First**: Never assume a hardware sensor or a UI reference is non-null. Use early returns.

---

## 2. Naming Conventions (C# & Shader)

Consistency here is the #1 defense against AI hallucination.

| Type | Convention | Example | Rationale |
| :--- | :--- | :--- | :--- |
| **Public / Inspector** | **PascalCase** | `public float NoiseScale;` | Required for Unity Inspector mapping. |
| **Private Fields** | **_camelCase** | `private Mesh _mesh;` | Standardized underscore prefix. |
| **Methods** | **PascalCase** | `void UpdateMesh()` | Unity standard method naming. |
| **Local Variables** | **camelCase** | `float delta = 0.5f;` | Standard local scope naming. |
| **Shader Properties** | **_Underscore** | `_HeightMin` | Compatibility with Material accessors. |

> [!IMPORTANT]
> **Key Rule**: If it is visible in the Unity Inspector, it MUST be **PascalCase**. 

---

## 3. Defensive Programming Standards

- **Early Returns**: Kill the "If-Else Pyramid". Handle invalid states (nulls, zero-bounds) at the top of the method.
- **Null-Safety**: Always null-check sensor providers (`IDepthProvider`) and UI elements before calling methods.
- **Index Safety**: When processing native arrays or buffers, use `Math.Clamp` or length checks to prevent `IndexOutOfRange` errors.

---

## 4. API Documentation & Logging

### 4.1 XML Documentation Tags
Use XML tags (`/// <summary>`) for every **public** method. Explain the **"Why"** (architectural intent) rather than the "What".

```csharp
/// <summary>
/// Triggers a re-capture of the floor plane. 
/// Used to zero-out the sandbox measurements after physical sand leveling.
/// </summary>
public void CalibrateFloor() { ... }
```

### 4.2 Logging Standards
Wrap all debug logs in the `DEVELOPMENT_BUILD` conditional attribute to ensure zero performance impact in production builds.

```csharp
[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
private void LogDebug(string message) { Debug.Log(message); }
```

---

> [!NOTE]
> For detailed technical implementation, hardware setup, and performance architecture, see the [Technical Implementation Manual](../Technical/Implementation_Manual.md).