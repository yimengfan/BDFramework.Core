# 服务容器与日志

## 服务容器

### `ServiceContainer`

```csharp
namespace BDFramework.GameServiceStore

public class ServiceContainer
{
    public void AddSingleton<T>() where T : class;              // Activator.CreateInstance(typeof(T))
    public void AddSingleton(object inst);                      // 同类型已存在 → LogError("已存在同类型的Singleton")
    public void AddTransient<T>(T obj) where T : class;         // ★ 只登记类型，不保存实例
    public T GetService<T>() where T : class;
    private object GetService(Type type);
}
```

**解析顺序**：

```text
GetService(type)
 ① singletonList.FindLast(o => o.GetType() == type)          ← 单例优先
 ② transientList.FindLast(t => t == type) → Activator.CreateInstance(t)
 ③ 都未命中 → 返回 null（不抛异常）
```

!!! danger "`AddTransient<T>(T obj)` 会丢弃传入的实例"
    ```csharp
    public void AddTransient<T>(T obj) where T : class
    {
        var type = obj.GetType();      // 只用类型
        this.transientList.Add(type);  // obj 本身被丢弃
    }
    ```
    每次 `GetService<T>()` 都 `Activator.CreateInstance`（要求 `T` 有无参构造）。**要注入有状态的实例请用 `AddSingleton(inst)`。**

!!! warning "类型比较是精确相等"
    `o.GetType() == type` —— **不做接口/基类匹配**。注册 `PlayerModel` 后 `GetService<IPlayerModel>()` 返回 `null`。

### `GameServiceStore`

```csharp
static public class GameServiceStore
{
    static public ServiceContainer GetService<T>() where T : new();   // key = typeof(T).FullName ?? typeof(T).Name
    static public ServiceContainer GetService(string moduleName);     // 不存在则 new 并缓存
}
```

**按模块名隔离**：不同模块拿到各自的容器。

```csharp
var battle = GameServiceStore.GetService("Battle");
battle.AddSingleton<BattleContext>();
var ctx = battle.GetService<BattleContext>();

var lobby = GameServiceStore.GetService("Lobby");
lobby.AddSingleton<LobbyContext>();      // 与 Battle 容器互不干扰
```

!!! note "`GameServiceStore` 与 UFlux 的 DI 互不相通"
    `UIManager.SetWindowDI` 只查 `UIManager` 自己的 `singletonList` / `transientList`。见[依赖注入](../ui/dependency-injection.md)。

`IWindow.ServiceContainer` 是**每个窗口独立**的容器，也不同于上面两者：

| 容器 | 作用域 | 注册入口 |
|------|--------|---------|
| `UIManager` 的 singleton/transient | 全局 | `UIManager.Inst.AddSingleton/AddTransient` |
| `IWindow.ServiceContainer` | 单窗口 | 窗口内部自行注册 |
| `GameServiceStore` | 按模块名 | `GameServiceStore.GetService("模块")` |

## 日志：`BDebug`

```csharp
// 全局命名空间（无 namespace），: MonoBehaviour
[DefaultExecutionOrder(-10000)]
public class BDebug : MonoBehaviour
{
    public readonly static string ENABLE_BDEBUG = "ENABLE_BDEBUG";   // ★ 字符串常量，不是宏本身
    public bool IsLog = true;

    [Header("启用Log加密")] public bool EnablePlayerLogEncryption = true;
    public string PlayerLogEncryptPassword = LogCrypto.DEFAULT_PASSWORD;

    public class LogTag { public string Tag; public bool IsLog; }
    public List<LogTag> DisableLogTagList = new List<LogTag>();

    public static string PlayerLogRootPath { get; }        // Editor 返回 string.Empty
    public static string CurrentPlayerLogFilePath { get; } // Editor 返回 string.Empty

    // ── 日志（全部 [Conditional("ENABLE_BDEBUG")]）──
    public static void Log(object log);
    public static void Log(string tagOrLog, string logOrColor);
    public static void Log(string log, Color color);
    public static void Log(string tagOrLog, string log, Color color);
    public static void Log(string tagOrLog, string log, string color);
    public static void LogFormat(string format, params object[] args);
    public static void LogFormat(string tag, string format, params object[] args);
    public static void LogError(object log);
    public static void LogError(string tag, object log);

    // ── Tag 过滤 ──
    static public void DisableLog(string tag);
    static public void EnableLog(string tag);

    // ── 耗时统计 ──
    static public void LogWatchBegin(string watchTag);
    static public void LogWatchEnd(string watchTag, string color = "");
    static public void LogWatchEnd(string logTag, string watchTag, string color = "");

    // ── 持久化 ──
    public static void FlushPlayerLogs();
    public static string ExportPlayerLogToText(string binFilePath, string txtFilePath = null, string password = null);
}
```

