# Window

A window is UFlux's **logical-layer abstraction**; it is not the same thing as a Unity node. `AWindow<TP>` inherits `ATComponent<TP>` and is therefore **not a `MonoBehaviour`**.

## Type hierarchy

```csharp
// 无 RenderData 的窗口（最常用）
public class AWindow : AWindow<NoRenderData>
{
    public AWindow(string path) : base(path) { }
    public AWindow(Transform transform) : base(transform) { }
}

// 所有窗口的真实基类（文件名是 AWindowProp.cs）
public class AWindow<TP> : ATComponent<TP>, IWindow, IUIMessage
    where TP : ARenderDataBase, new()
```

!!! note "`AWindowBase` does not exist"
    Some older docs mention `AWindowBase`; **no such type exists** in this repository. Every window derives, directly or indirectly, from `AWindow<TP>`.

## The `IWindow` interface

```csharp
public interface IWindow
{
    IWindow Root { get; set; }                          // 根窗口（SubWindow 链的顶端）
    IWindow Parent { get; set; }                        // 父窗口
    ServiceContainer ServiceContainer { get; }          // 该窗口的 DI 容器（BDFramework.GameServiceStore）
    List<IComponent> ComponentList { get; }             // 挂载的组件
    AStatusListener State { get; }                      // 状态/事件监听器
    bool IsFocus { get; }

    void AddComponent(params IComponent[] coms);
    void SendMessage(UIMsgData uiMsg);
    void Open(UIMsgData uiMsg = null);
    void Close();
    void OnFocus();
    void OnBlur();
    void RegisterSubWindow(IWindow subwin);
    T1 GetSubWindow<T1>() where T1 : class;
}
```

## Lifecycle

```mermaid
sequenceDiagram
    participant U as 业务代码
    participant M as UIManager
    participant W as AWindow

    U->>W: ① new Window_Xxx(resPath)
    Note over W: ATComponent&lt;string&gt; 存路径<br/>RegisterUIMessages() 扫 [UIMessageListener]
    M->>W: ② SetWindowDI(window)
    Note over W: 反射找 "Require" 方法注入服务
    U->>M: ③ LoadWindow(WinEnum.X)
    M->>W: Load() → Instantiate(prefab)
    Note over W: UFluxUtils.InitComponent(this)<br/>执行全部 AutoAssign 属性<br/>调用 virtual Init()
    M->>W: SetActive(false) + Setlayer() + PushCaheData()
    U->>M: ④ ShowWindow(WinEnum.X)
    M->>W: SetAsLastSibling() → Open(uiMsg)
    Note over W: SetActive(true), IsOpen=true<br/>SendMessage(uiMsg)<br/>State.TriggerEvent&lt;OnWindowOpen&gt;()
    U->>M: ⑤ CloseWindow(WinEnum.X)
    M->>W: Close()
    Note over W: IsOpen=false, SetActive(false)<br/>State.TriggerEvent&lt;OnWindowClose&gt;()
    U->>M: ⑥ UnLoadWindow(WinEnum.X)
    M->>W: Close() + Destroy()
    Note over W: UFluxUtils.Destroy(go) + Unload(resPath)<br/>IsDestroy=true, 从 windowMap 移除
```

!!! warning "`Init()` runs inside `Load()`, not inside `Open()`"
    Node auto-assignment (such as `[TransformPath]`) and `Init()` both happen during the **`Load()` phase**. If the asset loads asynchronously, `Init()` only runs inside the async callback — do not assume `Init()` runs after `ShowWindow`.

## Full `UIManager` API

```csharp
public enum UILayer { Bottom = 0, Center, Top }

public partial class UIManager : ManagerBase<UIManager, UIAttribute>
```

### Initialisation and layers

```csharp
public override void Init();
public void Setlayer(IComponent winCom, UILayer layer);
```

`Init()` does `GameObject.Find("UIRoot")` and then `Find("Bottom"/"Center"/"Top")` — **the scene must have a `UIRoot` with those three child nodes**.

### Load / unload

