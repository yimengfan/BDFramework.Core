# Structure Issues & Refactor Backlog

> This page records **known inconsistencies** found while mapping the code structure, as a to-do list for future refactors.
> Each item carries a severity and a suggested action. **None of them necessarily causes a failure today** — most are naming
> or responsibility drift left behind by historical evolution.
>
> Severity: **P1** affects correctness or debugging efficiency ｜ **P2** wrong responsibility/naming that raises comprehension cost ｜ **P3** dead code and clean-up

## P1 — Correctness risks

### 1. `BResources.UnloadAsset` ignores its parameters { #ref-1-unloadasset }

```csharp
public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null)
```

Neither `isForceUnload` nor `type` is **forwarded to `AssetBundleMgrV2.UnloadAsset`** (the implementation only calls `UnUseAssetBundle(loadPath)`).

**Impact**: callers believe they can force an unload or unload a specific type, but the behaviour is identical to the default.
**Suggestion**: either implement the parameter semantics, or mark them `[Obsolete]` on the signature and remove them.

### 2. Out-of-range condition in `ScreenViewLayer.BeginNavBack()` { #ref-2-beginnavback }

```csharp
if (currentViewIndex == 0 || currentViewIndex - 1 >= navViews.Count) { BDebug.LogError("别闹，前方没有view"); }
```

When `currentViewIndex == -1`, `-2 >= count` is `false`, so `currentViewIndex--` runs and makes it `-2`, after which accessing `navViews[-2]` **throws**.

**Suggestion**: change to `if (currentViewIndex <= 0) return;`.

### 3. Cursor direction in `UIManager.Back()` { #ref-3-uimanager-back }

`Forward()` / `Back()` only move a cursor and then call `ShowWindow(..., isAddToHistory: false)`, but `Back()` internally uses `curForwardBackUIIdx++` (the opposite direction to its semantics).

**Suggestion**: after verifying, make `Back()` decrement and `Forward()` increment, and add a navigation-history test.

### 4. Wrong comparison operand in `ComponentBindAdaptorManager.AnalysisRenderDataChanged` { #ref-4-analysisrenderdatachanged }

The value-type branch compares `newRenderData.Equals(LastValue)` — it compares **the RenderData against itself** rather than against the cached previous value. This makes "value unchanged" be judged as changed (or vice versa).

**Suggestion**: change to `LastValue == null || !LastValue.Equals(newValue)`, and add a differential-refresh test.

### 5. `StatusListenerService.Name` is always `null` { #ref-5-statuslistenerservice-name }

`Create(name)` / `GetService(name)` **never assign `Name`** (the dictionary key is the real name).

**Impact**: logs cannot distinguish between listener services.
**Suggestion**: set `service.Name = name` in the `StatusListenerServer` construction path.

### 6. Runtime coupling of the AOT logging subsystem { #ref-6-bdebug-runtime-coupling }

`BDebug` is a `MonoBehaviour` in the global namespace, and its static instance `Inst` calls `FindObjectOfType` or **creates a `new GameObject` outright** outside PlayMode. `DisableLog` / `EnableLog` use the force-instantiating `Inst` (rather than the lenient `inst`), so they conjure objects out of thin air on BatchMode / pure-logic paths.

**Suggestion**: make `DisableLog`/`EnableLog` take the lenient path too, or degrade to a no-op when no instance exists.

## P2 — Responsibility and naming

### 7. File name does not match type name { #ref-7-filename-type-mismatch }

| File | Actual type | Problem |
|------|-------------|---------|
| `Runtime/UI/View/Windows/AWindowProp.cs` | `AWindow<TP>` | The name suggests "a window with Props"; it is actually **the base class of every window** |
| `Runtime/Navigation/ScreenViewCenter.cs` | `ScreenViewLayer` | The file is called Center; there is **no `ScreenViewCenter` type** |
| `Editor/UI/UIManager.Editor.cs` | `UIManagerEditor` | Not a partial of `UIManager`; the name misleads |
| `Runtime/HotfixData/Sql/TableQueryForILRuntime.cs` | The only current query builder | Named after the removed ILRuntime |
| `Runtime/AssetsManager/VersionController/AssetsVersionController.DevOpsPureLogic.cs` | File-server protocol | "PureLogic" clashes with `GameConfigStartupPureLogic` |

