# State Management (Reducer/Store)

UFlux's Redux-style state layer, used for **state transitions shared across screens**.

!!! note "Namespaces"
    `Store<S>` / `IStore` / `StoreFactory` / `StoreWrapper` / `SubscribeAttribute` all live in **`BDFramework.UFlux.Contains`**; `AReducers<T>` / `UFluxAction` / `ReducerAttribute` live in **`BDFramework.UFlux.Reducer`**. It is easy to forget the `using` when writing code.

## Three concepts

| Concept | Type | Responsibility |
|------|------|------|
| **State** | A class derived from `AStateBase` | Business state data (pure data, with dirty marking) |
| **Reducer** | A class derived from `AReducers<T>` | The state transition function: `oldState + params → newState`, **stateless** |
| **Store** | `Store<S>` | Holds the State, dispatches Actions, notifies subscribers |

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

!!! danger "`Clone()` is a shallow copy"
    Before every Dispatch, `Store` calls `State.Clone()` to produce the `oldState` handed to the Reducer. A shallow copy means **reference-type members are shared** — mutating `state.SomeList` directly inside a Reducer **pollutes both the old State and the cached historical states**.

    When you need immutable semantics, the Reducer must create a new object itself.

!!! note "`StateFactory` has no `CreateState<T>()`"
    The `StateFactory.CreateState<T>()` mentioned in older documentation **does not exist**. `StateFactory` has only two methods:

    ```csharp
    public static Dictionary<string, MemberInfo> GetMemberinfoCache(Type t);
    public static void AddMemberinfoCache(Type t, Dictionary<string, MemberInfo> map);
    ```

    States are created directly with `new` (`Store<S>`'s constructor simply does `new S()`).

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

### Three ways to register

#### ① Delegate registration (explicit, recommended)

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

#### ② Method + `[Reducer]` attribute

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

`[Reducer(int)]` registers by `enum.GetHashCode()`. The method signature must satisfy:

- **1 parameter** (`T state`) → only triggered when `@params == null`
- **2 parameters** (`T state, Xxx params`) → **`Xxx` must be exactly equal to the runtime type of the `@params` passed in** (not an `is` / assignability check)

!!! warning "Parameter types must be exactly equal"
    `Excute` uses `methodParams[1].ParameterType == @params.GetType()`. Passing an `int` when the signature says `object` or `long` **will not match**.

    In the Editor the failure reason is printed: `触发失败，参数不匹配!同步Reducer:... 形参:X 传入:Y`.

#### ③ Async / callback

```csharp
[AsyncReducer((int)PlayerAction.Load)]
public async Task<State_Player> OnLoad(State_Player state, int playerId)
{
    state.Gold = await Fetch(playerId);
    return state;
}
```

`AsyncReducerAttribute` in this repository **has only fields, no constructor, and is unused** (see the [Refactor Backlog](../architecture/refactor-backlog.md#ref-11-empty-types)). For async work use the `RegisterAsyncReducer` delegate form.

The `Callback` mode is marked `TODO 即将在未来某个版本淘汰`.

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

### What `Dispatch` does

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

!!! tip "`Dispatch` returning `false` is not a failure"
    An action with no registered Reducer still **triggers all subscription callbacks with the current State** (equivalent to one "forced refresh") and merely returns `false`. `StoreWrapper` relies on this return value to route across multiple Stores.

### The state transition chain

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

!!! danger "A Reducer returning `null` abandons the change"
    When `null` is returned the Store sets `oldState` back (i.e. leaves everything as it was), but **the subscription callbacks are still triggered**. If the Reducer merely failed a validation, this produces a pointless screen refresh.

### Subscription filtering

```csharp
public class SubscribeCallback
{
    public Enum Tag { get; set; } = null;
    public int  IntTag { get; set; } = -9999;
    public bool IsSubscribeAll();          // Tag == null && IntTag == -9999
    public Action<S> Callback { get; set; }
}
```

The matching inside `DispachCallback(tag)`:

```csharp
if (cbw.IsSubscribeAll())
    cbw.Callback.Invoke(State);
else if (tag.GetHashCode() == cbw.IntTag || (cbw.Tag != null && tag.GetHashCode() == cbw.Tag.GetHashCode()))
    cbw.Callback.Invoke(State);
```

### `[Subscribe]` auto-registration

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

`ScanThisSubscribe`'s constraint: the method must have **exactly one parameter of type `S`**, otherwise `BDebug.LogError("注册函数参数不对:" + m.Name)`.

## `StoreFactory` and `StoreWrapper`

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

!!! warning "`GetStore<T>()` is misleadingly named"
    Internally it is `Activator.CreateInstance<Store<T>>()`, so **every call creates a new instance** — it does not retrieve a cached Store. When you really need cache semantics, hold the reference yourself.

`StoreWrapper`:

```csharp
public class StoreWrapper
{
    public StoreWrapper(params IStore[] stores);

    public void Subscribe<T>(Action<T> action) where T : AStateBase, new();   // 按 State 类型路由
    public void Dispatch(Enum actionEnum, object @params = null);              // 首个返回 true 的 Store 处理，其余跳过
}
```

## Full example

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

## When to use a Store

| Scenario | Recommendation |
|------|------|
| Local state inside a single window | **Skip the Store**; use `AWindow<TP>` + RenderData directly |
| Several windows sharing one piece of state | Use `Store<S>` |
| Complex state transition rules (undoable, replayable) | Use `Store<S>` + Reducer |
| You need to notify a crowd of unrelated modules when something changes | Use `AStatusListener` (lighter) |

!!! tip "Do not do Redux for Redux's sake"
    Adopting a Store costs four pieces of code: a State class, an Action enum, a Reducer class and the subscription bindings. The payoff is only positive when **the state really is shared in several places**.

## Common failures

| Symptom | Root cause |
|------|------|
| `未触发同步Reducer:<enum>` | The action enum does not match the registered key |
| `触发失败，参数不匹配!` | The `[Reducer]` method's second parameter type is **not exactly equal** to the `@params` type passed in |
| The state changed but the old value changed too | `Clone()` is a shallow copy and the Reducer mutated a shared reference-type member |
| The subscription fires but the screen does not change | The Reducer returned `null`, so the Store rolled back to `oldState` |
| `注册函数参数不对` | The `[Subscribe]` method does not take exactly one parameter, or its type is not `S` |
| `GetStore<T>()` does not return the previous state | It returns a new instance every time |
| `UnDo()` has no effect | Not implemented yet (an empty method) |

## Related pages

- [UFlux Overview](uflux-overview.md)
- [RenderData](render-data.md)
- [Event Bus](../api/event-bus.md)
- [Demo Walkthrough](../tutorials/demos.md) — `demo6_UFlux/06.Window_Reducer`
