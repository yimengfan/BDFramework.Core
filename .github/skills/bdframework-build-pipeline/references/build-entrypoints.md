# 构建入口与产物速查

> 来源：`Editor/EditorPipeline/**`

## 入口类 → 方法 → 产物

| 模块 | 入口类 | 入口方法 | 产物路径 |
|------|--------|---------|---------|
| AssetBundle | `BuildTools_AssetBundleV2` | `BuildAssetBundles(RuntimePlatform, string)` | `DevOps/PublishAssets/<platform>/art_assets/`（`art_assets.info` / `art_asset_type.info` / `EditorBuild.Info`） |
| 热更 DLL | `BuildTools_HotfixScript` → `HyCLREditorTools` | `BuildDLL(string outpath, RuntimePlatform)` | `.../script/hotfix/<asm>.zlua.bytes` |
| AOT 补充 | `HyCLREditorTools` | `CopyAOTMetadataDLL(string, string, BuildTarget)` | `.../script/aot_patch/<asm>.zlua.bytes` |
| 表格 | `BuildTools_Excel2SQLite` | `BuildSqlite(string, RuntimePlatform, DBType, bool)` | `.../local.db`、`.../server_data/server.db`、`.../package_build.info` |
| 资源总管道 | `BuildTools_Assets` | `BuildAll(RuntimePlatform, string, string, BuildPackageOption)` | 上述全部 + `.../assets.info` |
| 母包 Android | `BuildTools_ClientPackage` | `Build(BuildMode, string, string, bool, string, BuildTarget, …)` | `DevOps/PublishPackages/android/<identifier>.apk` |
| 母包 iOS | 同上 | `BuildIpa(BuildMode, string)` | `DevOps/PublishPackages/ios/<identifier>/` → `<identifier>.ipa` |
| 母包 Windows | 同上 | `BuildExe(BuildMode, string)` | `DevOps/PublishPackages/windows/<identifier>/Launcher.exe` |
| 资源发布 | `PublishPipelineTools` | `PublishAssetsToServer(string path)` | `DevOps/PublishAssets/_ReadyToUpload/<version>/<platform>/<hash>` + `assets.info` + `server_assets_version.info` |

## 完整签名

### `BuildTools_AssetBundleV2`

```csharp
namespace BDFramework.Editor.BuildPipeline.AssetBundle

public static class BuildTools_AssetBundleV2
{
    public static bool BuildAssetBundles(RuntimePlatform platform, string outputPath);
    static public void ExcuteAssetGraphBuild(BuildTarget buildTarget, string outPath);
    static public void MixAssetBundle(string outpath, RuntimePlatform platform);
    static public void Test();
}
```

### `BuildTools_HotfixScript` / `HyCLREditorTools`

```csharp
namespace BDFramework.Editor.HotfixScript

public static class BuildTools_HotfixScript
{
    static public void BuildDLL(string outpath, RuntimePlatform platform);
}

public static class HyCLREditorTools
{
    static public void SetHyCLRConfig();
    static public bool CheckEditorCode();
    static public void PreBuild(BuildTarget target);
    static public void BuildHotfixDLL(string outputDir, BuildTarget target);
    static public void CopyAOTMetadataDLL(string sourceDir, string outputRoot, BuildTarget target);
}
```

### `BuildTools_ClientPackage`

```csharp
namespace BDFramework.Editor.BuildPipeline

public static class BuildTools_ClientPackage
{
    static public bool Build(BuildMode buildMode, string buildScene, string buildConfig,
                             bool isGenAssets, string outdir, BuildTarget buildTarget,
                             BuildTools_Assets.BuildPackageOption buildOption = BuildAll,
                             string clientVersion = null);

    static public bool Build(BuildMode buildMode, bool isGenAssets, string outdir,
                             BuildTarget buildTarget, …);

    public static bool BuildAPK(BuildMode mode, string outdir);
    public static bool BuildIpa(BuildMode mode, string outdir);
    public static bool BuildExe(BuildMode mode, string outdir);

    public static void BuildClientPackageForBatchMode(BuildTarget buildTarget, BuildMode buildMode);
    public static string GetClientVersionForBatchMode();
    public static string GetDefaultClientVersion();

    internal const string ClientVersionBatchArgName = "-clientVersion";
    public const string DefaultClientVersion = "0.1.0";

    public const string SCENE_PATH    = "Assets/Scenes/BDFrame.unity";
    public const string QA_SCENE_PATH = "Assets/Scenes/BDFrameForQA.unity";
}
```

### `BuildTools_Assets`

```csharp
namespace BDFramework.Editor.BuildPipeline

public static class BuildTools_Assets
{
    public enum BuildPackageOption { BuildHotfixCode, BuildArtAssets, BuildAll }

    public static void BuildAll(RuntimePlatform platform, string outputPath,
                                string clientVersion, BuildPackageOption buildOption);
    public static void BuildClientResForBatchMode(BuildTarget buildTarget, BuildPackageOption option);

    public const string CIOutputRootBatchArgName = "-ciOutputRoot";
    public static string GetCIOutputRootForBatchMode(string defaultOutputRoot);
}
```

### `PublishPipelineTools`

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

### `BuildAssetInfos`

