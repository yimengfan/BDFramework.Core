# Managers (ManagerBase)

The framework's **only extension point**: any class that inherits `ManagerBase<T, TAttribute>` and carries an attribute derived from `ManagerAttribute` is automatically discovered, registered, and run through `Init()` + `Start()`.

## Core types

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

!!! warning "The namespace is `BDFramework.Mgr`"
    Not `BDFramework`. `ManagerBase<T,V>`, `ManagerAttribute`, `ManagerOrder`, `ManagerInstHelper`, `IMgr` and `ClassData` all live under **`BDFramework.Mgr`**.

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

### The contract for implementing a manager

| # | Requirement | What breaking it costs you |
|---|-------------|----------------------------|
| 1 | `: ManagerBase<own type, attribute type>`, where the attribute type derives from `ManagerAttribute` | Never recognised by `LoadManager` |
| 2 | **A parameterless constructor** (the `new()` constraint) | Compile error |
| 3 | Business classes carry an attribute derived from `V` (at least one of `IntTag` or `Tag`) | `GetClassData` returns nothing |
| 4 | Optional `[ManagerOrder(Order = n)]`; the smaller the value, the earlier it runs | Defaults to `0` |
| 5 | Overriding `Init()` / `Start()` **must call `base.Start()`** | `IsStarted` is never set → **called repeatedly** |

## Minimal working example

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

The real example in the repository: `Assets/Code/Game@hotfix/demo_EventManager/`.

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

`LoadManager`'s filtering and sorting:

```text
筛选：type.IsClass && !type.IsAbstract && typeof(IMgr).IsAssignableFrom(type)
取实例：type.BaseType.GetProperty("Inst", Static | Public) → GetValue(null)
排序：GetCustomAttribute<ManagerOrder>(false)?.Order ?? 0
热更接管：exsitMgrNames 命中 → 跳过，日志 "热更存在Mgr,由热更接管->{type.Name}"
找不到 Inst → BDebug.LogError("加载管理器失败,-" + type)
```

!!! note "`exsitMgrNames` is the hotfix-takeover mechanism"
    In the Editor, the Editor-side managers are loaded first; once the hotfix DLL is loaded, a manager with the same name is marked as "taken over by the hotfix", which avoids two live copies.

### The two-pass startup

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

**Order**: all `Init()` first, then all `Start()`. As a result, `Init()` may safely reference another manager's `Inst` (none of them have been `Start`ed yet).

## Every manager in the repository

| Type | File | Order | Responsibility |
|------|------|-------|----------------|
| `GameConfigManager` | `Runtime/GameConfig/GameConfigManager.cs` | 0 | Configuration centre |
| `UIManager` | `Runtime/UI/View/UIManager/UIManager.cs` | 0 | The UI entry point |
| `ComponentBindAdaptorManager` | `Runtime/UI/View/ComponentBindAdaptor/Manager/` | 0 | Binding adapters |
| `ScreenViewManager` | `Runtime/Navigation/ScreenViewManager.cs` | **99999** | Screen navigation |
| `DemoEventManager` | `Assets/Code/Game@hotfix/demo_EventManager/` | 0 | Business demo |

!!! note "`[ManagerOrder]` is used exactly once in the whole repository"
    `ScreenViewManager` uses `99999` to guarantee that it starts last (its `Start()` immediately calls `BeginNavTo` for the default page, so it has to run once the UI is ready).

    If your manager depends on the UI or on navigation, remember to add `[ManagerOrder]` explicitly.

## The ordering rule of `GetAllClassDatas`

```csharp
public IEnumerable<ClassData> GetAllClassDatas()
{
    // IntKey 非空 → 按 int 升序
    // 否则 StringKey 非空 → 按 string 升序
    // 都为空 → 返回空数组
}
```

**It never mixes** the two kinds of tag, int and string. If a business class somehow uses both tag forms, only one of them is returned.

`GameConfigManager` depends on this order: `GameConfigAttribute(-9999)` comes before `GameConfigAttribute(2)`, which guarantees that `GameBaseConfigProcessor` runs before `GameCipherConfigProcessor` — so `SqliteLoder.Password` is always injected before `SqliteLoder.Init`.

## `CreateInstance<T2>`

```csharp
public T2 CreateInstance<T2>(ClassData cd, params object[] args) where T2 : class;
public T2 CreateInstance<T2>(object tag, params object[] args) where T2 : class;
```

Internally it goes through `Activator.CreateInstance(cd.Type, args)` and casts the result. **Every call news up a fresh instance** (the comment suggests business code may want to pool these itself).

## Common failures

| Symptom | Root cause |
|---------|------------|
| The manager's `Init()` is never called | The class has no attribute derived from `ManagerAttribute`; or it does not implement `IMgr` |
| `Start()` is called twice | `base.Start()` was missed when overriding |
| `GetClassData(tag)` returns `null` | The tag type (int/string) differs from the one used at registration |
| Managers run in the wrong order | `[ManagerOrder]` was not added, so the default `0` ties with the others |
| `加载管理器失败,-<type>` | The `Inst` property of `ManagerBase<T,V>` cannot be reflected (check the generic closure) |
| Two copies of a manager after the hotfix update | `exsitMgrNames` was not passed correctly |

## Related pages

- [Startup Sequence](../architecture/bootstrap.md) — when managers are started
- [Assemblies & Dependencies](../architecture/assemblies.md) — the type-collection allow-list
- [Configuration (GameConfig)](game-config.md) — one real manager implementation
- [Service Container & Logging](utils.md)
