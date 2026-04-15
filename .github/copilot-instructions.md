# BDFramework Copilot Mandatory Rules

This file is the mandatory workspace instruction set for GitHub Copilot in this repository.

## Module Index

- Copilot mandatory rules file: `.github/copilot-instructions.md`
- Sync registry: `AI_RULES_INDEX.md`
- Module instructions: `.github/instructions/ci.instructions.md`, `.github/instructions/bdframework.instructions.md`, `.github/instructions/e2e.instructions.md`

## Baseline Code Standards

- **All comments and docstrings must be written in Chinese (中文).** This applies to module docstrings, class docstrings, function docstrings, process comments, fixture descriptions, test docstrings, inline comments, and configuration file comments. English-only docstrings do not satisfy this requirement.
- Every touched class must have a class-level comment or docstring in Chinese. It must explain the design role of the type, why it exists, and include an example or usage note for key business, protocol, pipeline, or orchestration classes.
- Every touched function or method must have a comment or docstring in Chinese that explains purpose and behavior. For non-trivial helpers, document inputs, outputs, side effects, fallback rules, or failure contract rather than repeating parameter names.
- Test files (pytest, Unity tests, etc.) are NOT exempt: every test module, test class, test function, fixture, and test helper must have a Chinese docstring explaining its purpose and the scenario it validates.
- Major workflows must stay concentrated around an explicit entry or coordinator method so the end-to-end path can be read in one place. Do not scatter the primary process across distant files or tiny helpers without a clear bridge.
- Major workflows and critical branches must include process comments in Chinese. Use phase-oriented comments so readers can follow the flow from top to bottom without reconstructing it from logs alone.
- Critical configuration files must be documented in code comments or docstrings in Chinese where they are declared, loaded, generated, or written. The comment must explain the file purpose and who produces and consumes it.
- Every new or changed code path must add or update automated tests. Prefer unit tests first; if a flow cannot be covered purely with unit tests, add the closest automated verification and explain the gap. Relevant tests must pass before the task is considered complete.

## Markdown Documentation Standards

- All `.md` documentation files must be written in English.
- Keep `.md` files concise and high-signal. Avoid bloated structure, repetitive narration, and low-value trivia.
- Do not update `.md` files for minor wording churn or routine noise. Update them only when behavior, entrypoints, ownership, or required policy actually changes.

## Mandatory Conventions

- Important multi-step flows must emit explicit logs at entry, key branch or fallback, and completion or error so runtime debugging does not rely on inference.
- Automated tests, batch verification entries, and CI validation entrypoints must print Chinese start logs with explicit `测试目的=` and `实现手段=` markers, and multi-step or long-running checks must continue emitting key progress logs so the current validation stage is visible in console and TeamCity output.
- Unity3D business-layer code must not use reflection.
- Reflection is allowed only lightly in framework or infrastructure code when needed for compatibility, platform isolation, or controlled extension points, and the reason must be documented in code comments.


## Naming vs Comment Language Boundary

- **File names and directory names** must use ASCII English only. No Chinese, Japanese, or other non-Latin characters.
- **C# identifiers** — class names, method names, property names, parameter names, enum values — must use English.
- **Attribute parameter default values** that serve as code-level conventions must use English (e.g. `suite: "default"`, not `"默认"`).
- **Runtime log text** may use Chinese, since it is developer-facing readable output.
- **Code comments and docstrings** must use Chinese per Baseline Code Standards.
- Mnemonic: **Names in English, comments in Chinese, logs may be Chinese.**

## Package Independence Constraint

- Packages marked as generic (e.g. `com.talosai.e2e`) must not contain any specific business-party test cases, configurations, or hardcoded logic.
- Business-party test code must live in the business party's own package or project directory, referencing the generic package to use its capabilities.
- Test: if removing a piece of code leaves the generic package still usable by other projects, that code does not belong in the package.

## Scope Guardrails

- Do not modify third-party packages or vendored plugin code, especially `Packages/com.code-philosophy.*`.
- Package-scoped code changes are allowed only under first-party embedded packages, currently `Packages/com.popo.bdframework` and `Packages/com.talosai.e2e`.
- If third-party behavior must change, solve it from `Packages/com.popo.bdframework`, `Packages/com.talosai.e2e`, or project-level files such as `ProjectSettings/`, not by patching the upstream package.

## Completion Checklist

Every task must pass all items below before being considered complete:

- [ ] Local tests pass (lint / unit test / smoke test)
- [ ] Changes are committed and pushed to remote
- [ ] Remote CI passes
- [ ] No Chinese file names or directory names (comments and logs may use Chinese)
- [ ] C# identifiers and Attribute default parameter values use English
- [ ] Generic packages contain no business-party-specific tests or hardcoded logic