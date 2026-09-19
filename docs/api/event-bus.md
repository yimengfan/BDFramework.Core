# 事件总线 EventBus

两条独立的监听链路：**值监听**（`ADataListenerT<T>`，强类型）与**状态监听**（`AStatusListener`，`object` + 运行时类型校验）。

## 类型体系

```csharp
namespace BDFramework.DataListener

abstract public class AStatusListener          // 状态监听（object 载体）
public class ADataListenerT<T> : ADataListenerTBase   // 值监听（强类型）
public class ADataListenerTBase                // 空基类，仅类型擦除占位
```

服务层（`Runtime/EventBus/Service/`）：

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

### 值缓存：上限 20 条

```text
SetData(name, value)
 ├─ 该 name 已有回调 → 直接派发，不写缓存
 └─ 该 name 无回调   → 写入 valueCacheMap
                       若 list.Count >= 20 → RemoveAt(0)（保留最新 20 条）
```

`AddListener(..., isTriggerCacheData: true)` 会把缓存全量重放后 `valueCacheMap[name].Clear()`。

!!! warning "20 条是硬上限"
    与 `ADataListenerT<T>`（缓存无上限）不同，`AStatusListener` 只保留最新 20 条。对"每次都要处理全部历史"的场景不要依赖它。

### 类型校验：同 key 不能换类型

```csharp
public virtual void SetData(string name, object value, bool isTriggerCallback = true)
{
    // 旧值类型与 new 值类型不同 → 拒绝写入
    Debug.LogErrorFormat("设置失败,类型不匹配:{0}  curType:{1}  setType:{2}",
        name, lastT.Name, currentT.Name);
    return;      // ← 不写入
}
```

!!! danger "同名 key 一旦是 `int`，就不能再塞 `string`"
    这是排查"数据设置了但读不到"的高频原因。**不同类型的数据必须用不同的 key**。

### `TriggerNum` 语义

| 值 | 含义 |
|----|------|
| `-1`（默认） | 永久，`Invoke` 不减计数 |
| `0` | 已耗尽，不再触发且会被回收 |
| `> 0` | 剩余触发次数，每次自减 |

### 回调安全

触发时会**复制一份 `_callbackList`** 再遍历，因此**允许在回调里增删监听**。遍历后回收 `TriggerNum == 0` 的条目。

`Order` 决定插入位置（`AddListener` 按 `Order` 升序插入；`order = -1` 默认排最前）。

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

行为要点：

| 行为 | 说明 |
|------|------|
| 触发顺序 | **先 `onceCallbackMap`（执行后逐个移除），再 `callbackMap`（倒序）** |
| 缓存 | 无监听者时把值推进 `valueCacheMap`（**无上限**） |
| 提前监听 | `dataMap` 不含该 key 时 `BDebug.LogError("暂时无数据,提前监听:" + name)`，但**仍然注册** |
| `GetData` | 对不存在的 key **会写入默认值**（`dataMap[name] = default`） |

## 扩展方法

### `EventListenerEx` —— 按类型监听（推荐）

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

全部以 **`typeof(T).FullName`** 作为 key。`TriggerEvent<T>(null)` 用 `typeof(T).FullName`；非 `null` 时用 `value.GetType().FullName`。

```csharp
// 定义消息类型
public class OnGoldChanged { public int Gold; }

// 监听
window.State.AddListener<OnGoldChanged>(e => RefreshGold(e.Gold));

// 触发
StatusListenerServer.Create("Player").TriggerEvent(new OnGoldChanged { Gold = 100 });
```

### `ValueListenerEx` —— 按 Enum / string 名监听

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

!!! tip "优先用 `Enum` 而不是裸字符串"
    `SetData("Gold", …)` 在重命名后静默失效；`SetData(PlayerData.Gold, …)` 会被编译器检查。

## `StatusListenerServer` —— 静态管理器

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

**两套独立字典**：

| 字典 | 类型 |
|------|------|
| `serviceMap` | `Dictionary<string, StatusListenerService>` |
| `serviceTMap` | `Dictionary<string, ADataListenerTBase>` |

!!! note "同名 key 在两个 map 里互不冲突"
    `Create("Player")` 与 `Create<PlayerData>("Player")` 是**两个不同的服务**。不要混用同一个名字。

!!! warning "`StatusListenerService.Name` 永远是 `null`"
    `Create(name)` / `GetService(name)` 内部**没有给 `Name` 赋值**（字典 key 才是真名）。见[重构清单](../architecture/refactor-backlog.md#ref-5-statuslistenerservice-name)。

## 推荐用法：按模块持有自己的 listener

框架作者的建议：**每个业务模块持有自己的 `StatusListenerService`**，而不是全局共用一个。

```csharp
public static class PlayerEvents
{
    private static StatusListenerService _svc;
    public static StatusListenerService Svc => _svc ??= StatusListenerServer.Create(nameof(PlayerEvents));

    public static void SetGold(int gold) => Svc.SetData(PlayerData.Gold, gold);
    public static void OnGoldChanged(Action<int> cb) => Svc.AddListener<int>(PlayerData.Gold, cb);
}
```

## 完整示例（`demo_StatusListener`）

`Assets/Code/Game@hotfix/demo_StatusListener/Window_StatusListener.cs` 的实际用法：

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

**参数类型版本**（推荐，避免拆箱）：

```csharp
var s2 = StatusListenerServer.Create(nameof(Msg_Test001.Msg2));

s2.AddListener<Msg_ParamTest>(nameof(Msg_Test001.Msg2), triggerNum: 10,
    action: o => { /* o.test1 */ });

s2.AddListener(Msg_Test001.Msg2, triggerNum: 10,
    action: o => { var _o = o as Msg_ParamTest; });
```

## 与 UFlux 消息机制的分工

| 维度 | `UIMsgData` + `SendMessage` | `AStatusListener` |
|------|---------------------------|-------------------|
| 目标 | 特定窗口 | 全局/具名服务，任意多监听者 |
| 匹配 | 消息类型精确匹配 | `Enum` / `string` / `Type.FullName` |
| 缓存 | 无上限待回放列表 | **上限 20 条** |
| 适用 | 界面内部定向指令 | 跨模块状态广播 |

→ 详见[消息 UIMessage](../ui/ui-message.md)。

## 常见故障

| 现象 | 根因 |
|------|------|
| `设置失败,类型不匹配:...` | 同一 key 前后类型不一致 |
| `暂时无数据,提前监听:<name>` | `AddListener` 时 `dataMap` 还没有该 key（仍会注册，只是提醒） |
| 只收到一次回调 | 用了 `AddListenerOnce` |
| 只收到前 10 次 | `triggerNum: 10` 限制 |
| 回调收不到 | key 不一致（`Enum` vs `string` vs `Type.FullName`）；或用了 `Create` 与 `Create<T>` 混名 |
| 缓存的历史值丢了 | `AStatusListener` 只保留最新 20 条 |
| 日志里 `Name` 是 `null` | 已知问题，`StatusListenerService.Name` 未被赋值 |

## 相关页面

- [消息 UIMessage](../ui/ui-message.md)
- [服务容器与日志](utils.md)
- [Demo 解读](../tutorials/demos.md) —— `demo_StatusListener`、`demo_EventManager`
- [Runtime 模块地图](../architecture/runtime-modules.md)
