# UX Audit & Pain Points Report

**Date**: 2026-02-06
**Scope**: Kiosk Admin & Setup Flow
**Auditor**: Antigravity (Agent)

## Executive Summary
The current Kiosk UX is functional but fragile. It relies heavily on "Hidden Knowledge" (keyboard shortcuts, specific click orders) and lacks robustness for a touch-first environment. The **ROI Editor** in particular is a high-friction area where a single mistake forces a full restart.

---

## 1. Critical Pain Points (High Severity)

### 🚨 ROI Editor: The "All-or-Nothing" Trap
*   **The Issue**: In `ROIEditorView.cs`, there is no "Undo" function.
*   **The Pain**: If a user places 3 perfect points and misplaces the 4th, their only option is `Clear Points` which wipes **everything**.
*   **Code Reference**: `ROIEditorView.cs:341` (`ClearPoints()` clears strict list).
*   **Recommendation**: Implement a "Remove Last Point" button or functionality.

### 🚨 Silent Failures
*   **The Issue**: Clicking "SAVE & EXIT" with fewer than 4 points does nothing visible.
*   **The Pain**: The system logs a warning (`Debug.LogWarning`), but the user on the kiosk screen sees no feedback. They will click frantically thinking the button is broken.
*   **Code Reference**: `ROIEditorView.cs:367` (Log only, no UI feedback).
*   **Recommendation**: Add a "Status Text" field in red clearly stating: *"Complete the box (4 points) to save."*

### 🚨 The "Hidden Key" Problem
*   **The Issue**: The Main UI is toggled via `Tab` or `~`.
*   **The Pain**: Kiosks often lack physical keyboards. If the UI is closed (or auto-hides), a touch-only operator has **no way** to access the settings without plugging in hardware.
*   **Code Reference**: `SandboxUI.cs:248` (Checks `Keyboard.current.tabKey`).
*   **Recommendation**: Implement a "Secret Gesture" (e.g., long-press Top-Left corner) or a specialized physical button mapped to this action.

---

## 2. Friction Points (Medium Severity)

### ⚠️ Invisible Instructions
*   **The Issue**: The ROI Editor opens a blank camera feed.
*   **The Pain**: There is no text telling the user: *"Click the 4 corners of the sand box."*
*   **Recommendation**: Add an overlay instruction text that fades out after interaction.

### ⚠️ Touch Target Sizing
*   **The Issue**: Standard Unity Sliders are used.
*   **The Pain**: The handle (Knob) size might be too small for an adult finger on a touch screen, leading to missed drags.
*   **Recommendation**: Ensure `KnobSprite` is visually distinct and has a large raycast target padding.

## 3. Workflow Analysis

### Current "Masking" Workflow
1.  Click "Edit Boundary"
2.  *User stares at screen (What do I do?)*
3.  User clicks a point. A red dot appears. *Okay.*
4.  User clicks 3 more points.
5.  User clicks "Save".
    *   *If User clicked 5 times by accident?* -> The 5th click is ignored.
    *   *If User clicked 3 times?* -> The Save button ignores them.

### Proposed "Masking" Workflow
1.  Click "Edit Boundary"
2.  **Instruction**: "Tap the 4 corners of the active sand area."
3.  User clicks point. **Feedback**: "Point 1/4 set".
4.  User mis-clicks.
5.  User hits **Undo**. Point removes.
6.  User finishes 4 points. **Feedback**: "Shape Complete. Save?".
7.  User clicks "Save".

## 4. Next Steps
1.  **Refactor `ROIEditorView`**: Add Undo, Status Label, and Instructions.
2.  **Update `SandboxUI`**: visual feedback for the operator.
