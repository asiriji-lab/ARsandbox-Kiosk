# Contributing to AR Sandbox Kiosk

Thank you for your interest in contributing! This guide will help you get started.

## Getting Started

1. **Fork** the repository.
2. **Clone** your fork locally.
3. Open the project in **Unity 2022.3 LTS**.
4. Create a **feature branch** from `main`.

## Coding Standards

Please follow the project's [Coding Standards](./Docs/Standards/Coding_Standard.md):

- **Public members**: `PascalCase`
- **Private fields**: `_camelCase`
- **Methods**: One responsibility per method
- **No magic numbers**: Extract constants

## Pull Request Process

1. Ensure your code compiles without errors in Unity.
2. Run EditMode tests via Unity Test Runner.
3. Write a clear PR description explaining **what** and **why**.
4. Reference any related issues.

## Architecture Overview

The project uses a decoupled MVVM pattern. See the [System Architecture](./Docs/Technical/System_Architecture.md) for details on the GPU pipeline and module responsibilities.

## Reporting Issues

When filing a bug report, please include:
- Unity version
- Hardware (Kinect model, GPU)
- Steps to reproduce
- Console log output
