# GitHub Copilot Agent Instructions for Books Inventory Solution

This repository is a polyglot monorepo containing multiple projects and tests organized by language and purpose:

- **/services**: Contains microservices projects
  - C# microservices
  - Python microservices
- **/ui**: React-based user interface applications
- **/shared**: Shared libraries and utilities for C#, Python, and UI code
- **/tests**: Separate folders for language-specific tests
  - `/tests/csharp` for C# unit and integration tests
  - `/tests/python` for Python tests including performance tests with Locust
  - `/tests/integration` for cross-service integration and end-to-end tests
- **/tools**: Scripts and CI/CD helpers
- **/build**: Bazel build configuration files managing multi-language builds

Please provide code completions and suggestions respecting the language and folder context.

- For files under `/services/*/` with `.cs` extension, suggest idiomatic C# code.
- For files under `/services/*/` with `.py` extension, suggest Python idioms.
- For files under `/ui/`, focus on React, JavaScript, and TypeScript patterns.
- For shared libraries, respect the language they are written in and suggest reusable, modular code.
- When asked about testing, consider the appropriate framework based on the language and folder.

Keep suggestions concise, context-aware, and aligned with best practices for microservices and frontend development.

If asked about build or CI/CD, assume Bazel is used as the build system with configuration files under `/build`.
