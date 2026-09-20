# Reducer / Store 状态管理参考

> 来源：`Runtime/UI/State/**`、`Runtime/UI/StateManager/**`

## 命名空间

| 类型 | 命名空间 |
|------|---------|
| `AStateBase` / `StateBase` / `IState` / `IPropertyChange` / `StateFactory` | `BDFramework.UFlux` |
| `AReducers<T>` / `UFluxAction` / `ReducerAttribute` / `AsyncReducerAttribute` | `BDFramework.UFlux.Reducer` |
| `Store<S>` / `IStore` / `StoreFactory` / `StoreWrapper` / `SubscribeAttribute` | `BDFramework.UFlux.Contains` |

!!! danger "`Store<S>` 在 `BDFramework.UFlux.Contains`"
    不在 `BDFramework.UFlux`。这是最高频的 `using` 遗漏点。

## `AStateBase`

```csharp
abstract public class AStateBase : IState, IPropertyChange
{
    public Dictionary<string, MemberInfo> MemberinfoMap { get; set; }   // 必须由外部注入
    public bool IsMunalMarkMode { get; private set; } = false;
    public int  Source { get; set; } = -1;
    public bool IsChanged { get; }                                       // changeProptyList.Count > 0

    public object GetValue(string fieldName);          // 未命中返回 null
    public void SetValue(string fieldName, object o);  // 写入 + SetPropertyChange
    public void SetPropertyChange(string name);        // 进入手动模式 + 记录脏字段
    public void SetAllPropertyChanged();
    public string[] GetChangedPropertise();            // ★ 返回并清空
    public string[] GetAllPropertise();
    public AStateBase Clone();                         // MemberwiseClone（浅拷贝）
}

public class StateBase : AStateBase { }
```

!!! note "`StateFactory` 没有 `CreateState<T>()`"
    旧文档提到的 `StateFactory.CreateState<T>()` **不存在**。`StateFactory` 只有两个方法：

    ```csharp
    public static Dictionary<string, MemberInfo> GetMemberinfoCache(Type t);
    public static void AddMemberinfoCache(Type t, Dictionary<string, MemberInfo> map);
    ```

    State 直接用 `new`（`Store<S>` 构造函数里就是 `new S()`）。

!!! danger "`Clone()` 是浅拷贝"
    `Store` 每次 Dispatch 前 `State.Clone()` 得到 `oldState` 传给 Reducer。**引用类型成员是共享的**：

    ```csharp
    // ✗ 会同时污染 oldState 与缓存的历史状态
    public State_X OnAdd(State_X state, int n) { state.MyList.Add(n); return state; }

    // ✓ 需要不可变语义时自己 new
    public State_X OnAdd(State_X state, int n)
    {
        var next = new State_X { MyList = new List<int>(state.MyList) };
        next.MyList.Add(n);
        return next;
    }
    ```

## `AReducers<T>`

```csharp
abstract public class AReducers<T> where T : AStateBase, new()
{
    public delegate bool DispatchDelegate(Enum actionEnum, object @params = null);

    public DispatchDelegate Dispatch { get; set; }    // 桥接 Store.Dispatch，可在 Reducer 内部再派发
    public T State { get; set; }

    public enum ExecuteTypeEnum { None, Sync, Async, Callback }

    public ExecuteTypeEnum GetExecuteType(Enum @enum);
    public T       Excute(Enum @enum, object @params, T oldState);
    public Task<T> ExcuteAsync(Enum @enum, object @params, T oldState);
    public void    ExcuteByCallback(Enum @enum, object @params,
                                    Store<T>.GetState getStateFunc, Action<T> callback);
}
```

### 执行模式判定

`GetExecuteType(enum)` 按以下顺序返回**第一个命中**：

```text
① ReducersMap[key]           或 ReducersMethodMap[enum.GetHashCode()]         → Sync
② AsyncReducersMap[key]      或 AsyncReducersMethodMap[enum.GetHashCode()]    → Async
③ CallbackReducersMap[key]   或 CallbackReducersMethodMap[enum.GetHashCode()] → Callback
④ 都不命中                                                                     → None
```

### 三种注册方式

#### ① 委托（显式）

```csharp
public class Reducer_Hero : AReducers<State_Hero>
{
    public override void Init()
    {
        base.Init();
        RegisterReducer(HeroAction.Refresh, (state, @params) => { state.Hp = (int)@params; return state; });
        RegisterAsyncReducer(HeroAction.Load, async (state, @params) => { state.Name = await Fetch(); return state; });
    }
}
```

