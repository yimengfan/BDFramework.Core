# Editor 模块地图

`Packages/com.popo.bdframework/Editor/` —— 程序集 `BDFramework.Editor`，共 118 个 `.cs`。

## 顶层划分

| 目录 | 职责 | 关键类型 |
|------|------|---------|
| `EditorPipeline/` | **构建/发布/CI 管线主体** | `BuildTools_*`、`PublishPipeLineCI`、`AssetBundleBuildingContext` |
| `EditorEnvironment/` | 编辑器环境初始化、设置、引导、E2E bridge | `BDFrameworkEditorEnvironment`、`BDEditorApplication`、`TalosE2EBatchBridge` |
| `EditorAutoSetting/` | asmdef 与宏的自动配置 | `EditorSetting`（写入 `ENABLE_HYCLR`、`ENABLE_BDEBUG`） |
| `EditorTask/` | 编辑器定时/钩子任务 | `EditorTask`、`UnityLoadOrCodeRecompiled`、`WillEnterPlaymode`、`EveryDay` |
| `EditorWindows/` | 全局菜单、NetProtocol 工具 | `GlobalEditorMenuItems`、`BDEditorGlobalMenuItemOrderEnum`、`Protobuf2ClassTools` |
| `UI/` | UIManager 编辑器辅助、UI 工作流菜单 | `UIManagerEditor`、`MenuItems` |
| `Inspector/` | 自定义 Inspector | — |
| `Extension/` | 编辑器扩展（含 `GameViewEditorEX`） | — |
| `EditorCoroutines/` | 编辑器协程 | — |
| `VersionControl/` | Git / SVN 集成 | `GitProcessor`、`SVNProcessor` |
| `Utils/` | 通用编辑器工具 | — |
| `Plugins/` `Unity.InternalAPIEngineBridge/` | 第三方与内部 API 桥 | — |

## `EditorPipeline/` 结构

```text
EditorPipeline/
├── EditorWindow_BuildPipeline.cs          Odin 构建总窗口
├── Behavior/EditorBehavior/
│   ├── ABDFrameworkPublishPipelineBehaviour.cs   管线生命周期抽象基类（业务方可继承）
│   ├── PublishBehaviour.cs                       默认空实现
│   └── BDFrameworkPipelineHelper.cs              事件分发 / SVC 版本号查询
├── BuildPipeline/
│   ├── MenuItem.cs                        构建菜单桥
│   ├── BuildAssetBundleEditor/            ← AB 构建
│   │   ├── BuildTools_AssetBundleV2.cs           入口
│   │   ├── AssetBundleBuildingContext.cs         AB 构建协调器
│   │   ├── EditorWindow_BuildAssetBundle.cs      资源打包面板
│   │   ├── AssetBundleBenchmarkToolsV2.cs        AB 加载验证
│   │   ├── AssetBundleCacheServer.cs
│   │   ├── AssetGraph/{AssetGraphTools,BuildAssetBundleParams}.cs
│   │   ├── AssetGraph/Node/                     20 个 CustomNode
│   │   ├── AssetsInfo/{BuildAssetInfos,AssetType}.cs
│   │   ├── AssetImporter/BuildPipelineAssetCacheImporter.cs
│   │   └── ShaderCollection/                    Shader 变体/AB 搜集
│   ├── BuildHotfixScriptEditor/           ← 热更 DLL
│   │   ├── BuildTools_HotfixScript.cs            入口（薄封装）
│   │   ├── EditorWindow_BuildHotfixDll.cs        脚本打包面板
│   │   ├── HotfixTestAssemblyInjector.cs         测试程序集注入/移除/校验
│   │   ├── Unity3dRoslynBuildTools.cs            ★ 旧 ILRuntime 实现（整段注释）
│   │   ├── HyCLR/HyCLREditorTools.cs             ★ HybridCLR 实现（生效）
│   │   └── ILRuntime/                            ILRuntime 遗留工具（宏门控）
│   ├── BuildPackage/                      ← 母包
│   │   ├── BuildTools_Assets.cs                  资源总管道 BuildAll / BatchMode 入口
│   │   ├── BuildTools_ClientPackage.cs           ★ 母包构建主体
│   │   └── BuildAndroid / BuildIOS / BuildWindowsPlayer / BuildMacOSX.cs
│   └── BuildTableEditor/                  ← 表格
│       ├── BuildTools_Excel2SQLite.cs            入口
│       ├── ExcelEditorTools.cs                   Excel 扫描/hash 增量
│       ├── ExcelExchangeTools.cs                 Excel 解析（表头/类型/`*` 过滤）
│       ├── Excel2CodeTools.cs                    Excel → C# 类
│       ├── EditorWindow_ExcelExchange.cs / EditorWindow_Table.cs
│       └── OnExcelImporter.cs / TableEditorTask.cs
├── DevOpsPipeline/
│   ├── CI/CI.cs                                  CIAttribute
│   ├── CI/PublishPipeLineCI.cs                   ★ BatchMode -executeMethod 入口集合
│   ├── CI/PublishPipeLineCI.BatchModeBridge.cs   平台 ↔ 验证请求桥接
│   ├── DevOpsTools.cs                            GetCIApis() 反射扫描 [CI]
│   ├── DevOpsEditorTasks.cs                      Git hooks 同步
│   └── EditorWindow_CICD.cs                      CI API 预览窗口
├── HotfixPipeline/                        ← 热更文件配置工作流
│   ├── HotfixPipelineTools.cs
│   ├── HotfixFileConfigLogic.cs                  读写 DevOps/Config/HotfixFile.conf
│   ├── HotfixCodeWorkFlow.cs
│   └── EditorWindow_HotfixFileSetting.cs
├── PublishPipeline/
│   ├── PublishPipelineTools.cs                   资源转 hash / 服务器清单生成
│   └── EditorWindow_PublishAssets.cs             发布资源窗口
└── SqliteBenchmark/SqliteOptimizationBenchmark.cs  MenuItem 桥
```

