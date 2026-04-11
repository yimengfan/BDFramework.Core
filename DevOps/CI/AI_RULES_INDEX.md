# DevOps CI AI Rules Index

## Scope

- Applies to `DevOps/CI/**`.
- Primary targets are BuildTools Python modules, pytest coverage, Unity batchmode wrappers, artifact upload helpers, and TeamCity-facing CI flows.

## Primary Rule Sources

1. `.github/copilot-instructions.md`
   Repository-wide mandatory baseline that applies to every task.
2. `.github/instructions/ci.instructions.md`
   Module-specific CI instructions that auto-attach for `DevOps/CI/**` edits.
3. `README.md`
   CI common rules, validation policy, TeamCity expectations, and module map.
4. `BuildTools/README.md`
   BuildTools directory index and drill-down entry.
5. `BuildTools/<module>/README.md`
   Module-specific flow, configuration, and test commands.

## Module Baseline

- **All comments and docstrings must be written in Chinese (中文).** This applies to Python modules, classes, functions, test files, fixtures, inline comments, and configuration file comments.
- All touched Python modules must keep module docstrings current in Chinese.
- All touched classes and non-trivial functions must keep docstrings current in Chinese and explain role, contract, fallback, or side effects.
- Test files (pytest, etc.) are NOT exempt: every test module, test class, test function, fixture, and test helper must have a Chinese docstring explaining its purpose and the scenario it validates.
- Main CI workflows must stay concentrated in explicit coordinator functions or entry scripts, with phase comments and matching stage logs.
- Any change to build parameters, output layout, upload protocol, CI logging, or TeamCity contract must update code, README text, and pytest assertions together.
- Every new or changed code path must add or update automated tests. Relevant pytest, smoke, and TeamCity validations must pass before the task is considered complete.

## Reading Order

1. Read `.github/copilot-instructions.md`.
2. Read `.github/instructions/ci.instructions.md`.
3. Read `README.md`.
4. Read `BuildTools/README.md`.
5. Read the target module README before editing code.

## Validation Entry

- Use `README.md` for the shared validation policy.
- Use the target module README for the exact pytest, smoke test, and TeamCity entrypoints.
- If a change affects TeamCity DSL, CI parameters, remote upload, or execution logs, include the relevant TeamCity verification in addition to pytest.