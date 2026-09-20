# 事件总线 API 参考

> 来源：`Runtime/EventBus/**`

## 类型体系

```csharp
namespace BDFramework.DataListener

abstract public class AStatusListener                // object 载体 + 运行时类型校验
public class ADataListenerT<T> : ADataListenerTBase  // 强类型
public class ADataListenerTBase                      // 空基类，类型擦除占位

public class StatusListenerService : AStatusListener
public class StatusListenerServer                    // 静态管理器
```

## `AStatusListener`

```csharp
abstract public class AStatusListener
{
    protected Dictionary<string, object> dataMap;
    protected Dictionary<string, List<ListenerCallbackData>> callbackMap;
    protected Dictionary<string, List<object>> valueCacheMap;

    private int maxCacheValueCount = 20;

    public AStatusListener();

    // ── 数据 ──
    virtual public void SetData(string name, object value, bool isTriggerCallback = true);
    virtual public void TriggerEvent(string name, object value = null, bool isTriggerCallback = true);
    virtual public T GetData<T>(string name);

    // ── 监听 ──
    virtual public void AddListener<T>(string name, Action<T> callback,
                                       int order = -1, int triggerNum = -1,
                                       bool isTriggerCacheData = false) where T : class;
    virtual public void AddListenerOnce<T>(string name, Action<T> callback,
                                           int order = -1, bool isTriggerCacheData = false) where T : class;
    virtual public void RemoveListener<T>(string name, Action<T> callback);
    virtual public void RemoveListener(string name);
    virtual public void ClearListener(string name);
    public    void ClearAllListener();

    // ── 查询 ──
    public List<string> GetDataNames();
    public List<string> GetListenerKeys();
    public bool ContainsKey(string name);

    public class ListenerCallbackData
    {
        public int Order { get; private set; } = 1;
        public int TriggerNum { get; private set; } = 1;
        public AActionAdaptor ActionAdaptor { get; private set; }

        public ListenerCallbackData(int order, int triggerNum, AActionAdaptor callback);
        public void Invoke(object value);      // TriggerNum == 0 直接 return；> 0 先自减；-1 不减
    }
}
```

### `SetData` 的类型校验

```csharp
public virtual void SetData(string name, object value, bool isTriggerCallback = true)
{
    // 查 dataMap[name] 已有值的类型
    // 若旧值与新值类型不同：
    Debug.LogErrorFormat("设置失败,类型不匹配:{0}  curType:{1}  setType:{2}",
        name, lastT.Name, currentT.Name);
    return;      // ★ 不写入
}
```

### 值缓存（20 条）

```text
该 name 有回调 → 直接派发，不写缓存
该 name 无回调 → 写 valueCacheMap
                 若 list.Count >= 20 → RemoveAt(0)     ← 保留最新 20 条
```

`AddListener(..., isTriggerCacheData: true)` 会把缓存全量重放后 `valueCacheMap[name].Clear()`。

### 回调安全

触发时复制一份 `_callbackList` 再遍历，因此**允许在回调里增删监听**。遍历后回收 `TriggerNum == 0` 的条目。

`AddListener` 按 `Order` 升序插入；`order = -1`（默认）排最前。

### `TriggerNum` 语义

| 值 | 含义 |
|----|------|
| `-1`（默认） | 永久，`Invoke` 不自减 |
| `0` | 已耗尽，不再触发且会被回收 |
| `> 0` | 剩余触发次数，每次自减 |

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
    virtual public void TriggerEvent(string name, T value, bool isUseCallback = true);   // 转调 SetData
    virtual public T GetData(string name);

    virtual public void AddListener(string name, Action<T> callback = null, bool isTriggerCacheData = false);
    virtual public void AddListenerOnce(string name, Action<T> callback = null, bool isTriggerCacheData = false);
    virtual public void RemoveListener(string name, Action<T> callback);
    virtual public void RemoveListener(string name);
    virtual public void ClearListener(string name);

    public List<string> GetDataNames();
}
```

### 与 `AStatusListener` 的差异

| 维度 | `AStatusListener` | `ADataListenerT<T>` |
|------|-------------------|---------------------|
| 载体 | `object` + 运行时类型校验 | 强类型 `T` |
| 缓存上限 | **20 条** | **无上限** |
| 触发顺序 | 按 `Order` 升序 | **先 `onceCallbackMap`（执行后移除），再 `callbackMap`（倒序）** |
| 提前监听 | `LogError` 但注册 | `LogError("暂时无数据,提前监听:" + name)` 但注册 |
| `GetData` 缺失 key | — | **会写入默认值**（`dataMap[name] = default`） |

## 扩展方法

### `EventListenerEx` —— 按类型

```csharp
namespace BDFramework.DataListener

unsafe static public class EventListenerEx
{
    static public void AddListener<T>(this AStatusListener dl, Action<T> action = null,
                                      int order = -1, int triggerNum = -1,
                                      bool isTriggerCacheData = false) where T : class;
    static public void AddListenerOnce<T>(this AStatusListener dl, Action<T> callback = null,
                                          int order = -1, bool isTriggerCacheData = false) where T : class;
    static public void RemoveListener<T>(this AStatusListener dl, Action<T> callback) where T : class;
    static public void ClearListener<T>(this AStatusListener dl) where T : class;
    static public void TriggerEvent<T>(this AStatusListener dl, T value = null) where T : class;
}
```

全部以 **`typeof(T).FullName`** 为 key。

`TriggerEvent<T>(null)` 用 `typeof(T).FullName`；非 `null` 时用 `value.GetType().FullName`。

```csharp
public class OnGoldChanged { public int Gold; }

