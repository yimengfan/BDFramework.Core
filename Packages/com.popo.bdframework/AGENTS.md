# BDFramework 包架构

作用域：`Packages/com.popo.bdframework/**`。编码规则见 `.github/instructions/bdframework*.instructions.md`（applyTo 自动加载），不在本文件重复。不在本 package 更深目录新增 `AGENTS.md`。

## 程序集

| 程序集 | 目录 | 平台 | 职责 |
|--------|------|------|------|
| `BDFramework.Core` | `Runtime/` | Any | 框架运行时 |
| `BDFramework.AOT` | `Runtime.AOT/` | Any | AOT 补充注册，供 IL2CPP 预剪裁保留 |
| `BDFramework.Editor` | `Editor/` | Editor | 构建管线、编辑器窗口、CI bridge |
| `BDFramework.Test` | `Runtime.Test/Runtime/` | Any | Runtime API 测试（Debug 构建，Release 排除） |
| `BDFramework.EditorTest` | `Runtime.Test/Editor/` | Editor | Editor-only 测试、BatchMode bridge |

依赖方向：`Editor → Core`、`Editor → AOT`、`AOT → Core`，Runtime 不得反向引用 Editor。

## 启动链路

`BDLauncher`(AOT MonoBehaviour) → `BDLauncherBridge.Launch(gameId)` → `GameConfigLoder.LoadFrameworkConfig()` → 双路径解析 + 母包资源修复 → `BResources.Init(loadPathType, dir1, dir2)` → `SqliteLoder.Init(loadPathType, dir1, dir2)` → `ScriptLoder.Start()` → Manager 系统就绪（UIManager / ScreenViewManager）。

## Runtime 模块

### 资源加载 — `Runtime/AssetsManager/`

静态门面 `BResources`（partial）持有 `IResMgr` 实例。`AssetLoadPathType.Editor` → `DevResourceMgr`（AssetDatabase）；`Hotfix` → `AssetBundleMgrV2`（AB 异步加载 + 缓存 + 配置）。热更版本控制：`AssetsVersionController`（下载/校验/更新全流程）、`ClientAssetsUtils`（`FIRST/SECOND_LOAD_DIR` 双寻址）。V2 扩展：`BResourcesV2` + `GameObjectWrapper` 安全实例化。关键数据：`AssetsVersionInfo`、`AssetItem`、`LoadTaskGroup`。

### SQLite 管理 — `Runtime/HotfixData/Sql/`

`SqliteLoder`（静态）为入口，支持加密（`Password` / `PasswordFallback` 解耦）和双库模型：`local.db`（只读，母包内置）+ `server.db`（读写，热更下载）。`SqliteHelper.SQLiteService` 封装 CRUD；`TableQueryForILRuntime` 兼容 HybridCLR。Editor 有独立 `LoadLocalDBOnEditor` / `LoadServerDBOnEditor` 入口。

### 热更脚本 — `Runtime/HotfixScript/`

`ScriptLoder`（静态）：`Init()` 收集托管类型 → 加载管理器 → 加载配置；`Start()` 启动 Manager 系统；`GetAppDomainHostingTypes()` 带缓存。常量：`HOTFIX_DLL_PATH` / `HOT_DLL_EXTENSION = ".zlua.bytes"`。

### UI 框架 (UFlux) — `Runtime/UI/`

三层架构：**View**（`UIManager` 三层 UILayer + `IWindow` 窗口生命周期 + 状态监听 Open/Close/Focus/Blur）→ **Component**（`AComponentBindAdaptor` 属性赋值映射 + `ComponentBindAdaptorManager` 绑定缓存 + `AutoAssign` / `ButtonOnclick` 属性标记）→ **State**（`AStateBase` 属性变更通知 + `StateFactory` 缓存 + `StateManager/Store/` Redux-like Store + `StateManager/Reducer/` Reducer 模式）。其他：`Localization/` 本地化、`PageList/` 分页列表、`PropsList` Props 工具。

### 事件总线 — `Runtime/EventBus/`

`ADataListenerT<T>`（泛型值监听，持久/一次性回调）+ `AStatusListener`（对象状态监听，值缓存上限 20）。服务层：`StatusListenerServer`（静态管理器，Create/Get/Remove 具名 `StatusListenerService`）。扩展：`EventListenerEx` / `ValueListenerEx`。

