# ManagerBase / ServiceContainer API 参考

> 来源：`Runtime/Utils/ManagerBase/**`、`Runtime/Service/**`

## 命名空间

```csharp
namespace BDFramework.Mgr          // ★ 不是 BDFramework
namespace BDFramework.GameServiceStore
```

## `ClassData` / `IMgr`

```csharp
namespace BDFramework.Mgr

public class ClassData
{
    public ManagerAttribute Attribute;
    public Type Type;
}

public interface IMgr
{
    bool IsStarted { get; }
    void Init();
    void Start();
    bool RegisterTypes(Type type, ManagerAttribute[] attributes);
    bool RegisterHotfixTypes(Type type, ManagerAttribute[] attributes);
}
```

## `ManagerAttribute` / `ManagerOrder`

```csharp
public class ManagerAttribute : Attribute
{
    public int    IntTag { get; private set; } = -1;
    public string Tag    { get; private set; } = null;

    public ManagerAttribute(int intTag);
    public ManagerAttribute(string tag);
}

public class ManagerOrder : Attribute
{
    public int Order { get; set; } = 0;
}
```

## `ManagerBase<T, V>`

```csharp
abstract public class ManagerBase<T, V> : IMgr
    where T : IMgr, new()
    where V : ManagerAttribute
{
    static public T Inst { get; }                        // 懒 new T()，每个闭合泛型各一份

    public bool IsStarted { get; private set; }

    virtual public void Init();                          // 空实现
    virtual public void Start();                         // IsStarted = true

    virtual public bool RegisterTypes(Type type, ManagerAttribute[] attributes);
    virtual public bool RegisterHotfixTypes(Type type, ManagerAttribute[] attributes);

    public ClassData GetClassData(object tag);           // int → IntKey map；string → StringKey map
    public ClassData GetClassData<TN>();
    public ClassData GetClassData(Type type);            // 线性遍历

    public IEnumerable<ClassData> GetAllClassDatas();    // IntKey 优先并升序，否则 StringKey 升序

    public T2 CreateInstance<T2>(ClassData cd, params object[] args) where T2 : class;
    public T2 CreateInstance<T2>(object tag, params object[] args) where T2 : class;
}
```

### 实现契约

| # | 要求 | 违反后果 |
|---|------|---------|
| 1 | `: ManagerBase<自类型, 属性类型>`，属性类型派生自 `ManagerAttribute` | 不会被 `LoadManager` 识别 |
| 2 | 有无参构造函数（`new()` 约束） | 编译失败 |
| 3 | 业务类挂 `V` 派生属性（`IntTag` 或 `Tag` 至少一个） | `GetClassData` 取不到 |
| 4 | 可选 `[ManagerOrder(Order = n)]`，越小越先 | 默认 `0` |
| 5 | 覆写 `Init()` / `Start()` 时**必须调 `base.Start()`** | `IsStarted` 不置位 → **被重复调用** |

### `GetAllClassDatas` 排序

```text
IntKey 非空    → 按 int 升序
否则 StringKey 非空 → 按 string 升序
都为空         → 返回空数组
```

!!! warning "不会混排 int 与 string tag"
    业务类同时用两种 tag 形式时，只会返回其中一种。

## `ManagerInstHelper`

```csharp
static public class ManagerInstHelper
{
    public static List<IMgr> MgrList { get; private set; }

    static public List<string> LoadManager(IEnumerable<Type> types, IEnumerable<string> exsitMgrNames = null);
    static public void RegisterType(IEnumerable<Type> types, bool isHotfixType = false);
    static public void Start();          // ① 全部 Init() ② 所有 !IsStarted 的 Start()
    static public void Start<T>();       // 只启动指定类型
}
```

### `LoadManager` 内部

```text
筛选：type.IsClass && !type.IsAbstract && typeof(IMgr).IsAssignableFrom(type)
取实例：type.BaseType.GetProperty("Inst", Static | Public) → GetValue(null)
排序：GetCustomAttribute<ManagerOrder>(false)?.Order ?? 0
热更接管：exsitMgrNames 命中 → 跳过，日志 "热更存在Mgr,由热更接管->{type.Name}"
找不到 Inst → BDebug.LogError("加载管理器失败,-" + type)
```

### 两遍启动

```csharp
public static void Start()
{
    // 第一遍：全部 Init()
    foreach (var mgr in MgrList) mgr.Init();

    // 第二遍：所有未启动的 Start()
    foreach (var mgr in MgrList)
        if (!mgr.IsStarted) mgr.Start();
}
```

**顺序**：先所有 `Init()`，再所有 `Start()`。因此 `Init()` 里可以安全引用其他管理器的 `Inst`。

!!! note "`exsitMgrNames` 是热更接管机制"
    Editor 下先加载了 Editor 侧管理器，热更 DLL 加载后同名管理器会被标记为"由热更接管"，避免双份实例。

## 仓库中的全部管理器

