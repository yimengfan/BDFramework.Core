# Editor

`BDFramework.Editor` 程序集（`Packages/com.popo.bdframework/Editor/`）的能力参考。

| 页面 | 内容 |
|------|------|
| [Editor 核心类](core-classes.md) | `BDFrameworkEditorEnvironment`、`BDEditorApplication`、`EditorTask` |
| [Editor Http 服务](http-server.md) | 内嵌 `HttpListener`、`WP_EditorInvoke`、`WP_LocalABFileServer` |
| [管线回调钩子](publish-hooks.md) | `ABDFrameworkPublishPipelineBehaviour` 的完整虚方法表 |
| [菜单与工具索引](menus.md) | 全部 `[MenuItem]` 路径分组 |

## 环境初始化

Editor 加载时（`[InitializeOnLoadMethod]`）自动执行：

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

!!! tip "这就是编辑器里能直接用运行时 API 的原因"
    环境初始化后，`BResources.Load` / `SqliteHelper.DB` / `UIManager` 在 Editor 中都能直接使用（走 Editor 后端），不需要进入 PlayMode。

## Editor 专属能力

| 能力 | 位置 | 说明 |
|------|------|------|
| `DevResourceMgr` | `Runtime/AssetsManager/ArtAsset/DevAssets/` | 整个类型被 `#if UNITY_EDITOR` 包裹，用 `AssetDatabase` 替代 AB |
| `SqliteLoder.LoadLocalDBOnEditor` / `LoadServerDBOnEditor` | Runtime | 手动挂载 db |
| `ClientAssetsUtils.GenBasePackageBuildInfo` / `SaveBasePackageBuildInfo` | Runtime | 写 `package_build.info` |
| `ConfigEditorUtil` | Runtime（内容 `#if UNITY_EDITOR`） | 配置读写 |
| `EditorHttpListener` | Editor | 内嵌 Http 服务 |
| `AssetBundleBenchmarkToolsV2` | Editor | AB 加载验证 |
| `GameViewEditorEX.SetGameviewSize` | Editor | 按 `UIRoot` 的 `CanvasScaler` 自动调 GameView 尺寸 |

!!! note "Editor 下的路径重写"
    `BApplication.streamingAssetsPath` → `DevOps/PublishAssets`；`persistentDataPath` → `<ProjectRoot>/.AppData`。

    见[资源加载寻址](../guide/asset-load-path.md#path-rewrite-matrix)。

## 编辑器任务系统

`Editor/EditorTask/` 提供三类钩子：

| 特性 | 触发时机 |
|------|---------|
| `UnityLoadOrCodeRecompiled` | Unity 加载完成 / 代码重编译后 |
| `WillEnterPlaymode` | 进入 PlayMode 前 |
| `EveryDay` | 每天一次 |

业务示例：`DevOpsEditorTasks.UpdateGitHookToLocalStore`（把 `DevOps/CI/githook` 同步到 `.git/hooks`）。

!!! warning "该任务引用的目录在本仓库不存在"
    `DevOps/CI/githook/` 未找到，应由使用方提供。见[重构清单](../architecture/refactor-backlog.md#ref-21-missing-githook-dir)。

## 版本控制集成

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

`GetVersion(6)` 用于写入 `BasePckScriptSVCVersion`。

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

`GreenSVN~/`（`~` 后缀）不被 Unity 导入。

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

`LaunchTalosE2EBatchMode` 流程：打开 `Assets/Scenes/BDFrame.unity` → `E2EEditorTools.LaunchE2EBatchMode()`。

关键常量：`-talosForceE2E`、`TalosE2E.EditorOnlyBDLauncher`；场景候选 `Assets/Scenes/BDFrame.unity` → `Assets/Scenes/BDFrame_Debug.unity`。

`PrepareEditorOnlyRuntime` 初始化链（**不依赖 `BDLauncher` MonoBehaviour**）：

```text
GameConfigLoder.LoadFrameworkConfig()
ClientAssetsUtils.GetMultiAssetsLoadPath(...)
CheckBaseClientAssets(...)
BResources.Init(config.ArtRoot, first, second)
SqliteLoder.Init(config.SQLRoot, first, second)
```

→ 详见 [E2E（Talos）](../testing/e2e.md)。
