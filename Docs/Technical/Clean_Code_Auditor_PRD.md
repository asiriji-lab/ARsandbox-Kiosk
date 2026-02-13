# Product Requirement Document: Clean Code Auditor

**Version**: 1.1
**Status**: DRAFT
**Last Updated**: 2026-02-09

## 1. Problem Statement
The codebase currently suffers from inconsistent adherence to "Clean Code" principles. This manifests as ambiguous naming, complex functions, and argument explosion.
**Critically**, this lack of clarity makes it harder to enforce the project's stricter **Architectural Protocols** (as defined in `.agent/rules.md`), such as MVVM and Composition Root.

## 2. Objectives
To implement an automated "Clean Code Auditor" skill that:
1.  **Strictly Enforces** a defined subset of Clean Code rules.
2.  **Supports Architecture**: Facilitates the breakdown of "God Classes" (like `ARSandboxController`) into smaller, single-responsibility components and Services.
3.  **Preserves Logic**: Ensures that all recommended refactorings are structural only and **NEVER** alter the business logic or runtime behavior.

## 3. Scope & Rules

### 3.1 Naming Conventions (Intent & Clarity)
-   **Reveal Intent**: Variable names must explain *why* they exist.
-   **Context**: Code related to Kiosk Safety or Calibration must use precise, unambiguous terminology (e.g., `calibrationOffset` not `offset`).
-   **No Noise**: Avoid `Manager` unless it manages a specific lifecycle.

### 3.2 Functions (Structure & Hierarchy)
-   **Step-Down Rule**: Code should read top-down.
-   **Single Responsibility**: A function should do one thing.
    -   *Architectural Alignment*: This directly supports the **Composition Root** pattern. If `ARSandboxController` has a 50-line function calculating vertex heights, it belongs in a `TerrainService`.
-   **Complexity**: No nested `if/else` > 2 levels.

### 3.3 Arguments (Interface Design)
-   **Max 3**: Functions should ideally have 0-2 arguments.
-   **Struct Wrapping**: Use data structures.
    -   *Architectural Alignment*: Encourages the use of strongly-typed Settings objects (`SandboxSettings`) rather than passing loose floats, aligning with the **Single Source of Truth** rule.

### 3.4 Comments
-   **Failures**: A comment is a failure to express code clearly.
-   **Self-Documenting Logic**: Refactor complex logic into named functions.

### 3.5 Formatting
-   **Newspaper Metaphor**: High-level concepts first.

## 4. Alignment with Agentic Protocols
The Clean Code Auditor operates within the constraints of `.agent/rules.md`:
1.  **MVVM**: When extracting logic from a View (`SandboxUI`), the auditor should suggest moving state/logic to the ViewModel (`SandboxViewModel`), not just a private helper method in the View.
2.  **Persistence**: Refactoring configuration code must respect the `SandboxSettingsSO` pattern; do not suggest hardcoding values to "simplify" code.
3.  **Safety**: "Self-Healing" watchdogs must remain explicit and easy to audit; Clean Code should make the watchdog logic *clearer*, not obscure it behind excessive abstraction.

## 5. Workflows
1.  **Audit**: Developer triggers the skill.
2.  **Report**: Skill generates a markdown report.
3.  **Refactoring**: Developer applies changes, ensuring **Architectural Invariants** are maintained.
4.  **Verification**: Regression testing.

## 6. Success Metrics
-   Reduction in Cognitive Complexity.
-   Successful decomposition of `ARSandboxController` methods.
-   Zero regression bugs.
