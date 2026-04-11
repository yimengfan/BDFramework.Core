# BDFramework Package AI Rules Index

## Scope

- Applies to `Packages/com.popo.bdframework/**`.
- Primary targets are runtime, editor, AOT, test assemblies, startup flow, resource update flow, and package-scoped documentation.

## Primary Rule Sources

1. `.github/copilot-instructions.md`
   Repository-wide mandatory baseline.
2. `.github/instructions/bdframework.instructions.md`
   Module-specific BDFramework instructions that auto-attach for `Packages/com.popo.bdframework/**` edits.
3. `.talos/AGENTS.md`
   Talos-side mirror of the public mandatory rules for this repository.
4. `Documentation~/README.md`
   Local documentation entry for startup, manager registration, and resource-update integration.
5. `README.md`
   Package overview and local documentation links.

## Module Baseline

- **All comments and docstrings must be written in Chinese (中文).** This applies to XML comments, module docs, class docs, function docs, test docs, inline comments, and configuration file comments.
- All touched C# classes must keep XML comments current in Chinese. Key business, protocol, pipeline, and orchestration classes must describe design role and include an example or usage note.
- All touched methods must keep XML comments current in Chinese. Key workflow methods must include phase comments so the process can be read top-down.
- Test files (Unity tests, etc.) are NOT exempt: every test module, test class, test method, and test helper must have a Chinese comment explaining its purpose and the scenario it validates.
- Keep major workflows concentrated in explicit entry or coordinator methods, then bridge to helpers. Do not spread the main process across unrelated files without a clear flow anchor.
- New or changed package code must add or update Unity automated tests. The affected tests must pass before the task is considered complete.
- When changing startup, asset management, resource update, or editor pipeline flows, keep logs, tests, and local documentation synchronized.

## Reading Order

1. Read `.github/copilot-instructions.md`.
2. Read `.github/instructions/bdframework.instructions.md`.
3. Read `.talos/AGENTS.md`.
4. Read `Documentation~/README.md`.
5. Read the specific local documentation or test assembly files related to the target feature.

## Validation Entry

- Prefer `Runtime.Test/` and other affected Unity test assemblies for automated coverage.
- If a flow already has a batch verification or explicit verification entrypoint, run it in addition to the closest unit tests.
- If a change affects startup or resource-update behavior, verify the related local documentation and test entrypoints together.