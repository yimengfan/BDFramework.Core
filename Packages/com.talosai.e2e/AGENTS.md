# Talos E2E Package Rules

## Non-Negotiable Rules

- Playwright owns the main orchestration flow. It decides when to launch the app, wait for status checks, open scenes, drive UI actions such as `page.click`, and trigger Unity-side unit or integration tests.
- Unity owns test execution and result delivery. Unity code should implement the interfaces that Playwright needs, run core module unit and integration tests, produce outputs, and return results back to Playwright. Capabilities such as screenshots belong to this interface layer.
- When Unity API behavior or Editor API details are needed, use the local reference under `Documentation~/` before adding wrappers or custom abstractions.
- Talos E2E is a capability package, not a host workflow package. Do not compose host-owned startup, initialization, scene sequencing, executeMethod wrappers, or fallback recovery workflows inside this package.
- If a scenario requires project-specific scenes, framework bootstrap, resource or database preparation, manager warm-up, or business launch order, define that flow in the host or business package and let Talos consume it through generic connectors or explicit host-owned entrypoints.
- Do not add business-party multi-step scenario scripts, launch recipes, or one-off recovery logic under `Playwright~/`, `Editor/`, or `Runtime/` just to make a single project pass. Business parties should reuse Talos fixtures, connectors, and transport primitives from their own package code.

## Scope

- Applies to `Packages/com.talosai.e2e/**`.

## Architecture Split

- Keep orchestration logic in `Playwright~/src/`, `Playwright~/tests/`, and the shell tools under `Playwright~/tools/`.
- Keep Unity runtime and editor responsibilities in `Runtime/` and `Editor/`.
- Do not move basic control flow from Playwright into Unity-side wrapper commands.
- Unity-side wrappers are allowed only for multi-step flows that need state coordination, recovery, or platform-specific fault handling.

## Package Layout

- `Runtime/Transport/`: TCP protocol and client/server transport.
- `Runtime/TestRunner/`: bootstrap, discovery, execution, and result export.
- `Runtime/Tests/`: Unity-side infrastructure-level test coverage only (generic, reusable examples). Business-party tests belong in their own package.
- `Editor/`: editor bridge entrypoints and editor-only helpers.
- `Playwright~/`: orchestration, fixtures, scripts, and platform runners.
- `Documentation~/`: local Unity API mirror for lookup only.

## Working Rules

- Keep all Markdown documentation in English and keep it short.
- Treat `Documentation~/` as a local reference mirror. Do not rely on it as tracked product content.
- Update both Unity and Playwright when TCP message shapes, reconnect behavior, or command contracts change.
- Prefer Unity official APIs plus the cached reflection gateway before adding new Unity-side editor commands.
- Keep package-owned Playwright orchestration generic. Package scripts and tests may validate reusable editor-control or runtime lanes, but they must not encode host-only acceptance flows or business-party scenario choreography.
- Add or update the closest automated verification for every behavior change.
- This package is generic infrastructure. Do not add business-party-specific test cases, configurations, or hardcoded logic here. Business-party test code belongs in the business party's own package directory, referencing this package for its capabilities.

## Test Naming

- Use `description-e2e.spec.ts` for cross-platform cases.
- Use `description-EditorPlayer-e2e.spec.ts`, `description-Android-e2e.spec.ts`, `description-Windows-e2e.spec.ts`, or `description-MacOS-e2e.spec.ts` for platform-specific cases.

## Validation Entry Points

- Use `Playwright~/tools/test-batchmode.sh` for batchmode validation.
- Use `Playwright~/tools/test-editorplayer.sh` for editor-control validation only.
- Use `Playwright~/tools/test-android.sh` for Android validation.
- Use `Playwright~/tools/test-pc.sh` for Windows or macOS player validation.

## Runtime Matrix

- `test-batchmode.sh` in `TALOS_MODE=sync` is the fallback runtime-complete gate when TCP orchestration is unavailable or unnecessary.
- `test-batchmode.sh` in default TCP mode is the main runtime-complete gate for cross-platform suites without a platform suffix.
- `test-editorplayer.sh` is not the runtime-complete gate. It validates editor-command, scene, and PlayMode-control flows through `*-EditorPlayer-e2e.spec.ts` only.
- Do not assume `unityplayer` can stand in for the runtime launch gate. Host runtime suites such as `launch`, `framework-contract`, and `framework-integration` belong to batchmode or device/player runtime projects.
- Static editor-only runs can emit step-screenshot warnings. Treat them as capability limits of the static mode unless the test explicitly requires screenshot success.

## Reading Order

1. `.github/copilot-instructions.md`
2. `AI_RULES_INDEX.md`
3. `.github/instructions/e2e.instructions.md`
4. This file
5. The specific `Runtime/`, `Editor/`, or `Playwright~/` files being changed
6. `Documentation~/` only when Unity API details are required
