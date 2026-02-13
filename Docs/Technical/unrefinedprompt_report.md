Technical Migration Guide: Transitioning from Legacy 3D Sensors to the Orbbec Femto/Gemini Ecosystem

This guide provides the architectural framework and implementation protocols for migrating Unity-based computer vision pipelines from legacy standards (e.g., Azure Kinect DK) to the Orbbec Gemini ecosystem. As a specialist, you are not simply replacing a sensor; you are expanding the "perceptual envelope" of your system and equipping your agents with modular, high-precision 3D vision "Skills."

1. Hardware Identification and the Perceptual Envelope

The Orbbec Gemini 305 and 345Lg represent a generational leap in close-range optics and environmental resilience. For robotics developers, the most significant shift is the reduction of the perception blind zone.

* Blind Zone Optimization: Legacy sensors typically bottom out at 7 cm. The Gemini 305 reduces this to 4 cm—a 43% improvement. This allows for "fingertip-class" precision in intimate manipulation tasks.
* The Perceptual Envelope: The Gemini series maintains sub-millimeter depth accuracy at 15 cm within a total working range of 4 cm to 100 cm.
* Robotic Wrist Integration (305g): To accommodate high-frequency dynamic routing and mechanical vibration, the Gemini 305g variant replaces standard USB-C with a GMSL2 serializer and FAKRA connector. This is a requirement for industrial-grade cabling where electromagnetic interference (EMI) and physical disconnects are common failure points in robotic arms.

2. Platform Compatibility and Computational Offloading

Migration involves moving toward tighter integration with the NVIDIA AI ecosystem, utilizing sensors as specialized data providers for the "Agentic Brain."

* NVIDIA Jetson Thor: The Gemini 330 series is validated for the NVIDIA Jetson Thor system-on-module (SoM). This allows for direct computational offloading of vision-language-action (VLA) models.
* Holoscan Sensor Bridge (HSB): The Gemini 335Lg is validated for the NVIDIA HSB, ensuring a low-latency, high-bandwidth pipeline (60 ms latency at 1280×800) essential for real-time collision avoidance.
* Global Supply Chain Resilience: Orbbec operates dual manufacturing hubs in Shunde, China, and Vietnam (operational May 2026), ensuring stable OEM capacity for global deployments.

3. Unity Integration: The Agent Skills Architecture

In the Antigravity ecosystem, the Orbbec sensor acts as the "eyes," while Agent Skills act as the "brains." By adopting a modular skill architecture, you prevent "Context Saturation"—where an agent's reasoning is slowed by irrelevant procedural data.

Standardized Directory Structure

Implement the following hierarchy to manage vision logic and hardware protocols:

* /.agent/skills/: Workspace-scoped logic (e.g., project-specific grasping).
* /scripts/: Deterministic Python/Bash execution (Binary Truth providers).
* /references/: Hardware datasheets and calibration JSONs.
* ~/.gemini/antigravity/skills/: Global utilities (e.g., universal FOV calculators).

The SKILL.md Definition

Every skill must be semantically discoverable via a SKILL.md file. Use precise descriptions to ensure the LLM matches developer intent to the correct hardware preset.

---
name: gemini-resolution-switcher
description: Use this skill to validate and switch Gemini 305 presets. Required for transitioning between semantic Dual RGB understanding and spatial sub-millimeter grasping.
---
# Goals
Ensure the sensor is in the optimal mode for the current task.
# Instructions
1. Check current task: If "object identification," use Dual RGB. If "precise grasping," use Depth + Color.
2. Execute C# preset trigger via the Unity Bridge.


4. Code Migration: API Decoupling and Stream Optimization

The Gemini 305 enables on-demand decoupling, allowing independent configuration of color and depth resolutions—a feature often locked in legacy SDKs.

One-Click Preset Switching

Developers must choose modes based on the specific perceptual requirement:

* Dual RGB: Optimized for semantic understanding and vision-language models.
* Depth + Color: Optimized for spatial perception and edge-computing efficiency.

// Architect's Note: Switch modes only when intent changes to preserve memory.
public void ConfigureSensorMode(bool requiresHighPrecision) {
    if (requiresHighPrecision) {
        // High-res depth for sub-millimeter tasks
        sensor.SetPreset(GeminiPresets.DepthPlusColor); 
    } else {
        // Dual streams for semantic analysis
        sensor.SetPreset(GeminiPresets.DualRGB);
    }
}


HLSL Optimization: Boolean Flag Encoding

To maximize instance data efficiency in Unity shaders (Shader Model 3.0), encode up to 23 boolean flags into the significand of a single float.

Application Side (C# Packing):

// Pack flags into the 3rd row, 4th column of the instance matrix (data[2][3])
int flags = (fullbright ? 1 : 0) | (clamped ? 2 : 0) | (xRepeat ? 4 : 0);
instanceMatrix[2, 3] = (float)flags; 


Vertex Shader (HLSL Decoding):

// Extracting the 23-bit significand data
int flags = data[2][3]; 
bool isFullbright = fmod(flags, 2.0) == 1.0;
bool isClamped = fmod(flags, 4.0) >= 2.0;
bool xRepeat = fmod(flags, 8.0) >= 4.0;


5. Technical Specifications Comparison

Specification	Gemini 305 / 305g	Gemini 345Lg (Rugged)
Connector	USB-C (305) / FAKRA (305g)	FAKRA (GMSL2)
Resolution/FPS	1280 × 800 @ 60 fps	1280 × 800 @ 60 fps
Latency	60 ms	60 ms
Depth FOV	88° × 65°	Dual Mode: 104°×87° (Std) / 91°×78° (Wide)
RGB/IR FOV	RGB: 94°×68°	RGB: 137°×71° / IR: 130°×95°
Perceptual Envelope	4 cm to 100 cm	4 cm to 100 cm (100 klux Sunlight)
Specialized Modes	Dual RGB + Decoupled Res	Dual RGB + Decoupled Res
Environment	Indoor / Robotic Wrist	IP67 / -20°C to 65°C

6. System Architecture: Level 4 Tool Use Pattern

For high-reliability robotics, LLM "guesswork" is a safety violation. Logic must be delegated to deterministic scripts—the Level 4 Tool Use Pattern.

1. Intent Matching: The agent identifies a need to validate a camera schema.
2. Delegation: The agent executes validate_schema.py rather than attempting to parse the SQL or JSON itself.
3. Binary Truth: The script returns Exit Code 0 (Success) or Exit Code 1 (Failure). This provides the agent with an empirical foundation for its next action.

7. Troubleshooting and Performance Optimization

* Handling Exit Code 1: Usually indicates a failure in schema validation (e.g., missing id primary keys or non-snake_case naming). Skills should instruct the agent to report the specific script output to the user immediately.
* Mitigating Context Rot: Large 3D datasets and long-running sessions cause "Context Rot." Use Progressive Disclosure: only load the heavy vision-processing "Skills" (and their associated procedural knowledge) when the sensor is actively being queried.
* Environmental Interference: In outdoor logistics or environments with intense lighting, the Gemini 345Lg is mandatory. Standard sensors will fail under 100 klux sunlight; the 345Lg uses specialized IR filtering to maintain depth integrity.

8. Reference Documentation

* Antigravity Official Site: https://antigravity.google/
* Skill Architecture Docs: https://antigravity.google/docs/skills
* Skill Assets & Templates: https://github.com/rominirani/antigravity-skills
