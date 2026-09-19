# Event Bus

Two independent listener chains: **value listening** (`ADataListenerT<T>`, strongly typed) and **status listening** (`AStatusListener`, an `object` payload plus a runtime type check).

## Type hierarchy

```csharp
namespace BDFramework.DataListener

abstract public class AStatusListener          // 状态监听（object 载体）
public class ADataListenerT<T> : ADataListenerTBase   // 值监听（强类型）
public class ADataListenerTBase                // 空基类，仅类型擦除占位
```

The service layer (`Runtime/EventBus/Service/`):

```csharp
public class StatusListenerService : AStatusListener
public class StatusListenerServer              // 静态管理器
```

## `AStatusListener`

```csharp
abstract public class AStatusListener
{
    protected Dictionary<string, object> dataMap;
    protected Dictionary<string, List<ListenerCallbackData>> callbackMap;
    protected Dictionary<string, List<object>> valueCacheMap;

    private int maxCacheValueCount = 20;       // ★ 值缓存上限

    public AStatusListener();

    virtual public void SetData(string name, object value, bool isTriggerCallback = true);
    virtual public void TriggerEvent(string name, object value = null, bool isTriggerCallback = true);
    virtual public T GetData<T>(string name);

    virtual public void AddListener<T>(string name, Action<T> callback,
                                       int order = -1, int triggerNum = -1, bool isTriggerCacheData = false)
        where T : class;
    virtual public void AddListenerOnce<T>(string name, Action<T> callback, int order = -1, bool isTriggerCacheData = false)
        where T : class;

    virtual public void RemoveListener<T>(string name, Action<T> callback);
    virtual public void RemoveListener(string name);
    virtual public void ClearListener(string name);
    public    void ClearAllListener();

    public List<string> GetDataNames();
    public List<string> GetListenerKeys();
    public bool ContainsKey(string name);

    public class ListenerCallbackData
    {
        public int Order { get; private set; } = 1;
        public int TriggerNum { get; private set; } = 1;
        public AActionAdaptor ActionAdaptor { get; private set; }
        public void Invoke(object value);
    }
}
```

### The value cache: capped at 20 entries

```text
SetData(name, value)
 ├─ 该 name 已有回调 → 直接派发，不写缓存
 └─ 该 name 无回调   → 写入 valueCacheMap
                       若 list.Count >= 20 → RemoveAt(0)（保留最新 20 条）
```

`AddListener(..., isTriggerCacheData: true)` replays the entire cache and then calls `valueCacheMap[name].Clear()`.

!!! warning "20 entries is a hard cap"
    Unlike `ADataListenerT<T>` (whose cache is unbounded), `AStatusListener` keeps only the latest 20. Do not lean on it for scenarios where "every historical value has to be handled".

### Type checking: a key cannot change its type

```csharp
public virtual void SetData(string name, object value, bool isTriggerCallback = true)
{
    // 旧值类型与 new 值类型不同 → 拒绝写入
    Debug.LogErrorFormat("设置失败,类型不匹配:{0}  curType:{1}  setType:{2}",
        name, lastT.Name, currentT.Name);
    return;      // ← 不写入
}
```

!!! danger "Once a key holds an `int`, you cannot put a `string` in it"
    This is a frequent cause of "the data was set but reads back as nothing". **Different data types must use different keys.**

### `TriggerNum` semantics

| Value | Meaning |
|-------|---------|
| `-1` (default) | Permanent; `Invoke` does not decrement the counter |
| `0` | Exhausted; it no longer fires and will be recycled |
| `> 0` | Remaining trigger count, decremented on every call |

### Callback safety

When firing, it **copies `_callbackList`** before iterating, so **adding or removing listeners from inside a callback is allowed**. After the iteration, entries with `TriggerNum == 0` are recycled.

`Order` determines the insertion position (`AddListener` inserts in ascending `Order`; `order = -1` sorts to the front by default).

## `ADataListenerT<T>`

```csharp
public class ADataListenerT<T> : ADataListenerTBase
{
    protected Dictionary<string, T> dataMap;
    protected Dictionary<string, List<Action<T>>> callbackMap;
    protected Dictionary<string, List<Action<T>>> onceCallbackMap;
    protected Dictionary<string, List<T>> valueCacheMap;

    public ADataListenerT();

    virtual public void InitData();
    virtual public void SetData(string name, T value, bool isTriggerCallback = true);
    virtual public void TriggerEvent(string name, T value, bool isUseCallback = true);   // 直接转调 SetData
    virtual public T GetData(string name);

    virtual public void AddListener(string name, Action<T> callback = null, bool isTriggerCacheData = false);
    virtual public void AddListenerOnce(string name, Action<T> callback = null, bool isTriggerCacheData = false);

    virtual public void RemoveListener(string name, Action<T> callback);
    virtual public void RemoveListener(string name);
    virtual public void ClearListener(string name);

    public List<string> GetDataNames();
}
```

