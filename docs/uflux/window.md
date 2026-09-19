# 窗口 Window

窗口是 UFlux 的**逻辑层抽象**，不等同于一个 Unity 节点。`AWindow<TP>` 继承 `ATComponent<TP>`，因此**不是 `MonoBehaviour`**。

## 类型体系

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

!!! note "`AWindowBase` 不存在"
    部分旧文档提到 `AWindowBase`，仓库中**没有这个类型**。所有窗口都直接或间接派生自 `AWindow<TP>`。

## `IWindow` 接口

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

## 生命周期

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

!!! warning "`Init()` 在 `Load()` 里，不在 `Open()` 里"
    节点自动赋值（`[TransformPath]` 等）与 `Init()` 都发生在 **`Load()` 阶段**。如果资源是异步加载的，`Init()` 会在异步回调里才执行——不要假设 `ShowWindow` 之后 `Init()` 才跑。

## `UIManager` 完整 API

```csharp
public enum UILayer { Bottom = 0, Center, Top }

public partial class UIManager : ManagerBase<UIManager, UIAttribute>
```

### 初始化与层级

```csharp
public override void Init();
public void Setlayer(IComponent winCom, UILayer layer);
```

`Init()` 中 `GameObject.Find("UIRoot")` 后 `Find("Bottom"/"Center"/"Top")` —— **场景中必须有 `UIRoot` 且其下有这三个同名子节点**。

### 加载 / 卸载

```csharp
public void LoadWindows(Enum[] uiIdxs, UILayer layer = UILayer.Bottom);
public void LoadWindow(Enum uiIndex, UILayer layer = UILayer.Bottom);
public void AsyncLoadWindow(Enum uiIndex, Action callback);                        // 无 layer 参数
public void AsyncLoadWindows(List<int> idxs, Action<int,int> loadProcessAction);   // (total, cur)
public void UnLoadWindows(List<Enum> idxs);
public void UnLoadWindow(Enum index);
public void UnLoadALLWindows();
```

!!! danger "异步加载恒挂 `Bottom` 层"
    `AsyncLoadWindow` 内部硬编码 `SetParent(this.Bottom, false)`，**不接受 `UILayer` 参数**。需要挂到 `Center`/`Top` 的窗口必须用同步 `LoadWindow`。

### 显示 / 关闭

```csharp
public void ShowWindow<T>(UIMsgData uiMsgData = null, bool isAddToHistory = true) where T : IWindow;
public void ShowWindow(Enum uiEnumIdx, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void ShowWindow(Enum uiEnumIdx, UILayer layer, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void CloseWindow(Enum uiEnumIdx);
public void CloseWindow(int uiIdx);
```

**`ShowWindow` 前置条件**：`!winCom.IsOpen && winCom.IsLoad`，否则 `BDebug.LogError("UI处于[unload,lock,open]状态之一")`。内部会先 `Transform.SetAsLastSibling()` 再 `Open()`。

**`ShowWindow` 会自动补加载**：若窗口未 `Load`，会先走 `LoadWindow` 路径。

### 导航历史

```csharp
public List<int> HistoryList { get; private set; }   // 上限 MAX_HISTORY_NUM = 50，去重
public void ClearHistory();
public void Forward();
public void Back();
```

