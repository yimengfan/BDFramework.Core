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

- Keep Unity-side orchestration minimal and centered around generic transport, test-runner, or editor-bridge entrypoints. Avoid scattering control flow across thin single-purpose commands.
- Editor-control changes must preserve the rule that Playwright is the primary orchestrator. Prefer the cached reflection gateway and Unity official APIs before adding Unity-side wrapper commands.
- If TCP protocol fields, message types, or reconnect semantics change, update both Unity and Playwright implementations together, then rerun the affected verification entrypoints.
- Playwright test naming must use ASCII English only: `description-e2e.spec.ts` for cross-platform cases or `description-platform-e2e.spec.ts` for platform-specific cases. Supported explicit platform suffixes are `EditorPlayer`, `Android`, `Windows`, and `MacOS`.
- Keep package documentation, scripts, and test names synchronized when execution entrypoints change.
- New or changed package behavior must add or update the closest automated verification. Prefer package tests first, then the matching Playwright or batchmode entrypoint.

## Validation Entry

- For runtime and Unity-side verification, use the package's documented Unity E2E entrypoints and any nearby automated tests.
- For Editor-control flows, prefer `Playwright~/tools/test-editorplayer.sh`.
- For batchmode coverage, prefer `Playwright~/tools/test-batchmode.sh` or the documented `RunE2EAndExport` flow.
- For platform device flows, use the matching `Playwright~/tools/test-android.sh` or `Playwright~/tools/test-pc.sh` entrypoints.