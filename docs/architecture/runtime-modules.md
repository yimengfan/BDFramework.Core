# Runtime 模块地图

`Packages/com.popo.bdframework/Runtime/` —— 程序集 `BDFramework.Core`，共 8 个模块 + 工具集。

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

## AssetsManager —— 资源加载

静态门面 `BResources`（partial）持有 `IResMgr` 实例，按 `AssetLoadPathType` 选择后端。

| 文件 | 职责 |
|------|------|
| `BResources.Loader.cs` | 加载/卸载/资源组/对象池/AUP 配置 |
| `BResources.VersionController.cs` | 版本控制入口 + 路径常量 |
| `BResourcesV2.cs` | 实验性 `Instantiate`（`BDFramework.ResourceMgrV2`） |
| `BResourceV2/GameObjectWrapper.cs` | 实例包装（`InstId` / `SourceGameObjectId` / `Clone()`） |
| `ArtAsset/IResMgr.cs` | `IResMgr` 接口 + `LoadPathType` 枚举 |
| `ArtAsset/DevAssets/DevResourceMgr.cs` | Editor 后端（整文件 `#if UNITY_EDITOR`） |
| `ArtAsset/AssetBundle/V2/AssetBundleMgrV2.cs` | AB 后端（真机/热更） |
| `ArtAsset/AssetBundle/V2/AssetBundleConfigLoader.cs` | `art_assets.info` / `art_asset_type.info` 解析 |
| `ArtAsset/AssetBundle/V2/AssetBundleItem.cs` | 单条资源记录 |
| `ArtAsset/AssetBundle/V2/LoadTaskGroup.cs` | 异步任务组（`CustomYieldInstruction, IDisposable`） |
| `ArtAsset/AssetBundle/V2/LoadTask.cs` / `UnLoadTask.cs` | 单 AB 加载/卸载任务 |
| `ArtAsset/AssetBundle/V2/Loader/{AssetLoder,ShaderLoder,SpriteAtlasLoder,AssetLoaderFactory}.cs` | AB 内资源读写 |
| `ArtAsset/AssetTypeEx/UnityFolder.cs` | 文件夹伪资源类型 |
| `VersionController/AssetsVersionController.cs` | 旧协议版控（下载/校验/更新） |
| `VersionController/AssetsVersionController.DevOps*.cs` | 文件服务器协议版控 + CI BatchMode 验证 |
| `VersionController/ClientAssetsUtils.cs` | 双寻址 / 母包资源检测 / hash 校验 |
| `VersionController/VersionNumHelper.cs` | 三段版本号运算 |
| `ObjectPools/GameObjectPoolManager.cs` | GameObject 对象池 |
| `UnitTest/*` | 手工挂载的 Benchmark/GUI 测试（**非自动化**） |

关键常量（`BResources`）：

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

`AssetBundleMgrV2` 调度参数：`MAX_LOAD_TASK_NUM = 10`、`MAX_UNLOAD_TASK_NUM = 5`，两条协程由 `IEnumeratorTool` 驱动。

→ 详见[资源加载 BResources](../api/resources.md)。

## HotfixData —— 数据层

| 文件 | 职责 |
|------|------|
| `Sql/SqliteLoder.cs` | 静态入口：连接、加密、只读 PRAGMA、Editor 变体 |
| `Sql/SqliteHelper.cs` | `SqliteHelper` + 内嵌 `SQLiteService` CRUD |
| `Sql/TableQueryForILRuntime.cs` | 链式查询构建器（**名字是历史遗留，实为当前唯一实现**） |
| `Sql/TableLog.cs` | 导表日志表 |
| `Sql/SqliteFastJsonConvert.cs` | 快速 JSON 转换 |
| `Sql/SqlitePerformanceMonitor.cs` | 查询/PRAGMA 性能监控 |
| `Sql/Sqlite/SqliteNet/SQLite.cs` | sqlite-net / SQLCipher 底层（含 HybridCLR 非泛型路径） |
| `Sql/Sqlite/SqliteNet/SQLiteAsync.cs` + `Sqlite4SqlCipherDllImport.cs` | 异步 + 原生绑定 |

双库模型：

| 库 | 路径 | 模式 | 加密 |
|----|------|------|------|
| `local.db` | `<FIRST_LOAD_DIR>/local.db` | `ReadOnly` | SqlCipher（`SqliteLoder.Password`） |
| `server.db` | `<root>/server_data/server.db` | Editor 下 `ReadWrite \| Create` | **不加密** |

→ 详见[表格 SQLite](../api/sqlite.md)。

## HotfixScript —— 热更脚本装载

| 文件 | 职责 |
|------|------|
| `ScriptLoder.cs` | 托管类型收集 + 管理器注册 + 配置加载 |

```csharp
ScriptLoder.HYCLR_AOT_PATCH_PATH = "script/aot_patch"
ScriptLoder.HOTFIX_DLL_PATH      = "script/hotfix"
ScriptLoder.HOT_DLL_EXTENSION    = ".zlua.bytes"
```

!!! note "ILRuntime 残留"
    本分支 ILRuntime **已完全不参与运行**。仅剩命名与注释级残留：`CreateHotfixInstance` 的 ILRuntime 分支被注释、`TableQueryForILRuntime` 是旧 API 名兼容壳、`Editor/.../BuildHotfixScriptEditor/ILRuntime/` 下是 `ENABLE_ILRUNTIME` 宏门控的编辑器工具。

## GameConfig —— 配置中心