!!! danger "`BDebug` 没有 `LogWarning` / `Assert` / `LogErrorAndThrow`"
    这三个 API **在仓库中不存在**。需要 warning 直接用 `UnityEngine.Debug.LogWarning`。

!!! danger "`[Conditional("ENABLE_BDEBUG")]` 会在编译期消除整个调用"
    宏未定义时，**连参数表达式都不会求值**。所以：

    ```csharp
    // ✗ 错误：宏关闭时 LoadConfig() 根本不会被调用
    BDebug.Log("配置已加载: " + LoadConfig());

    // ✓ 正确：先求值再传
    var cfg = LoadConfig();
    BDebug.Log("配置已加载: " + cfg);
    ```

    宏由 `Editor/EditorAutoSetting/Editor/EditorSetting.cs` 自动写入。

### Tag 过滤

```csharp
BDebug.DisableLog("Battle");       // 屏蔽所有 Battle tag 的日志
BDebug.EnableLog("Battle");
```

`IsEnableTag(tag)` 逻辑：实例不存在时返回 `true`；否则在 `DisableLogTagList` 里查（`lock` 保护），未登记 → `true`。

`IsConsoleLogEnabled` = `inst == null || inst.IsLog` —— **实例缺失时默认放行**。

!!! note "`DisableLog` / `EnableLog` 会强制创建实例"
    它们用的是 `Inst`（非宽松的 `inst`），在非 PlayMode 下会 `FindObjectOfType` 或**直接 `new GameObject`**。见[重构清单](../architecture/refactor-backlog.md#ref-6-bdebug-runtime-coupling)。

### 耗时统计

```csharp
BDebug.LogWatchBegin("LoadWindow");
// ...
BDebug.LogWatchEnd("LoadWindow");
```

`watchMap` 是 `ConcurrentDictionary<string, Stopwatch>`，`LogWatchEnd` 用 `TryRemove` —— **未配对时静默无事**。耗时换算 `ElapsedTicks / 10000f`（毫秒）。

## 日志持久化子系统

| 类型 | 路径 | 关键成员 |
|------|------|---------|
| `PersistenceSettings` | `Utils/Logs/PersistenceSettings.cs` | `DEFAULT_DIRECTORY_NAME = "playerlogs"`、`DEFAULT_FLUSH_INTERVAL_MS = 1000`、`MIN_FLUSH_INTERVAL_MS = 100`、`Normalize()`、`CloneNormalized()`、`CreatePlayerDefault()` |
| `Persistence` | `Utils/Logs/Persistence.cs` | `FILE_MAGIC = 0x474C4442`、`FILE_VERSION = 1`、`RECORD_FLAG_NONE = 0`、`RECORD_FLAG_ENCRYPTED = 1`、`Initialize(settings)`、`Flush()`、`Shutdown()` |
| `LogCrypto` | `Utils/Logs/LogCrypto.cs` | `DEFAULT_PASSWORD = "368219a2c858..."`、`DeriveKey(string)`、`Encrypt(byte[],int,byte[])`、`Decrypt(byte[], string = null)` |
| `LogReader` | `Utils/Logs/LogReader.cs` | `ReadAll(string filePath, string password = null)`、`ExportToText(string filePath, string outputPath = null, string password = null)` |
| `SerializedLogEntry` | `Utils/Logs/SerializedLogEntry.cs` | `public struct SerializedLogEntry` |
| `Editor_UnityLogHook` | `Utils/Logs/Editor_UnityLogHook.cs` | 仅 Editor，静态构造挂 `Application.logMessageReceived` |

初始化路径：

```csharp
[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
RuntimeInitPlayerLogSerialize()          // #if !UNITY_EDITOR → Persistence.Initialize(BuildPersistenceSettings())

Awake()                                  // Application.isPlaying && !Application.isEditor
  → ApplyPersistenceSettings()

OnApplicationQuit() → Persistence.Shutdown()
```

**Editor 下 `PlayerLogRootPath` / `CurrentPlayerLogFilePath` 返回 `string.Empty`** —— 持久化只在真机生效。

导出日志：

```csharp
var text = BDebug.ExportPlayerLogToText(binFilePath, txtFilePath);
```

## `BDebugPerformanceProfiler`

```csharp
static public class BDebugPerformanceProfiler
{
    static public void BeginStepTimer(string tag, string groupTag = "");
    static public void BeginStep(string tag, string stepName);
    static public void EndStep(string tag, string stepName);
    static public void AccumulateStep(string tag, string stepName);
    static public List<StepResult> EndStepTimerGetData(string tag);
    static public void EndStepTimer(string tag, bool logToConsole = true);
    static public List<StepResult> GetStepData(string tag);
    static public void Accumulate(string tag, float timeMs);
    static public void ReportAccumulator(string tag, bool resetAfterReport = true);
    static public void PrintPipelineReport(string tag, List<StepResult> steps, int rowCount, string sql = "");
    static public void Reset();
}
```

用于构建管线内部的阶段耗时统计（`PrintPipelineReport` 会输出带行数的表格）。

## 对象池

```csharp
namespace BDFramework.Utils

public class ObjectPool<T>
{
    public ObjectPool(Func<T> factoryFunc, int initialSize);
    public ObjectPool(Func<T> factoryFunc, Action<T> destroyFunc, int initialSize);   // 无 destroyFunc 时 Destroy() 会 NRE

    public T GetItem();                      // 环形扫描未使用项；不够则扩容
    public void ReleaseItem(object item);
    public void ReleaseItem(T item);         // 不在池中 → Debug.LogWarning
    public int Count { get; }
    public int CountUsedItems { get; }
    public void Destroy();                   // 对每项调 destroyFunc
}

public class ObjectPoolContainer<T>
{
    public bool Used { get; private set; }
    public T Item { get; set; }
    public void Consume();                   // Used = true
    public void Release();                   // Used = false
}
```

!!! warning "`getItem` 用 `lookup.Add` 而非索引器"
    同一个 item 被重复取用会抛 `ArgumentException`。`GameObjectPoolManager` 靠"一个 `ObjectPoolContainer` 对应一个 clone"避免这个问题。

GameObject 层的封装见 [资源加载 BResources](resources.md#object-pool)。

## `MonoSingleton`

```csharp
namespace BDFramework.ResourceMgr        // ★ 不是 BDFramework.Utils

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; set; }
}
```

`Instance` 懒查找行为：

| 场景 | 行为 |
|------|------|
| 找到 1 个 | 复用并把 GameObject 改名 `typeof(T).Name` |
| 找到多个 | `Debug.LogError("...exists multiple times in violation of singleton pattern. Destroying all copies")` 并**全部 Destroy** |
| 找到 0 个 | `new GameObject(typeof(T).Name, typeof(T))` + `DontDestroyOnLoad` |

!!! note "命名空间是 `BDFramework.ResourceMgr`"
    尽管文件在 `Runtime/Utils/MonoSingleton/` 下。容易 `using` 错。

## IO 与路径

!!! note "刻意的命名空间劫持"
    `IPath` 与 `FileHelper` 都声明在 **`namespace System.IO`** 中。这样业务代码写 `IPath.Combine(...)` 时不需要额外 `using`。

```csharp
static public class IPath
{
    static public string Combine(string a, string b);              // 修复 Mac 下 Path.Combine 的 bug
    static public string Combine(string a, string b, string c);
    static public string Combine(string a, string b, string c, string d);
    static public string AddEndSymbol(string path);
    static public string ReplaceBackSlash(string path);
    static public string FormatPathOnUnity3d(string path);
}

static public class FileHelper
{
    static public void WriteAllBytes(string path, byte[] bytes);   // 自动建父目录
    static public void WriteAllText(string path, string contents);
    static public void Copy(string path, string targetPath, bool overwrite);
    static public void Move(string path, string targetPath);
    static public void CopyFolderTo(string sourceDirt, string targetDirt, bool useLowerPath = false);
    static public void WriteAllLines(string path, string[] contents);

    public static string GetMurmurHash3(string filePath);          // 不存在返回 "null"
    public static string GetMurmurHash3(byte[] bytes);
    public static string GetMurmurHash2(string filePath);
    public static string GetMurmurHash2(byte[] bytes);
}

public class MurmurHash3
{
    public static uint Hash32(ReadOnlySpan<byte> bytes, uint seed = 12345678u);
}
```

**`FileHelper.GetMurmurHash3` 是整个资源版本控制的核心 hash 函数**（`assets.info` 的 `HashName` 就是它）。

## 序列化

```csharp
static public class CSVHelper      // ServiceStack.Text 包装
{
    public static List<T> LoadObjects<T>(string filename, bool strict = true) where T : new();
    public static List<T> LoadObjects<T>(TextReader rdr, bool strict = true) where T : new();
    public static void LoadObject<T>(string filename, ref T destObject);
    public static void SaveObject<T>(T obj, string filename);
    public static void SaveObjects<T>(IEnumerable<T> objs, string filename);
}
```

`art_assets.info` / `assets.info` 都通过它读写。

## 协程：`IEnumeratorTool`

```csharp
// 全局命名空间
public class IEnumeratorTool : MonoBehaviour
{
    public class ActionTask { public Action willDoAction; public Action callBackAction; }

    static public void ExecAction(Action action, Action callBack = null);
    static public void ExecActionImmediately(Action action, Action callBack = null);
    static public new int StartCoroutine(IEnumerator ie);
    static public void StopCoroutine(int id);
    static public void StopAllCroutine();
    static public void WaitingForExec(float f, Action action);
}
```

!!! danger "`StartCoroutine` 只是入队"
    真正的 `base.StartCoroutine` 发生在 `Update()` 里。**场景中没有 `IEnumeratorTool` 组件时，所有调用永远不会推进，且不报错。**

    `BDLauncherBridge.Launch()` 会自动 `AddComponent<IEnumeratorTool>()`；纯逻辑/BatchMode 场景需要自己保证。

`Update()` 的驱动顺序：停协程 → 起协程队列 → 立即队列 → 每帧 1 个普通队列。

## 其他扩展

```csharp
// 全局命名空间
static public class DateTimeEx { static public long GetTotalSeconds(); }

public class HashHelper
{
    public static string CreateMD5ByString(string input);
    public static string CreateMD5ByFile(string fileName);
}

static public class StringEX
{
    public static bool Contains(this string source, string value, StringComparison comparisonType);
    public static string ToMD5(this string source);
}

static public class TypeEx { public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type); }

static public class ReflectionExtension
{
    static public T   GetAttributeInILRuntime<T>(this MemberInfo memberInfo) where T : Attribute;
    static public T[] GetAttributeInILRuntimes<T>(this MemberInfo memberInfo) where T : Attribute;
}
```

!!! note "`GetAttributeInILRuntime` 名字是历史遗留"
    现在只是常规反射封装（`GetCustomAttribute`），被 `ATComponent`、`AWindow<TP>`、`ComponentBindAdaptorManager` 使用。

## `BApplication` —— 路径与平台

```csharp
namespace BDFramework.Core.Tools

static public class BApplication
{
    static public bool   IsPlaying { get; set; }
    static public string persistentDataPath { get; private set; }
    static public string streamingAssetsPath { get; private set; }

    static public string ProjectRoot / BDWorkSpace / Library / Package / RuntimeResourceLoadPath { get; private set; }
    public static string EditorResourcePath / EditorResourceRuntimePath { get; private set; }
    public static string DevOpsPath / DevOpsCodePath / DevOpsPublishAssetsPath
                       / DevOpsPublishClientPackagePath / DevOpsConfigPath / DevOpsCIPath / BDEditorCachePath { get; private set; }

    static public RuntimePlatform   RuntimePlatform { get; }      // 宏判定，绝不返回 Editor 枚举
    public static RuntimePlatform[] SupportPlatform { get; }

    static public string GetRuntimePlatformPath();
    public static string GetPlatformLoadPath(RuntimePlatform platform);      // "windows"/"android"/"osx"/"ios"
    public static string GetPlatformPath(BuildTarget);
    public static BuildTarget     GetBuildTarget(RuntimePlatform);
    public static BuildTargetGroup GetBuildTargetGroup(RuntimePlatform | BuildTarget);
    public static RuntimePlatform GetRuntimePlatform(BuildTarget);
    public static string GetPlatformLoadPath(BuildTarget);
    public static string GetPlatformDevOpsPublishAssetsPath(RuntimePlatform | BuildTarget);
    public static string GetPlatformDevOpsPublishPackagePath(RuntimePlatform | BuildTarget);

    public static List<string> GetAllRuntimeDirects();      // 仅 Editor：扫描 Assets/*/Runtime
    public static List<string> GetAllRuntimeAssetsPath();   // 仅 Editor
}
```

!!! danger "静态构造可能在 loading thread 触发"
    `Application.dataPath` 在非主线程会抛 `UnityException`。框架用 `TryInitializePathState`（吞异常返回 `false`）+ `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] InitializePathStateBeforeSceneLoad` 兜底。

    业务侧应尽量**在主线程首次访问**（`ScriptLoder.Init()` 里就显式预热了一次 `persistentDataPath`）。

Editor 下的路径重写见[资源加载寻址](../guide/asset-load-path.md#path-rewrite-matrix)。

## 低内存容器

```csharp
namespace BDFramework.LowMemory.Container

public class LMDictionary<K,V> : IDisposable
{
    public Dictionary<K,int> dict;
    public List<V> objectList;

    public LMDictionary(int capacity = 10);   // K 非 int/string → throw InvalidOperationException
    public void Add(K key, V value);          // 重复 key → throw ArgumentException
    public void Remove(K key);                // 移除后重建 >idx 的索引（O(n)）
    public void Dispose();
}
```

值列表 + 索引字典的紧凑存储。**注释明确："这个不是线程安全的!!!!"**

## 本地化类型

```csharp
namespace BDFramework.L2

public enum L2Type
{
    zh_CN = 1, zh_CNT,                       // 简体 / 繁体
    ru_RU, en_US, en_GB, ja_JP, ko_KR,
    fr_FR, de_DE, pt_BR, it_IT,
    es_ES, es_MX,
    ar_SA, ar_AE, ar_EG, ar_JO,
}
```

**这就是 L2 的全部** —— 只是一个语言枚举，没有加载器/表结构。被 `GameBaseConfigProcessor.Config.L2Type` 引用，消费方在 UI 层（`Runtime/UI/Component/Localization/L2Text.cs`、`L2Image.cs`，两者都是**空壳 MonoBehaviour**）。

## 相关页面

- [管理器体系 ManagerBase](manager-base.md)
- [依赖注入](../ui/dependency-injection.md)
- [资源加载寻址](../guide/asset-load-path.md)
- [Runtime 模块地图](../architecture/runtime-modules.md)
