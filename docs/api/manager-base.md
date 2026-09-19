# 管理器体系 ManagerBase

框架的**唯一扩展点**：任何继承 `ManagerBase<T, TAttribute>` 并挂上派生自 `ManagerAttribute` 的属性的类，都会被自动发现、注册、`Init()` + `Start()`。

## 核心类型

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

!!! warning "命名空间是 `BDFramework.Mgr`"
    不是 `BDFramework`。`ManagerBase<T,V>`、`ManagerAttribute`、`ManagerOrder`、`ManagerInstHelper`、`IMgr`、`ClassData` 全部在 **`BDFramework.Mgr`** 下。

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

### 实现一个管理器的契约

| # | 要求 | 违反后果 |
|---|------|---------|
| 1 | `: ManagerBase<自类型, 属性类型>`，属性类型派生自 `ManagerAttribute` | 不会被 `LoadManager` 识别 |
| 2 | **有无参构造函数**（`new()` 约束） | 编译失败 |
| 3 | 业务类挂 `V` 派生属性（`IntTag` 或 `Tag` 至少一个） | `GetClassData` 取不到 |
| 4 | 可选 `[ManagerOrder(Order = n)]`，越小越先 | 默认 `0` |
| 5 | 覆写 `Init()` / `Start()` 时**必须调 `base.Start()`** | `IsStarted` 不置位 → **被重复调用** |

## 最小示例

```csharp
// ① 定义属性
public class DemoEventAttribute : ManagerAttribute
{
    public DemoEventAttribute(int eventEnum) : base(eventEnum) { }
}

// ② 定义管理器
public class DemoEventManager : ManagerBase<DemoEventManager, DemoEventAttribute>
{
    public override void Init()
    {
        base.Init();
        // 遍历已注册的 ClassData 做初始化
    }

    public void Do(DemoEventEnum @enum, object o = null)
    {
        var @event = CreateInstance<IDemoEvent>((int)@enum);
        @event?.Do();
    }
}

// ③ 业务类挂属性即可被注册
[DemoEvent((int)DemoEventEnum.TestEvent1)]
public class Event_demo1 : IDemoEvent
{
    public void Do() => BDebug.Log("这是demo1的 log", "red");
}
```

仓库中的真实示例：`Assets/Code/Game@hotfix/demo_EventManager/`。

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

`LoadManager` 的筛选与排序：

```text
筛选：type.IsClass && !type.IsAbstract && typeof(IMgr).IsAssignableFrom(type)
取实例：type.BaseType.GetProperty("Inst", Static | Public) → GetValue(null)
排序：GetCustomAttribute<ManagerOrder>(false)?.Order ?? 0
热更接管：exsitMgrNames 命中 → 跳过，日志 "热更存在Mgr,由热更接管->{type.Name}"
找不到 Inst → BDebug.LogError("加载管理器失败,-" + type)
```

!!! note "`exsitMgrNames` 是热更接管机制"
    Editor 下先加载了 Editor 侧管理器，热更 DLL 加载后同名管理器会被标记为"由热更接管"，避免双份实例。

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

**顺序**：先所有 `Init()`，再所有 `Start()`。因此 `Init()` 里可以安全地引用其他管理器的 `Inst`（此时都还没 `Start`）。

## 仓库中的全部管理器

| 类型 | 文件 | Order | 职责 |
|------|------|-------|------|
| `GameConfigManager` | `Runtime/GameConfig/GameConfigManager.cs` | 0 | 配置中心 |
| `UIManager` | `Runtime/UI/View/UIManager/UIManager.cs` | 0 | UI 总入口 |
| `ComponentBindAdaptorManager` | `Runtime/UI/View/ComponentBindAdaptor/Manager/` | 0 | 绑定适配器 |
| `ScreenViewManager` | `Runtime/Navigation/ScreenViewManager.cs` | **99999** | 屏幕导航 |
| `DemoEventManager` | `Assets/Code/Game@hotfix/demo_EventManager/` | 0 | 业务示例 |

!!! note "`[ManagerOrder]` 在全仓库只用了 1 次"
    `ScreenViewManager` 用 `99999` 保证自己最后启动（它的 `Start()` 会立刻 `BeginNavTo` 默认页，必须在 UI 就绪后执行）。

    如果你的管理器依赖 UI 或导航，记得显式加 `[ManagerOrder]`。

## `GetAllClassDatas` 的排序规则

```csharp
public IEnumerable<ClassData> GetAllClassDatas()
{
    // IntKey 非空 → 按 int 升序
    // 否则 StringKey 非空 → 按 string 升序
    // 都为空 → 返回空数组
}
```

**不会混排** int 与 string 两种 tag。如果业务类同时用了两种 tag 形式，只会返回其中一种。

`GameConfigManager` 依赖这个顺序：`GameConfigAttribute(-9999)` 先于 `GameConfigAttribute(2)`，保证 `GameBaseConfigProcessor` 早于 `GameCipherConfigProcessor` 执行——这样 `SqliteLoder.Password` 在 `SqliteLoder.Init` 之前一定已注入。

## `CreateInstance<T2>`

```csharp
public T2 CreateInstance<T2>(ClassData cd, params object[] args) where T2 : class;
public T2 CreateInstance<T2>(object tag, params object[] args) where T2 : class;
```

内部走 `Activator.CreateInstance(cd.Type, args)` 并转型。**每次调用都 new 一个新实例**（注释提示业务方可自行池化）。

## 常见故障

| 现象 | 根因 |
|------|------|
| 管理器 `Init()` 没被调用 | 类没挂派生自 `ManagerAttribute` 的属性；或没实现 `IMgr` |
| `Start()` 被调用两次 | 覆写时漏了 `base.Start()` |
| `GetClassData(tag)` 返回 `null` | tag 类型（int/string）与注册时不一致 |
| 管理器顺序不对 | 未加 `[ManagerOrder]`，默认 `0` 与其他并列 |
| `加载管理器失败,-<type>` | `ManagerBase<T,V>` 的 `Inst` 属性反射不到（检查泛型闭合） |
| 热更后出现两份管理器 | `exsitMgrNames` 未正确传入 |

## 相关页面

- [启动链路](../architecture/bootstrap.md) —— 管理器何时被启动
- [程序集与依赖](../architecture/assemblies.md) —— 类型收集白名单
- [配置中心 GameConfig](game-config.md) —— 一个真实的管理器实现
- [服务容器与日志](utils.md)
