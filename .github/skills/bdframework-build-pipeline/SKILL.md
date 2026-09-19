---
name: bdframework-build-pipeline
description: 'BDFramework 构建管线技能。使用场景：构建热更 DLL（HybridCLR PreBuild/BuildHotfixDLL/AOT 补充元数据）、打包 AssetBundle（AssetGraph 节点图、颗粒度 SetABPack、分包 MultiplePackage、自定义节点）、构建母包（Android/iOS/Windows、BuildMode、版本注入）、发布资源（hash 布局、_ReadyToUpload、assets.info）、挂载管线回调钩子（ABDFrameworkPublishPipelineBehaviour）。关键字：BuildTools_HotfixScript、HyCLREditorTools、PreBuild、BuildHotfixDLL、aot_patch、zlua.bytes、BuildTools_AssetBundleV2、AssetBundleBuildingContext、SetABPack、SetABPackLevel、CustomNode、BDFrameworkAssetsEnv、MultiplePackage、BuildTools_ClientPackage、BuildMode、BuildTools_Assets、BuildAll、PublishPipelineTools、GenServerHashAssets、ABDFrameworkPublishPipelineBehaviour、OnBeginBuildPackage、OnBeginPublishAssets、母包、出包。'
---

# BDFramework 构建管线技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 构建热更 DLL / AssetBundle / 表格 / 母包
- 写自定义 AssetGraph 打包节点
- 挂载构建/发布生命周期钩子
- 排查构建产物缺失、版本号不递增、测试程序集混入

不适用：**CI 调度与上传**（用 `bdframework-ci`）。

## 2. 四条链路与入口

| # | 链路 | 入口类 | 入口方法 |
|---|------|--------|---------|
| ① | 热更 DLL | `BDFramework.Editor.HotfixScript.BuildTools_HotfixScript` | `BuildDLL(string outpath, RuntimePlatform platform)` |
| ② | 表格 | `BDFramework.Editor.Table.BuildTools_Excel2SQLite` | `BuildSqlite(string ouptputPath, RuntimePlatform, DBType, bool isUseCache)` |
| ③ | AssetBundle | `BDFramework.Editor.BuildPipeline.AssetBundle.BuildTools_AssetBundleV2` | `BuildAssetBundles(RuntimePlatform platform, string outputPath)` |
| ④ | 母包 | `BDFramework.Editor.BuildPipeline.BuildTools_ClientPackage` | `Build(BuildMode, string buildScene, string buildConfig, bool isGenAssets, string outdir, BuildTarget, …)` |

**资源总管道**：

```csharp
BuildTools_Assets.BuildAll(platform, outputPath, clientVersion, buildOption)
  0. OnBeginBuildAllAssets      → 取版本号
  1. 热更 DLL
  2. SQLite
  3. AssetBundle
  4. GenBasePackageBuildInfo(bundleVersion)
  5. assets.info 生成            ← 源码注释强制要求放最后
  6. OnEndBuildAllAssets
```

`BuildPackageOption`：`BuildHotfixCode` / `BuildArtAssets` / `BuildAll`。

## 3. 热更 DLL（HybridCLR）

!!! danger "ILRuntime 相关文档已全部失效"
    旧文档中的 CLRBinding、Adaptor 生成、`ILRuntimeCLRBinding` 报错排查**完全不适用**。相关实现整段注释在 `Unity3dRoslynBuildTools.cs`，不生效。

### 程序集配置

`HyCLREditorTools.SetBDFramework2HCLRConfig()`：

| 配置项 | 值 |
|--------|-----|
| `preserveHotUpdateAssemblies`（追加） | `Assembly-CSharp`、`Assembly-CSharp-firstpass`、`BDFramework.Core` |
| `hotUpdateAssemblies`（移除） | 原列表全部清空 |
| `patchAOTAssemblies`（追加） | `mscorlib`、`System`、`System.Core` |

`SetHyCLRConfig()` 还会 `Unity3dEditorEx.AddSymbols("ENABLE_HYCLR")`。

### `PreBuild(BuildTarget target)` 步骤

