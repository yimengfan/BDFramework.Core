---
description: "Use when editing the BDFramework package runtime, editor, AOT, tests, asmdefs, startup flow, or resource update logic under Packages/com.popo.bdframework."
applyTo: "Packages/com.popo.bdframework/**"
---

# BDFramework Package Instructions

- Read `Packages/com.popo.bdframework/AI_RULES_INDEX.md` before substantial changes.
- Then read `Packages/com.popo.bdframework/Documentation~/README.md` and any linked local docs relevant to the task.
- Runtime partials that extend resource version control must use business-oriented names such as `file server`, `version server`, or `resource server`. `DevOps` may appear only as an explicit mode name or public API suffix. Do not introduce new runtime abstractions named `BuildTools`.
- When extending `AssetsVersionController`, keep edits in `AssetsVersionController.cs` minimal. Prefer outer entry bridges or new partial helpers over inserting hooks into multiple existing private methods.
- When introducing an alternative resource-update protocol, expose it through an explicit public API. Do not make legacy public entrypoints implicitly route into the new protocol unless the requirement explicitly asks for that behavior.
- Resource-update protocol config files and cache files must include comments or documentation that explain the file purpose, who writes it, and who consumes it.
- All touched C# classes must keep XML comments current. Key classes must describe design role and include an example or usage note.
- All touched methods must keep XML comments current, and coordinator methods must use phase comments around the critical flow.
- New C# protocol or pipeline files must include XML summaries on key classes and key entry/helper methods. For non-trivial flows, add concise process comments and at least one usage example in comments or documentation.
- Unity test files must follow `source-file-name + Test.cs`. Example: `AssetsVersionController.DevOps.cs` -> `AssetsVersionController.DevOpsTest.cs`.
- Add or update Unity automated tests for every changed code path and run the affected tests before finishing.