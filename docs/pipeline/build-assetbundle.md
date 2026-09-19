# AssetBundle 打包

打包通过 **AssetGraph**（BDFramework 维护分支）以可视化节点图驱动。

## 入口

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

## 构建流程

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

## AssetGraph 图资产

位于 `Assets/AssetGraph/`：

| 文件 | 用途 |
|------|------|
| `BResourceAssetBundleConfig.asset` | **主图**（含 `BDFrameworkAssetsEnv` 节点） |
| `BResourceAssetBundleConfigEX.asset` / `EX2.asset` | 扩展图 |
| `GroupTest.asset` | 分组测试图 |
| `NodeEx/Editor/SetGranularity_Battle.cs`、`SetGranularity_Map.cs` | **业务侧自定义节点示例** |

图发现逻辑（`GetBDFrameExAssetGraph()`）：

```csharp
AssetDatabase.FindAssets("t: UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph", {"Assets"})
// 返回第一个含 BDFrameworkAssetsEnv 节点的图
```

打包面板打开路径**硬编码**：`win.OpenGraph("Assets/AssetGraph/BResourceAssetBundleConfig.asset")`。

## 全部内置节点

`[CustomNode("<category>/<名字>", <order>)]`，共 20 个：

| 节点文件 | 菜单路径 | 排序 |
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

!!! danger "分包节点必须位于 `BuildAssetBundle` 之后"
    排序值 110/111 > 100，这是刻意的。把分包节点放在打包节点之前会导致分包信息未参与打包。

## 资源收集：`Runtime` 目录

`LoderBuiltinRuntimeAssetDirectories` 节点扫描 `Assets/*/Runtime/**`（`BApplication.GetAllRuntimeDirects()`）。

**只有这里的资源会被打进 AB。** 其他位置的资源只能作为依赖被自动收集。

!!! tip "资产管理的关键是规范，不是打包工具"
    - **可显式加载**的资源 → 放在 `Assets/<任意子目录>/Runtime/**` 下
    - **仅被依赖**的资源（贴图、材质、模型）→ 放哪都行，依赖会自动收集
    - 放错位置的后果是"资源没被打包"或"冗余打包"

## 颗粒度规则

### 等级

```csharp
BuildAssetInfos.SetABPackLevel
    None = 0  →  Simple  →  Force  →  FrameworkDefault  →  Lock
```

**高级别覆盖低级别**。`Lock` 最高，任何节点都无法再改。

### 设置 API

```csharp
public (bool, string) SetABPack(string assetName, string newABName,
                                SetABPackLevel setLevel, string owerLog, bool isSetAllDependAsset);
```

### 内置颗粒度节点语义

| 节点 | 效果 |
|------|------|
| 按文件夹打包 | 同一文件夹的资源打进一个 AB |
| 按文件夹 Tag 打包 | 按文件夹上的 Tag 分组 |
| 按子文件夹打包 | 每个子文件夹一个 AB（比"按文件夹"更细） |
| 按 Prefab 打包 | 每个 Prefab 一个 AB（最细，AB 数量最多） |

**默认行为是最小颗粒**；图集/变体规则会合并；文件夹规则覆盖默认。

### `Group by path`

`FilterGroupByPath` 按路径分组，典型分组为 `Runtime` / `Depend` / `SpriteAtlas` / `Shaders`。分组是后续颗粒度规则的作用域。

## 分包

```csharp
public class MultiplePackage
{
    static public List<SubPackageConfigItem> AssetMultiplePackageConfigList;
}
```

| 节点 | 作用 |
|------|------|
| `[分包]资源路径分包` | 按资源路径划分包 |
| `[分包]设置分包名` | 指定子包名 |

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
    分包粒度与颗粒度规则耦合。按 `Runtime` 下的逻辑模块路径分包，框架能自动推导依赖闭包；按其他维度分包需要手工维护 `ArtAssetsIdList`。

## AB 命名

| 方式 | 说明 |
|------|------|
| 默认 | `GetAssetBundleItems()` 使用 `ABName` |
| GUID | `[设置]使用GUID加载` 节点把 `ABName` 替换为 GUID；失败时 `LogError("主资源，获取GUID失败：...")` |

GUID 命名可以避免重命名资源导致的 AB 名变化（对增量更新更友好）。

## 打包参数

| 参数 | 值 |
|------|-----|
| `buildParams.BundleCompression` | `BuildCompression.LZ4` |
| `buildParams.AppendHash` | `false` |
| CacheServer | 由 `EditorSettings.cacheServerEndpoint` + `AssetDatabase.CanConnectToCacheServer(ip, port)` 决定 |
| SBP 本地缓存 | `Library/BuildCache`，`PruneCache_Background(200GB)`（before-build / after-build 各一次） |

## 输出

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

`build_result.info` 与 `buildlogtep.json` 会被发布管线**黑名单剔除**，不上传服务器。

## 资源混淆

```csharp
BuildTools_AssetBundleV2.MixAssetBundle(string outpath, RuntimePlatform platform);
```

源目录常量：`BResources.MIX_SOURCE_FOLDER = "Assets/Resource/Runtime/MIX_AB_SOURCE"`。
黑名单：`GetMixAssets()`。

## 自定义节点

→ 详见 [AssetGraph 节点扩展](build-assetbundle-extension.md)。

## 加载验证

```csharp
public class AssetBundleBenchmarkToolsV2      // 菜单：BDFrameWork工具箱/测试
```

在 Editor 中验证 AB 能否正确加载（走 `BResources.InitLoadAssetBundleEnv`）。

## 常见故障

| 现象 | 根因 |
|------|------|
| 资源没打进 AB | 不在 `Assets/*/Runtime/**` 下 |
| AB 数量爆炸 | 用了「按 Prefab 打包」这类最细颗粒度 |
| 加载时 `依赖获取失败,art_assets.info不存在资产配置` | 资源未进索引；或 `art_assets.info` 未生成 |
| 分包不生效 | 分包节点排在了 `BuildAssetBundle` 之前 |
| 颗粒度规则被覆盖 | `Lock` 级别的规则优先级最高 |
| 增量更新每次都全量 | `AppendHash = false` + AB 命名用了会变的 `ABName`（改用 GUID 节点） |
| 磁盘缓存膨胀 | SBP `Library/BuildCache` 上限 200GB |

## 相关页面

- [AssetGraph 节点扩展](build-assetbundle-extension.md)
- [资源加载 BResources](../api/resources.md) —— 运行时加载链路
- [资源发布](publish-assets.md) —— hash 布局与上传
- [目录结构与约定](../guide/project-structure.md) —— `Runtime` 目录约定
