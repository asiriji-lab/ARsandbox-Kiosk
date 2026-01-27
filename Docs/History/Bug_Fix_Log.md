# Bug Fix & Issue Log 🛠️

This file tracks granular bug fixes, technical issues, and their resolutions to prevent regression and assist in future debugging.

---

## [2026-01-26] Black Peak Artifacts (FIXED)
- **Issue**: Visible black pixelated blobs appearing on terrain peaks.
- **Cause**: `normalize(glitterVec)` in the sparkle logic was producing NaN when `glitterVec` was zero (at the world origin).
- **Fix**: Added a safety check `if (any(glitterVec))` before normalization and added an epsilon to triplanar weight normalization.
- **Result**: Black artifacts eliminated.

---

## [2026-01-26] Documentation & Standards Conflict
- **Issue**: Three separate coding standard documents provided conflicting naming conventions.
- **Fix**: Consolidated all standards into a single [Coding_Standard.md](../Standards/Coding_Standard.md).
- **Result**: Reduced potential for AI hallucination.

## [2026-01-26] Rogue Documentation Management
- **Issue**: Technical markdown files were scattered across the `Assets/` root.
- **Fix**: Moved files into a structured `Assets/Docs/` hierarchy.
- **Result**: Improved project navigability.

## [2026-01-26] Workflow Path Breakage
- **Issue**: The `chunked_implementation` workflow broke due to deleted files.
- **Fix**: Updated workflow to reference the new structure.
- **Result**: Restored workflow functionality.
