---
description: "Use when editing DevOps CI, BuildTools, Python build scripts, TeamCity integration, artifact upload helpers, client resource flow, or pytest coverage under DevOps/CI."
applyTo: "DevOps/CI/**"
---

# DevOps CI Instructions

## Scope

- Applies to `DevOps/CI/**`.
- Primary targets are BuildTools Python modules, pytest coverage, Unity batchmode wrappers, artifact upload helpers, and TeamCity-facing CI flows.

## Reading Order

1. Read `.github/copilot-instructions.md`.
2. Read this file (CI module instructions).
3. Read `DevOps/CI/README.md`.
4. Read `DevOps/CI/BuildTools/README.md`.
5. Read the target module README before editing code.

## Rules

- **All comments and docstrings must be written in paired Chinese and English.** This applies to Python modules, classes, functions, test files, fixtures, inline comments, and configuration file comments. Put Chinese first and follow with the English version in the same comment block. Chinese-only or English-only comments do not satisfy this requirement.
- All touched Python modules must keep module docstrings current in bilingual Chinese and English form.
- All touched Python classes and non-trivial functions must keep docstrings current in bilingual Chinese and English form and explain role, contract, fallback, or side effects.
- New or changed Python modules under `DevOps/CI/BuildTools` must include a bilingual module docstring, keep non-trivial class and function docstrings current, and explain role, contract, fallback, or side effects.
- Test files (pytest, etc.) are NOT exempt: every test module, test class, test function, fixture, and test helper must have a bilingual docstring explaining its purpose and the scenario it validates.
- Keep the main CI process concentrated in explicit coordinator functions or entry scripts, with phase comments and matching stage logs.
- **CRITICAL**: Business-independent external config such as file servers, CI servers, signing or certificate metadata, and remote test settings must live in `DevOps/CI/BuildTools/buildtools.toml` and be read through shared config interfaces under `DevOps/CI/BuildTools/Common/`, not parsed ad hoc in individual scripts. This rule is enforced by `buildtools_config_guard.py` in CI regression tests; violations will cause pytest to fail. Do not introduce `import tomllib`/`tomli`/`toml.load()`, custom `load_toml`/`parse_simple_value` helpers, or direct `config[section_key]` reads outside `Common/buildtools_config.py`.
- Any change to build parameters, output layout, upload protocol, CI logging, or TeamCity contract must update code, README text, and pytest assertions together.
- Every new or changed code path must add or update automated tests. Relevant pytest, smoke, and TeamCity validations must pass before the task is considered complete.

## Validation Entry

- Use `DevOps/CI/README.md` for the shared validation policy.
- Use the target module README for the exact pytest, smoke test, and TeamCity entrypoints.
- If a change affects TeamCity DSL, CI parameters, remote upload, or execution logs, include the relevant TeamCity verification in addition to pytest.