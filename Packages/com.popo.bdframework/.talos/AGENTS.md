# BDFramework Talos Rules

## Mirror Index

- This file is the Talos-side mirror of the project-level Copilot mandatory instructions.
- Canonical sync peer: `.github/copilot-instructions.md`
- Maintenance rule: any rule added, removed, or reworded here must be mirrored in `.github/copilot-instructions.md` in the same change.
- Sync registry: `AI_RULES_INDEX.md`
- Module instructions: `.github/instructions/ci.instructions.md`, `.github/instructions/bdframework.instructions.md`
- Module indexes: `DevOps/CI/AI_RULES_INDEX.md`, `Packages/com.popo.bdframework/AI_RULES_INDEX.md`

## Baseline Code Standards

- Every touched class must have a class-level comment or docstring. It must explain the design role of the type, why it exists, and include an example or usage note for key business, protocol, pipeline, or orchestration classes.
- Every touched function or method must have a comment or docstring that explains purpose and behavior. For non-trivial helpers, document inputs, outputs, side effects, fallback rules, or failure contract rather than repeating parameter names.
- Major workflows must stay concentrated around an explicit entry or coordinator method so the end-to-end path can be read in one place. Do not scatter the primary process across distant files or tiny helpers without a clear bridge.
- Major workflows and critical branches must include process comments. Use phase-oriented comments so readers can follow the flow from top to bottom without reconstructing it from logs alone.
- Critical configuration files must be documented in code comments or docstrings where they are declared, loaded, generated, or written. The comment must explain the file purpose and who produces and consumes it.
- Every new or changed code path must add or update automated tests. Prefer unit tests first; if a flow cannot be covered purely with unit tests, add the closest automated verification and explain the gap. Relevant tests must pass before the task is considered complete.

## Mandatory Conventions

- Important multi-step flows must emit explicit logs at entry, key branch or fallback, and completion or error so runtime debugging does not rely on inference.
- Unity3D business-layer code must not use reflection.
- Reflection is allowed only lightly in framework or infrastructure code when needed for compatibility, platform isolation, or controlled extension points, and the reason must be documented in code comments.

## Scope Guardrails

- Do not modify third-party packages or vendored plugin code, especially `Packages/com.code-philosophy.*`.
- Package-scoped code changes are allowed only under `Packages/com.popo.bdframework`.
- If third-party behavior must change, solve it from `Packages/com.popo.bdframework` or project-level files such as `ProjectSettings/`, not by patching the upstream package.