#### ② `[Reducer(int)]` 特性

```csharp
public class Reducer_Hero : AReducers<State_Hero>
{
    [Reducer((int)HeroAction.Refresh)]
    public State_Hero OnRefresh(State_Hero state, int hp)     // 2 形参
    {
        state.Hp = hp;
        return state;
    }

    [Reducer((int)HeroAction.Reset)]
    public State_Hero OnReset(State_Hero state)               // 1 形参
    {
        state.Hp = 0;
        return state;
    }
}
```

**方法签名匹配规则**（`Excute` 内部）：

| 形参数量 | 触发条件 |
|---------|---------|
| 1（`T state`） | `@params == null` |
| 2（`T state, Xxx params`） | **`methodParams[1].ParameterType == @params.GetType()`（精确相等）** |

!!! danger "类型必须精确相等，不是 `is` / 可赋值判断"
    ```csharp
    store.Dispatch(HeroAction.Refresh, 80);        // @params.GetType() == typeof(int)

    [Reducer(...)] State_Hero A(State_Hero s, int  n) { }    // ✓ 命中
    [Reducer(...)] State_Hero B(State_Hero s, long n) { }    // ✗ 不命中
    [Reducer(...)] State_Hero C(State_Hero s, object n) { }  // ✗ 不命中
    ```

    同一个 tag 可以挂多个方法（`ReducersMethodMap` 是 `List<MethodInfo>`），靠形参类型区分。Editor 下不匹配会打印：

    ```text
    触发失败，参数不匹配!同步Reducer:<类>.<方法> , 形参:<X> 传入:<Y>
    ```

#### ③ 异步

```csharp
[Reducer((int)HeroAction.Load)]
async public Task<State_Hero> OnLoad(State_Hero state)
{
    state.Name = await Fetch();
    return state;
}
```

!!! warning "`AsyncReducerAttribute` 不可用"
    `AsyncReducerAttribute` 在仓库中**只有字段、没有构造函数，且未被使用**。异步请用 `RegisterAsyncReducer` 委托方式。

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

    public Store();                                          // State = new S()

    public void AddReducer(AReducers<S> reducer);             // ★ 只接受一个；内部桥接 reducer.Dispatch
    public bool Dispatch(Enum actionEnum, object @params = null);

    public void Subscribe(Action<S> callback);                // 订阅所有变化
    public void Subscribe(Enum tag, Action<S> callback);      // 只订阅指定 action
    public void Subscribe(int tag, Action<S> callback);
    public void SubscribeWrapper(Action<object> callback);    // IStore 接口用
    public void ScanThisSubscribe(object obj);                // 扫 [Subscribe] 自动注册

    public void SetNewState(Enum reducerEnum, S newState);
    public void UnDo();          // ★ 空实现
    public void CancelUnDo();    // ★ 空实现
}
```

### `Dispatch` 语义

```csharp
public bool Dispatch(Enum actionEnum, object @params = null)
{
    var type = reducer.GetExecuteType(actionEnum);
    if (type != ExecuteTypeEnum.None)
    {
        var action = new UFluxAction { ActionTag = actionEnum };
        action.SetParams(@params);
        Dispatch(action);
        return true;                                  // 已处理
    }
    else
    {
        SetNewState(actionEnum, this.State);          // ★ 无 Reducer 也会触发回调
    }
    return false;                                     // 未处理
}
```

!!! tip "返回 `false` 不等于失败"
    没有注册 Reducer 的 action 仍会用**当前 State** 触发所有订阅回调（相当于强制刷新）。`StoreWrapper` 正是靠返回值做多 Store 路由。

### 状态迁移链路

```text
Dispatch(enum, params)
 ├─ GetExecuteType
 ├─ oldState = State.Clone()                                ← 浅拷贝
 ├─ newState = reducer.Excute(enum, params, oldState)
 ├─ newState != null → SetNewState(enum, newState)
 │  newState == null → SetNewState(enum, oldState)          ← 回滚语义
 └─ SetNewState
      ├─ stateCacheQueue.Enqueue(this.State)（上限 MaxCacheNumber = 20）
      ├─ State = newState
      └─ DispachCallback(tag)
