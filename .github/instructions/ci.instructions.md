---
description: "Use when editing DevOps CI, BuildTools, Python build scripts, TeamCity integration, artifact upload helpers, client resource flow, or pytest coverage under DevOps/CI."
applyTo: "DevOps/CI/**"
---

# DevOps CI Instructions

- Read `DevOps/CI/AI_RULES_INDEX.md` before substantial changes.
- Then read `DevOps/CI/README.md`, `DevOps/CI/BuildTools/README.md`, and the target module README.
- Keep the main CI process concentrated in explicit coordinator functions or entry scripts, with phase comments and matching stage logs.
- All touched Python modules, classes, and non-trivial functions must keep docstrings current.
- New or changed Python modules under `DevOps/CI/BuildTools` must include a module docstring, keep non-trivial class and function docstrings current, and explain role, contract, fallback, or side effects.
- Business-independent external config such as file servers, CI servers, signing or certificate metadata, and remote test settings must live in `DevOps/CI/BuildTools/buildtools.toml` and be read through shared config interfaces under `DevOps/CI/BuildTools/Common/`, not parsed ad hoc in individual scripts.
- Workspace hook `.github/hooks/buildtools-config-guard.json` is the deterministic guard for that rule: BuildTools source edits that reintroduce ad hoc TOML parsing or direct config-section reads outside `Common/buildtools_config.py` should be blocked, not merely discouraged.
- Any change to build parameters, output layout, upload protocol, CI logging, or TeamCity contract must update code, README text, and pytest assertions together.
- Add or update pytest for every changed path, and run the affected pytest plus smoke or TeamCity validation when CI entrypoints, upload paths, logs, or parameters change.