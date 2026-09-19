# Editor Core Classes

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

Triggered by `[InitializeOnLoadMethod] BDFrameworkEditorEnvironmentInit()`, and can also be **invoked manually** in BatchMode (that is exactly what `PublishPipeLineCI`'s static constructor does).

**Init chain**:

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

!!! tip "You must initialise manually in BatchMode"
    ```csharp
    static PublishPipeLineCI()
    {
        if (Application.isBatchMode) BDFrameworkEditorEnvironment.InitEditorEnvironment();
    }
    ```
    Otherwise `ManagerInstHelper`, `BResources` and the rest are not ready.

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

`SwitchToBuildTarget` is used by `HyCLREditorTools.PreBuild` to guarantee platform consistency.

## `BDFrameworkEditorSetting`

Settings container, persisted at `DevOps/Config/BDFrameworkSetting.conf`:

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

Corresponding menu: `BDFrameWork工具箱/框架设置`.

## `EditorTask`

```csharp
public class EditorTask
```

Three kinds of hook attribute (`Editor/EditorTask/`):

| Attribute | When it fires | Typical use |
|-----------|---------------|-------------|
| `UnityLoadOrCodeRecompiled` | After Unity finishes loading / after code recompilation | Environment preparation, code generation |
| `WillEnterPlaymode` | Before entering PlayMode | Clear caches, reset state |
| `EveryDay` | Once per day | Pull updates, daily report |

`BDFrameworkEditorEnvironment.EditorUpdate()` drives these tasks.

## `BDFrameworkAssetImporter`

```csharp
public class BDFrameworkAssetImporter : AssetPostprocessor
```

Cache location: `BApplication.BDEditorCachePath + "/ImporterCache"` (that is, `Library/BDFrameCache/ImporterCache`).

## `EditorHttpListener`

```csharp
// Editor/EditorWindows/EditorHttp/…
```

→ See [Editor Http Server](http-server.md) for details.

## `GameViewEditorEX`

```csharp
// Editor/Extension/Unity3d/GameViewEditorEX.cs
public static class GameViewEditorEX
{
    public static void SetGameviewSize(Vector2 size);
}
```

`UIManagerEditor` (`Editor/UI/UIManager.Editor.cs`) calls `EnsureGameViewMatchesCanvas()` on `[InitializeOnLoadMethod]`: it adjusts the GameView size using the `CanvasScaler.referenceResolution` of `GameObject.Find("UIRoot")`.

!!! note "`UIManagerEditor` is not a partial of `UIManager`"
    Even though the file is named `UIManager.Editor.cs`, it defines a **standalone class `UIManagerEditor`**. See [Refactor Backlog](../architecture/refactor-backlog.md#ref-7-filename-type-mismatch).

## UI workflow menus

```csharp
// Editor/UI/Workflow/MenuItems.cs
[MenuItem("GameObject/UI工作流/1.创建UIPrefab", priority = 1)]
// 按 GUID d25ba607a4a7bc740be5c7838d063260 实例化并 UnpackPrefabInstance 到选中节点

[MenuItem("GameObject/UI工作流/2.创建SubWindow节点", priority = 2)]
// ★ 空实现
```

`OverrideUIComponent` overrides Unity's built-in menus so that newly created `Text` / `Image` / `Raw Image` default to `raycastTarget = false`.

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

The SVC (Source Version Control) version number records "the source version this build corresponds to" and is written into `package_build.info`.

## Related pages

- [Editor Http Server](http-server.md)
- [Pipeline Hooks](publish-hooks.md)
- [Menus & Tools Index](menus.md)
- [Editor Module Map](../architecture/editor-modules.md)
