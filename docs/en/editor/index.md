# Editor

Capability reference for the `BDFramework.Editor` assembly (`Packages/com.popo.bdframework/Editor/`).

| Page | Content |
|------|---------|
| [Editor Core Classes](core-classes.md) | `BDFrameworkEditorEnvironment`, `BDEditorApplication`, `EditorTask` |
| [Editor Http Server](http-server.md) | Embedded `HttpListener`, `WP_EditorInvoke`, `WP_LocalABFileServer` |
| [Pipeline Hooks](publish-hooks.md) | The complete virtual method table of `ABDFrameworkPublishPipelineBehaviour` |
| [Menus & Tools Index](menus.md) | All `[MenuItem]` paths, grouped |

## Environment initialisation

When the Editor loads (`[InitializeOnLoadMethod]`) this runs automatically:

```text
BDEditorApplication.Init()
ScriptLoder.GetAppDomainHostingTypes()
BResources.Init(AssetLoadPathType.Editor)      ← 走 DevResourceMgr
ManagerInstHelper.LoadManager(Types)
GameConfigLoder.LoadFrameworkConfig()
BDFrameworkPipelineHelper.Init()
HotfixPipelineTools.Init()
InitEditorTask()
OnUnityLoadOrCodeRecompiled()
InitEditorHttpServer()
```

!!! tip "This is exactly why runtime APIs work directly inside the Editor"
    Once the environment is initialised, `BResources.Load` / `SqliteHelper.DB` / `UIManager` are all usable directly in the Editor (through the Editor backend) without entering PlayMode.

## Editor-only capabilities

| Capability | Location | Notes |
|------------|----------|-------|
| `DevResourceMgr` | `Runtime/AssetsManager/ArtAsset/DevAssets/` | The entire type is wrapped in `#if UNITY_EDITOR`; uses `AssetDatabase` instead of AB |
| `SqliteLoder.LoadLocalDBOnEditor` / `LoadServerDBOnEditor` | Runtime | Manually mount a database |
| `ClientAssetsUtils.GenBasePackageBuildInfo` / `SaveBasePackageBuildInfo` | Runtime | Writes `package_build.info` |
| `ConfigEditorUtil` | Runtime (contents under `#if UNITY_EDITOR`) | Config read/write |
| `EditorHttpListener` | Editor | Embedded Http server |
| `AssetBundleBenchmarkToolsV2` | Editor | AB load verification |
| `GameViewEditorEX.SetGameviewSize` | Editor | Resizes the GameView automatically from the `UIRoot`'s `CanvasScaler` |

!!! note "Path rewriting inside the Editor"
    `BApplication.streamingAssetsPath` → `DevOps/PublishAssets`; `persistentDataPath` → `<ProjectRoot>/.AppData`.

    See [Asset Load Paths](../guide/asset-load-path.md#path-rewrite-matrix).

## Editor task system

`Editor/EditorTask/` provides three kinds of hook:

| Attribute | When it fires |
|-----------|---------------|
| `UnityLoadOrCodeRecompiled` | After Unity finishes loading / after code recompilation |
| `WillEnterPlaymode` | Before entering PlayMode |
| `EveryDay` | Once per day |

Business example: `DevOpsEditorTasks.UpdateGitHookToLocalStore` (syncs `DevOps/CI/githook` into `.git/hooks`).

!!! warning "The directory this task references does not exist in this repository"
    `DevOps/CI/githook/` was not found and is expected to be provided by the consumer. See [Refactor Backlog](../architecture/refactor-backlog.md#ref-21-missing-githook-dir).

## Version control integration

### `GitProcessor`

```csharp
namespace Game.Editor.PublishPipeline

public class GitProcessor
{
    static public string GetBranchName();      // macOS 直接返回；Windows 找 dev_ 前缀行
    static public string GetVersion(int lenth); // git log -1 --pretty=format:%H，lenth < 5 强制 5
    static public void   GetVersion();
}
```

`GetVersion(6)` is used to write `BasePckScriptSVCVersion`.

### `SVNProcessor`

```csharp
namespace BDFramework.Editor.SVN

public class SVNProcessor
{
    public static SVNProcessor Create(string svnurl, string localpath, string user = null, string psw = null, bool islog = true);
    public static SVNProcessor OpenStore(string localpath, bool islog = true);
    public static bool IsExsitSvnStore(string path = "./");

    public void CheckOut(string checkOutTo = "./");
    public void Update(string path);
    public void RevertForce(string path);
    public void CleanUp(SvnOption.CleanUp option);
    public void Add(params string[] paths);
    public void ForceAdd(params string[] paths);
    public void AddFloder(string direct, bool isIncludeAllFile);
    public void Delete(params string[] paths);
    public void ForceDelete(params string[] paths);
    public Status GetStatus(Status status, string workpath);
    public Status GetStatus(string workpath);
    public string GetFileNameByStatus(Status status, string prefixPath);
    public void Switch(string newUrl, string localPath);
    public void Relocate(string newUrl, string oldUrl);
    public string GetInfo(string workpath, string svnurl);
    public string GetRevision();
    public string GetLastChangeRev();
    public string GetServerVersion();
    public string GetRelativeUrl();
    public string GetAllBranchesInfo(string svnRepoUrl);
    public string GetLog(int len);
    public void CommitFolder(string floder, string log);
    public void Commit(string workpath, string log);
    public void SetSVNExe(string svnExe);

    // 常量
    SvnOption.CleanUp_RemoveUnversioned
    SvnOption.CleanUp_RemoveIgnored
}
```

`GreenSVN~/` (the `~` suffix) is not imported by Unity.

## E2E Bridge

```csharp
namespace BDFramework.Editor.Environment

public static class TalosE2EBatchBridge
{
    public static void LaunchTalosE2EBatchMode();
    public static void LaunchTalosE2EEditorOnly();
    public static void RunTalosE2EAndExport();
}
```

The `LaunchTalosE2EBatchMode` flow: open `Assets/Scenes/BDFrame.unity` → `E2EEditorTools.LaunchE2EBatchMode()`.

Key constants: `-talosForceE2E`, `TalosE2E.EditorOnlyBDLauncher`; scene candidates `Assets/Scenes/BDFrame.unity` → `Assets/Scenes/BDFrame_Debug.unity`.

The `PrepareEditorOnlyRuntime` init chain (**does not depend on the `BDLauncher` MonoBehaviour**):

```text
GameConfigLoder.LoadFrameworkConfig()
ClientAssetsUtils.GetMultiAssetsLoadPath(...)
CheckBaseClientAssets(...)
BResources.Init(config.ArtRoot, first, second)
SqliteLoder.Init(config.SQLRoot, first, second)
```

→ See [E2E (Talos)](../testing/e2e.md) for details.