```text
① 校验 HybridCLRSettings.Instance != null
     否则 throw new Exception("请先生成HCLR Setting!")
② 平台校验：不一致 → BDEditorApplication.SwitchToBuildTarget(target) 并复检，失败抛异常
③ SetBDFramework2HCLRConfig()
④ CleanupLegacyGeneratedOutputs()
     删除 Assets/HybridCLRGenerate/link.xml、AOTGenericReferences.cs（+ .meta）
⑤ EnsureHybridClrInstalled(new InstallerController())
     优先 InstallFromLocal(HybridCLRData/il2cpp_plus_repo/libil2cpp)
     失败回退 InstallDefaultHybridCLR()
⑥ PrebuildCommand.GenerateAll()
⑦ CopyAOTMetadataDLL(sourceDir, outputRoot, target)
     sourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target)
     输出根 = GetAotMetadataOutputRoots(Application.streamingAssetsPath, BApplication.DevOpsPublishAssetsPath)
     ★ 两个根都要写
```

!!! danger "`PreBuild` 会把 AOT patch 写入两个位置"
    `Application.streamingAssetsPath`（母包内置）与 `BApplication.DevOpsPublishAssetsPath`（构建产物）。两个都写才完整。

### `BuildHotfixDLL` 步骤

```text
① 临时目录 HybridCLRData/out_hotfixdlls_temp（先删后建）
② CompileDllCommand.CompileDll(tmpOutputPath, target, false)
     挂 Application.logMessageReceived 检测含 "Fail" 的 Error
     失败 → throw "build hotfix_dll failed"
③ CopyHotfixDLLs(tmpOutputPath, outputDir, target)
④ HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(destDLLRootDir, isReleaseBuild)
```

### 输出与常量

| 常量 | 值 | 定义位置 |
|------|-----|---------|
| `ScriptLoder.HOTFIX_DLL_PATH` | `script/hotfix` | `Runtime/HotfixScript/ScriptLoder.cs` |
| `ScriptLoder.HYCLR_AOT_PATCH_PATH` | `script/aot_patch` | 同上 |
| `ScriptLoder.HOT_DLL_EXTENSION` | `.zlua.bytes` | 同上 |

```text
DevOps/PublishAssets/<platform>/script/hotfix/<assembly>.zlua.bytes
DevOps/PublishAssets/<platform>/script/aot_patch/<assembly>.zlua.bytes
```

!!! note "`.zlua.bytes` 只是扩展名伪装"
    **没有额外的 DLL 加密步骤**。AB 有独立混淆（`MixAssetBundle`）。

!!! warning "常量在两个程序集里各有一份"
    `Runtime/HotfixScript/ScriptLoder.cs` 与 `Runtime.AOT/ScriptLoderAOT.cs` 各定义一次（刻意的程序集隔离）。**改一处必须改另一处**。

### 测试程序集隔离

```csharp
HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(destDLLRootDir, isReleaseBuild);
```

检查 `.dll.bytes` **和** `.zlua.bytes`；Release 下发现测试程序集**抛异常**。

`IsCurrentBuildDebug()` 优先级：`-buildMode` → `-buildDebug` → `EditorUserBuildSettings.development`。

### 运行时装载顺序

| 排名 | 匹配 |
|------|------|
| 0 | `bdframework.core*` |
| 1 | `assembly-csharp-firstpass*` |
| 2 | `assembly-csharp*` |
| 10 | 其他（同级按文件名 `OrdinalIgnoreCase`） |

**AOT 元数据始终从母包 `StreamingAssets/<platform>/script/aot_patch/` 读，不跟随版本目录。**

## 4. AssetBundle 打包

### 构建流程

```text
AssetBundleBuildingContext.StartBuildAssetBundle
 ① 清 art_assets/
 ② ReorganizeAssetBundleUnit()        ← 按颗粒度规则归整
 ③ GetAssetBundleItems()
 ④ SBP 打包
 ⑤ 删旧 AB
 ⑥ 写 art_assets.info / art_asset_type.info / EditorBuild.Info
 ⑦ manifest 依赖校验
 ⑧ MixAssetBundle() 混淆
 ⑨ 回写 ArtAssetsSVCVersion
```

### 资源收集：`Runtime` 目录

`LoderBuiltinRuntimeAssetDirectories` 节点扫描 `Assets/*/Runtime/**`（`BApplication.GetAllRuntimeDirects()`）。

!!! danger "只有 `Assets/*/Runtime/**` 下的资源会被打进 AB"
    其他位置的资源只能作为依赖被自动收集。放错位置 = "资源没被打包"或"冗余打包"。