window.State.AddListener<OnGoldChanged>(e => RefreshGold(e.Gold));
StatusListenerServer.Create("Player").TriggerEvent(new OnGoldChanged { Gold = 100 });
```

!!! warning "`T : class` 约束"
    值类型无法直接监听。需要监听 `int` 时用 `ValueListenerEx` 的 `Enum` / `string` 重载（内部装箱），或自己包一层 class。

### `ValueListenerEx` —— 按 Enum / string

```csharp
static public class ValueListenerEx
{
    // SetData
    static public void SetData(this AStatusListener dl, Enum @enum, object value, bool isTriggerCallback = true);
    static public void SetData(this AStatusListener dl, string @enum, object value, bool isTriggerCallback = true);

    // GetData
    static public T GetData<T>(this AStatusListener dl, Enum name);
    static public T GetData<T>(this AStatusListener dl, string name);

    // TriggerEvent
    static public void TriggerEvent(this AStatusListener dl, Enum name, object value = null, bool isTriggerCallback = true);
    static public void TriggerEvent(this AStatusListener dl, string name, object value = null, bool isTriggerCallback = true);

    // AddListener
    static public void AddListener(this AStatusListener dl, Enum name, Action<object> action = null, …);
    static public void AddListener<T>(this AStatusListener dl, Enum name, Action<T> action = null, …);
    static public void AddListenerOnce(this AStatusListener dl, Enum name, …);

    // Remove / Clear
    static public void RemoveListener<T>(this AStatusListener dl, Enum name, Action<T> callback);
    static public void RemoveListener(this AStatusListener dl, Enum name, Action<object> callback);
    static public void RemoveListener(this AStatusListener dl, Enum name);
    static public void RemoveListener(this AStatusListener dl, string name);
    static public void ClearListener(this AStatusListener dl, Enum name);
    static public void ClearListener(this AStatusListener dl, string name);
}
```

`Enum` 版本内部用 `enum.GetHashCode().ToString()` 或 `ToString()` 作为 key。

## `StatusListenerServer`

```csharp
public class StatusListenerServer
{
    // 两套独立字典
    private static Dictionary<string, StatusListenerService> serviceMap;
    private static Dictionary<string, ADataListenerTBase>    serviceTMap;

    public static StatusListenerService Create(string name);        // 已存在则返回 GetService(name)
    public static StatusListenerService GetService(string name);    // 不存在则新建并缓存
    static public void RemoveService(string name);
    static public void RemoveALLService();

    public static ADataListenerT<T> Create<T>(string name);
    public static ADataListenerT<T> GetService<T>(string name);
}
```

!!! danger "同名 key 在两个 map 里互不冲突"
    ```csharp
    var a = StatusListenerServer.Create("Player");        // serviceMap["Player"]
    var b = StatusListenerServer.Create<PlayerData>("Player");   // serviceTMap["Player"]
    // a 与 b 是两个完全不同的服务
    ```

!!! warning "`StatusListenerService.Name` 永远是 `null`"
    `Create` / `GetService` 内部**没有给 `Name` 赋值**（字典 key 才是真名）。日志里无法区分服务。

## 完整示例

```csharp
// ── 定义 key ──
public enum PlayerData { Gold, Level, Name }

// ── 按模块持有 ──
public static class PlayerEvents
{
    private static StatusListenerService _svc;
    public static StatusListenerService Svc => _svc ??= StatusListenerServer.Create(nameof(PlayerEvents));

    public static void SetGold(int gold)             => Svc.SetData(PlayerData.Gold, gold);
    public static void OnGoldChanged(Action<int> cb) => Svc.AddListener<int>(PlayerData.Gold, cb);
    public static void OffGoldChanged(Action<int> cb)=> Svc.RemoveListener(PlayerData.Gold, cb);
}

// ── 消费 ──
public class Window_Hud : AWindow
{
    public override void Init()
    {
        base.Init();
        PlayerEvents.OnGoldChanged(RefreshGold);
        PlayerEvents.Svc.AddListenerOnce(PlayerData.Level, o => Debug.Log("升级一次"));
        PlayerEvents.Svc.AddListener(PlayerData.Name, o => RefreshName((string)o));
    }

    public override void Destroy()
    {
        PlayerEvents.OffGoldChanged(RefreshGold);      // ★ 记得反注册
        base.Destroy();
    }

    private void RefreshGold(int gold) { /* … */ }
    private void RefreshName(string name) { /* … */ }
}
```

## 与 UI 消息的分工

| 维度 | `UIMsgData` + `SendMessage` | `AStatusListener` |
|------|---------------------------|-------------------|
| 目标 | **特定窗口** | 全局/具名服务，任意多监听者 |
| 匹配 | 消息类型精确匹配 | `Enum` / `string` / `Type.FullName` |
| 缓存 | 无上限待回放列表 | **上限 20 条** |
| 适用 | 界面内部定向指令 | 跨模块状态广播 |

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | 同一 key 前后类型不一致 → `设置失败,类型不匹配` 且**拒绝写入** |
| 2 | `AStatusListener` 只保留最新 20 条缓存 |
| 3 | `ADataListenerT<T>` 缓存**无上限** |
| 4 | `Create("X")` 与 `Create<T>("X")` 是两个不同服务 |
| 5 | `StatusListenerService.Name` 永远是 `null` |
| 6 | `EventListenerEx` 的 `T : class` 约束挡住值类型 |
| 7 | `GetData` 对不存在的 key 会写入默认值（`ADataListenerT<T>`） |
| 8 | `ADataListenerT<T>` 触发顺序是 once 先、callback 后（倒序） |
| 9 | `triggerNum` 用完后条目被回收，反注册会找不到 |
| 10 | 忘记反注册导致回调持有已销毁窗口的引用 |
