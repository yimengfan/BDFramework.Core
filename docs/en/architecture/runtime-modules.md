# Runtime Module Map

`Packages/com.popo.bdframework/Runtime/` — assembly `BDFramework.Core`, 8 modules plus a utility set.

```text
Runtime/
├── AssetsManager/     资源加载（BResources + IResMgr 双后端 + 版本控制 + 对象池）
├── HotfixData/        数据层（SqliteLoder / SqliteHelper / 查询构建器）
├── HotfixScript/      热更脚本装载（ScriptLoder）
├── GameConfig/        配置中心（GameConfigLoder / GameConfigManager / Processor）
├── EventBus/          事件与值监听（AStatusListener / ADataListenerT / Server）
├── Navigation/        屏幕导航（ScreenViewManager / ScreenViewLayer / IScreenView）
├── Service/           服务容器（ServiceContainer / GameServiceStore）
├── UI/                UFlux UI 框架
├── Utils/             工具集（日志、对象池、ManagerBase、单例、序列化、扩展…）
├── Core/              空目录（无 .cs）
└── BDLauncherHotfix.cs 桥接入
```

## AssetsManager — asset loading

The static façade `BResources` (partial) holds an `IResMgr` instance and selects a backend according to `AssetLoadPathType`.

| File | Responsibility |
|------|----------------|
| `BResources.Loader.cs` | Load / unload / asset groups / object pool / AUP config |
| `BResources.VersionController.cs` | Version-control entry point + path constants |
| `BResourcesV2.cs` | Experimental `Instantiate` (`BDFramework.ResourceMgrV2`) |
| `BResourceV2/GameObjectWrapper.cs` | Instance wrapper (`InstId` / `SourceGameObjectId` / `Clone()`) |
| `ArtAsset/IResMgr.cs` | The `IResMgr` interface + the `LoadPathType` enum |
| `ArtAsset/DevAssets/DevResourceMgr.cs` | Editor backend (the whole file is `#if UNITY_EDITOR`) |
| `ArtAsset/AssetBundle/V2/AssetBundleMgrV2.cs` | AB backend (device / hotfix) |
| `ArtAsset/AssetBundle/V2/AssetBundleConfigLoader.cs` | Parses `art_assets.info` / `art_asset_type.info` |
| `ArtAsset/AssetBundle/V2/AssetBundleItem.cs` | A single asset record |
| `ArtAsset/AssetBundle/V2/LoadTaskGroup.cs` | Async task group (`CustomYieldInstruction, IDisposable`) |
| `ArtAsset/AssetBundle/V2/LoadTask.cs` / `UnLoadTask.cs` | Per-AB load / unload tasks |
| `ArtAsset/AssetBundle/V2/Loader/{AssetLoder,ShaderLoder,SpriteAtlasLoder,AssetLoaderFactory}.cs` | Reading assets inside an AB |
| `ArtAsset/AssetTypeEx/UnityFolder.cs` | Pseudo asset type for folders |
| `VersionController/AssetsVersionController.cs` | Legacy protocol version control (download / verify / update) |
| `VersionController/AssetsVersionController.DevOps*.cs` | File-server protocol version control + CI BatchMode verification |
| `VersionController/ClientAssetsUtils.cs` | Dual addressing / client asset checks / hash verification |
| `VersionController/VersionNumHelper.cs` | Three-segment version number arithmetic |
| `ObjectPools/GameObjectPoolManager.cs` | GameObject pool |
| `UnitTest/*` | Manually attached benchmark/GUI tests (**not automated**) |

Key constants (`BResources`):

```csharp
ART_ASSET_ROOT_PATH        = "art_assets"
ART_ASSET_INFO_PATH        = "art_assets/art_assets.info"
ART_ASSET_TYPES_PATH       = "art_assets/art_asset_type.info"
ASSETS_INFO_PATH           = "assets.info"
ASSETS_SUB_PACKAGE_CONFIG_PATH = "assets_subpack.info"
SERVER_ASSETS_VERSION_INFO_PATH = "server_assets_version.info"
SOUND_ASSET_PATH           = "sound"
ALL_SHADER_VARAINT_RUNTIME_PATH = "Shader"
MIX_SOURCE_FOLDER          = "Assets/Resource/Runtime/MIX_AB_SOURCE"
SBPBuildLog / SBPBuildLog2 = "buildlogtep.json" / "build_result.info"
```

`AssetBundleMgrV2` scheduling parameters: `MAX_LOAD_TASK_NUM = 10`, `MAX_UNLOAD_TASK_NUM = 5`; two coroutines driven by `IEnumeratorTool`.