**Suggestion**: `AWindowProp.cs` → `AWindow.Generic.cs`; `ScreenViewCenter.cs` → `ScreenViewLayer.cs`; `UIManager.Editor.cs` → `UIManagerEditor.cs`.

### 8. `ConfigEditorUtil` lives in the Runtime assembly { #ref-8-configeditorutil }

The contents of `Runtime/GameConfig/ConfigEditorUtil.cs` are wrapped in `#if UNITY_EDITOR`, yet the type is declared in `BDFramework.Core`.

**Consequence**: `GameConfigManager` cannot reuse it and has to **keep its own copy of the `DefaultEditorConfigPath` constant**.

**Suggestion**: move `ConfigEditorUtil` under `Editor/`, or extract a UnityEditor-free constants class on the Runtime side that both can reference.

### 9. `AssetsManager/UnitTest/*` are manual GUI tests { #ref-9-assetsmanager-unittest }

`Runtime/AssetsManager/UnitTest/` holds a set of `MonoBehaviour` benchmark scripts that **must be attached to a scene by hand** (`AssetBundleBenchmark01`, `AssetBundleTestLoad`, object-pool examples). They sit in the Runtime assembly and are never covered by automated tests.

**Suggestion**: move them to `Runtime.Test/` or `Assets/Code/BDFramework.UnitTest/Runtime/`.

### 10. Classes in the global namespace { #ref-10-global-namespace }

`BDebug`, `IEnumeratorTool`, `PageList`, `L2Text` and `L2Image` all live in the **global namespace** (no `namespace` declaration).

**Impact**: short names such as `Text` / `Image` / `Log` in business code easily collide with Unity types, and cannot be narrowed with `using`.

**Suggestion**: at minimum give `PageList` / `L2Text` / `L2Image` a `BDFramework.UI` namespace.

### 11. Empty shells and unused interfaces { #ref-11-empty-types }

| Type | Status |
|------|--------|
| `Runtime/UI/View/Component/ISubComponent.cs` | The `RegisterSubComponent` interface has **no implementations at all** |
| `StateManager/Reducer/Attribute/AsyncReducerAttribute.cs` | Fields only, **no constructor**, unused |
| `Component/Localization/L2Text.cs` / `L2Image.cs` | Empty `MonoBehaviour` shells with **no logic whatsoever** |
| `Runtime/GameConfig/Config.cs` | `Config : MonoBehaviour` has **no fields**; only its value as an enum host remains |
| `Runtime/Core/` | The directory exists but has **no `.cs`** |
| `PublishPipeLineCI.BuildDLL()` | Marked with `[CI]` but **has an empty body** |

**Suggestion**: delete or implement. `Config.cs` should at least be renamed to `FrameworkEnums.cs` to stop misleading readers.

### 12. `PageList` is disconnected from UFlux { #ref-12-pagelist }

`Runtime/UI/Component/PageList/PageList.cs` is a **global-namespace `MonoBehaviour`** that hardcodes `Resources.Load` and ships its own `TestScene.unity` sample.

**Suggestion**: either convert it to an `AComponent` loading through `BResources`, or move it out of the framework package into business code.

## P3 — Clean-up items

### 13. ILRuntime residue { #ref-13-ilruntime-residue }

| Location | Status |
|----------|--------|
| `EditorPipeline/.../BuildHotfixScriptEditor/Unity3dRoslynBuildTools.cs` | Entirely commented out, gated by `ENABLE_ILRUNTIME` |
| `EditorPipeline/.../BuildHotfixScriptEditor/ILRuntime/` | Leftover editor tooling |
| `ScriptLoder.CreateHotfixInstance` | ILRuntime branch commented out |
| `ScriptLoder.Dispose()` | `AppDomain?.Dispose()` commented out |
| `TableQueryForILRuntime` | Legacy API name |

**Suggestion**: once this repo is confirmed to have no `ENABLE_ILRUNTIME` build, delete the whole directory and strip the commented blocks inside `ScriptLoder`.

### 14. Duplicated constants { #ref-14-duplicated-constants }

`HYCLR_AOT_PATCH_PATH` / `HOTFIX_DLL_PATH` / `HOT_DLL_EXTENSION` exist **twice** — in `Runtime/HotfixScript/ScriptLoder.cs` and in `Runtime.AOT/ScriptLoderAOT.cs`.