```csharp
public void LoadWindows(Enum[] uiIdxs, UILayer layer = UILayer.Bottom);
public void LoadWindow(Enum uiIndex, UILayer layer = UILayer.Bottom);
public void AsyncLoadWindow(Enum uiIndex, Action callback);                        // 无 layer 参数
public void AsyncLoadWindows(List<int> idxs, Action<int,int> loadProcessAction);   // (total, cur)
public void UnLoadWindows(List<Enum> idxs);
public void UnLoadWindow(Enum index);
public void UnLoadALLWindows();
```

!!! danger "Async loading always lands on the `Bottom` layer"
    `AsyncLoadWindow` hardcodes `SetParent(this.Bottom, false)` and **accepts no `UILayer` parameter**. A window that must sit on `Center`/`Top` has to use the synchronous `LoadWindow`.

### Show / close

```csharp
public void ShowWindow<T>(UIMsgData uiMsgData = null, bool isAddToHistory = true) where T : IWindow;
public void ShowWindow(Enum uiEnumIdx, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void ShowWindow(Enum uiEnumIdx, UILayer layer, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void CloseWindow(Enum uiEnumIdx);
public void CloseWindow(int uiIdx);
```

**`ShowWindow` preconditions**: `!winCom.IsOpen && winCom.IsLoad`, otherwise `BDebug.LogError("UI处于[unload,lock,open]状态之一")`. Internally it calls `Transform.SetAsLastSibling()` before `Open()`.

**`ShowWindow` will auto-load**: if the window is not loaded, it goes through the `LoadWindow` path first.

### Navigation history

```csharp
public List<int> HistoryList { get; private set; }   // 上限 MAX_HISTORY_NUM = 50，去重
public void ClearHistory();
public void Forward();
public void Back();
```