### 游戏配置 — `Runtime/GameConfig/`

`GameConfigLoder`（静态入口）→ `GameConfigManager : ManagerBase`（配置中心，`GameConfigAttribute(intTag, title)` 标记模块）。处理器：`IConfigProcessor` / `ConfigDataBase` 基类 + `GameBaseConfigProcessor` / `GameCipherConfigProcessor`。`Config : MonoBehaviour` 定义 `AssetLoadPathType` 和 `HotfixCodeRunMode` 枚举。`GameConfigStartupPureLogic` 支持无 MonoBehaviour 的纯逻辑启动。

### 屏幕导航 — `Runtime/Navigation/`

`ScreenViewManager : ManagerBase`（`ManagerOrder=99999` 最后启动），`MainLayer` 主导航层。`ScreenViewLayer` 管理多个 `IScreenView`，堆栈式导航（`RegisterScreen` / `BeginNavTo`，`navViews` 显示栈）。`IScreenView` 生命周期：`BeginInit()` / `BeginExit()`。

### 服务容器 — `Runtime/Service/`

`ServiceContainer`（单例/瞬态注册 + `GetService<T>()`），`GameServiceStore`（静态，按模块名隔离容器）。



### 工具集 — `Runtime/Utils/`

**日志 `Logs/`**：`BDebug`（`ExecutionOrder=-10000`）统一入口，支持加密持久化 + Tag 过滤；`LogCrypto` / `LogReader` / `Persistence` / `SerializedLogEntry`；`Editor_UnityLogHook` 仅 Editor。**对象池 `ObjectPools/`**：`ObjectPool<T>` / `ObjectPoolContainer`。**Manager 基础 `ManagerBase/`**：`ManagerBase<T,TAttribute>` 自动注册 + 属性驱动。**其他**：`MonoSingleton/`、`IO/`、`Serialize/`、`Extensions/`、`LowMemory/`、`OdinHelper/`、`L2/`。

## AOT — `Runtime.AOT/`

`BDLauncher.cs` AOT 侧 MonoBehaviour 入口；`ScriptLoderAOT.cs` 防止 IL2CPP 剪裁关键类型。

## Editor 模块

| 子模块 | 目录 | 职责 |
|--------|------|------|
| AB 构建 | `EditorPipeline/BuildPipeline/BuildAssetBundleEditor/` | AssetBundle 构建、粒度规则 |
| 热更构建 | `EditorPipeline/BuildPipeline/BuildHotfixScriptEditor/` | DLL 编译、AOT 补充生成 |
| 表构建 | `EditorPipeline/BuildPipeline/BuildTableEditor/` | Excel → SQLite、Schema 管理 |
| 母包构建 | `EditorPipeline/BuildPipeline/BuildPackage/` | 多平台打包 |
| 热更管线 | `EditorPipeline/HotfixPipeline/` | 热更代码工作流 |
| 发布管线 | `EditorPipeline/PublishPipeline/` | 资源发布工具集 |
| DevOps | `EditorPipeline/DevOpsPipeline/` | CI/CD 窗口、BatchMode bridge |
| 编辑器环境 | `EditorEnvironment/` | 环境检测、`TalosE2EBatchBridge` |
| 版本控制 | `VersionControl/` | Git/SVN 集成 |
| UI 编辑器 | `UI/` | UIManager.Editor、Workflow 编辑器 |
| 其他 | `EditorWindows/` `Inspector/` `Extension/` `EditorCoroutines/` `EditorTask/` | 菜单、Inspector、协程、异步任务 |

## 模块依赖

```
BDLauncherBridge → GameConfigLoder → GameConfigManager
                → BResources → IResMgr → AssetBundleMgrV2 | DevResourceMgr
                → SqliteLoder → SqliteHelper.SQLiteService
                → ScriptLoder → ManagerInstHelper → UIManager / ScreenViewManager / ...

UIManager → ComponentBindAdaptorManager → AComponentBindAdaptor
          → StateFactory → AStateBase
          → Store / Reducer (Redux-like)

ScreenViewManager → ScreenViewLayer → IScreenView (堆栈导航)
StatusListenerServer → StatusListenerService → AStatusListener / ADataListenerT<T>
GameServiceStore → ServiceContainer (按模块隔离)
```
