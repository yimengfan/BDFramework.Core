# 启动时序详细参考

> 来源：`Runtime.AOT/BDLauncher.cs`、`Runtime.AOT/ScriptLoderAOT.cs`、`Runtime/BDLauncherHotfix.cs`、`Runtime/HotfixScript/ScriptLoder.cs`、`Runtime/GameConfig/GameConfigStartupPureLogic.cs`

## 阶段 1 — AOT

### `BDLauncher`

```csharp
// Runtime.AOT/BDLauncher.cs
[DefaultExecutionOrder(int.MinValue)]
public class BDLauncher : MonoBehaviour
{
    public static BDLauncher Inst { get; private set; }
    public TextAsset ConfigText;              // 框架配置 JSON
    public string ClientVersion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void PreLoadHotfixAssembliesAfterAssembliesLoadedFromLauncher();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void PreLoadHotfixAssembliesBeforeSceneLoadFromLauncher();

    void Awake();
    void InitHotfixScriptLoder();
}
```

`Awake()` 步骤：

```text
1. Inst = this
   校验 ConfigText != null → 否则 Debug.LogError("GameConfig配置为null,请检查!")

2. if (Application.isPlaying) DontDestroyOnLoad(this)

3. if (!ScriptLoderAOT.HasLoadedHotfixAssembliesBeforeSceneLoad)
       ScriptLoderAOT.Load(ClientVersion)

4. InitHotfixScriptLoder()
       遍历 AppDomain.CurrentDomain.GetAssemblies()
       找 FullName == "BDFramework.ScriptLoder" 的类型
       method.Invoke(null, null)

5. if (!Application.isEditor)
       反射设置 BDFramework.Core.Tools.BApplication.IsPlaying = true
```

!!! danger "为什么用 `AppDomain.GetAssemblies()` 而不是 `Type.GetType()`"
    `BDFramework.AOT` 不能引用 `BDFramework.Core`（循环依赖）。且 **`System.Type.GetType("BDFramework.ScriptLoder, BDFramework.Core")` 在 IL2CPP 下返回 `null`**。源码注释明确记录了这个坑。

### `ScriptLoderAOT`

```csharp
[Preserve] static public class ScriptLoderAOT
{
    static public bool HasLoadedHotfixAssembliesBeforeSceneLoad { get; }

    static readonly public string HYCLR_AOT_PATCH_PATH = "script/aot_patch";
    static readonly public string HOTFIX_DLL_PATH      = "script/hotfix";
    static readonly public string HOT_DLL_EXTENSION    = ".zlua.bytes";

    static public void Load(string clientVersion);                  // Editor：仅打日志；否则 LoadHotfixDLL
    static public void LoadHotfixDLL(string clientVersion);
    [Preserve] static internal void TryPreLoadHotfixAssembliesAtRuntime(string stageName);
}
```

### `LoadHotfixDLL` 四阶段

```text
① 计算双寻址根
     firstLoadDir  = persistentDataPath/<clientVersion>/<platform>
     secondLoadDir = Android ? "<platform>"（BetterStreamingAssets）: streamingAssetsPath/<platform>
     clientVersion 来自 StreamingAssets/<platform>/package_build.info 的 Version（LitJson 反序列化）
       失败/为空时返回 ""（表示用 persistentDataPath//<platform>，即"母包内置 DLL"语义）

② AOT patch —— ★ 始终从母包 StreamingAssets 读，不跟随版本目录
     StreamingAssets/<platform>/script/aot_patch/*.zlua.bytes
     逐个 RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)

③ 热更 DLL —— 优先 FIRST_LOAD_DIR
     firstLoadDir/script/hotfix/*.zlua.bytes
     命中则 File.ReadAllBytes + Assembly.Load

④ 回退母包
     Android: BetterStreamingAssets.ReadAllBytes
     其他:    File.ReadAllBytes
     ★ 一个都没有 → throw new Exception("【AOT.Load】HyCLR热更DLL不存在! 路径:" + hotfixdllRootPath)
```

**装载顺序**（`GetHotfixDllLoadRank`）：

| 排名 | 匹配 |
|------|------|
| 0 | `bdframework.core*` |
| 1 | `assembly-csharp-firstpass*` |
| 2 | `assembly-csharp*` |
| 10 | 其他（同级按文件名 `OrdinalIgnoreCase`） |

单个文件 `FileNotFoundException` / `DirectoryNotFoundException` 降级为 `Debug.LogWarning` 并继续。

`ShouldSkipAlreadyLoadedHotfixAssembly` 跳过已由 Player 预加载（`preserveHotUpdateAssemblies`）的同名程序集，避免 `AppDomain` 出现同名类型副本。

## 阶段 2 — 桥接

```csharp
// Runtime/BDLauncherHotfix.cs
public class BDLauncherHotfix : MonoBehaviour
{
    public void Launch(string gameId = "default");           // 转发别名
    void OnApplicationQuit();                                 // 仅 Editor: SqliteLoder.Close() + ScriptLoder.Dispose()
}

public class BDLauncherBridge
{
    public void Launch(string gameId = "default");
    public void OnApplicationQuit();
}
```

