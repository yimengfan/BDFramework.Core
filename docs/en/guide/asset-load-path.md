# Asset Load Paths

The framework has three kinds of assets that need addressing: **code (DLLs)**, **tables (SQLite)** and **art assets (AssetBundle)**. Each has its own root source and addressing rules, but they all share the same "dual addressing" directory structure.

## Two enums you must not confuse

These two types are the most commonly mixed up — similar names, completely different semantics:

| Enum | Namespace | Values | Question it answers |
|------|-----------|--------|---------------------|
| `AssetLoadPathType` | `BDFramework` | `Editor = 0`, `Hotfix = 1` | **Where does the asset root come from?** (publish directory or device persistent directory) |
| `LoadPathType` | `BDFramework.ResourceMgr` | `RuntimePath`, `GUID`, `AssetsPath` | **What path semantics does this one load use?** |

`AssetLoadPathType` is configured independently in three places on `GameBaseConfigProcessor.Config` — `CodeRoot`, `SQLRoot`, `ArtRoot` — and each may take a different value:

```csharp
var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
// config.CodeRoot / config.SQLRoot / config.ArtRoot : AssetLoadPathType
// config.CodeRunMode : HotfixCodeRunMode { HyCLR = 1, Mono64 }
```

Resolution implementation (`GameBaseConfigProcessor.GetLoadPath`):

| `AssetLoadPathType` | Resolves to |
|---------------------|-------------|
| `Editor` | `BApplication.DevOpsPublishAssetsPath` → `<ProjectRoot>/DevOps/PublishAssets` |
| `Hotfix` | `BApplication.persistentDataPath` → `<ProjectRoot>/.AppData` in the Editor |

## Path rewrite matrix { #path-rewrite-matrix }

`BApplication` (`Runtime/Utils/Extensions/Unity3d/BApplication.cs`) **rewrites Unity's native paths** in the Editor. This is the number one cause of "works in the Editor, breaks on device":

| `BApplication` property | Editor | Device |
|-------------------------|--------|--------|
| `persistentDataPath` | `<ProjectRoot>/.AppData` | Windows/macOS: `<dataPath>/.AppData`; otherwise: Unity native |
| `streamingAssetsPath` | `<ProjectRoot>/DevOps/PublishAssets` | Unity native |
| `ProjectRoot` | Project root | — |
| `BDEditorCachePath` | `Library/BDFrameCache` | — |
| `DevOpsPath` | `DevOps` | — |
| `DevOpsPublishAssetsPath` | `DevOps/PublishAssets` | — |
| `DevOpsPublishClientPackagePath` | `DevOps/PublishPackages` | — |
| `EditorResourcePath` | `Assets/Resource_SVN` | — |
| `EditorResourceRuntimePath` | `Assets/Resource_SVN/Runtime` | — |
| `RuntimePlatform` | Determined by macros, **never returns the `Editor` enum** | Current platform |

Platform directory names come from `BApplication.GetPlatformLoadPath(platform)`:

```text
RuntimePlatform.WindowsPlayer → "windows"
RuntimePlatform.Android       → "android"
RuntimePlatform.OSXPlayer     → "osx"
RuntimePlatform.IPhonePlayer  → "ios"
```

## Dual addressing: FIRST / SECOND

At runtime on device, every asset is looked up in a two-level order: "check the writable directory first, then the read-only directory inside the client".

```csharp
// BDLauncherBridge.Launch() 内部
var (firstLoadDir, secondLoadDir) =
    ClientAssetsUtils.GetMultiAssetsLoadPath(BApplication.RuntimePlatform, Config.ClientVersionNum);

if (Application.isEditor)
{
    firstLoadDir = secondLoadDir;      // Editor 下强制合并为同一个目录
}

ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
BResources.Init(Config.ArtRoot, firstLoadDir, secondLoadDir);
SqliteLoder.Init(Config.SQLRoot, firstLoadDir, secondLoadDir);
```

| Directory | Semantics | Composition |
|-----------|-----------|-------------|
| `FIRST_LOAD_DIR` | **Writable**, hotfix download target | `persistentDataPath/<major.minor.0>/<platform>` |
| `SECOND_LOAD_DIR` | **Read-only**, shipped in the client | Android: `"<platform>"` (reads assets inside the APK via `BetterStreamingAssets`)<br/>Other platforms: `streamingAssetsPath/<platform>` |

!!! note "The version directory is downgraded to `x.y.0`"
    `VersionNumHelper.ParseVersion` **zeroes the patch version and the build digit**, so `1.4.7` and `1.4.0` share the `1.4.0` directory. The intent is for one major version to reuse a single asset directory, avoiding a full re-download on every release.

    **Side effect**: be careful with canary releases and rollbacks inside the same major version — two builds will write to the same directory.

### What `CheckBaseClientAssets` does

1. If `firstPath == secondPath` (the Editor case) → return immediately, no checks.
2. Read `secondPath/package_build.info`; **if missing, throw outright on non-Editor platforms**:
   `【母包资源检测】严重错误！母包配置不存在：...`
