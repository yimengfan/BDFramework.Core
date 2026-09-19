# AssetBundle Building

Packing is driven by **AssetGraph** (a BDFramework-maintained fork) through a visual node graph.

## Entry points

```csharp
namespace BDFramework.Editor.BuildPipeline.AssetBundle

public static class BuildTools_AssetBundleV2
{
    public static bool BuildAssetBundles(RuntimePlatform platform, string outputPath);
    static public void ExcuteAssetGraphBuild(BuildTarget buildTarget, string outPath);   // → AssetGraphUtility.ExecuteGraph
    static public void MixAssetBundle(string outpath, RuntimePlatform platform);          // AB 混淆
    static public void Test();                                                            // 菜单入口
}

public class AssetBundleBuildingContext      // 构建上下文（SBP 协调器）
{
    public BuildAssetBundleParams   BuildParams;
    public BuildAssetInfos          BuildAssetInfos;
    public List<AssetBundleItem>    AssetBundleItemList;
    public List<string>             RuntimeAssetsList;
    public List<string>             DependAssetList;

    private void BuildAssetBundle_SBP(List<AssetBundleItem> items,
        List<KeyValuePair<string, BuildAssetInfos.AssetInfo>> assetInfos,
        BuildAssetBundleParams buildParams, RuntimePlatform platform);
}
```

## Build flow

```text
StartBuildAssetBundle
 ① 清 art_assets/
 ② ReorganizeAssetBundleUnit()        ← 按颗粒度规则归整
 ③ GetAssetBundleItems()              ← 生成 AB 列表
 ④ SBP 打包（Scriptable Build Pipeline）
 ⑤ 删旧 AB
 ⑥ 写 art_assets.info / art_asset_type.info / EditorBuild.Info
 ⑦ manifest 依赖校验
 ⑧ MixAssetBundle() 混淆
 ⑨ 回写 ArtAssetsSVCVersion
```

## AssetGraph graph assets

Located in `Assets/AssetGraph/`:

| File | Purpose |
|------|------|
| `BResourceAssetBundleConfig.asset` | **Main graph** (contains the `BDFrameworkAssetsEnv` node) |
| `BResourceAssetBundleConfigEX.asset` / `EX2.asset` | Extension graphs |
| `GroupTest.asset` | Grouping test graph |
| `NodeEx/Editor/SetGranularity_Battle.cs`, `SetGranularity_Map.cs` | **Business-side custom node examples** |

Graph discovery logic (`GetBDFrameExAssetGraph()`):

```csharp
AssetDatabase.FindAssets("t: UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph", {"Assets"})
// 返回第一个含 BDFrameworkAssetsEnv 节点的图
```

The packing panel's open path is **hardcoded**: `win.OpenGraph("Assets/AssetGraph/BResourceAssetBundleConfig.asset")`.

## All built-in nodes

`[CustomNode("<category>/<名字>", <order>)]` — 20 in total:

| Node file | Menu path | Order |
|---------|---------|------|
| `BDFrameworkAssetsEnv.cs` | `BDFramework/[*]初始化框架Assets环境` | 1 |
| `LoderBuiltinRuntimeAssetDirectories.cs` | `BDFramework/[Loder]加载Runtime资产路径` | 1 |
| `LoderOtherAssetDirectory.cs` | `BDFramework/[Loder]附加打包资产路径(避免使用)` | 1 |
| `FilterGroupByPath.cs` | `BDFramework/[分组]Group by path` | 10 |
| `FilterGroupSplitDirectory.cs` | `BDFramework/[分组]Group SubDirectory` | 10 |
| `SetGranularityByFolder.cs` | `BDFramework/[颗粒度]按文件夹打包` | 30 |
| `SetGranularityByFolderTag.cs` | `BDFramework/[颗粒度]按文件夹 Tag 打包` | 30 |
| `SetGranularityBySubFolder.cs` | `BDFramework/[颗粒度]按子文件夹打包` | 31 |
| `SetGranularityByPrefab.cs` | `BDFramework/[颗粒度]按Prefab打包` | 32 |
| `SetGranularityMoudule.cs` | `BDFramework/[辅助]模块预览` | 35 |
| `SetGranularityNull.cs` | `BDFramework/[辅助]预览` | 36 |
| `CollectShaderKeyWord.cs` | `BDFramework/[逻辑]搜集shader变体` | 60 |
| `CollectSpriteAtlas.cs` | `BDFramework/[逻辑]搜集图集` | 60 |
| `CollectVideoClip.cs` | `BDFramework/[逻辑]搜集Video` | 60 |
| `SetKeepGUID.cs` | `BDFramework/[设置]使用GUID加载` | 70 |
| `BuildAssetBundle.cs` | `BDFramework/[Build]打包AssetBundle` | 100 |
| `BuildAssetBundlePreView.cs` | `BDFramework/[Build]预览AB` | 101 |
| `BuildCriware.cs` | `BDFramework/[Build]打包Criware音频` | 102 |
| `MultiplePackage.cs` | `BDFramework/[分包]资源路径分包` | 110 |
| `MultiplePackageName.cs` | `BDFramework/[分包]设置分包名` | 111 |

!!! danger "Split package nodes must come after `BuildAssetBundle`"
    The order values 110/111 > 100 are deliberate. Putting split package nodes before the build node means the split information never takes part in the build.

## Asset collection: the `Runtime` directory

The `LoderBuiltinRuntimeAssetDirectories` node scans `Assets/*/Runtime/**` (`BApplication.GetAllRuntimeDirects()`).

