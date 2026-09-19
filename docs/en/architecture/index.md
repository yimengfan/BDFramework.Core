# Architecture Overview

This section answers one question: **where does code live, and why does it live there**.

| Page | Contents |
|------|----------|
| [Assemblies & Dependencies](assemblies.md) | The 5 first-party assemblies: boundaries, dependency direction, asmdef locations |
| [Startup Sequence](bootstrap.md) | Exact timing from `RuntimeInitializeOnLoadMethod` to managers being ready |
| [Runtime Module Map](runtime-modules.md) | The 8 modules of `BDFramework.Core` and their file inventory |
| [Editor Module Map](editor-modules.md) | Sub-module split of `BDFramework.Editor` |
| [Structure Issues & Refactor Backlog](refactor-backlog.md) | Current inconsistencies, dead code and recommended actions |

## Architecture in one picture

```text
AOT 层（不可热更）          桥接层                  Core 层（可热更）
─────────────────      ──────────────      ─────────────────────────────
BDLauncher             BDLauncherBridge     BResources / SqliteLoder
ScriptLoderAOT    →    GameConfigLoder  →   ScriptLoder → ManagerInstHelper
（装载 DLL/元数据）     ClientAssetsUtils     ├─ UIManager (UFlux)
                                              ├─ ScreenViewManager
                                              ├─ ComponentBindAdaptorManager
                                              └─ GameConfigManager
```

**Key design points**:

1. **The AOT layer and the Core layer do not reference each other.** `BDLauncher` cannot `using BDFramework` (that would be a circular dependency), so all of its access to Core goes through `AppDomain.CurrentDomain.GetAssemblies()` + `Type.GetType(fullName)` reflection.
2. **Startup has two phases.** `ScriptLoder.Init()` collects types and registers managers (**without starting them**); only when business code calls `BDLauncherBridge.Launch()` after the update screen completes are assets initialised and managers started.
3. **The manager system is the single extension point.** A new module only has to inherit `ManagerBase<T, TAttribute>` and carry an attribute derived from `ManagerAttribute` to be discovered, registered, `Init()`-ed and `Start()`-ed automatically.

## Layer responsibilities

| Layer | Assembly | Hot-updatable | Responsibility | Forbidden |
|-------|----------|---------------|----------------|-----------|
| AOT | `BDFramework.AOT` | ✗ | Bootstrap, AOT metadata supplement, hotfix DLL loading, stripping protection | Referencing `BDFramework.Core` |
| Runtime | `BDFramework.Core` | ✓ | Assets / tables / config / UI / navigation / events / services | Referencing `BDFramework.Editor` |
| Editor | `BDFramework.Editor` | ✗ | Build pipeline, publish pipeline, CI bridge, editor windows | Being referenced by runtime |
| Tests | `BDFramework.Test` / `BDFramework.EditorTest` | ✗ | Runtime API tests / BatchMode tests | Appearing in Release builds |
| CI scripts | `Editor.DevOps~` | ✗ | Python/shell build & upload | Being compiled by Unity (the `~` suffix) |

## Comparison with other frameworks

| Concept | BDFramework | Typical counterpart |
|---------|-------------|---------------------|
| UI state management | UFlux (`AStateBase` + `Reducer` + `Store`) | Redux / Vuex |
| Asset addressing | Explicit paths (path relative to the `Runtime` directory) | Addressables (with an address registry) |
| Hotfix | HybridCLR (interpreted execution + AOT metadata supplement) | ILRuntime (removed) / Lua |
| Service container | `ServiceContainer` + `GameServiceStore` | Simple DI without lifecycle management |
| Config centre | `GameConfigManager` + `IConfigProcessor` | ScriptableObject config (the framework uses `.bytes` JSON) |

!!! note "Why not Addressables"
    The framework performs dual addressing between `StreamingAssets` and `persistentDataPath`, and needs **self-built hash naming plus incremental download from a file server** (see [Asset Publishing](../pipeline/publish-assets.md)). Addressables' catalog mechanism conflicts with this bespoke versioning, so the choice was explicit paths plus a custom AB index (`art_assets.info`).
