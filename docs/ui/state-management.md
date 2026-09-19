# 状态管理 Reducer/Store

UFlux 的 Redux 式状态层，用于**跨界面共享的状态迁移**。

!!! note "命名空间"
    `Store<S>` / `IStore` / `StoreFactory` / `StoreWrapper` / `SubscribeAttribute` 都在 **`BDFramework.UFlux.Contains`**；`AReducers<T>` / `UFluxAction` / `ReducerAttribute` 在 **`BDFramework.UFlux.Reducer`**。写代码时容易漏 `using`。

## 三个概念

| 概念 | 类型 | 职责 |
|------|------|------|
| **State** | `AStateBase` 派生类 | 业务状态数据（纯数据，带脏标记） |
| **Reducer** | `AReducers<T>` 派生类 | 状态迁移函数：`oldState + params → newState`，**无状态** |
| **Store** | `Store<S>` | 持有 State、派发 Action、通知订阅者 |

## `AStateBase`

```csharp
abstract public class AStateBase : IState, IPropertyChange
{
    public Dictionary<string, MemberInfo> MemberinfoMap { get; set; }   // 必须由外部注入
    public bool IsMunalMarkMode { get; private set; } = false;          // 一旦手动标记过就永久为 true
    public int Source { get; set; } = -1;
    public bool IsChanged { get; }                                      // changeProptyList.Count > 0

    public object GetValue(string fieldName);          // 未命中返回 null
    public void SetValue(string fieldName, object o);  // 写入 + SetPropertyChange
    public void SetPropertyChange(string name);        // 进入手动模式 + 记录脏字段
    public void SetAllPropertyChanged();               // 从 StateFactory 缓存取全部字段名
    public string[] GetChangedPropertise();            // ★ 返回并清空
    public string[] GetAllPropertise();                // MemberinfoMap.Keys
    public AStateBase Clone();                         // MemberwiseClone（浅拷贝）
}

public class StateBase : AStateBase { }                // 业务 State 用这个
```

!!! danger "`Clone()` 是浅拷贝"
    `Store` 在每次 Dispatch 前会 `State.Clone()` 得到 `oldState` 传给 Reducer。浅拷贝意味着**引用类型的成员是共享的**——Reducer 里直接改 `state.SomeList` 会**同时污染旧 State 与缓存的历史状态**。

    需要不可变语义时，Reducer 必须自己 new 一个新对象。

!!! note "`StateFactory` 没有 `CreateState<T>()`"
    旧文档提到的 `StateFactory.CreateState<T>()` **不存在**。`StateFactory` 只有两个方法：

    ```csharp
    public static Dictionary<string, MemberInfo> GetMemberinfoCache(Type t);
    public static void AddMemberinfoCache(Type t, Dictionary<string, MemberInfo> map);
    ```

    State 直接用 `new` 创建（`Store<S>` 的构造函数里就是 `new S()`）。

## `AReducers<T>`

```csharp
abstract public class AReducers<T> where T : AStateBase, new()
{
    public delegate bool DispatchDelegate(Enum actionEnum, object @params = null);

    public DispatchDelegate Dispatch { get; set; }    // 桥接 Store.Dispatch，可在 Reducer 内部再派发
    public T State { get; set; }

    public enum ExecuteTypeEnum { None, Sync, Async, Callback }

    public ExecuteTypeEnum GetExecuteType(Enum @enum);
    public T    Excute(Enum @enum, object @params, T oldState);
    public Task<T> ExcuteAsync(Enum @enum, object @params, T oldState);
    public void ExcuteByCallback(Enum @enum, object @params, Store<T>.GetState getStateFunc, Action<T> callback);
}
```

### 三种注册方式

#### ① 委托注册（显式，推荐）

```csharp
public class Reducer_Player : AReducers<State_Player>
{
    public override void Init()
    {
        base.Init();
        // 同步
        RegisterReducer(PlayerAction.AddGold, (state, @params) =>
        {
            state.Gold += (int)@params;
            return state;
        });

        // 异步
        RegisterAsyncReducer(PlayerAction.LoadFromServer, async (state, @params) =>
        {
            state.Gold = await FetchGoldFromServer();
            return state;
        });
    }
}
```

#### ② 方法 + `[Reducer]` 特性

```csharp
public class Reducer_Player : AReducers<State_Player>
{
    [Reducer((int)PlayerAction.AddGold)]
    public State_Player OnAddGold(State_Player state, int amount)
    {
        state.Gold += amount;
        return state;
    }

    [Reducer((int)PlayerAction.Reset)]
    public State_Player OnReset(State_Player state)      // 无形参也支持
    {
        state.Gold = 0;
        return state;
    }
}
```

`[Reducer(int)]` 按 `enum.GetHashCode()` 注册。方法签名必须满足：