```

!!! danger "Reducer 返回 `null` = 放弃变更，但回调仍会触发"
    会产生一次无意义的界面刷新。如果只是校验失败，考虑在 Reducer 内部提前判断而不是返回 `null`。

### 订阅过滤

```csharp
public class SubscribeCallback
{
    public Enum Tag    { get; set; } = null;
    public int  IntTag { get; set; } = -9999;
    public bool IsSubscribeAll();                    // Tag == null && IntTag == -9999
    public Action<S> Callback { get; set; }
}
```

```csharp
public void DispachCallback(Enum tag = null)
{
    for (int i = 0; i < SubscribeCallbackList.Count; i++)
    {
        var cbw = SubscribeCallbackList[i];
        if (cbw.IsSubscribeAll())
            cbw.Callback.Invoke(this.State);
        else if (tag.GetHashCode() == cbw.IntTag
              || (cbw.Tag != null && tag.GetHashCode() == cbw.Tag.GetHashCode()))
            cbw.Callback.Invoke(this.State);
    }
}
```

### `[Subscribe]` 自动注册

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SubscribeAttribute : Attribute
{
    public Enum SubscribeTag    { get; set; }
    public int  SubscribeIntTag { get; set; } = -1;
    public SubscribeAttribute(int @enum);
}
```

```csharp
store.ScanThisSubscribe(this);

[Subscribe((int)HeroAction.Refresh)]
private void OnRefreshed(State_Hero state) => RefreshUI(state.Hp);
```

**约束**：方法必须**恰好 1 个形参且类型为 `S`**，否则 `BDebug.LogError("注册函数参数不对:" + m.Name)`。

## `StoreFactory` 与 `StoreWrapper`

```csharp
static public class StoreFactory
{
    static public Store<T> CreateStore<T>(AReducers<T> reducer) where T : AStateBase, new();
    static public Store<T> GetStore<T>() where T : AStateBase, new();

    static public StoreWrapper CreateStore<A, B>(AReducers<A> r1, AReducers<B> r2);
    static public StoreWrapper CreateStore<A, B, C>(…);
    static public StoreWrapper CreateStore<A, B, C, D>(…);
    static public StoreWrapper CreateStore<A, B, C, D, E>(…);
}
```

!!! warning "`GetStore<T>()` 名字有误导"
    内部是 `Activator.CreateInstance<Store<T>>()` —— **每次调用都创建新实例**，不是"获取缓存"。需要缓存语义请自己持有引用。

```csharp
public class StoreWrapper
{
    public StoreWrapper(params IStore[] stores);

    public void Subscribe<T>(Action<T> action) where T : AStateBase, new();   // 按 State 类型路由
    public void Dispatch(Enum actionEnum, object @params = null);              // 首个返回 true 的 Store 处理
}
```

## 何时该用 Store

| 场景 | 建议 |
|------|------|
| 单窗口内的局部状态 | **不用 Store**，`AWindow<TP>` + RenderData 即可 |
| 多个窗口共享同一份状态 | 用 `Store<S>` |
| 状态迁移有复杂规则（可撤销、可回放） | 用 `Store<S>` + Reducer |
| 只需"变化时通知一堆不相关的模块" | 用 `AStatusListener`（更轻） |

!!! tip "不要为了 Redux 而 Redux"
    引入 Store 的代价：State 类 + Action 枚举 + Reducer 类 + 订阅绑定**四份代码**。只有状态确实被多处共享时收益才为正。

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | `using BDFramework.UFlux.Contains;` 漏写 |
| 2 | `[Reducer]` 第 2 形参类型与 `@params` 类型不精确相等 |
| 3 | Reducer 返回 `null` 导致回滚，但订阅回调仍触发 |
| 4 | `Clone()` 浅拷贝 → Reducer 改引用成员会污染 `oldState` 与历史缓存 |
| 5 | `AddReducer` **只接受一个** Reducer（`if (this.reducer == null)` 判断） |
| 6 | `GetStore<T>()` 每次返回新实例 |
| 7 | `UnDo()` / `CancelUnDo()` 是空实现（`stateCacheQueue` 只是预留） |
| 8 | `[Subscribe]` 方法形参不是恰好 1 个 / 类型不是 `S` |
| 9 | `Store.Dispatch` 返回 `false` 时仍触发了回调 |
| 10 | 同名 tag 挂多个方法时，靠形参类型区分 —— 类型写错就静默不触发 |