→ See [Assets (BResources)](../api/resources.md).

## HotfixData — data layer

| File | Responsibility |
|------|----------------|
| `Sql/SqliteLoder.cs` | Static entry point: connections, encryption, read-only PRAGMA, Editor variants |
| `Sql/SqliteHelper.cs` | `SqliteHelper` + the embedded `SQLiteService` CRUD |
| `Sql/TableQueryForILRuntime.cs` | Fluent query builder (**the name is legacy; this is the only current implementation**) |
| `Sql/TableLog.cs` | Table export log table |
| `Sql/SqliteFastJsonConvert.cs` | Fast JSON conversion |
| `Sql/SqlitePerformanceMonitor.cs` | Query / PRAGMA performance monitoring |
| `Sql/Sqlite/SqliteNet/SQLite.cs` | sqlite-net / SQLCipher core (including the HybridCLR non-generic path) |
| `Sql/Sqlite/SqliteNet/SQLiteAsync.cs` + `Sqlite4SqlCipherDllImport.cs` | Async + native bindings |

Dual-database model:

| Database | Path | Mode | Encryption |
|----------|------|------|------------|
| `local.db` | `<FIRST_LOAD_DIR>/local.db` | `ReadOnly` | SqlCipher (`SqliteLoder.Password`) |
| `server.db` | `<root>/server_data/server.db` | `ReadWrite \| Create` in the Editor | **Not encrypted** |

→ See [Tables (SQLite)](../api/sqlite.md).

## HotfixScript — hotfix script loading

| File | Responsibility |
|------|----------------|
| `ScriptLoder.cs` | Managed type collection + manager registration + config loading |

```csharp
ScriptLoder.HYCLR_AOT_PATCH_PATH = "script/aot_patch"
ScriptLoder.HOTFIX_DLL_PATH      = "script/hotfix"
ScriptLoder.HOT_DLL_EXTENSION    = ".zlua.bytes"
```

!!! note "ILRuntime leftovers"
    On this branch ILRuntime **plays no part in execution at all**. What remains is only naming and comments: the ILRuntime branch of `CreateHotfixInstance` is commented out, `TableQueryForILRuntime` is a compatibility shell keeping the old API name, and `Editor/.../BuildHotfixScriptEditor/ILRuntime/` holds editor tooling gated behind an `ENABLE_ILRUNTIME` macro.

## GameConfig — configuration centre

| File | Responsibility |
|------|----------------|
| `GameConfigLoder.cs` | Static entry point (`namespace BDFramework.Configure`) |
| `GameConfigManager.cs` | Config centre + `GameConfigAttribute` |
| `Base/IConfigProcessor.cs` | `void OnConfigLoad(ConfigDataBase config)` |
| `Base/ConfigDataBase.cs` | Config data base class (the `ClassType` field is used for matching) |
| `GameBaseConfigProcessor.cs` | Framework base config (`[GameConfig(-9999, "框架基础")]`) |
| `GameCipherConfigProcessor.cs` | Encryption config (`[GameConfig(2, "加密")]`) |
| `GameConfigStartupPureLogic.cs` | Side-effect-free startup decision |
| `Config.cs` | Hosts the `AssetLoadPathType` / `HotfixCodeRunMode` enums |
| `ConfigEditorUtil.cs` | Editor config read/write (**sits in the Runtime assembly but is wrapped in `#if UNITY_EDITOR`**) |

Config files: `Assets/Scenes/Config/*.bytes`, whose contents are a **LitJson-serialized JSON array**.

→ See [Configuration (GameConfig)](../api/game-config.md).

## EventBus — event bus

| File | Responsibility |
|------|----------------|
| `Core/ADataListenerT.cs` | Generic value listener (`dataMap` / `callbackMap` / `onceCallbackMap` / `valueCacheMap`) |
| `Core/ADataListenerTBase.cs` | Empty base class, a type-erasure placeholder |
| `Core/AStatusListener.cs` | Abstract base for object status listening (**value cache capped at 20 entries**) |
| `Service/StatusListenerService.cs` | Named service instance |
| `Service/StatusListenerServer.cs` | Static service manager |
| `EventListenerEx.cs` | Listen by type (`typeof(T).FullName`) |
| `ValueListenerEx.cs` | Listen by `Enum` / `string` name |

Two independent dictionaries: `serviceMap` (`StatusListenerService`) and `serviceTMap` (`ADataListenerTBase`) — **the same key name does not collide between them**.