| 类型 | 文件 | Order |
|------|------|-------|
| `GameConfigManager` | `Runtime/GameConfig/GameConfigManager.cs` | 0 |
| `UIManager` | `Runtime/UI/View/UIManager/UIManager.cs` | 0 |
| `ComponentBindAdaptorManager` | `Runtime/UI/View/ComponentBindAdaptor/Manager/` | 0 |
| `ScreenViewManager` | `Runtime/Navigation/ScreenViewManager.cs` | **99999** |
| `DemoEventManager` | `Assets/Code/Game@hotfix/demo_EventManager/` | 0（业务示例） |

## `ServiceContainer`

```csharp
namespace BDFramework.GameServiceStore

public class ServiceContainer
{
    private List<object> singletonList = new List<object>();
    private List<Type>   transientList = new List<Type>();

    public void AddSingleton<T>() where T : class;              // Activator.CreateInstance(typeof(T))
    public void AddSingleton(object inst);                      // 同类型已存在 → BDebug.LogError("已存在同类型的Singleton")
    public void AddTransient<T>(T obj) where T : class;         // ★ 只登记类型
    public T GetService<T>() where T : class;
    private object GetService(Type type);
}
```

### 解析顺序

```text
GetService(type)
 ① singletonList.FindLast(o => o.GetType() == type)          ← 单例优先
 ② transientList.FindLast(t => t == type) → Activator.CreateInstance(t)
 ③ 都未命中 → 返回 null（不抛异常）
```

!!! danger "`AddTransient<T>(T obj)` 丢弃传入实例"
    ```csharp
    public void AddTransient<T>(T obj) where T : class
    {
        var type = obj.GetType();        // 只用类型
        var find = this.transientList.Find(t => t == type);
        if (find == null) this.transientList.Add(type);
        else BDebug.LogError("已存在同类型的Transient");
    }
    ```
    每次 `GetService<T>()` 都 `Activator.CreateInstance`（要求 `T` 有无参构造）。

!!! warning "类型比较是精确相等"
    `o.GetType() == type` —— **不做接口/基类匹配**。注册 `PlayerModel` 后 `GetService<IPlayerModel>()` 返回 `null`。

!!! warning "重复注册不会覆盖"
    同类型第二次调用只 `LogError`，`GetService` 仍返回第一个。

## `GameServiceStore`

```csharp
static public class GameServiceStore
{
    static public ServiceContainer GetService<T>() where T : new();   // key = typeof(T).FullName ?? typeof(T).Name
    static public ServiceContainer GetService(string moduleName);     // 不存在则 new 并缓存
}
```

```csharp
var battle = GameServiceStore.GetService("Battle");
battle.AddSingleton<BattleContext>();
var ctx = battle.GetService<BattleContext>();

var lobby = GameServiceStore.GetService("Lobby");
lobby.AddSingleton<LobbyContext>();      // 与 Battle 容器互不干扰
```

## 三种容器的关系

| 容器 | 作用域 | 注册入口 | 与 UFlux DI 的关系 |
|------|--------|---------|-------------------|
| `UIManager` 的 singleton/transient | 全局 | `UIManager.Inst.AddSingleton/AddTransient` | 就是它本身 |
| `IWindow.ServiceContainer` | 单窗口 | 窗口内部自行注册 | **独立** |
| `GameServiceStore` | 按模块名 | `GameServiceStore.GetService("模块")` | **独立** |

`UIManager.SetWindowDI` 只查 `UIManager` 自己的两个列表。

## 最小示例

```csharp
using BDFramework.Mgr;

// ① 属性
public class DemoEventAttribute : ManagerAttribute
{
    public DemoEventAttribute(int eventEnum) : base(eventEnum) { }
}

// ② 管理器
public class DemoEventManager : ManagerBase<DemoEventManager, DemoEventAttribute>
{
    public void Do(DemoEventEnum @enum, object o = null)
    {
        var @event = CreateInstance<IDemoEvent>((int)@enum);   // 每次 new，可自行池化
        if (@event != null) @event.Do();
        else BDebug.Log("获取不到 event:" + @enum.ToString());
    }

    public override void Init() { base.Init(); }
}

// ③ 业务类挂属性即可被注册（零注册代码）
[DemoEvent((int)DemoEventEnum.TestEvent1)]
public class Event_demo1 : IDemoEvent
{
    public void Do() => BDebug.Log("这是demo1的 log", "red");
}
```

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | `using BDFramework.Mgr;` 漏写 |
| 2 | 覆写 `Start()` 漏 `base.Start()` → 被重复调用 |
| 3 | `GetAllClassDatas()` 不混排 int/string tag |
| 4 | 需要晚于 UI 启动的管理器未加 `[ManagerOrder]` |
| 5 | `CreateInstance` 每次 new，高频调用需自行池化 |
| 6 | `AddTransient<T>(T obj)` 丢弃实例 |
| 7 | `GetService` 精确类型比较，接口注册无效 |
| 8 | 重复注册只报错不覆盖 |
| 9 | `GameServiceStore` / `ServiceContainer` / UFlux DI **互不相通** |
| 10 | `GetService` 未命中返回 `null` 而非抛异常，需自行 null 检查 |