### 全部内置节点（20 个）

| 节点 | 菜单路径 | 排序 |
|------|---------|------|
| `BDFrameworkAssetsEnv` | `BDFramework/[*]初始化框架Assets环境` | 1 |
| `LoderBuiltinRuntimeAssetDirectories` | `BDFramework/[Loder]加载Runtime资产路径` | 1 |
| `LoderOtherAssetDirectory` | `BDFramework/[Loder]附加打包资产路径(避免使用)` | 1 |
| `FilterGroupByPath` | `BDFramework/[分组]Group by path` | 10 |
| `FilterGroupSplitDirectory` | `BDFramework/[分组]Group SubDirectory` | 10 |
| `SetGranularityByFolder` | `BDFramework/[颗粒度]按文件夹打包` | 30 |
| `SetGranularityByFolderTag` | `BDFramework/[颗粒度]按文件夹 Tag 打包` | 30 |
| `SetGranularityBySubFolder` | `BDFramework/[颗粒度]按子文件夹打包` | 31 |
| `SetGranularityByPrefab` | `BDFramework/[颗粒度]按Prefab打包` | 32 |
| `SetGranularityMoudule` | `BDFramework/[辅助]模块预览` | 35 |
| `SetGranularityNull` | `BDFramework/[辅助]预览` | 36 |
| `CollectShaderKeyWord` | `BDFramework/[逻辑]搜集shader变体` | 60 |
| `CollectSpriteAtlas` | `BDFramework/[逻辑]搜集图集` | 60 |
| `CollectVideoClip` | `BDFramework/[逻辑]搜集Video` | 60 |
| `SetKeepGUID` | `BDFramework/[设置]使用GUID加载` | 70 |
| `BuildAssetBundle` | `BDFramework/[Build]打包AssetBundle` | 100 |
| `BuildAssetBundlePreView` | `BDFramework/[Build]预览AB` | 101 |
| `BuildCriware` | `BDFramework/[Build]打包Criware音频` | 102 |
| `MultiplePackage` | `BDFramework/[分包]资源路径分包` | 110 |
| `MultiplePackageName` | `BDFramework/[分包]设置分包名` | 111 |

!!! danger "分包节点必须位于 `BuildAssetBundle` 之后"
    排序 110/111 > 100 是刻意的。

### 颗粒度

```csharp
BuildAssetInfos.SetABPackLevel
    None = 0  →  Simple  →  Force  →  FrameworkDefault  →  Lock
```

**高级别覆盖低级别**（`Lock` 最高）。

```csharp
public (bool, string) SetABPack(string assetName, string newABName,
                                SetABPackLevel setLevel, string owerLog, bool isSetAllDependAsset);
```

!!! danger "旧 API 名 `SetABName` 不存在"
    当前是 **`SetABPack`**，多了 `SetABPackLevel` 与 `isSetAllDependAsset` 两个参数，并返回 `(成功, 消息)`。

### 分包

```csharp
public class MultiplePackage
{
    static public List<SubPackageConfigItem> AssetMultiplePackageConfigList;
}
```

产物：`assets_subpack.info`。

```csharp
public class SubPackageConfigItem
{
    public string       PackageName;
    public List<int>    ArtAssetsIdList;
    public List<string> HotfixCodePathList;
    public List<string> TablePathList;
    public List<string> ConfAndInfoList;
}
```

!!! tip "按 `Runtime` 路径分包才能自动算依赖"
    按 `Runtime` 下的逻辑模块路径分包，框架能自动推导依赖闭包；按其他维度分包需手工维护 `ArtAssetsIdList`。

### 打包参数

| 参数 | 值 |
|------|-----|
| `buildParams.BundleCompression` | `BuildCompression.LZ4` |
| `buildParams.AppendHash` | `false` |
| CacheServer | `EditorSettings.cacheServerEndpoint` + `AssetDatabase.CanConnectToCacheServer` |
| SBP 本地缓存 | `Library/BuildCache`，`PruneCache_Background(200GB)` |

### 图资产位置

`Assets/AssetGraph/`：`BResourceAssetBundleConfig.asset`（主图）、`BResourceAssetBundleConfigEX.asset` / `EX2.asset`、`GroupTest.asset`、`NodeEx/Editor/SetGranularity_{Battle,Map}.cs`（业务自定义节点示例）。