This is **deliberate assembly isolation** (AOT cannot reference Core), but no comment says so. **Suggestion**: add cross-referencing comments so nobody changes one copy and misses the other.

### 15. Stale comments { #ref-15-stale-comments }

- The XML comment on `ScriptLoder.Init()` mentions "bridging the Talos E2E auto-discovery entry point", but **there is no such logic in the method body** (it is actually done by `Packages/com.talosai.e2e/Runtime/TestRunner/E2EAutoInit.cs`).
- A `PublishPipelineTools` comment gives the upload path as `{UPLOAD_FOLDER_SUFFIX}/{platform}/{version}`, but the **implementation is `_ReadyToUpload/<version>/<platform>`** (the order is reversed).
- `EditorPipeline/Behavior/EditorBehavior/BDFrameworkPipelineHelper.cs` still contains a commented-out `[MenuItem("xxx/xxxx")]` example.

### 16. Typos (baked into the API; changes need a compatibility assessment) { #ref-16-typos }

| Location | Current spelling | Correct |
|----------|------------------|---------|
| `BResources.UnloadAssetByGouroup` | `Gouroup` | `Group` |
| `TableQueryForILRuntime.EnableSqlCahce` | `Cahce` | `Cache` |
| `BuildTools_Excel2SQLite.BuildSqlite(string ouptputPath, …)` | `ouptput` | `output` |
| The historical `docs` page `CI相关操作（Jekins，TeamCity）` | `Jekins` | `Jenkins` |

**Suggestion**: add correctly-spelled aliases marked `[Obsolete("拼写错误，请使用 Xxx")]` and drop them in the next major version.

### 17. Dead parameters { #ref-17-dead-parameters }

- The `name` parameter of `ScreenViewLayer.BeginNavForward(string name)` is **never used**.
- In `SqliteLoder.Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)`, neither `assetLoadPathType` nor `secondDir` **participates in any decision** — the database is always `firstDir/local.db`.

**Suggestion**: remove the parameters or give them meaning. `SqliteLoder.Init`'s signature deliberately mirrors `BResources.Init`; if that symmetry is worth keeping, document it.

### 18. Asymmetric SQLite façade { #ref-18-sqlite-facade }

`SqliteHelper.SQLiteService` exposes **no public `Update` / `Delete` / `Execute`**, yet it constructs a `TableQueryForILRuntime`. Writing to the database requires reaching the underlying connection via `.Connection` (which is exactly what the Editor build pipeline does).

**Suggestion**: expose `Update` / `Delete` explicitly, or state clearly that the service layer is read-only.

### 19. `WhereOr` / `WhereAnd` overwrite rather than append { #ref-19-whereor-whereand }

`TableQueryForILRuntime.WhereOr` / `WhereAnd` **overwrite `@where` wholesale**, while the `And` / `Or` properties **mutate on read** (they concatenate inside the `get`). This means you cannot write branch code like `if (cond) q.And;` — it will silently produce wrong SQL.

**Suggestion**: at minimum add a bold warning to the XML docs; better, turn `And`/`Or` into methods.

## Repository-level issues

### 20. Duplicated config file { #ref-20-duplicated-config }

| File | Status |
|------|--------|
| `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/buildtools.toml` | Designated single source of truth |
| `DevOps/CI/buildtools.toml` | A **content-duplicated copy** |

**Suggestion**: delete `DevOps/CI/buildtools.toml`, or add a consistency check in CI.

### 21. Reference to a missing path { #ref-21-missing-githook-dir }

`DevOpsEditorTasks.UpdateGitHookToLocalStore` still references `Path.Combine(BApplication.DevOpsCIPath, "githook")`, but **`DevOps/CI/githook/` does not exist in this repository** (consumers are expected to provide it). This task runs on every Unity load / code recompile.

**Suggestion**: degrade to a `Log` instead of failing silently when the directory is missing, and document that consumers supply it.

### 22. TeamCity DSL location { #ref-22-teamcity-dsl-location }

The Kotlin DSL lives in `.test-DevOps/.teamcity/`, **not under `DevOps/`**. Every CI doc and script path makes it easy to assume `DevOps/CI/`.

**Suggestion**: state the DSL's real location explicitly in `DevOps/CI/README.md` (some of it is there now; strengthen it).

## Related pages

- [Runtime Module Map](runtime-modules.md)
- [Editor Module Map](editor-modules.md)
- [Archive: raw Wolai export](../archive/index.md)
