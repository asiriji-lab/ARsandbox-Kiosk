---
description: Pipeline for executing complex technical tasks in manageable chunks with mandatory evaluation steps.
---

# Chunked Implementation Framework (Ralph Enhanced)

This workflow ensures that complex refactors or features are implemented safely by breaking them into logical "Chunks." It leverages **Ralph-style memory persistence** to ensure continuity across AI iterations.

## Core Principles
1. **Fresh Context, Shared Memory**: Every major task starts with a fresh mindset, but relies on `.agent/progress.txt` and `.agent/AGENTS.md` for shared context.
2. **Atomic Iterations**: Each chunk is a single unit of work that ends with a commit.
3. **Mandatory Verification**: Code is never "done" until verified by tools or the user.

## Pipeline Structure

### 1. Preparation
- **Synchronize**: Read `.agent/progress.txt` and any local `.agent/AGENTS.md` to understand current architecture and patterns.
- **Chunking**: Break the `implementation_plan.md` into numbered **Chunks**. Each chunk should be an atomic user story or technical unit.

### 2. Chunk Execution Loop
For each Chunk:

#### **A. Implementation Phase**
- Implement the specific logic for the chunk.
- **Document Patterns**: If you discover a reusable pattern, add it to the local `.agent/AGENTS.md` or the "Codebase Patterns" section of `.agent/progress.txt`.

#### **B. Evaluation Phase (Mandatory)**
- Perform at least one:
  - **Compiler/Lint**: `npm run build`, `dotnet build`, etc.
  - **Unit Testing**: Run relevant tests.
  - **Browser/UI Check**: Use the browser subagent if the UI changed.
- **Log Progress**: Append a summary to `.agent/progress.txt` (ID, thread URL, learnings).
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