| 文件 | 职责 |
|------|------|
| `GameConfigLoder.cs` | 静态入口（`namespace BDFramework.Configure`） |
| `GameConfigManager.cs` | 配置中心 + `GameConfigAttribute` |
| `Base/IConfigProcessor.cs` | `void OnConfigLoad(ConfigDataBase config)` |
| `Base/ConfigDataBase.cs` | 配置数据基类（`ClassType` 字段用于匹配） |
| `GameBaseConfigProcessor.cs` | 框架基础配置（`[GameConfig(-9999, "框架基础")]`） |
| `GameCipherConfigProcessor.cs` | 加密配置（`[GameConfig(2, "加密")]`） |
| `GameConfigStartupPureLogic.cs` | 无副作用启动判定 |
| `Config.cs` | `AssetLoadPathType` / `HotfixCodeRunMode` 枚举宿主 |
| `ConfigEditorUtil.cs` | 编辑器配置读写（**位于 Runtime 程序集但被 `#if UNITY_EDITOR` 包裹**） |

配置文件：`Assets/Scenes/Config/*.bytes`，内容是 **LitJson 序列化的 JSON 数组**。

→ 详见[配置中心 GameConfig](../api/game-config.md)。

## EventBus —— 事件总线

| 文件 | 职责 |
|------|------|
| `Core/ADataListenerT.cs` | 泛型值监听（`dataMap` / `callbackMap` / `onceCallbackMap` / `valueCacheMap`） |
| `Core/ADataListenerTBase.cs` | 空基类，类型擦除占位 |
| `Core/AStatusListener.cs` | 对象状态监听抽象基类（**值缓存上限 20 条**） |
| `Service/StatusListenerService.cs` | 具名服务实例 |
| `Service/StatusListenerServer.cs` | 静态服务管理器 |
| `EventListenerEx.cs` | 按类型（`typeof(T).FullName`）监听 |
| `ValueListenerEx.cs` | 按 `Enum` / `string` 名监听 |

两套独立字典：`serviceMap`（`StatusListenerService`）与 `serviceTMap`（`ADataListenerTBase`），**同名 key 互不冲突**。

→ 详见[事件总线 EventBus](../api/event-bus.md)。

## Navigation —— 屏幕导航

| 文件 | 职责 |
|------|------|
| `IScreenView.cs` | 界面接口（`Name` / `IsLoad` / `BeginInit()` / `BeginExit()`） |
| `ScreenViewManager.cs` | `ScreenViewAttribute` + `ScreenViewManager`（`ManagerOrder(99999)`） |
| `ScreenViewCenter.cs` | `ScreenViewLayer` + `ScreenViewCenter`（**文件名叫 Center，但没有 `ScreenViewCenter` 类型**） |

`BeginNavTo` 内部顺序：`newView.BeginInit()` → **然后** `currentView.BeginExit()` → 更新 `navViews`（上限 10）。

→ 详见[屏幕导航 ScreenView](../api/screen-navigation.md)。

## Service —— 服务容器

| 文件 | 职责 |
|------|------|
| `ServiceContainer.cs` | 单例/瞬态注册 + `GetService<T>()` |
| `GameServiceStore.cs` | 按模块名隔离的容器仓库 |

!!! warning "`AddTransient<T>(T obj)` 会丢弃传入实例"
    它只用 `obj` 取类型，`GetService<T>()` 每次 `Activator.CreateInstance` 一个新对象（要求 `T` 有无参构造）。

## UI —— UFlux

目录结构（→ 详见 [UI（UFlux）](../uflux/index.md)）：

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

## Utils —— 工具集

| 子目录 | 关键类型 |
|--------|---------|
| `ManagerBase/` | `IMgr`、`ClassData`、`ManagerAttribute`、`ManagerBase<T,V>`、`ManagerOrder`、`ManagerInstHelper` |
| `Logs/` | `BDebug`、`Persistence`、`PersistenceSettings`、`LogCrypto`、`LogReader`、`SerializedLogEntry`、`Editor_UnityLogHook`、`BDebugPerformanceProfiler` |
| `MonoSingleton/` | `Singleton<T>`（实际命名空间是 `BDFramework.ResourceMgr`） |
| `ObjectPools/` | `ObjectPool<T>`、`ObjectPoolContainer<T>` |
| `Serialize/` | `CSVHelper`（ServiceStack.Text 包装） |
| `IO/` | `IPath`、`FileHelper`、`MurmurHash3`（都在 `namespace System.IO`） |
| `Extensions/` | `BApplication`、`IEnumeratorTool`、`DateTimeEx`、`HashHelper`、`StringEX`、`TypeEx`、`ReflectionExtension` |
| `L2/` | `L2Type` 语言枚举（**仅枚举，无加载器**） |
| `LowMemory/` | `LMDictionary<K,V>` |
| `OdinHelper/` | Odin 特性辅助 |
| `ActionAdaptor/` | `AActionAdaptor`（事件回调适配） |

!!! note "`namespace System.IO` 命名空间劫持"
    `IPath` 与 `FileHelper` 都声明在 `namespace System.IO` 中，是**刻意**的——这样业务代码写 `IPath.Combine(...)` 时不需要额外 `using`。

## `Runtime/Core/` 是空目录

`Runtime/Core/` 存在但**不含任何 `.cs`**。历史遗留空壳，可以直接删除（见[重构清单](refactor-backlog.md)）。

## 相关页面

- [Editor 模块地图](editor-modules.md)
- [程序集与依赖](assemblies.md)
- [启动链路](bootstrap.md)