**Only assets here are packed into AssetBundles.** Assets anywhere else can only be collected automatically as dependencies.

!!! tip "Asset management is about conventions, not the packing tool"
    - Assets that are **explicitly loadable** → put them under `Assets/<any subdirectory>/Runtime/**`
    - Assets that are **only referenced** (textures, materials, models) → anywhere is fine, dependencies are collected automatically
    - Getting the location wrong results in either "the asset was never packed" or "redundantly packed"

## Granularity rules

### Levels

```csharp
BuildAssetInfos.SetABPackLevel
    None = 0  →  Simple  →  Force  →  FrameworkDefault  →  Lock
```

**A higher level overrides a lower one.** `Lock` is the highest, and no node can change it any further.

### Setting API

```csharp
public (bool, string) SetABPack(string assetName, string newABName,
                                SetABPackLevel setLevel, string owerLog, bool isSetAllDependAsset);
```

### Built-in granularity node semantics

| Node | Effect |
|------|------|
| Pack by folder | Assets in the same folder are packed into one AB |
| Pack by folder Tag | Grouped by the Tag on the folder |
| Pack by subfolder | One AB per subfolder (finer than "by folder") |
| Pack by Prefab | One AB per Prefab (finest, largest number of ABs) |

**The default behaviour is the finest granularity**; atlas/variant rules merge; folder rules override the default.

### `Group by path`

`FilterGroupByPath` groups by path; typical groups are `Runtime` / `Depend` / `SpriteAtlas` / `Shaders`. A group is the scope that the granularity rules applied later operate on.

## Split packages

```csharp
public class MultiplePackage
{
    static public List<SubPackageConfigItem> AssetMultiplePackageConfigList;
}
```

| Node | Purpose |
|------|------|
| `[分包]资源路径分包` | Partition packages by asset path |
| `[分包]设置分包名` | Specify the sub-package name |

Artifact: `assets_subpack.info`.

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

!!! tip "Only splitting by `Runtime` path lets dependencies be computed automatically"
    Split package granularity is coupled with the granularity rules. When you split along the logical module paths under `Runtime`, the framework can derive the dependency closure automatically; splitting along any other dimension means maintaining `ArtAssetsIdList` by hand.

## AssetBundle naming

| Approach | Description |
|------|------|
| Default | `GetAssetBundleItems()` uses `ABName` |
| GUID | The `[设置]使用GUID加载` node replaces `ABName` with a GUID; on failure it logs `LogError("主资源，获取GUID失败：...")` |

GUID naming avoids AssetBundle names changing when an asset is renamed (which is friendlier to incremental updates).

## Build parameters

| Parameter | Value |
|------|-----|
| `buildParams.BundleCompression` | `BuildCompression.LZ4` |
| `buildParams.AppendHash` | `false` |
| CacheServer | Decided by `EditorSettings.cacheServerEndpoint` + `AssetDatabase.CanConnectToCacheServer(ip, port)` |
| SBP local cache | `Library/BuildCache`, `PruneCache_Background(200GB)` (once before-build and once after-build) |

## Output

```text
DevOps/PublishAssets/<platform>/art_assets/
├── <ABName 或 GUID>           AB 本体（无扩展名）
├── <name>.manifest            Unity manifest（发布时剔除）
├── art_assets.info            List<AssetBundleItem> CSV   ← 运行时加载索引
├── art_asset_type.info        AssetTypeConfig CSV
└── EditorBuild.Info           BuildAssetInfos JSON（editor only）

DevOps/PublishAssets/<platform>/
├── build_result.info          results.BundleInfos JSON
└── buildlogtep.json           SBP build log
```

`build_result.info` and `buildlogtep.json` are **excluded by the publish pipeline's blacklist** and are never uploaded to the server.

## Asset obfuscation

```csharp
BuildTools_AssetBundleV2.MixAssetBundle(string outpath, RuntimePlatform platform);
```

Source directory constant: `BResources.MIX_SOURCE_FOLDER = "Assets/Resource/Runtime/MIX_AB_SOURCE"`.
Blacklist: `GetMixAssets()`.

## Custom nodes

→ See [AssetGraph Node Extension](build-assetbundle-extension.md) for details.

## Load verification

```csharp
public class AssetBundleBenchmarkToolsV2      // 菜单：BDFrameWork工具箱/测试
```

Verifies in the Editor that AssetBundles load correctly (through `BResources.InitLoadAssetBundleEnv`).

## Common failures

| Symptom | Root cause |
|------|------|
| An asset was not packed into any AB | It is not under `Assets/*/Runtime/**` |
| The number of ABs explodes | You used the finest granularity such as "pack by Prefab" |
| `依赖获取失败,art_assets.info不存在资产配置` at load time | The asset never entered the index, or `art_assets.info` was not generated |
| Split packages have no effect | Split package nodes were ordered before `BuildAssetBundle` |
| Granularity rules are being overridden | The `Lock` level rule has the highest priority |
| Every incremental update is a full rebuild | `AppendHash = false` plus an AB name that uses the volatile `ABName` (switch to the GUID node) |
| Disk cache grows without bound | SBP `Library/BuildCache` is capped at 200GB |

## Related pages

- [AssetGraph Node Extension](build-assetbundle-extension.md)
- [Assets (BResources)](../api/resources.md) —— the runtime loading chain
- [Asset Publishing](publish-assets.md) —— hash layout and upload
- [Project Layout & Conventions](../guide/project-structure.md) —— the `Runtime` directory convention