3. If `firstPath` does not exist, run `ClearOldPersistentAssets()` — deleting all old version directories matching `^\d+\.\d+\.\d+$`.
4. Copy the files listed in `ClientAssetsUtils.PersistentOnlyFiles` from the client to the writable directory (only when the target does not exist). The current list has **exactly one entry**: `SqliteLoder.LOCAL_DB_PATH = "local.db"`.

    In other words, `local.db` must be copied out of the client into the writable directory before it can be opened — which is why you must call `SqliteLoder.LoadLocalDBOnEditor()` manually in the Editor.

## Path semantics for a single load (`LoadPathType`)

| Value | Meaning | Example |
|-------|---------|---------|
| `RuntimePath` (default) | **Relative path after the `Runtime` directory, without extension** | `"Test/Cube"` → `Assets/*/Runtime/Test/Cube.prefab` |
| `GUID` | Unity GUID | the `GUID` column in `art_assets.info` |
| `AssetsPath` | Full path starting at `Assets/` | `"Assets/Resource/Runtime/Test/Cube.prefab"` |

```csharp
// 推荐写法：不带扩展名
var go = BResources.Load<GameObject>("AssetTest/Cube");

// 异步
int taskId = BResources.AsyncLoad<GameObject>("Test/Cube", o => { /* main thread */ });
BResources.LoadCancel(taskId);
```

!!! danger "Path casing"
    - `AssetBundleMgrV2`'s index `LoadPathIdxMap` uses `StringComparer.OrdinalIgnoreCase` — **case-insensitive**.
    - `DevResourceMgr` (Editor) matches real file names through `Directory.GetFiles` — **case-sensitive** (subject to the file system: insensitive by default on macOS, sensitive on Linux).

    → **Loading successfully in the Editor does not mean it will load on device.** Always write paths using the asset's real file-name casing.

## On-disk layout of each asset kind

Using `DevOps/PublishAssets/` as the build root (the default `BuildParams.OutputPath`):

```text
DevOps/PublishAssets/
├── <platform>/
│   ├── art_assets/
│   │   ├── <ABName 或 GUID>            # AB 本体（无扩展名）
│   │   ├── art_assets.info             # List<AssetBundleItem> CSV —— 加载索引
│   │   ├── art_asset_type.info         # AssetTypeConfig CSV —— 资源类型表
│   │   └── EditorBuild.Info            # BuildAssetInfos JSON（仅 Editor 用）
│   ├── script/
│   │   ├── hotfix/<asm>.zlua.bytes     # 热更 DLL
│   │   └── aot_patch/<asm>.zlua.bytes  # AOT 补充元数据
│   ├── local.db                        # 主库（SqlCipher 加密）
│   ├── assets.info                     # 资源清单
│   ├── assets_subpack.info             # 分包配置
│   ├── package_build.info              # 母包构建信息（含三段 SVC 版本号）
│   └── server_assets_version.info      # 版本信息
├── server_data/server.db               # 服务器库（不加密）
└── _ReadyToUpload/<version>/<platform> # 待上传（发布管线产出）
```

Constants for the mandatory files are defined on `BResources` — do not hardcode the strings:

```csharp
BResources.ART_ASSET_ROOT_PATH            // "art_assets"
BResources.ART_ASSET_INFO_PATH            // "art_assets/art_assets.info"
BResources.ART_ASSET_TYPES_PATH           // "art_assets/art_asset_type.info"
BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH // "art_assets/EditorBuild.Info"
BResources.ASSETS_INFO_PATH               // "assets.info"
BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH // "assets_subpack.info"
BResources.SERVER_ASSETS_VERSION_INFO_PATH // "server_assets_version.info"
```

## Common failures

| Symptom | Root cause | What to do |
|---------|-----------|------------|
| `加载不到资源`, no exception | `art_assets.info` missing, or the path is not in the index | Check whether `DevResourceMgr` finds it; check the `Runtime` directory convention |
| `DB不存在:<path>` | `local.db` is not in `FIRST_LOAD_DIR` | Call `SqliteLoder.LoadLocalDBOnEditor()` in the Editor; on device check the client copy step |
| `【母包资源检测】严重错误！` | `package_build.info` missing from the client | Rebuild the client package |
| AB async load hangs with no callback | `IEnumeratorTool` is not attached | Confirm `BDLauncherBridge.Launch()` has run |
| Fine in the Editor, white screen on device | The `AssetLoadPathType.Editor` branch was taken | On device `ResLoader` will be `null` (see below) |

!!! danger "`BResources.Load` throws an NRE before initialisation"
    The `AssetLoadPathType.Editor` branch of `BResources.Init` is entirely wrapped in `#if UNITY_EDITOR`. **Passing `Editor` on a non-Editor platform silently leaves `ResLoader == null`**, after which `BResources.Load` throws `NullReferenceException` (the source has no null check).

    `BResources.UnloadAll()` / `FindShader()` use the safe `ResLoader?.` form and will not throw. The behaviour is asymmetric — be aware of it.

## Related pages

- [Assets (BResources)](../api/resources.md) — full API plus load/unload/object-pool semantics
- [Project Layout](project-structure.md) — the `Runtime` / `Table` / `@hotfix` conventions
- [Startup Sequence](../architecture/bootstrap.md) — initialisation order
- [Asset Publishing](../pipeline/publish-assets.md) — `_ReadyToUpload` and the upload protocol