Behaviour notes:

| Behaviour | Notes |
|-----------|-------|
| Trigger order | **`onceCallbackMap` first (each entry removed as it runs), then `callbackMap` (in reverse)** |
| Cache | Pushes values into `valueCacheMap` when there is no listener (**unbounded**) |
| Listening ahead of time | When `dataMap` has no such key, `BDebug.LogError("暂时无数据,提前监听:" + name)`, but **it still registers** |
| `GetData` | For a missing key it **writes the default value** (`dataMap[name] = default`) |

## Extension methods

### `EventListenerEx` — listen by type (recommended)

```csharp
namespace BDFramework.DataListener

unsafe static public class EventListenerEx
{
    static public void AddListener<T>(this AStatusListener dl, Action<T> action = null,
                                      int order = -1, int triggerNum = -1, bool isTriggerCacheData = false)
        where T : class;
    static public void AddListenerOnce<T>(this AStatusListener dl, Action<T> callback = null,
                                          int order = -1, bool isTriggerCacheData = false) where T : class;
    static public void RemoveListener<T>(this AStatusListener dl, Action<T> callback) where T : class;
    static public void ClearListener<T>(this AStatusListener dl) where T : class;
    static public void TriggerEvent<T>(this AStatusListener dl, T value = null) where T : class;
}
```

Everything uses **`typeof(T).FullName`** as the key. `TriggerEvent<T>(null)` uses `typeof(T).FullName`; when the value is not `null` it uses `value.GetType().FullName`.

```csharp
// 定义消息类型
public class OnGoldChanged { public int Gold; }

// 监听
window.State.AddListener<OnGoldChanged>(e => RefreshGold(e.Gold));

// 触发
StatusListenerServer.Create("Player").TriggerEvent(new OnGoldChanged { Gold = 100 });
```

### `ValueListenerEx` — listen by `Enum` / string name

```csharp
static public class ValueListenerEx
{
    // Enum 版本内部用 enum.GetHashCode().ToString() 或 ToString() 作 key
    static public void SetData(this AStatusListener dl, Enum @enum, object value, bool isTriggerCallback = true);
    static public void SetData(this AStatusListener dl, string @enum, object value, bool isTriggerCallback = true);
    static public T GetData<T>(this AStatusListener dl, Enum name);
    static public T GetData<T>(this AStatusListener dl, string name);
    static public void TriggerEvent(this AStatusListener dl, Enum name, object value = null, bool isTriggerCallback = true);
    static public void TriggerEvent(this AStatusListener dl, string name, object value = null, bool isTriggerCallback = true);

    static public void AddListener(this AStatusListener dl, Enum name, Action<object> action = null, …);
    static public void AddListener<T>(this AStatusListener dl, Enum name, Action<T> action = null, …);
    static public void AddListenerOnce(this AStatusListener dl, Enum name, …);
    static public void RemoveListener<T>(this AStatusListener dl, Enum name, Action<T> callback);
    static public void RemoveListener(this AStatusListener dl, Enum name, Action<object> callback);
    static public void RemoveListener(this AStatusListener dl, Enum name);
    static public void ClearListener(this AStatusListener dl, Enum name);
}
```

```csharp
public enum PlayerData { Gold, Level, Name }

var svc = StatusListenerServer.Create("Player");
svc.SetData(PlayerData.Gold, 100);
svc.AddListener<int>(PlayerData.Gold, v => RefreshGold(v));
svc.AddListenerOnce(PlayerData.Level, o => Debug.Log("升级一次"), order: 0);
svc.TriggerEvent(PlayerData.Gold, 200);
svc.RemoveListener(PlayerData.Gold);
```

!!! tip "Prefer `Enum` over bare strings"
    `SetData("Gold", …)` silently stops working after a rename; `SetData(PlayerData.Gold, …)` is checked by the compiler.

## `StatusListenerServer` — the static manager