→ See [Event Bus](../api/event-bus.md).

## Navigation — screen navigation

| File | Responsibility |
|------|----------------|
| `IScreenView.cs` | Screen interface (`Name` / `IsLoad` / `BeginInit()` / `BeginExit()`) |
| `ScreenViewManager.cs` | `ScreenViewAttribute` + `ScreenViewManager` (`ManagerOrder(99999)`) |
| `ScreenViewCenter.cs` | `ScreenViewLayer` + `ScreenViewCenter` (**the file is called Center but there is no `ScreenViewCenter` type**) |

`BeginNavTo` internally runs: `newView.BeginInit()` → **then** `currentView.BeginExit()` → update `navViews` (capped at 10).

→ See [Screen Navigation](../api/screen-navigation.md).

## Service — service container

| File | Responsibility |
|------|----------------|
| `ServiceContainer.cs` | Singleton/transient registration + `GetService<T>()` |
| `GameServiceStore.cs` | A repository of containers isolated by module name |

!!! warning "`AddTransient<T>(T obj)` discards the instance you pass in"
    It only uses `obj` to obtain the type. `GetService<T>()` then `Activator.CreateInstance`s a new object every time (so `T` must have a parameterless constructor).

## UI — UFlux

Directory structure (→ see [UI (UFlux)](../uflux/index.md)):

```text
Runtime/UI/
├── UFluxUtils.cs                     InitComponent / SetComponentRenderData / 资源转发
├── View/
│   ├── UIManager/{UIManager,UIManagerExDI,Status}.cs
│   ├── Windows/{IWindow,IUIMessage,UIMsgData,AWindow,AWindowProp}.cs
│   ├── Windows/Attribute/{UIAttribute,UIMessageListenerAttribute}.cs
│   ├── Component/{IComponent,ATComponent,AComponent,ISubComponent}.cs
│   ├── Attribute/{ComponentAttribute,ComponentValueBindAttribute}.cs
│   ├── Attribute/AutoInitComponent/*.cs
│   ├── ComponentBindAdaptor/{AComponentBindAdaptor,Manager/*}.cs
│   └── Props/ARenderDataBase.cs
├── State/{AStateBase,StateBase,IState,IPropertyChange,StateFactory}.cs
├── StateManager/Reducer/{AReducers,UFluxAction,Attribute/*}.cs
├── StateManager/Store/{Store,IStore,StoreFactory,StoreWrapper}.cs
├── Collections/PropsList.cs
├── Component/{IButton,Localization/*,PageList/*}.cs
└── Extension/ObjectsExtension.cs
```

## Utils — utility set

| Subdirectory | Key types |
|--------------|-----------|
| `ManagerBase/` | `IMgr`, `ClassData`, `ManagerAttribute`, `ManagerBase<T,V>`, `ManagerOrder`, `ManagerInstHelper` |
| `Logs/` | `BDebug`, `Persistence`, `PersistenceSettings`, `LogCrypto`, `LogReader`, `SerializedLogEntry`, `Editor_UnityLogHook`, `BDebugPerformanceProfiler` |
| `MonoSingleton/` | `Singleton<T>` (the actual namespace is `BDFramework.ResourceMgr`) |
| `ObjectPools/` | `ObjectPool<T>`, `ObjectPoolContainer<T>` |
| `Serialize/` | `CSVHelper` (a ServiceStack.Text wrapper) |
| `IO/` | `IPath`, `FileHelper`, `MurmurHash3` (all in `namespace System.IO`) |
| `Extensions/` | `BApplication`, `IEnumeratorTool`, `DateTimeEx`, `HashHelper`, `StringEX`, `TypeEx`, `ReflectionExtension` |
| `L2/` | `L2Type` language enum (**enum only, no loader**) |
| `LowMemory/` | `LMDictionary<K,V>` |
| `OdinHelper/` | Odin attribute helpers |
| `ActionAdaptor/` | `AActionAdaptor` (event callback adaptation) |

!!! note "The `namespace System.IO` hijack"
    `IPath` and `FileHelper` are declared inside `namespace System.IO` **deliberately** — this way business code writing `IPath.Combine(...)` needs no extra `using`.

## `Runtime/Core/` is an empty directory

`Runtime/Core/` exists but contains **no `.cs` files**. A historical empty shell that can simply be deleted (see the [Refactor Backlog](refactor-backlog.md)).

## Related pages

- [Editor Module Map](editor-modules.md)
- [Assemblies & Dependencies](assemblies.md)
- [Startup Sequence](bootstrap.md)
