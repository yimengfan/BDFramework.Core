---
description: "Use when editing the Talos E2E package runtime, editor bridge, Playwright orchestrator, documentation, or test assets under Packages/com.talosai.e2e."
applyTo: "Packages/com.talosai.e2e/**"
---

# Talos E2E Package Instructions

## Scope

- Applies to `Packages/com.talosai.e2e/**`.
- Primary targets are runtime transport, E2E test runner, editor bridge, Playwright orchestration, local Unity documentation mirror, and package-scoped documentation.

## Reading Order

1. Read `.github/copilot-instructions.md`.
2. Read `AI_RULES_INDEX.md`.
3. Read this file.
4. Read `Packages/com.talosai.e2e/AGENTS.md`.
6. Read the specific runtime, editor, Playwright, or test files related to the change.

## Rules

- All comments and docstrings under this package must be written in paired Chinese and English form. Put Chinese first and follow with the English version in the same comment block. Chinese-only or English-only comments do not satisfy this requirement.
- Keep Unity-side orchestration minimal and centered around generic transport, test-runner, or editor-bridge entrypoints. Avoid scattering control flow across thin single-purpose commands.
- Talos E2E is a capability package, not a host workflow package. Do not add host-owned startup composition, framework initialization, resource bootstrapping, scene sequencing, executeMethod wrappers, or fallback recovery flows here.
- If a scenario depends on project or business scenes, config, assets, manager initialization, or launch order, define that flow in the host or business package and keep Talos limited to reusable transport, fixtures, connectors, and editor or runtime primitives.
- Keep Playwright code under this package generic. Do not add business-party scenario scripts, one-project launch recipes, or host-only acceptance choreography under `Playwright~/` just to help another package pass.
- Editor-control changes must preserve the rule that Playwright is the primary orchestrator. Prefer the cached reflection gateway and Unity official APIs before adding Unity-side wrapper commands.
- Treat the `unityplayer` Playwright project as an editor-control lane, not a runtime-complete validation lane. Route editor-control cases to `*-EditorPlayer-e2e.spec.ts` and keep reusable runtime suites in batchmode or platform runtime projects.
- If TCP protocol fields, message types, or reconnect semantics change, update both Unity and Playwright implementations together, then rerun the affected verification entrypoints.
- Playwright test naming must use ASCII English only: `description-e2e.spec.ts` for cross-platform cases or `description-platform-e2e.spec.ts` for platform-specific cases. Supported explicit platform suffixes are `EditorPlayer`, `Android`, `Windows`, and `MacOS`.
- Keep package documentation, scripts, and test names synchronized when execution entrypoints change.
- New or changed package behavior must add or update the closest automated verification. Prefer package tests first, then the matching Playwright or batchmode entrypoint.

## Validation Entry

- For runtime and Unity-side verification, use the package's documented Unity E2E entrypoints and any nearby automated tests.
- For Editor-control flows, prefer `Playwright~/tools/test-editorplayer.sh`.
- `Playwright~/tools/test-editorplayer.sh` should validate editor-command, scene-control, and PlayMode-control scenarios only; do not use it as the sole gate for host runtime suites.
- For batchmode coverage, prefer `Playwright~/tools/test-batchmode.sh` or the documented `RunE2EAndExport` flow.
- Use `Playwright~/tools/test-batchmode.sh` in default TCP mode or `TALOS_MODE=sync` as the runtime-complete gate for reusable cross-platform suites.
- For platform device flows, use the matching `Playwright~/tools/test-android.sh` or `Playwright~/tools/test-pc.sh` entrypoints.