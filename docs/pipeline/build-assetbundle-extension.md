# AssetGraph 节点扩展

当内置的 20 个节点无法满足打包需求时，写一个自定义节点。

## 节点基类与特性

```csharp
using UnityEngine.AssetGraph;

[CustomNode("BDFramework/[颗粒度]我的规则", 33)]
public class SetGranularity_MyRule : Node
{
    public override void Prepare(BuildTarget target, NodeData nodeData,
        IEnumerable<PerformGraph.AssetGroups> incoming,
        IEnumerable<ConnectionData> connections,
        PerformGraph.Output output)
    {
        // ...
    }

    public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
        NodeGUIEditor editor, Action onValueChanged)
    {
        // 节点面板绘制
    }

    public override void Initialize(NodeData data) { }
    public override Node Clone(NodeData newNodeData) { }
}
```

| 项 | 说明 |
|----|------|
| 基类 | `UnityEngine.AssetGraph.Node` |
| 框架抽象基类 | `BDFramework.Editor.AssetGraph.Node.SetGranularityBase`（含 `IsIncludeDependAssets`、`SetLevel`） |
| 特性 | `[CustomNode("<category>/[分组]<名字>", <order>)]` |
| 核心回调 | `Prepare(...)`、`OnInspectorGUI(...)`、`Initialize(NodeData)`、`Clone(NodeData)` |

!!! danger "节点类必须在 `Editor` 目录下"
    AssetGraph 是 Editor-only 能力。节点脚本放 Runtime 目录会导致打包失败。

    业务侧示例的位置：`Assets/AssetGraph/NodeEx/Editor/`。

## `Prepare` vs `Build`

| 阶段 | 职责 | 是否允许改打包结果 |
|------|------|------------------|
| `Prepare` | 遍历输入分组、调用 `SetABPack` 声明 AB 归属 | ✓ 修改 `BuildAssetInfos` |
| `Build` | 实际的 AB 产出 | 由 `[Build]打包AssetBundle` 节点负责 |

**自定义颗粒度节点只在 `Prepare` 里工作**，通过 `SetABPack` 声明规则；输出仅用于预览。

## 关键 API

```csharp
// 获取上游资产
AssetGraphTools.GetComingAssets(incoming)

// 图操作
AssetGraphTools.UpdateNodeGraph(NodeGUI)
AssetGraphTools.UpdateConnectLine(...)
AssetGraphTools.RemoveOutputNode(...)

// 构建上下文（静态）
BDFrameworkAssetsEnv.BuildingCtx
BDFrameworkAssetsEnv.BuildingCtx.BuildParams
BDFrameworkAssetsEnv.BuildingCtx.BuildAssetInfos

// 设置环境
bdenv.SetBuildParams(outPath, isBuilding: true);

// 声明 AB 归属
BuildAssetInfos.SetABPack(assetName, newABName, setLevel, owerLog, isSetAllDependAsset);
```

!!! warning "`SetABPack` 的签名已变更"
    旧文档里的 `SetABName` **不存在**。当前 API 是：

    ```csharp
    public (bool, string) SetABPack(string assetName, string newABName,
                                    SetABPackLevel setLevel, string owerLog, bool isSetAllDependAsset);
    ```

    多了 `SetABPackLevel`（优先级）与 `isSetAllDependAsset`（是否同时作用于依赖）两个参数，并返回 `(成功, 消息)`。

## 颗粒度等级

```csharp
BuildAssetInfos.SetABPackLevel
    None = 0  →  Simple  →  Force  →  FrameworkDefault  →  Lock
```

**高级别覆盖低级别**。自定义节点应选择合适的 `SetLevel`：

| Level | 适用 |
|-------|------|
| `Simple` | 建议性规则，可被其他节点覆盖 |
| `Force` | 强规则，但允许更高优先级覆盖 |
| `FrameworkDefault` | 框架默认规则（内置节点使用） |
| `Lock` | 最高优先级，任何节点都无法再改 |