`Launch` 的 9 步见 [SKILL.md 第 3 节](../SKILL.md#3-三段启动)。

## 阶段 3 — `ScriptLoder`

```csharp
// Runtime/HotfixScript/ScriptLoder.cs
static public class ScriptLoder
{
    static readonly public string HYCLR_AOT_PATCH_PATH = "script/aot_patch";
    static readonly public string HOTFIX_DLL_PATH      = "script/hotfix";
    static readonly public string HOT_DLL_EXTENSION    = ".zlua.bytes";

    [Preserve] public static void Init();
    static public void Start();
    public static bool IsRunning { get; private set; } = false;
    public static void Dispose();                            // 仅 IsRunning = false
    static public IEnumerable<Type> GetAppDomainHostingTypes();
    static public object CreateHotfixInstance(Type value_type);   // Activator.CreateInstance
}
```

`Init()` 的 5 步见 [SKILL.md 第 3 节](../SKILL.md#3-三段启动)。

!!! note "ILRuntime 残留"
    本分支 ILRuntime **已完全不参与运行**。仅剩：
    - `CreateHotfixInstance` 的 ILRuntime 分支被注释
    - `Dispose()` 里的 `AppDomain?.Dispose()` 被注释
    - `TableQueryForILRuntime` 是旧 API 名兼容壳
    - `Editor/.../BuildHotfixScriptEditor/ILRuntime/` 是 `ENABLE_ILRUNTIME` 宏门控的编辑器工具

## 类型收集

```csharp
static public IEnumerable<Type> GetAppDomainHostingTypes()
```

收集规则（`assembly.FullName` 前缀）：

```text
"BDFramework"  |  "Assembly-CSharp,"  |  "Assembly-CSharp-firstpass,"
"UnityEngine.UI"  |  "Game."  |  含 "@main"
```

只取 `t.IsClass && !t.IsNested`；结果**首次扫描后缓存**在 `hostingTypeList`；Editor 下按 `FullName` 排序并逐条打印 `框架托管DLL:...`。

## 无 MonoBehaviour 路径

### `GameConfigStartupPureLogic`（`internal static`）

```csharp
internal static class GameConfigStartupPureLogic
{
    internal enum FrameworkConfigTextSourceKind
    { None, RuntimeLauncherTextAsset, SceneLauncherTextAsset, EditorDefaultFile }

    internal sealed class FrameworkConfigTextSourcePlan
    {
        FrameworkConfigTextSourceKind SourceKind;
        string SourceIdentifier;
        bool   ShouldLogSource;
    }

    internal static bool ShouldLoadFrameworkConfigManager(bool hasGameConfigManagerInstance);

    internal static FrameworkConfigTextSourcePlan ResolveFrameworkConfigTextSource(
        bool isPlaying, bool hasRuntimeLauncherConfigText, string runtimeLauncherConfigName,
        bool hasSceneLauncherConfigText, string sceneLauncherConfigName,
        bool isEditor, bool defaultEditorConfigExists, string defaultEditorConfigPath);

    internal static string FormatFrameworkConfigSourceLogMessage(string sourceIdentifier);
}
```

`LoadFrameworkConfig()`：

```csharp
if (!GameConfigStartupPureLogic.ShouldLoadFrameworkConfigManager(GameConfigManager.Inst != null))
    return;                              // 直接返回，不查场景、不读文件
GameConfigManager.Inst.Start();
```

### `TalosE2EBatchBridge.PrepareEditorOnlyRuntime`

```text
GameConfigLoder.LoadFrameworkConfig()
ClientAssetsUtils.GetMultiAssetsLoadPath(...)
CheckBaseClientAssets(...)
BResources.Init(config.ArtRoot, first, second)
SqliteLoder.Init(config.SQLRoot, first, second)
```

`AssignEditorOnlyLauncherInstance` 会**反射回写** `BDLauncher.Inst` 的私有 setter，让运行时逻辑认为 launcher 已就绪。

### `TalosE2EBootstrap.LaunchE2EStatic()`

支持在没有 `MonoBehaviour` 的情况下启动 E2E（TCP 模式）。

`IEnumeratorTool` **只在需要协程时才必需**（AB 异步加载等）；纯逻辑 E2E 可以不挂。

## 管理器启动顺序

`ManagerInstHelper.LoadManager` 的排序依据是 `[ManagerOrder(Order = n)]`（缺省 `0`，越小越先）。

| 管理器 | Order | 说明 |
|--------|-------|------|
| `GameConfigManager` | 0 | 配置中心 |
| `UIManager` | 0 | UI |
| `ComponentBindAdaptorManager` | 0 | 绑定适配器 |
| `ScreenViewManager` | **99999** | 全仓库唯一的 `ManagerOrder`，保证导航最后启动 |

业务管理器（如 `DemoEventManager`）无需注册——`"Game."` / `"Assembly-CSharp,"` 前缀会收集。

!!! warning "`base.Start()` 不能省"
    `ManagerBase.Start()` 会置 `IsStarted = true`。覆写时忘调 → `ManagerInstHelper.Start()` 会**重复调用**。

## 启动失败的排查顺序

| 症状 | 检查点 |
|------|--------|
| `GameConfig配置为null,请检查!` | 场景 `BDLauncher.ConfigText` |
| `【AOT.Load】HyCLR热更DLL不存在!` | `StreamingAssets/<platform>/script/hotfix/` |
| `[GameconfigManger]启动失败，class data 数量为0.` | 业务程序集是否被收集（前缀白名单） |
| 管理器 `Init()` 没被调用 | 属性是否派生自 `ManagerAttribute` |
| AB 异步加载无回调 | `IEnumeratorTool` 是否已挂载 |
| Editor 里一切正常，真机黑屏 | `AssetLoadPathType` 是否误配为 `Editor` |
| BatchMode 下什么都没发生 | `BDFrameworkEditorEnvironment.InitEditorEnvironment()` 是否被调用 |

## 相关页面

- [启动链路](https://yimengfan.github.io/BDFramework.Core/architecture/bootstrap.md)
- [程序集与依赖](https://yimengfan.github.io/BDFramework.Core/architecture/assemblies.md)
- [热更代码 HybridCLR](https://yimengfan.github.io/BDFramework.Core/pipeline/build-hotfix-dll.md)