```csharp
namespace BDFramework.Editor.BuildPipeline.AssetBundle.AssetsInfo

public class BuildAssetInfos
{
    public enum SetABPackLevel { None = 0, Simple, Force, FrameworkDefault, Lock }

    public (bool, string) SetABPack(string assetName, string newABName,
                                    SetABPackLevel setLevel, string owerLog,
                                    bool isSetAllDependAsset);

    public void ReorganizeAssetBundleUnit();
    public List<AssetBundleItem> GetAssetBundleItems();
}
```

## 磁盘布局

```text
DevOps/PublishAssets/                     ← 构建根
├── <platform>/
│   ├── art_assets/
│   │   ├── <ABName 或 GUID>
│   │   ├── art_assets.info             List<AssetBundleItem> CSV
│   │   ├── art_asset_type.info         AssetTypeConfig CSV
│   │   └── EditorBuild.Info            BuildAssetInfos JSON（仅 Editor）
│   ├── script/
│   │   ├── hotfix/<asm>.zlua.bytes
│   │   └── aot_patch/<asm>.zlua.bytes
│   ├── local.db
│   ├── assets.info
│   ├── assets_subpack.info
│   ├── package_build.info
│   ├── server_assets_version.info
│   ├── build_result.info               SBP（发布时剔除）
│   └── buildlogtep.json                SBP（发布时剔除）
├── server_data/server.db
└── _ReadyToUpload/<version>/<platform>/
```

平台目录名：`windows` / `android` / `osx` / `ios`。

```text
DevOps/PublishPackages/
├── android/<identifier>.apk
├── ios/<identifier>/          → <identifier>.ipa
└── windows/<identifier>/Launcher.exe
```

## 菜单索引

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `BDFrameWork工具箱/Odin BuildPipeline` | — | `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/1.DLL打包` | 52 | `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/2.AssetBundle打包` | 53 | `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/3.表格/表格预览` | 54 | `EditorWindow_Table` |
| `BDFrameWork工具箱/3.表格/表格->生成Class[程序目录]` | 54 | `Excel2CodeTools` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite` | 55 | `BuildTools_Excel2SQLite.ExecuteGenSqlite` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite[Server]` | 56 | `BuildTools_Excel2SQLite.ExecuteJsonToSqlite` |
| `BDFrameWork工具箱/4.网络协议/Protobuf->生成Class` | 57 | `Protobuf2ClassTools` |
| `BDFrameWork工具箱/5.构建包体` | 58 | `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/PublishPipeline/1.发布资源` | 101 | `EditorWindow_PublishAssets.Open` |
| `BDFrameWork工具箱/HotfixPipeline/1.配置热更文件` | 111 | `EditorWindow_HotfixFileSetting` |
| `BDFrameWork工具箱/DevOps/CI - API预览` | 121 | `EditorWindow_CICD.Open` |
| `BDFrameWork工具箱/测试` | — | `BuildTools_AssetBundleV2.Test` |

排序枚举：

```csharp
public enum BDEditorGlobalMenuItemOrderEnum
{
    BDFrameworkGuid = 0,              BDFrameworkSetting = 1,
    BuildPipeline = 50,               BuildPackage_DLL = 52,
    BuildPackage_Assetbundle = 53,    BuildPackage_Table_Table2Class = 54,
    BuildPackage_Table_GenSqlite = 55, BuildPackage_Table_Json2Sqlite = 56,
    BuildPipeline_NetProtocol_Proto2Class = 57,
    BuildPipeline_BuildPackage = 58,  PublishPipeline = 100,
    PublishPipeline_BuildAsset = 101, PublishPipeline_PublishPackage = 102,
    HotfixPipeline = 111,             DevOps = 121,
    TestPepeline = 201,               TestPepelineEditor = 202
}
```

## 关键常量

```csharp
// 资源路径
BResources.ART_ASSET_ROOT_PATH              = "art_assets"
BResources.ART_ASSET_INFO_PATH              = "art_assets/art_assets.info"
BResources.ART_ASSET_TYPES_PATH             = "art_assets/art_asset_type.info"
BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH = "art_assets/EditorBuild.Info"
BResources.ASSETS_INFO_PATH                 = "assets.info"
BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH   = "assets_subpack.info"
BResources.SERVER_ASSETS_VERSION_INFO_PATH  = "server_assets_version.info"
BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH = "server_assets_subpack_{0}.info"
BResources.MIX_SOURCE_FOLDER                = "Assets/Resource/Runtime/MIX_AB_SOURCE"
BResources.SBPBuildLog                      = "buildlogtep.json"
BResources.SBPBuildLog2                     = "build_result.info"

// 热更脚本
ScriptLoder.HOTFIX_DLL_PATH      = "script/hotfix"
ScriptLoder.HYCLR_AOT_PATCH_PATH = "script/aot_patch"
ScriptLoder.HOT_DLL_EXTENSION    = ".zlua.bytes"

// 数据库
SqliteLoder.LOCAL_DB_PATH  = "local.db"
SqliteLoder.SERVER_DB_PATH = "server.db"

// 母包
ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH = "package_build.info"
```

## Editor 环境初始化链

`BDFrameworkEditorEnvironment.InitEditorEnvironment()`：

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

!!! tip "BatchMode 下必须手动调用"
    `PublishPipeLineCI` 的静态构造就是这样做：
    ```csharp
    static PublishPipeLineCI()
    {
        if (Application.isBatchMode) BDFrameworkEditorEnvironment.InitEditorEnvironment();
    }
    ```
