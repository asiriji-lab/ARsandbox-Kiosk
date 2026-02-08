Refactoring Report and Implementation Plan: AR Sandbox Unity Project

1. Executive Summary

The current AR Sandbox codebase has reached a state of "monolithic liability." While functional as a prototype, its reliance on the ARSandboxController "God Class" creates an unsustainable environment of tight coupling and technical debt. This architecture is not merely an aesthetic concern; it is a direct threat to hardware reliability and project scalability. Specifically, the current CPU-to-GPU mesh transfer pipeline risks PCIe bus saturation at high resolutions (512x512), leading to unacceptable latency in an exhibit meant for real-time interaction.

This refactoring plan mandates a transition to a decoupled, reactive architecture that moves the system from a fragile prototype to a production-grade interactive exhibit. By isolating simulation logic from the presentation layer and migrating heavy computation to a GPGPU pipeline, we will ensure the stability required for 24/7 museum operations.

Current State (Technical Debt)	Proposed State (Architecture Target)
Monolithic: ARSandboxController as a logic hub.	Modular: ARSandboxController as a Composition Root.
Tight Coupling: UI dependencies stop the simulation.	Headless-Ready: Model is agnostic of MonoBehaviours.
Polling-Based UI: High CPU overhead/frame drops.	Event-Driven: R3 framework utilizing reactive push.
PCIe Bottleneck: High-frequency vertex array transfers.	GPU-Resident: Drawing via Graphics.RenderPrimitives.
Fragile Recovery: Single-point software hangs.	Resilient: Two-stage Watchdog Timer with safe-state.


--------------------------------------------------------------------------------


2. Architectural Strategy: The New Structural Foundation

Maintaining a high-performance simulation requires the total deconstruction of the ARSandboxController. I mandate that this class be stripped of all hardware and simulation logic, repurposing it exclusively as the Composition Root. In this role, it will solely be responsible for the "Inversion of Control" (IoC) required to inject dependencies into the system.

MVVM (Model-View-ViewModel) Adaptation

We are adopting a strict MVVM pattern. This separation allows us to execute and test simulation logic in a "headless" state, entirely free of Unity’s UI layer or MonoBehaviours.

* Model (Agnostic Logic): The Model must be implemented using pure C# data structures to facilitate unit testing. While SandboxSettings will exist as a ScriptableObject for persistent data and Editor tuning, the simulation logic must operate on raw data arrays. This layer governs the heightmap, erosion math, and water flow logic.
* ViewModel (Reactive Connector): The SandboxViewModel serves as the source of truth for the UI. It uses the R3 framework to bridge the Model’s data to the View.
* View (Passive Presentation): The SandboxUI is a passive observer. It binds to the ViewModel’s ReactiveProperty<T> fields and raises events for user actions.

Service-Based Logic & Dependency Injection

I command the extraction of all specialized logic into independent managers (e.g., IDepthProvider, SimulationLoop). To avoid the common pitfall of "Service Locators"—which hide dependencies and make unit testing difficult—I mandate the use of Constructor Injection. The Composition Root will instantiate these services and pass them to the objects that require them, ensuring a transparent and traceable object graph.


--------------------------------------------------------------------------------


3. Phase 1: The "Clean-Up" (System Decoupling)

Phase 1 focuses on "logical isolation." The goal is a system where the simulation can run without a single UI element present in the scene.

ScriptableObject & Model Isolation

* Action: Convert SandboxSettings to a ScriptableObject.
* Rigor: Ensure the core simulation logic is detached from MonoBehaviour. All math-heavy methods must take raw parameters or structs, enabling high-speed simulation testing in a non-graphical environment.

Reactive Implementation with R3

We are replacing the legacy Rx/polling systems with R3.

