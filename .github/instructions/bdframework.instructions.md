---
description: "Use when editing the BDFramework package runtime, editor, AOT, tests, asmdefs, startup flow, or resource update logic under Packages/com.popo.bdframework."
applyTo: "Packages/com.popo.bdframework/**"
---

# BDFramework Package Instructions

## Scope

- Applies to `Packages/com.popo.bdframework/**`.
- Primary targets are runtime, editor, AOT, test assemblies, startup flow, resource update flow, and package-scoped documentation.

## Reading Order

1. Read `.github/copilot-instructions.md`.
2. Read this file (BDFramework module instructions).
3. Read `Packages/com.popo.bdframework/.talos/AGENTS.md`.
4. Read `Packages/com.popo.bdframework/Documentation~/README.md`.
5. Read the specific local documentation or test assembly files related to the target feature.

## Rules

- **All comments and docstrings must be written in Chinese (中文).** This applies to XML summaries, inline comments, process comments, test docstrings, and configuration file comments. English-only docstrings do not satisfy this requirement.
- All touched C# classes must keep XML comments current in Chinese. Key business, protocol, pipeline, and orchestration classes must describe design role and include an example or usage note.
- All touched methods must keep XML comments current in Chinese. Key workflow methods must include phase comments so the process can be read top-down.
- New C# protocol or pipeline files must include XML summaries on key classes and key entry/helper methods in Chinese. For non-trivial flows, add concise process comments and at least one usage example in comments or documentation.
- Unity test files are NOT exempt: every test class and test method must have a Chinese comment explaining its purpose and the scenario it validates.
- Runtime partials that extend resource version control must use business-oriented names such as `file server`, `version server`, or `resource server`. `DevOps` may appear only as an explicit mode name or public API suffix. Do not introduce new runtime abstractions named `BuildTools`.
- When extending `AssetsVersionController`, keep edits in `AssetsVersionController.cs` minimal. Prefer outer entry bridges or new partial helpers over inserting hooks into multiple existing private methods.
- When introducing an alternative resource-update protocol, expose it through an explicit public API. Do not make legacy public entrypoints implicitly route into the new protocol unless the requirement explicitly asks for that behavior.
- Resource-update protocol config files and cache files must include comments or documentation in Chinese that explain the file purpose, who writes it, and who consumes it.
- Keep major workflows concentrated in explicit entry or coordinator methods, then bridge to helpers. Do not spread the main process across unrelated files without a clear flow anchor.
- New or changed package code must add or update Unity automated tests. The affected tests must pass before the task is considered complete.
- When changing startup, asset management, resource update, or editor pipeline flows, keep logs, tests, and local documentation synchronized.
- Unity test files must follow `source-file-name + Test.cs`. Example: `AssetsVersionController.DevOps.cs` -> `AssetsVersionController.DevOpsTest.cs`.
- Add or update Unity automated tests for every changed code path and run the affected tests before finishing.

## Validation Entry

- Prefer `Runtime.Test/` and other affected Unity test assemblies for automated coverage.
- If a flow already has a batch verification or explicit verification entrypoint, run it in addition to the closest unit tests.
- If a change affects startup or resource-update behavior, verify the related local documentation and test entrypoints together.