图发现：`AssetDatabase.FindAssets("t: UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph", {"Assets"})`，返回第一个含 `BDFrameworkAssetsEnv` 节点的图。

## 5. 母包构建

### `BuildMode` 四级

| 模式 | BuildOptions | 测试程序集 |
|------|-------------|-----------|
| `Debug` | `CompressWithLz4HC \| Development \| AllowDebugging` | **注入** |
| `DebugForProfiler` | 上述 + `ConnectWithProfiler \| EnableDeepProfilingSupport` | **不注入** |
| `Release` | `CompressWithLz4HC` | 不注入 |
| `ReleaseForTest` | `CompressWithLz4HC` | **注入** |

### 三平台

| 平台 | 方法 | 产物 | 特殊处理 |
|------|------|------|---------|
| Android | `BuildAPK(BuildMode, string outdir)` | `DevOps/PublishPackages/android/<identifier>.apk` | keystore **必填**；`bundleVersionCode++`；HyCLR 时 `gcIncremental=false` + `ApiCompatibilityLevel.NET_4_6` |
| iOS | `BuildIpa(BuildMode, string outdir)` | `DevOps/PublishPackages/ios/<identifier>/` → `<identifier>.ipa` | 调 `Editor.DevOps~/BuildTools/BuildClientPackage/build_xcode.shell`；`buildNumber++` |
| Windows | `BuildExe(BuildMode, string outdir)` | `DevOps/PublishPackages/windows/<identifier>/Launcher.exe` | 先删目录；**必须** `EnsureHybridClrHotUpdateAssembliesCopiedToManaged` |
| macOS | **未实现** | — | `throw new Exception("未实现打包平台:" + buildTarget)` |

!!! danger "Windows 构建后必须补拷贝热更程序集"
    否则首场景的热更 `MonoBehaviour` 会变成 **missing script**。

### 版本号

```csharp
internal const string ClientVersionBatchArgName = "-clientVersion";
public const string DefaultClientVersion = "0.1.0";
```

缺失时回退顺序：`EditorSetting.BuildClientPackage.ClientVersion` → `GameBaseConfigProcessor.Config.ClientVersionNum` → `"0.1.0"`。

### 产物命名

非 `Release` 模式：

```csharp
PlayerSettings.productName        += "." + buildMode.ToString().ToLower();
PlayerSettings.applicationIdentifier += 同后缀;
```

构造前做 4 种后缀（`.Debug` / `.DebugForProfiler` / `.ReleaseForTest` / `.Profiler`）**容错剥离**，`finally` 中还原。

## 6. 发布管线

```csharp
namespace BDFramework.Editor.PublishPipeline

public static class PublishPipelineTools
{
    static public void PublishAssetsToServer(string path);
    static public List<AssetItem> GetGameAssetItemList(string assetsRootPath, RuntimePlatform platform);
    static public string GenServerHashAssets(string source, string outputRootPath,
                                            RuntimePlatform platform, string version);
    static public string UPLOAD_FOLDER_SUFFIX = "_ReadyToUpload";
}
```

上传布局：

```csharp
IPath.Combine(path, UPLOAD_FOLDER_SUFFIX, baseconfig.ClientVersionNum,
              BApplication.GetPlatformLoadPath(platform))
// → <path>/_ReadyToUpload/<ClientVersionNum>/<platform>/
```

!!! warning "源码注释与实现顺序相反"
    注释写 `{UPLOAD_FOLDER_SUFFIX}/{platform}/{version}`，**实现是 `<version>/<platform>`**。

!!! warning "分包路径有双层 `_ReadyToUpload`"
    `BResources.GetAssetsSubPackageInfoPath(IPath.Combine(outputRootPath, UPLOAD_FOLDER_SUFFIX), platform, name)` 会产生重复层级。

### 清单黑名单

`IsExcludedServerAssetLocalPath` 剔除：`art_assets/EditorBuild.Info`、`assets.info`、`assets_subpack.info`、`build_result.info`、`package_build.info`、`art_assets/art_assets`、文件名含 `buildlogtep.json` / `build_result.info`。

### hash 冲突

```csharp
throw new Exception("【ServerAssetsItem.Info】错误! hash重复! ...");
```

### 无改动跳过

