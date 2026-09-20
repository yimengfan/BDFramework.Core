# Skill Index

The Agent Skills, split by module, live in `.agents/skills/<name>/SKILL.md`.

!!! note "Directory conventions"
    This project's Skills follow VS Code's official Agent Skills convention. The optional project-level directories are:

    | Directory | Notes |
    |-----------|-------|
    | `.agents/skills/<name>/` | **Used by this repository's module Skills** (cross-tool: Claude / Cursor and others recognise it too) |
    | `.github/skills/<name>/` | Used by this repository's `teamcity` skill |
    | `.claude/skills/<name>/` | Claude-specific |

    Module-knowledge Skills all live under `.agents/skills/`.
    `teamcity` stays in `.github/skills/teamcity/`: the `.test-DevOps` submodule (a separate repository)
    references it as a path contract, so moving it would break a cross-repository reference.

## Skill inventory

### Core Runtime { #core-runtime }

| Skill | Coverage | Trigger keywords |
|-------|----------|------------------|
| [`bdframework-bootstrap`](#bdframework-bootstrap) | Startup sequence, configuration centre, logging, coroutines, paths, singletons | `BDLauncher`, `ScriptLoder`, `GameConfigManager`, `BDebug` |
| [`bdframework-uflux`](#bdframework-uflux) | The full UFlux UI framework stack | `AWindow`, `ATComponent`, `RenderData`, `Reducer`, `Store` |
| [`bdframework-uimanager`](#bdframework-uimanager) | Screen navigation, event bus, managers, service containers | `ScreenView`, `AStatusListener`, `ManagerBase` |
| [`bdframework-resources`](#bdframework-resources) | Asset loading, dual addressing, hotfix downloads, object pools | `BResources`, `AssetsVersionController` |
| [`bdframework-sqlite`](#bdframework-sqlite) | Table queries, dual-database model, table export | `SqliteHelper`, `TableQueryForILRuntime` |

### Build & CI { #build-ci }

| Skill | Coverage | Trigger keywords |
|-------|----------|------------------|
| [`bdframework-build-pipeline`](#bdframework-build-pipeline) | The four build tracks + publishing + hooks | `HyCLREditorTools`, `SetABPack`, `BuildMode` |
| [`bdframework-ci`](#bdframework-ci) | BatchMode entry points, Python tooling, TeamCity | `PublishPipeLineCI`, `-executeMethod`, `pytest` |
| [`teamcity`](https://github.com/yimengfan/BDFramework.Core/blob/v4/v-4.0.0/.github/skills/teamcity/SKILL.md) | TeamCity Web API operations (already present) | `run-build`, `versioned settings` |

## Details

### `bdframework-bootstrap`

**When to use**: diagnosing "the framework will not start", adding a configuration module, logging and coroutine problems.

**Contents**: the three-stage startup timeline (AOT → bridge → ScriptLoder), hotfix DLL loading, the type-collection whitelist, the `IConfigProcessor` contract, the complete `BDebug` API and its `[Conditional]` pitfalls, the enqueue semantics of `IEnumeratorTool`, `BApplication` path rewriting.

**Reference files**: `references/startup-sequence.md` (method-by-method timeline, the no-MonoBehaviour path)

### `bdframework-uflux`

**When to use**: writing or changing windows, components and subwindows; a screen that does not refresh; a button that does not respond; extending auto-assign attributes or binding adaptors.

**Contents**: the `AWindow<T>` / `ATComponent<T>` lifecycle, the complete `UIManager` API, the 5 `AutoAssignAttribute`s, `[ComponentValueBind]` and the adaptor system, the diff-refresh algorithm, `AReducers<T>` / `Store<S>` / `StoreFactory`, `Require` DI.

**Reference files**:

- `references/auto-assign-attributes.md`
- `references/component-binding.md`
- `references/state-store.md`
- `references/lifecycle.md`

### `bdframework-uimanager`

**When to use**: stage-level screen switching, cross-module event broadcasting, adding a manager, module-level service containers.

**Contents**: the `IScreenView` lifecycle and the `BeginNavTo` ordering, the two-track `AStatusListener` / `ADataListenerT<T>` with its 20-entry cache, the `StatusListenerServer` dual dictionary, the `ManagerBase<T,V>` contract, `ServiceContainer` / `GameServiceStore`.

**Reference files**:

- `references/event-bus-api.md`
- `references/manager-service.md`

!!! warning "This skill's name is a historical leftover"
    What it actually covers is **navigation + events + managers + service containers**, not `UIManager` window management (that lives in `bdframework-uflux`).

### `bdframework-resources`

**When to use**: loading and unloading assets, object pools, diagnosing "the asset will not load", hotfix downloads.

**Contents**: `AssetLoadPathType` vs `LoadPathType`, the complete `BResources` API, FIRST/SECOND dual addressing, the `DevResourceMgr` vs `AssetBundleMgrV2` differences, the `AssetsVersionController` download flow, the on-disk layout constants.

**Reference files**: `references/version-controller.md`

### `bdframework-sqlite`

**When to use**: writing table queries, diagnosing database problems, adding a business table, exporting tables.

**Contents**: the dual-database model and encryption, `SqliteLoder` / `SqliteHelper` / `SQLiteService`, every method of `TableQueryForILRuntime` plus its three pitfalls, the table-class naming contract, the Excel format conventions.

**Reference files**: `references/table-pipeline.md`

### `bdframework-build-pipeline`

**When to use**: building the hotfix DLL / ABs / tables / client package, writing custom AssetGraph nodes, hooking into the pipeline.

**Contents**: the entry points of the four tracks, the master asset pipeline, HybridCLR `PreBuild` / `BuildHotfixDLL`, the 20 built-in AssetGraph nodes, granularity and split packages, `BuildMode`, and the complete virtual method table of `ABDFrameworkPublishPipelineBehaviour`.

**Reference files**: `references/build-entrypoints.md`

### `bdframework-ci`

**When to use**: triggering a BatchMode build, diagnosing a BatchMode crash or a parameter problem, running Python scripts and pytest, understanding the upload protocol.

**Contents**: every `PublishPipeLineCI` entry point, the command-line parameter matrix, the Python scripts and CLI, the upload protocol, the CI output directory, platform isolation, the TeamCity DSL structure.

**Reference files**: `references/ci-commands.md`

## Skill design conventions

Skills in this repository follow this structure:

```text
.github/skills/<skill-name>/
├── SKILL.md                  # 必需。name 必须与目录名一致
├── references/               # 按需加载的详细 API 参考
│   └── <topic>.md
└── scripts/ / assets/        # 可选（本仓库暂未使用）
```

Conventions for organising `SKILL.md`:

| Section | Contents |
|---------|----------|
| frontmatter `description` | **Keyword-dense**, listing trigger scenarios and identifiers — this is the key to being discovered |
| 1. When to use | Positive trigger conditions + the cases where it does not apply |
| 2. Iron rules | 3–8 of the most frequent, most destructive constraints |
| 3–N. Per-module content | API quick reference + standard workflow + code examples |
| N. Troubleshooting table | Symptom → root cause → resolution |
| N. Detailed reference | Points at `references/` and the online documentation |
| N. Pre-change checklist | Checkbox items |

!!! tip "Progressive loading"
    `description` is used for discovery (about 100 tokens); the body of `SKILL.md` is loaded on demand; `references/*.md` are only loaded when explicitly referenced.

    That is why **`SKILL.md` must stay lean** and deep API detail sinks into `references/`.

## Relationship to rule documents

| Layer | Carrier | How it is loaded |
|-------|---------|------------------|
| L0 Global root rules | `.github/copilot-instructions.md` | Always |
| L1 File-level constraints | `.github/instructions/*.instructions.md` | `applyTo` automatic matching |
| L2 Package architecture | `AGENTS.md` (repository root / package root) | On demand |
| Skill | `.agents/skills/<name>/SKILL.md` | On demand (the model decides) |
| L3 Scratch memory | `.agent_memory/**` | Task-triggered |

How Skills and instructions divide the work:

| | Instruction | Skill |
|---|-------------|-------|
| Trigger | Loaded **automatically** when you edit a certain kind of file | Loaded **on demand** when a task matches |
| Contents | Coding rules, boundary constraints | Domain knowledge, API quick reference, workflows |
| Granularity | By file path | By business module |

See [Documentation Governance](doc-governance.md) for details.
