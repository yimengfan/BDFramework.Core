# Editor Module Map

`Packages/com.popo.bdframework/Editor/` — assembly `BDFramework.Editor`, 118 `.cs` files in total.

## Top-level split

| Directory | Responsibility | Key types |
|-----------|----------------|-----------|
| `EditorPipeline/` | **The bulk of the build / publish / CI pipelines** | `BuildTools_*`, `PublishPipeLineCI`, `AssetBundleBuildingContext` |
| `EditorEnvironment/` | Editor environment init, settings, bootstrap, E2E bridge | `BDFrameworkEditorEnvironment`, `BDEditorApplication`, `TalosE2EBatchBridge` |
| `EditorAutoSetting/` | Automatic asmdef and macro configuration | `EditorSetting` (writes `ENABLE_HYCLR`, `ENABLE_BDEBUG`) |
| `EditorTask/` | Editor scheduled/hooked tasks | `EditorTask`, `UnityLoadOrCodeRecompiled`, `WillEnterPlaymode`, `EveryDay` |
| `EditorWindows/` | Global menus, NetProtocol tools | `GlobalEditorMenuItems`, `BDEditorGlobalMenuItemOrderEnum`, `Protobuf2ClassTools` |
| `UI/` | UIManager editor helpers, UI workflow menus | `UIManagerEditor`, `MenuItems` |
| `Inspector/` | Custom inspectors | — |
| `Extension/` | Editor extensions (including `GameViewEditorEX`) | — |
| `EditorCoroutines/` | Editor coroutines | — |
| `VersionControl/` | Git / SVN integration | `GitProcessor`, `SVNProcessor` |
| `Utils/` | General editor utilities | — |
| `Plugins/` `Unity.InternalAPIEngineBridge/` | Third-party and internal API bridges | — |

## `EditorPipeline/` structure

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

## The four build tracks and their entry points

| Track | Entry class | Entry method | Output |
|-------|-------------|--------------|--------|
| AssetBundle | `BDFramework.Editor.BuildPipeline.AssetBundle.BuildTools_AssetBundleV2` | `BuildAssetBundles(RuntimePlatform platform, string outputPath)` | `<out>/<platform>/art_assets/` |
| Hotfix DLL | `BDFramework.Editor.HotfixScript.BuildTools_HotfixScript` | `BuildDLL(string outpath, RuntimePlatform platform)` | `<out>/<platform>/script/hotfix/*.zlua.bytes` |
| Tables | `BDFramework.Editor.Table.BuildTools_Excel2SQLite` | `BuildSqlite(string ouptputPath, RuntimePlatform platform, DBType dbType, bool isUseCache)` | `<out>/<platform>/local.db`, `<out>/server_data/server.db` |
| Client package | `BDFramework.Editor.BuildPipeline.BuildTools_ClientPackage` | `Build(BuildMode, string buildScene, string buildConfig, bool isGenAssets, string outdir, BuildTarget, …)` | `DevOps/PublishPackages/<platform>/…` |

**The overall asset pipeline** (chaining the three asset tracks together):

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

## Editor environment init chain

`BDFrameworkEditorEnvironment.InitEditorEnvironment()` (triggered by `[InitializeOnLoadMethod]`):

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

This is also **why you can read `local.db` and call `BResources.Load` directly inside the Editor** — the environment is set up when the editor loads, going through `DevResourceMgr`.

## Menu system

All first-party menus hang off `BDFrameWork工具箱/`, and their ordering is centralised in `BDEditorGlobalMenuItemOrderEnum`:

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

For the complete menu list see [Menus & Tools Index](../editor/menus.md).

## Key capabilities that exist only in the Editor

| Capability | Location | Notes |
|------------|----------|-------|
| `DevResourceMgr` | `Runtime/AssetsManager/ArtAsset/DevAssets/` | The entire type is wrapped in `#if UNITY_EDITOR` |
| `SqliteLoder.LoadLocalDBOnEditor` / `LoadServerDBOnEditor` | Runtime | Manually mount a database |
| `ClientAssetsUtils.GenBasePackageBuildInfo` / `SaveBasePackageBuildInfo` | Runtime | Writes `package_build.info` |
| `ConfigEditorUtil` | Runtime (contents under `#if UNITY_EDITOR`) | Config read/write |
| `AssetBundleBenchmarkToolsV2` | Editor | AB load verification |
| `EditorHttpListener` | Editor | Embedded Http server (see [Editor Http Server](../editor/http-server.md)) |

## Related pages

- [Runtime Module Map](runtime-modules.md)
- [Build & Release](../pipeline/index.md)
- [Editor Core Classes](../editor/core-classes.md)
- [Menus & Tools Index](../editor/menus.md)
- [Structure Issues & Refactor Backlog](refactor-backlog.md)