比对源/目标 `assets.info` 的 MurmurHash3 一致 → `【PublishPipeline】资源无改动，无需重新生成服务器文件.`

## 7. 管线回调钩子

```csharp
namespace BDFramework.Editor      // ★ 不是 BDFramework.Editor.BuildPipeline

abstract public class ABDFrameworkPublishPipelineBehaviour
{
    // 编译 DLL
    virtual public void OnBeginBuildDLL();
    virtual public void OnEndBuildDLL(string outputPath);

    // SQLite
    virtual public void OnBeginBuildSqlite();
    virtual public void OnEndBuildSqlite(string outputPath);

    // 导表
    virtual public void OnExportExcel(Type type);

    // AssetBundle
    virtual public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);
    virtual public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);

    // 资源总管道
    virtual public void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,
                                              string lastVersionNum, out string newVersionNum);
    virtual public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath,
                                            string newVersionNum);

    // SVC 版本号
    virtual public string GetArtSVCNum(RuntimePlatform platform, string outputPath);      // 默认 "0"
    virtual public string GetTableSVCNum(RuntimePlatform platform, string outputPath);    // 默认 "0"
    virtual public string GetScriptSVCNum(RuntimePlatform platform, string outputPath);   // 默认 "0"

    // 母包
    virtual public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath, string clientVersion);
    virtual public void OnEndBuildPackage(BuildTarget buildTarget, string outputpath);

    // 发布
    virtual public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
    virtual public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
}
```

!!! danger "`OnBeginBuildAllAssets` 是唯一有默认语义的钩子"
    ```csharp
    virtual public void OnBeginBuildAllAssets(..., out string newVersionNum)
    {
        newVersionNum = lastVersionNum;      // 默认沿用上一个版本号
    }
    ```
    覆写时**必须给 `newVersionNum` 赋值**，否则是 `null`。

!!! danger "`OnBeginBuildPackage` 的基类实现非空"
    它做这些事：
    - `GitProcessor.GetVersion(6)` → 写 `BasePckScriptSVCVersion`
    - `ClientAssetsUtils.GenBasePackageBuildInfo(...)`
    - `config.ClientVersionNum = clientVersion`
    - Android `bundleVersionCode++` / iOS `buildNumber++` / Windows `bundleVersion = clientVersion`

    **覆写时务必 `base.OnBeginBuildPackage(...)`**，否则版本号不递增、`package_build.info` 不生成。

仓库中的示例：`Assets/Code/BDFramework.Game/Editor/BDFrameworkPublishPipelineBehaviourTest.cs`。

## 8. 自定义 AssetGraph 节点

```csharp
using UnityEngine.AssetGraph;

[CustomNode("BDFramework/[颗粒度]我的规则", 33)]
public class SetGranularity_MyRule : SetGranularityBase        // BDFramework.Editor.AssetGraph.Node
{
    public override void Prepare(BuildTarget target, NodeData nodeData,
        IEnumerable<PerformGraph.AssetGroups> incoming,
        IEnumerable<ConnectionData> connections,
        PerformGraph.Output output)
    {
        var ctx = BDFrameworkAssetsEnv.BuildingCtx;
        if (ctx == null) return;

        foreach (var group in incoming)
            foreach (var kv in group.assetGroups)
                foreach (var asset in kv.Value)
                {
                    ctx.BuildAssetInfos.SetABPack(
                        asset.importFrom,
                        "MyAB",
                        SetLevel,                 // 基类提供
                        "[我的规则]",
                        IsIncludeDependAssets);   // 基类提供
                }
    }

    public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
        NodeGUIEditor editor, Action onValueChanged) { }

    public override void Initialize(NodeData data) { }
    public override Node Clone(NodeData newNodeData) { }
}
```

| 项 | 说明 |
|----|------|
| 基类 | `UnityEngine.AssetGraph.Node`；框架抽象基类 `BDFramework.Editor.AssetGraph.Node.SetGranularityBase` |
| 特性 | `[CustomNode("<category>/[分组]<名字>", <order>)]` |
| 必须放 `Editor` 目录 | AssetGraph 是 Editor-only |
| `Prepare` | 声明 AB 归属（改 `BuildAssetInfos`）；**输出仅用于预览** |
| 辅助 API | `AssetGraphTools.GetComingAssets(incoming)` / `UpdateNodeGraph(NodeGUI)` / `UpdateConnectLine` / `RemoveOutputNode` |
| 环境 | `BDFrameworkAssetsEnv.BuildingCtx`（静态）；`bdenv.SetBuildParams(outPath, isBuilding: true)` |

