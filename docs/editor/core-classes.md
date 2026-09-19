# Editor 核心类

## `BDFrameworkEditorEnvironment`

```csharp
namespace BDFramework.Editor.Environment

public static class BDFrameworkEditorEnvironment
{
    static public bool               IsInited { get; }
    static public EditorTask         EditorTaskInstance { get; }
    static public EditorHttpListener EditorHttpListener { get; }
    static public Type[]             Types { get; }

    static public void InitEditorEnvironment();
    static public void EditorUpdate();
    static public void EditorUpdate_CheckGuideWindow();
}
```

由 `[InitializeOnLoadMethod] BDFrameworkEditorEnvironmentInit()` 触发，也可在 BatchMode 下**手动调用**（`PublishPipeLineCI` 的静态构造就是这样做）。

**初始化链**：

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

!!! tip "BatchMode 下必须手动初始化"
    ```csharp
    static PublishPipeLineCI()
    {
        if (Application.isBatchMode) BDFrameworkEditorEnvironment.InitEditorEnvironment();
    }
    ```
    否则 `ManagerInstHelper`、`BResources` 等都未就绪。

## `BDEditorApplication`

```csharp
public static class BDEditorApplication
{
    static public BDFrameworkEditorSetting EditorSetting { get; }
    static public BDFrameworkEditorStatus  EditorStatus  { get; }

    static public void Init();
    static public void SwitchToBuildTarget(BuildTarget target);
    static public void SwitchToAndroid();
    static public void SwitchToiOS();
    static public void SwitchToWindows();
    static public void SwitchToMacOSX();
    static public bool IsPlatformModuleInstalled(BuildTargetGroup group, BuildTarget target);
}
```

`SwitchToBuildTarget` 在 `HyCLREditorTools.PreBuild` 中用于保证平台一致。

## `BDFrameworkEditorSetting`

配置容器，持久化在 `DevOps/Config/BDFrameworkSetting.conf`：

```csharp
public class BDFrameworkEditorSetting
{
    public DevOpsSetting              DevOpsSetting;
    public BuildHotfixDLLSetting      BuildHotfixDLLSetting;
    public BuildSqlSetting            BuildSqlSetting;
    public BuildAssetBundleSetting    BuildAssetBundleSetting;
    public ABFileEditorServerSetting  ABFileEditorServerSetting;
    public BuildClientPackage         BuildClientPackage;
    public Android                    Android;              // AndroidDebug
    public WindowsPlayer              WindowsPlayer;        // WindowsPlayerDebug
    public MacOSX                     MacOSX;               // MacOSXDebug

    public bool IsSetConfig();
    public void Load();
    public void Save();
}
```

对应菜单：`BDFrameWork工具箱/框架设置`。

## `EditorTask`

```csharp
public class EditorTask
```

三类钩子特性（`Editor/EditorTask/`）：

| 特性 | 触发时机 | 典型用途 |
|------|---------|---------|
| `UnityLoadOrCodeRecompiled` | Unity 加载完成 / 代码重编译后 | 环境准备、代码生成 |
| `WillEnterPlaymode` | 进入 PlayMode 前 | 清缓存、重置状态 |
| `EveryDay` | 每天一次 | 拉取更新、日报 |

`BDFrameworkEditorEnvironment.EditorUpdate()` 驱动这些任务。

## `BDFrameworkAssetImporter`

```csharp
public class BDFrameworkAssetImporter : AssetPostprocessor
```

缓存位置：`BApplication.BDEditorCachePath + "/ImporterCache"`（即 `Library/BDFrameCache/ImporterCache`）。

## `EditorHttpListener`

```csharp
// Editor/EditorWindows/EditorHttp/…
```

→ 详见 [Editor Http 服务](http-server.md)。

## `GameViewEditorEX`

```csharp
// Editor/Extension/Unity3d/GameViewEditorEX.cs
public static class GameViewEditorEX
{
    public static void SetGameviewSize(Vector2 size);
}
```

`UIManagerEditor`（`Editor/UI/UIManager.Editor.cs`）在 `[InitializeOnLoadMethod]` 时调 `EnsureGameViewMatchesCanvas()`：用 `GameObject.Find("UIRoot")` 的 `CanvasScaler.referenceResolution` 调整 GameView 尺寸。

!!! note "`UIManagerEditor` 不是 `UIManager` 的 partial"
    尽管文件名是 `UIManager.Editor.cs`，它定义的是**独立类 `UIManagerEditor`**。见[重构清单](../architecture/refactor-backlog.md#ref-7-filename-type-mismatch)。

## UI 工作流菜单

```csharp
// Editor/UI/Workflow/MenuItems.cs
[MenuItem("GameObject/UI工作流/1.创建UIPrefab", priority = 1)]
// 按 GUID d25ba607a4a7bc740be5c7838d063260 实例化并 UnpackPrefabInstance 到选中节点

[MenuItem("GameObject/UI工作流/2.创建SubWindow节点", priority = 2)]
// ★ 空实现
```

`OverrideUIComponent` 覆写 Unity 原生菜单，让新建的 `Text` / `Image` / `Raw Image` 默认 `raycastTarget = false`。

## `BDEditorGlobalMenuItemOrderEnum`

```csharp
public enum BDEditorGlobalMenuItemOrderEnum
{
    BDFrameworkGuid = 0,              BDFrameworkSetting = 1,
    BuildPipeline = 50,
    BuildPackage_DLL = 52,            BuildPackage_Assetbundle = 53,
    BuildPackage_Table_Table2Class = 54,
    BuildPackage_Table_GenSqlite = 55,
    BuildPackage_Table_Json2Sqlite = 56,
    BuildPipeline_NetProtocol_Proto2Class = 57,
    BuildPipeline_BuildPackage = 58,
    PublishPipeline = 100,            PublishPipeline_BuildAsset = 101,
    PublishPipeline_PublishPackage = 102,
    HotfixPipeline = 111,             DevOps = 121,
    TestPepeline = 201,               TestPepelineEditor = 202
}
```

## `BDFrameworkPipelineHelper`

```csharp
public static class BDFrameworkPipelineHelper
{
    public static void Init();
    // 事件分发 + SVC 版本号查询
    public static string GetTableSVCNum(RuntimePlatform platform, string outputPath);
    // GetArtSVCNum / GetScriptSVCNum 等同族
}
```

SVC（Source Version Control）版本号用于记录"本次构建对应的源码版本"，写入 `package_build.info`。

## 相关页面

- [Editor Http 服务](http-server.md)
- [管线回调钩子](publish-hooks.md)
- [菜单与工具索引](menus.md)
- [Editor 模块地图](../architecture/editor-modules.md)
