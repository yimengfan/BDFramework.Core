# Custom AssetGraph Nodes

Write a custom node when the 20 built-in nodes cannot satisfy your packing needs.

## Node base class and attribute

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

| Item | Description |
|----|------|
| Base class | `UnityEngine.AssetGraph.Node` |
| Framework abstract base class | `BDFramework.Editor.AssetGraph.Node.SetGranularityBase` (provides `IsIncludeDependAssets`, `SetLevel`) |
| Attribute | `[CustomNode("<category>/[分组]<名字>", <order>)]` |
| Core callbacks | `Prepare(...)`, `OnInspectorGUI(...)`, `Initialize(NodeData)`, `Clone(NodeData)` |

!!! danger "Node classes must live in an `Editor` directory"
    AssetGraph is an Editor-only capability. Putting a node script in a Runtime directory makes the build fail.

    Business-side examples live in: `Assets/AssetGraph/NodeEx/Editor/`.

## `Prepare` vs `Build`

| Stage | Responsibility | Can it change the build result |
|------|------|------------------|
| `Prepare` | Walk the incoming groups and call `SetABPack` to declare AB ownership | ✓ modifies `BuildAssetInfos` |
| `Build` | The actual AB production | Handled by the `[Build]打包AssetBundle` node |

**A custom granularity node only works inside `Prepare`**, declaring its rules through `SetABPack`; its output is used for preview only.

## Key APIs

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

!!! warning "The `SetABPack` signature has changed"
    The `SetABName` from older docs **does not exist**. The current API is:

    ```csharp
    public (bool, string) SetABPack(string assetName, string newABName,
                                    SetABPackLevel setLevel, string owerLog, bool isSetAllDependAsset);
    ```

    It adds the `SetABPackLevel` (priority) and `isSetAllDependAsset` (whether the rule also applies to dependencies) parameters, and returns `(success, message)`.

## Granularity levels

```csharp
BuildAssetInfos.SetABPackLevel
    None = 0  →  Simple  →  Force  →  FrameworkDefault  →  Lock
```

**A higher level overrides a lower one.** A custom node should pick an appropriate `SetLevel`:

| Level | Applies to |
|-------|------|
| `Simple` | Advisory rules that other nodes may override |
| `Force` | Strong rules, but still overridable by a higher priority |
| `FrameworkDefault` | Framework default rules (used by the built-in nodes) |
| `Lock` | Highest priority; no node can change it any further |

## Complete example: pack by name prefix

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

!!! note "`SetGranularityBase` provides `SetLevel` and `IsIncludeDependAssets`"
    Both fields are serialized by the base class and drawn in the Inspector, so a custom node can read them directly and does not need to declare its own.

## Real business-side examples

`Assets/AssetGraph/NodeEx/Editor/`:

| File | Purpose |
|------|------|
| `SetGranularity_Battle.cs` | Granularity rules for the battle module |
| `SetGranularity_Map.cs` | Granularity rules for the map module |

These two files are **reference implementations of business-side custom nodes**.

## Preview and debugging

| Node | Purpose |
|------|------|
| `[辅助]预览` (`SetGranularityNull`) | Outputs the asset list of the current group without applying any rule |
| `[辅助]模块预览` (`SetGranularityMoudule`) | Previews along the module dimension |
| `[Build]预览AB` (`BuildAssetBundlePreView`) | Previews the AB partition (without actually packing) |

Recommended debugging flow:

```text
Group by path → [辅助]预览 → 查看资产清单
             → 加颗粒度节点 → [Build]预览AB → 对比 AB 划分
             → [Build]打包AssetBundle → 实际打包
```

## Node order reference

| Order band | Purpose |
|--------|------|
| 1 | Environment/path loading (must come first) |
| 10 | Grouping |
| 30–36 | Granularity rules |
| 60 | Asset collection (shader variants / atlases / video) |
| 70 | Settings |
| 100–102 | Packing |
| 110–111 | Split packages (**must come after packing**) |

When adding a node, pick an order value that does not clash with existing nodes.

## Common failures

| Symptom | Root cause |
|------|------|
| The node does not appear in the right-click menu | A typo in `[CustomNode]`; or the class is not in an Editor directory; or the code was never recompiled |
| The node has no effect | `SetLevel` is too low and another node overrides it; or it never went through `Prepare` |
| `BuildingCtx` is `null` | The `[初始化框架Assets环境]` node is missing or in the wrong order |
| The group is empty | The upstream `Group by path` node is not configured |
| `SetABPack` was changed but the ABs did not | It only takes effect through the `[Build]打包AssetBundle` node; preview nodes merely display the result |

## Related pages

- [AssetBundle Building](build-assetbundle.md) —— built-in nodes and granularity semantics
- [Project Layout & Conventions](../guide/project-structure.md) —— the `Runtime` directory