```csharp
public class StatusListenerServer
{
    public static StatusListenerService Create(string name);        // 已存在则返回已有的
    public static StatusListenerService GetService(string name);    // 不存在则新建并缓存
    static public void RemoveService(string name);
    static public void RemoveALLService();

    public static ADataListenerT<T> Create<T>(string name);
    public static ADataListenerT<T> GetService<T>(string name);
}
```

**Two independent dictionaries**:

| Dictionary | Type |
|------------|------|
| `serviceMap` | `Dictionary<string, StatusListenerService>` |
| `serviceTMap` | `Dictionary<string, ADataListenerTBase>` |

!!! note "The same key name does not collide across the two maps"
    `Create("Player")` and `Create<PlayerData>("Player")` are **two different services**. Do not use the same name for both.

!!! warning "`StatusListenerService.Name` is always `null`"
    `Create(name)` / `GetService(name)` **never assign `Name`** internally (the dictionary key is the real name). See [Refactor Backlog](../architecture/refactor-backlog.md#ref-5-statuslistenerservice-name).

## Recommended usage: each module owns its listener

The framework author's advice: **each business module should hold its own `StatusListenerService`** rather than sharing one globally.

```csharp
public static class PlayerEvents
{
    private static StatusListenerService _svc;
    public static StatusListenerService Svc => _svc ??= StatusListenerServer.Create(nameof(PlayerEvents));

    public static void SetGold(int gold) => Svc.SetData(PlayerData.Gold, gold);
    public static void OnGoldChanged(Action<int> cb) => Svc.AddListener<int>(PlayerData.Gold, cb);
}
```

## Full example (`demo_StatusListener`)

The actual usage in `Assets/Code/Game@hotfix/demo_StatusListener/Window_StatusListener.cs`:

```csharp
var serviceEnum = StatusListenerServer.Create(nameof(StatusListenerEnum));

// 值读写
serviceEnum.SetData(StatusListenerEnum.Test, 1);
var v = serviceEnum.GetData<int>(StatusListenerEnum.Test);

// 持久监听 / 一次性监听
serviceEnum.AddListener(StatusListenerEnum.Test, o => Debug.Log("监听热更Enum :" + o.ToString()));
serviceEnum.AddListenerOnce(StatusListenerEnum.Once, o => Debug.Log("监听热更Enum Once:" + o.ToString()));

// 触发 / 移除
serviceEnum.TriggerEvent(StatusListenerEnum.Test, 2);
serviceEnum.RemoveListener(StatusListenerEnum.Test);
serviceEnum.ClearListener(StatusListenerEnum.Test);
```

**The typed-parameter version** (recommended, avoids boxing):

```csharp
var s2 = StatusListenerServer.Create(nameof(Msg_Test001.Msg2));

s2.AddListener<Msg_ParamTest>(nameof(Msg_Test001.Msg2), triggerNum: 10,
    action: o => { /* o.test1 */ });

s2.AddListener(Msg_Test001.Msg2, triggerNum: 10,
    action: o => { var _o = o as Msg_ParamTest; });
```

## How it divides work with the UFlux message mechanism

| Dimension | `UIMsgData` + `SendMessage` | `AStatusListener` |
|-----------|---------------------------|-------------------|
| Target | A specific window | A global / named service, with any number of listeners |
| Matching | Exact message-type match | `Enum` / `string` / `Type.FullName` |
| Cache | An unbounded list awaiting replay | **Capped at 20 entries** |
| Fits | Point-to-point commands inside a screen | Cross-module status broadcast |

→ See [Messages (UIMessage)](../uflux/ui-message.md).

## Common failures

| Symptom | Root cause |
|---------|------------|
| `设置失败,类型不匹配:...` | The same key was used with different types |
| `暂时无数据,提前监听:<name>` | `dataMap` did not have that key yet when `AddListener` ran (it still registers; this is only a heads-up) |
| Only one callback arrives | `AddListenerOnce` was used |
| Only the first 10 callbacks arrive | Limited by `triggerNum: 10` |
| No callback at all | Key mismatch (`Enum` vs `string` vs `Type.FullName`); or the same name was mixed between `Create` and `Create<T>` |
| Cached historical values are gone | `AStatusListener` only keeps the latest 20 |
| `Name` is `null` in the logs | A known issue: `StatusListenerService.Name` is never assigned |

## Related pages

- [Messages (UIMessage)](../uflux/ui-message.md)
- [Service Container & Logging](utils.md)
- [Demo Walkthroughs](../tutorials/demos.md) — `demo_StatusListener`, `demo_EventManager`
- [Runtime Module Map](../architecture/runtime-modules.md)