* Performance Insight: Unlike standard Rx, R3 avoids ImmutableArray allocations when adding/removing subscriptions, which is critical for long-running exhibits.
* Resilience via OnErrorResume: In standard Rx, an error stops the pipeline. I mandate the use of R3’s OnErrorResume. A hardware sensor glitch or a minor calculation error must not kill the entire simulation stream.
* Leak Prevention: During this phase, you must enable ObservableTracker.EnableTracking = true. For a 24/7 exhibit, memory leaks are non-negotiable failures; use the tracker to identify and eliminate subscription leaks immediately.
* Terminology Note: Use R3-specific nomenclature. Use Chunk for windowing operations and ThrottleLast for sampling.

Deconstruction of ARSandboxController

* Logic to Services: Move HeightMap processing and FlowLogic to the Model/Service layer.
* UI to ViewModel: Move all references to sliders, dropdowns, and debug text to the SandboxViewModel.
* Result: The controller is reduced to a simple script that wires these components together at runtime.


--------------------------------------------------------------------------------


4. Phase 2: The "Optimization" (Performance & GPGPU)

To eliminate the lag at 512x512 resolution, we must move from a "CPU-heavy" model to a "GPU-resident" model.

GPGPU Pipeline and PCIe Mitigation

* Compute Shader Migration: Replace the Burst-based MeshGenJob with a TerrainSimulation.compute shader. The CPU should only orchestrate the simulation, not process the vertex data.
* Direct Drawing: Mandate the use of Graphics.RenderPrimitives. By keeping the mesh data entirely on the GPU and drawing directly from GPU buffers, we bypass the PCIe bottleneck. This eliminates the need to transfer large vertex arrays every frame, preventing PCIe bus saturation and ensuring a stable 60FPS.

Two-Stage Watchdog and Fail-Safes

Industrial-grade exhibits require a multi-level fallback mechanism. We will implement a Two-Stage Watchdog Timer.

* Stage 1 (Safe State): If the software fails to "kick" the watchdog within the programmable timeout (indicating a software hang), the hardware must immediately enter a Safe State. Following DAQmx principles, this involves setting outputs to a known safe voltage or tristate (high impedance) to prevent hardware damage.
* Stage 2 (System Reset): If the system remains unresponsive after the Stage 1 safe-state is triggered, a second timer stage will force a full hardware/system reboot.


--------------------------------------------------------------------------------


5. Implementation Roadmap & Checklist

Step 1: Preparation & Infrastructure

* [ ] Audit all ARSandboxController dependencies.
* [ ] Install R3 framework; configure ObservableSystem.RegisterUnhandledExceptionHandler to route errors to a persistent file logger.
* [ ] Setup the Composition Root to manage service instantiation.

Step 2: Logical Refactoring

* [ ] Extract Model logic into pure C# classes (Agnostic of UnityEngine).
* [ ] Implement SandboxViewModel using R3 ReactiveProperty.
* [ ] Mandate Constructor Injection for the IDepthProvider and SimulationLoop.
* [ ] Integrate ObservableTracker and verify zero subscription leaks after a 1-hour run.

Step 3: Graphics & Performance Migration

* [ ] Port erosion and mesh logic to TerrainSimulation.compute.
* [ ] Implement Graphics.RenderPrimitives for the primary terrain draw call.
* [ ] Verify that vertex data is resident on the GPU, with zero per-frame PCIe transfers of the mesh array.

Step 4: Reliability & Validation

* [ ] Implement the Two-Stage Watchdog:
  * [ ] Stage 1: Trigger safe-state (tristate/safe voltage).
  * [ ] Stage 2: Trigger system reboot.
* [ ] Verify UnhandledException logs are properly saved upon simulation failure.

Definition of Done

1. Performance: Stable 60FPS at 512x512 resolution with no PCIe bus saturation spikes.
2. Architecture: ARSandboxController contains zero business logic and serves only as a Composition Root.
3. Headless-Ready: The simulation Model passes unit tests without requiring the Unity UI or active GameObjects.
4. Resilience: The system successfully enters a Safe State within 500ms of an induced software hang.
5. Memory: ObservableTracker confirms zero unmanaged subscriptions after continuous operation.
