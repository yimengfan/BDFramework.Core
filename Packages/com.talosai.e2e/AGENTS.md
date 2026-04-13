# Talos E2E Package Rules

## Non-Negotiable Rules

- Playwright owns the main orchestration flow. It decides when to launch the app, wait for status checks, open scenes, drive UI actions such as `page.click`, and trigger Unity-side unit or integration tests.
- Unity owns test execution and result delivery. Unity code should implement the interfaces that Playwright needs, run core module unit and integration tests, produce outputs, and return results back to Playwright. Capabilities such as screenshots belong to this interface layer.
- When Unity API behavior or Editor API details are needed, use the local reference under `Documentation~/` before adding wrappers or custom abstractions.

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
- `Runtime/Tests/`: Unity-side unit and integration coverage for framework modules.
- `Editor/`: editor bridge entrypoints and editor-only helpers.
- `Playwright~/`: orchestration, fixtures, scripts, and platform runners.
- `Documentation~/`: local Unity API mirror for lookup only.

## Working Rules

- Keep all Markdown documentation in English and keep it short.
- Treat `Documentation~/` as a local reference mirror. Do not rely on it as tracked product content.
- Update both Unity and Playwright when TCP message shapes, reconnect behavior, or command contracts change.
- Prefer Unity official APIs plus the cached reflection gateway before adding new Unity-side editor commands.
- Add or update the closest automated verification for every behavior change.

## Test Naming

- Use `description-e2e.spec.ts` for cross-platform cases.
- Use `description-EditorPlayer-e2e.spec.ts`, `description-Android-e2e.spec.ts`, `description-Windows-e2e.spec.ts`, or `description-MacOS-e2e.spec.ts` for platform-specific cases.

## Validation Entry Points

- Use `Playwright~/tools/test-batchmode.sh` for batchmode validation.
- Use `Playwright~/tools/test-editorplayer.sh` for editor-control validation.
- Use `Playwright~/tools/test-android.sh` for Android validation.
- Use `Playwright~/tools/test-pc.sh` for Windows or macOS player validation.

## Reading Order

1. `.github/copilot-instructions.md`
2. `AI_RULES_INDEX.md`
3. `.github/instructions/e2e.instructions.md`
4. This file
5. The specific `Runtime/`, `Editor/`, or `Playwright~/` files being changed
6. `Documentation~/` only when Unity API details are required