排序段参考：1 环境 / 10 分组 / 30–36 颗粒度 / 60 搜集 / 70 设置 / 100–102 打包 / 110–111 分包。

业务侧示例：`Assets/AssetGraph/NodeEx/Editor/SetGranularity_{Battle,Map}.cs`。

## 9. 故障对照

| 现象 | 根因 | 处理 |
|------|------|------|
| `请先生成HCLR Setting!` | 未执行 HybridCLR 初始化 | `HybridCLR/Installer...` + `HybridCLR/Settings...` |
| `build hotfix_dll failed` | 编译错误 | 查看 `Application.logMessageReceived` 捕获的 Error |
| `【AOT.Load】HyCLR热更DLL不存在!` | 缺少 `script/hotfix/*.zlua.bytes` | 检查构建产物 |
| Release 包里有测试程序集 | 注入器未移除 | 检查 `-buildMode` 参数 |
| AOT 元数据版本不匹配 | `script/aot_patch` 与母包 IL2CPP 产物不同源 | 它**不跟随版本目录**，需随母包一起更新 |
| Android 构建耗时膨胀 | deep profiling 未在 `HyCLR PreBuild` 之前清除 | 见下方警告 |
| 资源没打进 AB | 不在 `Assets/*/Runtime/**` 下 | 移动到 `Runtime` 目录 |
| AB 数量爆炸 | 用了「按 Prefab 打包」这类最细颗粒度 | 调整颗粒度规则 |
| 分包不生效 | 分包节点排在了 `BuildAssetBundle` 之前 | 调整节点顺序 |
| 颗粒度规则被覆盖 | `Lock` 级别优先级最高 | 检查 `SetLevel` |
| Windows 包 missing script | 未补拷贝热更程序集 | `EnsureHybridClrHotUpdateAssembliesCopiedToManaged` |
| 版本号不递增 | 覆写 `OnBeginBuildPackage` 漏 `base.` | 补上 |
| `newVersionNum` 为 `null` | 覆写 `OnBeginBuildAllAssets` 未赋值 out 参数 | 补上 |
| SVC 版本号恒为 `"0"` | 未覆写 `Get*SVCNum` | 覆写 |

!!! danger "deep profiling 必须在 `HyCLR PreBuild` 之前清除"
    否则 `GenerateStripedAOTDlls` 会继承 `EnableDeepProfilingSupport`，Tundra 缓存被污染，Android IL2CPP 从 ~10 分钟膨胀到 ~34 分钟。

## 10. 详细参考

| 文件 | 内容 |
|------|------|
| [references/build-entrypoints.md](./references/build-entrypoints.md) | 全部入口类/方法的完整签名、产物路径速查、菜单索引 |

在线文档：

- [构建与发布](https://yimengfan.github.io/BDFramework.Core/pipeline/index.md)
- [热更代码 HybridCLR](https://yimengfan.github.io/BDFramework.Core/pipeline/build-hotfix-dll.md)
- [AssetBundle 打包](https://yimengfan.github.io/BDFramework.Core/pipeline/build-assetbundle.md)
- [AssetGraph 节点扩展](https://yimengfan.github.io/BDFramework.Core/pipeline/build-assetbundle-extension.md)
- [母包构建](https://yimengfan.github.io/BDFramework.Core/pipeline/build-package.md)
- [资源发布](https://yimengfan.github.io/BDFramework.Core/pipeline/publish-assets.md)

## 11. 改动前检查清单

- [ ] 资源在 `Assets/*/Runtime/**` 下
- [ ] 自定义节点放在 `Editor` 目录
- [ ] 分包节点排在 `[Build]打包AssetBundle` 之后
- [ ] 覆写 `OnBeginBuildPackage` 调了 `base.`
- [ ] 覆写 `OnBeginBuildAllAssets` 给 `newVersionNum` 赋了值
- [ ] 改动了 `ScriptLoder` 的路径常量时**同步改了 `ScriptLoderAOT`**
- [ ] Release 构建确认无测试程序集
- [ ] 新增/修改的日志含中文 `测试目的=` / `实现手段=`（BatchMode 入口）