- **1 个形参**（`T state`）→ 只在 `@params == null` 时触发
- **2 个形参**（`T state, Xxx params`）→ **`Xxx` 必须与传入 `@params` 的实际类型完全相等**（不是 `is`/可赋值判断）

!!! warning "参数类型必须精确相等"
    `Excute` 里用的是 `methodParams[1].ParameterType == @params.GetType()`。传 `int` 而方法签名是 `object` 或 `long` **都不会匹配**。

    Editor 下会打印失败原因：`触发失败，参数不匹配!同步Reducer:... 形参:X 传入:Y`。

#### ③ 异步 / 回调

```csharp
[AsyncReducer((int)PlayerAction.Load)]
public async Task<State_Player> OnLoad(State_Player state, int playerId)
{
    state.Gold = await Fetch(playerId);
    return state;
}
```

`AsyncReducerAttribute` 在仓库中**只有字段没有构造函数，且未被使用**（见[重构清单](../architecture/refactor-backlog.md#ref-11-empty-types)）。异步请用 `RegisterAsyncReducer` 委托方式。

`Callback` 模式已被标注 `TODO 即将在未来某个版本淘汰`。

## `Store<S>`

```csharp
public class Store<S> : IStore where S : AStateBase, new()
{
    public delegate Task<S> ReducerAsync(S oldState, object @params = null);
    public delegate S       Reducer(S oldState, object @params = null);
    public delegate void    ReducerCallback(GetState getStateFunc, object @params = null, Action<S> callback = null);
    public delegate S       GetState();

    public S State { get; private set; }

    public Store();                                     // State = new S()

    public void AddReducer(AReducers<S> reducer);        // 只接受一个 Reducer，并桥接 Dispatch
    public bool Dispatch(Enum actionEnum, object @params = null);

    public void Subscribe(Action<S> callback);                     // 订阅所有变化
    public void Subscribe(Enum tag, Action<S> callback);           // 只订阅指定 action
    public void Subscribe(int tag, Action<S> callback);
    public void SubscribeWrapper(Action<object> callback);         // IStore 接口用
    public void ScanThisSubscribe(object obj);                     // 扫 [Subscribe] 特性自动注册

    public void SetNewState(Enum reducerEnum, S newState);
    public void UnDo();          // ★ 空实现
    public void CancelUnDo();    // ★ 空实现
}
```

### `Dispatch` 的行为

```csharp
public bool Dispatch(Enum actionEnum, object @params = null)
{
    var type = reducer.GetExecuteType(actionEnum);
    if (type != ExecuteTypeEnum.None)
    {
        var action = new UFluxAction { ActionTag = actionEnum };
        action.SetParams(@params);
        Dispatch(action);
        return true;      // 已处理
    }
    else
    {
        SetNewState(actionEnum, this.State);   // 无对应 Reducer：直接用当前 State 触发回调
    }
    return false;         // 未处理
}
```

!!! tip "`Dispatch` 返回 `false` 不等于失败"
    没有注册 Reducer 的 action 仍会**用当前 State 触发所有订阅回调**（相当于一次"强制刷新"），只是返回 `false`。`StoreWrapper` 正是靠这个返回值做多 Store 路由。

### 状态迁移链路

```text
Dispatch(enum, params)
 ├─ GetExecuteType → Sync / Async / Callback / None
 ├─ oldState = State.Clone()                       ← 浅拷贝
 ├─ newState = reducer.Excute(enum, params, oldState)
 ├─ newState != null → SetNewState(enum, newState)
 │  newState == null → SetNewState(enum, oldState)   ← 回滚语义
 └─ SetNewState
      ├─ stateCacheQueue 缓存旧 State（上限 MaxCacheNumber = 20）
      ├─ State = newState
      └─ DispachCallback(tag)
```

!!! danger "Reducer 返回 `null` 等于"放弃本次变更""
    返回 `null` 时 Store 会把 `oldState` 设回去（即保持原样），但**订阅回调仍会被触发**。如果 Reducer 只是校验失败，这会产生一次无意义的界面刷新。

### 订阅过滤

```csharp
public class SubscribeCallback
{
    public Enum Tag { get; set; } = null;
    public int  IntTag { get; set; } = -9999;
    public bool IsSubscribeAll();          // Tag == null && IntTag == -9999
    public Action<S> Callback { get; set; }
}
```

`DispachCallback(tag)` 的匹配：

```csharp
if (cbw.IsSubscribeAll())
    cbw.Callback.Invoke(State);
else if (tag.GetHashCode() == cbw.IntTag || (cbw.Tag != null && tag.GetHashCode() == cbw.Tag.GetHashCode()))
    cbw.Callback.Invoke(State);
```

### `[Subscribe]` 自动注册

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SubscribeAttribute : Attribute
{
    public Enum SubscribeTag { get; set; }
    public int  SubscribeIntTag { get; set; } = -1;
    public SubscribeAttribute(int @enum);
}
```

```csharp
public class Window_Player : AWindow
{
    public override void Init()
    {
        base.Init();
        _store = StoreFactory.CreateStore(new Reducer_Player());
        _store.ScanThisSubscribe(this);       // 扫描本类的 [Subscribe] 方法
    }

    [Subscribe((int)PlayerAction.AddGold)]
    private void OnGoldChanged(State_Player state) => RefreshGold(state.Gold);
}
```

`ScanThisSubscribe` 的约束：方法必须**恰好 1 个形参且类型为 `S`**，否则 `BDebug.LogError("注册函数参数不对:" + m.Name)`。

## `StoreFactory` 与 `StoreWrapper`

```csharp
static public class StoreFactory
{
    static public Store<T> CreateStore<T>(AReducers<T> reducer) where T : AStateBase, new();
    static public Store<T> GetStore<T>() where T : AStateBase, new();       // ★ 每次返回新实例，不是缓存

    // 多 Store 组合
    static public StoreWrapper CreateStore<A, B>(AReducers<A> r1, AReducers<B> r2);
    static public StoreWrapper CreateStore<A, B, C>(…);
    static public StoreWrapper CreateStore<A, B, C, D>(…);
    static public StoreWrapper CreateStore<A, B, C, D, E>(…);
}
```

!!! warning "`GetStore<T>()` 名字有误导"
    它内部是 `Activator.CreateInstance<Store<T>>()`，**每次调用都创建新实例**，不是"获取缓存的 Store"。真正需要缓存语义时请自己持有引用。

`StoreWrapper`：

```csharp
public class StoreWrapper
{
    public StoreWrapper(params IStore[] stores);

    public void Subscribe<T>(Action<T> action) where T : AStateBase, new();   // 按 State 类型路由
    public void Dispatch(Enum actionEnum, object @params = null);              // 首个返回 true 的 Store 处理，其余跳过
}
```

## 完整示例

```csharp
// ── State ──
public class State_Player : StateBase
{
    public int Gold;
    public int Level;
}

public enum PlayerAction { AddGold = 1, LevelUp = 2 }

// ── Reducer ──
public class Reducer_Player : AReducers<State_Player>
{
    public override void Init()
    {
        base.Init();

        RegisterReducer(PlayerAction.AddGold, (state, @params) =>
        {
            state.Gold += (int)@params;
            return state;
        });

        RegisterReducer(PlayerAction.LevelUp, (state, @params) =>
        {
            state.Level++;
            return state;
        });
    }
}

// ── 使用 ──
public class Window_Player : AWindow
{
    private Store<State_Player> _store;

    public override void Init()
    {
        base.Init();
        _store = StoreFactory.CreateStore(new Reducer_Player());
        _store.Subscribe(PlayerAction.AddGold, s => RefreshGold(s.Gold));
    }

    [ButtonOnclick("btnAddGold")]
    private void OnClickAddGold() => _store.Dispatch(PlayerAction.AddGold, 100);

    private void RefreshGold(int gold) { /* 更新界面 */ }
}
```

## 何时该用 Store

| 场景 | 建议 |
|------|------|
| 单窗口内的局部状态 | **不用 Store**，直接 `AWindow<TP>` + RenderData |
| 多个窗口共享同一份状态 | 用 `Store<S>` |
| 状态迁移有复杂规则（可撤销、可回放） | 用 `Store<S>` + Reducer |
| 需要"变化时通知一堆不相关的模块" | 用 `AStatusListener`（更轻） |

!!! tip "不要为了 Redux 而 Redux"
    引入 Store 的代价是：State 类 + Action 枚举 + Reducer 类 + 订阅绑定四份代码。只有当**状态确实被多处共享**时收益才为正。

## 常见故障

| 现象 | 根因 |
|------|------|
| `未触发同步Reducer:<enum>` | action 枚举与注册的 key 不一致 |
| `触发失败，参数不匹配!` | `[Reducer]` 方法第 2 个形参类型与传入 `@params` 类型**不完全相等** |
| 状态改了但旧值也被改 | `Clone()` 是浅拷贝，Reducer 里改了共享的引用类型成员 |
| 订阅回调触发但界面没变 | Reducer 返回 `null` → Store 回滚到 `oldState` |
| `注册函数参数不对` | `[Subscribe]` 方法的形参不是恰好 1 个、或类型不是 `S` |
| `GetStore<T>()` 拿不到之前的状态 | 它每次返回新实例 |
| `UnDo()` 无效 | 尚未实现（空方法） |

## 相关页面

- [UFlux 架构总览](uflux-overview.md)
- [渲染数据 RenderData](render-data.md)
- [事件总线 EventBus](../api/event-bus.md)
- [Demo 解读](../tutorials/demos.md) —— `demo6_UFlux/06.Window_Reducer`