## 完整示例：按名字前缀打包

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AssetGraph;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.Editor.BuildPipeline.AssetBundle.AssetsInfo;

[CustomNode("BDFramework/[颗粒度]按名字前缀打包", 33)]
public class SetGranularity_ByPrefix : SetGranularityBase
{
    [SerializeField] private string prefix = "UI_";

    public override void Prepare(BuildTarget target, NodeData nodeData,
        IEnumerable<PerformGraph.AssetGroups> incoming,
        IEnumerable<ConnectionData> connections,
        PerformGraph.Output output)
    {
        var ctx = BDFrameworkAssetsEnv.BuildingCtx;
        if (ctx == null) return;

        foreach (var group in incoming)
        {
            foreach (var kv in group.assetGroups)
            {
                foreach (var asset in kv.Value)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(asset.fileNameAndExtension);
                    if (!name.StartsWith(prefix)) continue;

                    ctx.BuildAssetInfos.SetABPack(
                        asset.importFrom,              // assetName
                        prefix + name,                 // newABName
                        SetLevel,                      // 基类提供的级别
                        $"[按名字前缀] {name}",         // owerLog
                        IsIncludeDependAssets);        // 基类提供的开关
                }
            }
        }
    }

    public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
        NodeGUIEditor editor, System.Action onValueChanged)
    {
        editor.Layout.Space(4);
        var newPrefix = editor.Layout.TextField("前缀", prefix);
        if (newPrefix != prefix) { prefix = newPrefix; onValueChanged?.Invoke(); }
    }
}
```

!!! note "`SetGranularityBase` 提供 `SetLevel` 与 `IsIncludeDependAssets`"
    这两个字段由基类序列化并在 Inspector 中绘制，自定义节点直接读取即可，不需要自己声明。

## 业务侧真实示例

`Assets/AssetGraph/NodeEx/Editor/`：

| 文件 | 作用 |
|------|------|
| `SetGranularity_Battle.cs` | 战斗模块的颗粒度规则 |
| `SetGranularity_Map.cs` | 地图模块的颗粒度规则 |

这两个文件是**业务方自定义节点的参考实现**。

## 预览与调试

| 节点 | 作用 |
|------|------|
| `[辅助]预览`（`SetGranularityNull`） | 输出当前分组的资产清单，不做任何规则 |
| `[辅助]模块预览`（`SetGranularityMoudule`） | 按模块维度预览 |
| `[Build]预览AB`（`BuildAssetBundlePreView`） | 预览 AB 划分结果（不实际打包） |

推荐调试流程：

```text
Group by path → [辅助]预览 → 查看资产清单
             → 加颗粒度节点 → [Build]预览AB → 对比 AB 划分
             → [Build]打包AssetBundle → 实际打包
```

## 节点排序参考

| 排序段 | 用途 |
|--------|------|
| 1 | 环境/路径加载（必须在最前） |
| 10 | 分组 |
| 30–36 | 颗粒度规则 |
| 60 | 资源搜集（shader 变体 / 图集 / video） |
| 70 | 设置类 |
| 100–102 | 打包 |
| 110–111 | 分包（**必须在打包之后**） |

新增节点时选择一个不与现有节点冲突的排序值。

## 常见故障

| 现象 | 根因 |
|------|------|
| 节点不出现在右键菜单 | `[CustomNode]` 拼写错误；或类不在 Editor 目录；或没有重新编译 |
| 节点没有效果 | `SetLevel` 太低被其他节点覆盖；或没走 `Prepare` |
| `BuildingCtx` 为 `null` | `[初始化框架Assets环境]` 节点缺失或顺序不对 |
| 分组为空 | 上游 `Group by path` 节点未配置 |
| 改了 `SetABPack` 但 AB 没变 | 需要走 `[Build]打包AssetBundle` 节点才生效；预览节点只显示结果 |

## 相关页面

- [AssetBundle 打包](build-assetbundle.md) —— 内置节点与颗粒度语义
- [目录结构与约定](../guide/project-structure.md) —— `Runtime` 目录
