# Architecture Overview

> [!IMPORTANT]
> **This document has been superseded.**
>
> Please refer to the new **[System Architecture](../System_Architecture.md)** document for the canonical description of the GPGPU pipeline, Compute Shaders, and system modules.

---

## Legacy Context (Retained for Reference)

The original architecture (Job System + Burst) described here is now considered **Legacy**. The current implementation uses a **Zero-Copy Compute Shader** pipeline for all depth processing and mesh generation.