!!! warning "`Back()`'s cursor direction is suspect"
    `Forward()` / `Back()` only move a cursor and then call `ShowWindow(idx, isAddToHistory: false)`, but `Back()` internally uses `curForwardIdx++` (the opposite of its semantics). See the [Refactor Backlog](../architecture/refactor-backlog.md#ref-3-uimanager-back).

### Messages

```csharp
public void SendMessage(Enum index, UIMsgData uiMsg);
```

### Queries

```csharp
public IWindow GetWindow(Enum uiIndex);
public AStatusListener Status { get; private set; }    // 全局窗口状态监听
```

### DI extensions (`UIManagerExDI.cs`)

```csharp
public void SetWindowDI(IWindow window);
public void AddSingleton<T>() where T : class;
public void AddSingleton(object inst);
public void AddTransient<T>(T obj) where T : class;
public T GetService<T>(T t) where T : class;
public object GetService(Type type);
```

→ See [Dependency Injection](dependency-injection.md).

## Window cache model

| Operation | `windowMap` | GameObject | `IsOpen` |
|-----------|-------------|------------|----------|
| `LoadWindow` | Added | Created, `SetActive(false)` | `false` |
| `ShowWindow` | Unchanged | `SetActive(true)` + moved to front | `true` |
| `CloseWindow` | Unchanged | `SetActive(false)` | `false` |
| `UnLoadWindow` | **Removed** | `Destroy()` + `Unload(resPath)` | — |

Calling `LoadWindow` twice for the same window only logs (`已经加载过并未卸载`) and **does not rebuild**.

!!! tip "When to call `UnLoadWindow`"
    Keeping windows resident costs memory and keeps `ComponentList` populated. For **low-frequency, heavy** screens (large images, complex lists) you should `UnLoadWindow` after closing; for high-frequency small screens (popups, toasts) staying resident is the better trade.

## Message cache

When a window is not loaded, `SendMessage` goes into `uiDataCacheMap`; once the window finishes loading, `PushCaheData(uiIdx)` replays it in one go.

```csharp
// 即使窗口还没加载，消息也不会丢
UIManager.Inst.SendMessage(WinEnum.Detail, new Msg_ShowItem { ItemId = 1001 });
UIManager.Inst.LoadWindow(WinEnum.Detail);   // 加载后自动回放
```

## Window registration

Register with `UIManager` using `[UI(int, string)]`:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class UIAttribute : ManagerAttribute
{
    public string ResourcePath { get; }
    public UIAttribute(int intTag, string resourcePath) : base(intTag) { … }
}
```

```csharp
// 业务侧通常定义一个枚举集中管理窗口 ID
public enum WinEnum
{
    Win_DemoMain = 0,
    Win_Demo1,
    Win_Demo5,
    // ...
}

[UI((int)WinEnum.Win_DemoMain, "Windows/Window_DemoMain")]
public class Window_DemoMain : AWindow { … }
```

The implementation path of `CreateWindow(int uiIdx)`:

```text
GetClassData(uiIdx)                                   // 从 UIManager 的 ClassData 表取
Activator.CreateInstance(classData.Type, new object[]{ attr.ResourcePath })
SetWindowDI(window)                                   // 反射注入
window.State 挂 4 个监听 → 转发到 UIManager.Status
```

!!! danger "A window must have a `(string)` constructor"
    `CreateWindow` instantiates via `Activator.CreateInstance(type, new object[]{ resPath })`. Writing a custom constructor without keeping `base(path)` **fails at runtime**.

## Window status listening

`UIManager.Status` is the global `AStatusListener`; four events on every window's `State` are forwarded to it:

```csharp
namespace BDFramework.UFlux.WindowStatus
{
    public class OnWindowOpen  { }
    public class OnWindowClose { }
    public class OnWindowFocus { }
    public class OnWindowBlur  { }
}
```

```csharp
// 监听任意窗口打开
UIManager.Inst.Status.AddListener<OnWindowOpen>(e => { /* … */ });
```

A window's own status changes (`Open`/`Close`/`OnFocus`/`OnBlur`) are triggered inside `AWindow<TP>`:

```csharp
public virtual void Open(UIMsgData uiMsg = null)
{
    base.Open();                                  // SetActive(true), IsOpen = true
    if (uiMsg != null) SendMessage(uiMsg);        // 先派发消息
    State.TriggerEvent<OnWindowOpen>();           // 再触发状态事件
}
```

## `[UIMessageListener]` registration

At the end of `AWindow<TP>`'s constructor, `RegisterUIMessages()` runs:

- Reflects over this class (including the inheritance chain) for `Instance | Public | NonPublic` methods
- Takes those carrying `[UIMessageListener]`
- **The method must have exactly one parameter**; that parameter's type becomes the key in `msgCallbackMap`

```csharp
[UIMessageListener]
private void OnMsg_Refresh(Msg_Refresh msg) { /* … */ }   // key = typeof(Msg_Refresh)
```

`SendMessage` looks up by `uiMsg.GetType()` exactly → if not found it silently does nothing (no log).

## Full walkthrough: writing a window

```csharp
// ① 定义窗口
[UI((int)WinEnum.Shop, "Windows/Window_Shop")]
public class Window_Shop : AWindow
{
    // ② 节点自动赋值
    [TransformPath("bg/title")]
    private Text _title;

    [TransformPath("list")]
    private Transform _listRoot;

    // ③ 事件自动绑定
    [ButtonOnclick("btnClose")]
    private void OnClickClose() => UIManager.Inst.CloseWindow(WinEnum.Shop);

    // ④ 初始化（Load 阶段调用）
    public override void Init()
    {
        base.Init();
        _title.text = "商店";
    }

    // ⑤ 消息接收
    [UIMessageListener]
    private void OnMsg_OpenShop(Msg_OpenShop msg) => Refresh(msg.ShopId);
}
```

```csharp
// ⑥ 使用
UIManager.Inst.LoadWindow(WinEnum.Shop, UILayer.Center);
UIManager.Inst.ShowWindow(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

## Common failures

| Symptom | Root cause |
|---------|-----------|
| `UI处于[unload,lock,open]状态之一` | `ShowWindow` called twice, or `Show` before `Load` |
| Window appears but is blank | `UIRoot` is missing its `Bottom`/`Center`/`Top` children |
| `窗口初始化出错` | An exception inside `Init()` or AutoAssign (`Load()` has a `try/catch` that only logs) |
| `窗口资源不存在` | `[UI]`'s `resourcePath` does not match the real path under the `Runtime` directory |
| Node reference is null | A wrong `[TransformPath]` path (**only `BDebug.LogError`, no throw**) |
| `[ButtonOnclick]` reports `未找到Btn:` | The path has no `Button` component (**this one does throw**) |

## Related pages

- [SubWindow](sub-window.md)
- [Messages (UIMessage)](ui-message.md)
- [Auto-Assign Attributes](auto-assign-attributes.md)
- [Dependency Injection](dependency-injection.md)
