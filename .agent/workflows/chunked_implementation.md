---
description: Pipeline for executing complex technical tasks in manageable chunks with mandatory evaluation steps.
---

# Chunked Implementation Framework (Ralph Enhanced)

This workflow ensures that complex refactors or features are implemented safely by breaking them into logical "Chunks." It leverages **Ralph-style memory persistence** to ensure continuity across AI iterations.

## Core Principles
1. **Macro & Micro Memory**: Every major task starts with a fresh mindset, but relies on `.agent/Chunks_Overview.md` for the long-term roadmap and `.agent/progress.txt` for the immediate sub-steps of the active chunk.
2. **Atomic Iterations**: Each chunk is a single unit of work that ends with a commit.
3. **Mandatory Verification**: Code is never "done" until verified by tools or the user.
4. **One-at-a-Time Constraint**: The agent MUST only execute one chunk per turn. Do not attempt to merge chunks or work ahead.

## Pipeline Structure

### 1. Preparation
- **Synchronize**: Read `.agent/Chunks_Overview.md` (roadmap), `.agent/progress.txt` (current sub-tasks), and `Assets/Docs/Standards/Coding_Standard.md` (project rules).
- **Chunking**: Define the roadmap in `.agent/Chunks_Overview.md`. Break the current chunk into micro-tasks in `.agent/progress.txt`.

### 2. Chunk Execution Loop
For each Chunk:

#### **A. Implementation Phase**
- Implement the specific logic for the chunk.
- **Document Patterns**: If you discover a reusable pattern, add it to `Assets/Docs/Standards/Coding_Standard.md` or the `Assets/Docs/Technical/Implementation_Manual.md`. Use `.agent/progress.txt` only for immediate task-specific notes.

#### **B. Evaluation Phase (Mandatory)**
- Perform at least one:
  - **Compiler/Lint**: `npm run build`, `dotnet build`, etc.
  - **Unit Testing**: Run relevant tests.
  - **Browser/UI Check**: Use the browser subagent if the UI changed.
- **Log Progress**: 
  - Update micro-tasks in `.agent/progress.txt`.
  - When a chunk is finished, update its status in `.agent/Chunks_Overview.md`.
  - **MANDATORY**: Record any bug fixes or non-obvious resolutions in `Assets/Docs/History/Bug_Fix_Log.md`.
- **Commit**: If evaluation passes, commit changes: `feat: [Chunk ID] - [Description]`.

### 3. Final Verification
- Run full system verification.
- Create/Update `walkthrough.md` with proof of work (screenshots/recordings).

---

## Example Task Format
- [ ] Chunk 1: [Name]
    - [ ] Implementation
    - [ ] Log Learning to `.agent/progress.txt`
    - [ ] Evaluation & Commit
- [ ] Chunk 2: [Name]
    - [ ] Implementation
    - [ ] Log Learning to `.agent/progress.txt`
    - [ ] Evaluation & Commit