## 四条构建链路与入口方法

| 链路 | 入口类 | 入口方法 | 产物 |
|------|--------|---------|------|
| AssetBundle | `BDFramework.Editor.BuildPipeline.AssetBundle.BuildTools_AssetBundleV2` | `BuildAssetBundles(RuntimePlatform platform, string outputPath)` | `<out>/<platform>/art_assets/` |
| 热更 DLL | `BDFramework.Editor.HotfixScript.BuildTools_HotfixScript` | `BuildDLL(string outpath, RuntimePlatform platform)` | `<out>/<platform>/script/hotfix/*.zlua.bytes` |
| 表格 | `BDFramework.Editor.Table.BuildTools_Excel2SQLite` | `BuildSqlite(string ouptputPath, RuntimePlatform platform, DBType dbType, bool isUseCache)` | `<out>/<platform>/local.db`、`<out>/server_data/server.db` |
| 母包 | `BDFramework.Editor.BuildPipeline.BuildTools_ClientPackage` | `Build(BuildMode, string buildScene, string buildConfig, bool isGenAssets, string outdir, BuildTarget, …)` | `DevOps/PublishPackages/<platform>/…` |

**资源总管道**（把三条资源链路串起来）：

```csharp
BuildTools_Assets.BuildAll(platform, outputPath, clientVersion, buildOption)
  0. OnBeginBuildAllAssets      → 取版本号
  1. 热更 DLL
  2. SQLite
  3. AssetBundle
  4. GenBasePackageBuildInfo(bundleVersion)
  5. assets.info 生成            ← 注释强制要求放最后
  6. OnEndBuildAllAssets
```

## Editor 环境初始化链

`BDFrameworkEditorEnvironment.InitEditorEnvironment()`（由 `[InitializeOnLoadMethod]` 触发）：

```text
BDEditorApplication.Init()
ScriptLoder.GetAppDomainHostingTypes()
BResources.Init(AssetLoadPathType.Editor)
ManagerInstHelper.LoadManager(Types)
GameConfigLoder.LoadFrameworkConfig()
BDFrameworkPipelineHelper.Init()
HotfixPipelineTools.Init()
InitEditorTask()
OnUnityLoadOrCodeRecompiled()
InitEditorHttpServer()
```

这也是为什么**编辑器里可以直接读 `local.db`、直接 `BResources.Load`** —— 环境在编辑器加载时就初始化好了，走 `DevResourceMgr`。

## 菜单体系

所有一方菜单挂在 `BDFrameWork工具箱/` 下，排序由 `BDEditorGlobalMenuItemOrderEnum` 统一管理：

```csharp
BDFrameworkGuid = 0,              BDFrameworkSetting = 1,
BuildPipeline = 50,               BuildPackage_DLL = 52,       BuildPackage_Assetbundle = 53,
BuildPackage_Table_Table2Class = 54, BuildPackage_Table_GenSqlite = 55,
BuildPackage_Table_Json2Sqlite = 56, BuildPipeline_NetProtocol_Proto2Class = 57,
BuildPipeline_BuildPackage = 58,  PublishPipeline = 100,
PublishPipeline_BuildAsset = 101, PublishPipeline_PublishPackage = 102,
HotfixPipeline = 111,             DevOps = 121,
TestPepeline = 201,               TestPepelineEditor = 202
```

完整菜单列表见[菜单与工具索引](../editor/menus.md)。

## 只在 Editor 存在的关键能力

| 能力 | 位置 | 说明 |
|------|------|------|
| `DevResourceMgr` | `Runtime/AssetsManager/ArtAsset/DevAssets/` | 整个类型被 `#if UNITY_EDITOR` 包裹 |
| `SqliteLoder.LoadLocalDBOnEditor` / `LoadServerDBOnEditor` | Runtime | 手动挂载 db |
| `ClientAssetsUtils.GenBasePackageBuildInfo` / `SaveBasePackageBuildInfo` | Runtime | 写 `package_build.info` |
| `ConfigEditorUtil` | Runtime（`#if UNITY_EDITOR` 内容） | 配置读写 |
| `AssetBundleBenchmarkToolsV2` | Editor | AB 加载验证 |
| `EditorHttpListener` | Editor | 内嵌 Http 服务（见 [Editor Http 服务](../editor/http-server.md)） |

## 相关页面

- [Runtime 模块地图](runtime-modules.md)
- [构建与发布](../pipeline/index.md)
- [Editor 核心类](../editor/core-classes.md)
- [菜单与工具索引](../editor/menus.md)
- [结构问题与重构清单](refactor-backlog.md)