!!! warning "`Back()` 的游标方向存疑"
    `Forward()` / `Back()` 只改游标后调 `ShowWindow(idx, isAddToHistory: false)`，但 `Back()` 内部用的是 `curForwardBackUIIdx++`（与语义相反）。见[重构清单](../architecture/refactor-backlog.md#ref-3-uimanager-back)。

### 消息

```csharp
public void SendMessage(Enum index, UIMsgData uiMsg);
```

### 查询

```csharp
public IWindow GetWindow(Enum uiIndex);
public AStatusListener Status { get; private set; }    // 全局窗口状态监听
```

### DI 扩展（`UIManagerExDI.cs`）

```csharp
public void SetWindowDI(IWindow window);
public void AddSingleton<T>() where T : class;
public void AddSingleton(object inst);
public void AddTransient<T>(T obj) where T : class;
public T GetService<T>(T t) where T : class;
public object GetService(Type type);
```

→ 详见[依赖注入](dependency-injection.md)。

## 窗口缓存模型

| 操作 | `windowMap` | GameObject | `IsOpen` |
|------|-------------|-----------|----------|
| `LoadWindow` | 新增 | 创建，`SetActive(false)` | `false` |
| `ShowWindow` | 不变 | `SetActive(true)` + 置顶 | `true` |
| `CloseWindow` | 不变 | `SetActive(false)` | `false` |
| `UnLoadWindow` | **移除** | `Destroy()` + `Unload(resPath)` | — |

重复 `LoadWindow` 同一窗口只打日志（`已经加载过并未卸载`），**不会重建**。

!!! tip "什么时候需要 `UnLoadWindow`"
    窗口常驻的代价是内存与 `ComponentList` 的持续占用。对**低频、重资源**的界面（大图、复杂列表）应在关闭后 `UnLoadWindow`；高频小界面（弹窗、提示）常驻更划算。

## 消息缓存

窗口未加载时，`SendMessage` 会进 `uiDataCacheMap`；窗口加载完成后由 `PushCaheData(uiIdx)` 一次性回放。

```csharp
// 即使窗口还没加载，消息也不会丢
UIManager.Inst.SendMessage(WinEnum.Detail, new Msg_ShowItem { ItemId = 1001 });
UIManager.Inst.LoadWindow(WinEnum.Detail);   // 加载后自动回放
```

## 窗口注册

用 `[UI(int, string)]` 注册到 `UIManager`：

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

`CreateWindow(int uiIdx)` 的实现路径：

```text
GetClassData(uiIdx)                                   // 从 UIManager 的 ClassData 表取
Activator.CreateInstance(classData.Type, new object[]{ attr.ResourcePath })
SetWindowDI(window)                                   // 反射注入
window.State 挂 4 个监听 → 转发到 UIManager.Status
```

!!! danger "窗口必须有 `(string)` 构造函数"
    `CreateWindow` 用 `Activator.CreateInstance(type, new object[]{ resPath })` 创建实例。自定义构造函数而不保留 `base(path)` 会**运行时报错**。

## 窗口状态监听

`UIManager.Status` 是全局 `AStatusListener`；每个窗口的 `State` 上的 4 个事件会被转发过去：

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

窗口自身的状态变化（`Open`/`Close`/`OnFocus`/`OnBlur`）由 `AWindow<TP>` 内部触发：

```csharp
public virtual void Open(UIMsgData uiMsg = null)
{
    base.Open();                                  // SetActive(true), IsOpen = true
    if (uiMsg != null) SendMessage(uiMsg);        // 先派发消息
    State.TriggerEvent<OnWindowOpen>();           // 再触发状态事件
}
```

## `[UIMessageListener]` 注册

`AWindow<TP>` 的构造函数末尾会调 `RegisterUIMessages()`：

- 反射本类（含继承链）的 `Instance | Public | NonPublic` 方法
- 取带 `[UIMessageListener]` 的
- **形参必须恰好 1 个**，以该参数类型为 key 存入 `msgCallbackMap`

```csharp
[UIMessageListener]
private void OnMsg_Refresh(Msg_Refresh msg) { /* … */ }   // key = typeof(Msg_Refresh)
```

`SendMessage` 按 `uiMsg.GetType()` 精确查表 → 找不到就静默不处理（无日志）。

## 编写一个窗口的完整步骤

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

## 常见故障

| 现象 | 根因 |
|------|------|
| `UI处于[unload,lock,open]状态之一` | 重复 `ShowWindow` 或未 `Load` 就 `Show` |
| 窗口出现但一片空白 | `UIRoot` 下缺 `Bottom`/`Center`/`Top` 子节点 |
| `窗口初始化出错` | `Init()` 或 AutoAssign 内部抛异常（`Load()` 里 `try/catch` 只打日志） |
| `窗口资源不存在` | `[UI]` 的 `resourcePath` 与实际 `Runtime` 目录下路径不符 |
| 节点引用为 null | `[TransformPath]` 路径写错（**只 `BDebug.LogError`，不抛异常**） |
| `[ButtonOnclick]` 报 `未找到Btn:` | 路径没找到 `Button` 组件（**会抛异常**） |

## 相关页面

- [子窗口 SubWindow](sub-window.md)
- [消息 UIMessage](ui-message.md)
- [自动赋值属性](auto-assign-attributes.md)
- [依赖注入](dependency-injection.md)
