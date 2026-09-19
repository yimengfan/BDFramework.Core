# 窗口与组件生命周期参考

> 来源：`Runtime/UI/View/UIManager/**`、`Runtime/UI/View/Windows/**`、`Runtime/UI/View/Component/**`

## `UIManager`

```csharp
public enum UILayer { Bottom = 0, Center, Top }

public partial class UIManager : ManagerBase<UIManager, UIAttribute>
```

### 初始化

```csharp
public override void Init();
public void Setlayer(IComponent winCom, UILayer layer);
```

`Init()` 中 `GameObject.Find("UIRoot")` 后 `Find("Bottom"/"Center"/"Top")`。

!!! danger "场景必须有 `UIRoot` 且其下有 `Bottom` / `Center` / `Top` 三个同名子节点"
    缺失时层级挂载会失败（表现为窗口不显示或位置错误）。

### 加载 / 卸载

```csharp
public void LoadWindows(Enum[] uiIdxs, UILayer layer = UILayer.Bottom);
public void LoadWindow(Enum uiIndex, UILayer layer = UILayer.Bottom);
public void AsyncLoadWindow(Enum uiIndex, Action callback);                          // ★ 无 layer 参数
public void AsyncLoadWindows(List<int> idxs, Action<int,int> loadProcessAction);     // (total, cur)
public void UnLoadWindows(List<Enum> idxs);
public void UnLoadWindow(Enum index);
public void UnLoadALLWindows();
```

!!! danger "`AsyncLoadWindow` 恒挂 `Bottom` 层"
    内部硬编码 `SetParent(this.Bottom, false)`，**不接受 `UILayer`**。需要挂到 `Center`/`Top` 的窗口必须用同步 `LoadWindow`。

### 显示 / 关闭

```csharp
public void ShowWindow<T>(UIMsgData uiMsgData = null, bool isAddToHistory = true) where T : IWindow;
public void ShowWindow(Enum uiEnumIdx, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void ShowWindow(Enum uiEnumIdx, UILayer layer, UIMsgData uiMsgData = null, bool isAddToHistory = true);
public void CloseWindow(Enum uiEnumIdx);
public void CloseWindow(int uiIdx);
```

**前置条件**：`!winCom.IsOpen && winCom.IsLoad`，否则 `BDebug.LogError("UI处于[unload,lock,open]状态之一")`。

内部顺序：`Transform.SetAsLastSibling()` → `win.Open(uiMsgData)`。

**自动补加载**：若窗口未 `Load`，`ShowWindow` 会先走 `LoadWindow` 路径。

### 历史导航

```csharp
public List<int> HistoryList { get; private set; }   // MAX_HISTORY_NUM = 50，去重（先 Remove 再 Add）
public void ClearHistory();
public void Forward();
public void Back();
```

!!! warning "`Back()` 的游标方向存疑"
    `Back()` 内部用的是 `curForwardBackUIIdx++`（与语义相反）。已在重构清单中记录。

### 其他

```csharp
public void SendMessage(Enum index, UIMsgData uiMsg);
public IWindow GetWindow(Enum uiIndex);
public AStatusListener Status { get; private set; }      // 全局窗口状态监听
```

### DI 扩展

```csharp
public void SetWindowDI(IWindow window);
public void AddSingleton<T>() where T : class;
public void AddSingleton(object inst);
public void AddTransient<T>(T obj) where T : class;
public T GetService<T>(T t) where T : class;
public object GetService(Type type);
```

`SetWindowDI` 实现：

```csharp
var mi = type.GetMethod("Require");              // ★ 必须 public
if (mi == null) return;

var @params = mi.GetParameters();
if (@params.Length > 0)                          // ★ 形参为 0 时不执行
{
    object[] paramsObjs = new object[@params.Length];
    for (int j = 0; j < @params.Length; j++)
        paramsObjs[j] = this.GetService(@params[j].ParameterType);
    mi.Invoke(window, paramsObjs);
}
```

`GetService` 解析顺序（**精确类型比较**）：

```text
① singletonList.FindLast(o => o.GetType() == type)
② transientList.FindLast(t => t == type) → Activator.CreateInstance(t)
③ 都未命中 → 返回 null（不抛异常）
```

!!! danger "`AddTransient<T>(T obj)` 会丢弃传入的实例"
    ```csharp
    public void AddTransient<T>(T obj) where T : class
    {
        var type = obj.GetType();        // 只用类型
        this.transientList.Add(type);    // obj 被丢弃
    }
    ```
    每次 `GetService` 都 `Activator.CreateInstance`（要求无参构造）。要注入有状态实例请用 `AddSingleton(inst)`。

## 窗口缓存模型

| 操作 | `windowMap` | GameObject | `IsOpen` |
|------|------------|-----------|----------|
| `LoadWindow` | 新增 | 创建，`SetActive(false)` | `false` |
| `ShowWindow` | 不变 | `SetActive(true)` + 置顶 | `true` |
| `CloseWindow` | 不变 | `SetActive(false)` | `false` |
| `UnLoadWindow` | **移除** | `Destroy()` + `Unload(resPath)` | — |

重复 `LoadWindow` 只打日志（`已经加载过并未卸载`），**不重建**。

## 窗口创建

```csharp
private IWindow CreateWindow(int uiIdx)
{
    var classData = GetClassData(uiIdx);
    var window = Activator.CreateInstance(classData.Type, new object[]{ attr.ResourcePath }) as IWindow;
    SetWindowDI(window);
    // 给 window.State 挂 4 个监听转发到 UIManager.Status
}
```

!!! danger "窗口必须有 `(string)` 构造函数"
    `Activator.CreateInstance(type, new object[]{ resPath })` 要求存在 `(string)` 构造。自定义构造而不保留 `base(path)` 会**运行时报错**。

## 完整生命周期

```text
① new Window_Xxx(resPath)
     ATComponent<string>(resPath)：存 resPath + RenderData = new TP()
     RegisterUIMessages()：扫 [UIMessageListener]

② UIManager.SetWindowDI(window)                        ← 构造后、Load 前
     反射 "Require" 注入服务

③ LoadWindow(WinEnum.X)
     Load()
       → Instantiate(prefab)
       → UFluxUtils.InitComponent(this)                ← 执行全部 AutoAssign 属性
       → Init()                                        ← 业务覆写（try/catch）
     SetActive(false)
     Setlayer(winCom, layer)
     PushCaheData(uiIdx)                               ← 回放缓存的 UIMsgData

④ ShowWindow(WinEnum.X)
     Transform.SetAsLastSibling()
     Open(uiMsg)
       → base.Open()：SetActive(true), IsOpen = true
       → SendMessage(uiMsg)                            ← 先派发消息
       → State.TriggerEvent<OnWindowOpen>()            ← 再触发状态事件

⑤ CloseWindow(WinEnum.X)
     Close()
       → IsOpen = false, SetActive(false)
       → State.TriggerEvent<OnWindowClose>()

⑥ UnLoadWindow(WinEnum.X)
     Close() + Destroy()
       → UFluxUtils.Destroy(go) + Unload(resPath)
       → IsDestroy = true，从 windowMap 移除
```

!!! warning "`Init()` 在 `Load()` 里，不在 `Open()` 里"
    节点赋值与 `Init()` 都发生在 **`Load()` 阶段**。异步加载时 `Init()` 会在异步回调里才执行——不要假设 `ShowWindow` 之后 `Init()` 才跑。

## `IWindow`

```csharp
public interface IWindow
{
    IWindow Root { get; set; }
    IWindow Parent { get; set; }
    ServiceContainer ServiceContainer { get; }          // 每个窗口独立的 DI 容器
    List<IComponent> ComponentList { get; }
    AStatusListener State { get; }
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

`AddComponent`：

```csharp
foreach (var com in coms)
{
    com.Parent = this;
    ComponentList.Add(com);
}
```

!!! warning "`AddComponent` 不调 `Init()`"
    只设 `Parent` 并加入列表。`Init()` 由构造（`Transform` 版本）或 `Load()` 触发。

## `IComponent` / `ATComponent<T>`

```csharp
public interface IComponent
{
    IWindow Parent { get; set; }
    Transform Transform { get; }
    bool IsLoad { get; }
    bool IsOpen { get; }
    bool IsDestroy { get; }
    void Load();
    void AsyncLoad(Action callback);
    void Init();
    void Open(UIMsgData uiMsg = null);
    void Close();
    void Destroy();
    void SetRenderData(ARenderDataBase renderDataBase);
}

abstract public class ATComponent<T> : IComponent where T : ARenderDataBase, new()
{
    public T RenderData { get; private set; }
    public IWindow Parent { get; set; }
    public Transform Transform { get; private set; }
    public bool IsLoad { get; private set; } = false;
    public bool IsOpen { get; private set; } = false;
    public bool IsDestroy { get; private set; } = false;

    public ATComponent(bool isLoadAsset = true);
    public ATComponent(Transform trans);
    public ATComponent(string resPath);

    public void Load();
    public void AsyncLoad(Action callback = null);
    public void SetRenderData(T props);
    public void SetRenderData(ARenderDataBase renderDataBase);
    protected void CommitRenderData(Transform transform = null);
    virtual public void Init();
    virtual public void Open(UIMsgData uiMsg = null);
    virtual public void Close();
    virtual public void Destroy();
}
```

### 三种构造语义

| 构造 | 加载资源 | 立即 `Init()` | 场景 |
|------|---------|--------------|------|
| `(bool isLoadAsset = true)` | 按 `[Component]` 决定 | `Load()` 内部调 | 自加载型 |
| `(Transform trans)` | ✗ 接管既有节点 | ✓ 构造末尾 `InitComponent(this)` | 绑定场景已有节点 |
| `(string resPath)` | ✗ 只记路径 | ✗ | 延迟加载 |

### `Load()` / `AsyncLoad()` 差异

```csharp
public void Load()
{
    if (resPath == null) return;
    // UFluxUtils.Load<GameObject> → Instantiate
    IsLoad = true;
    UFluxUtils.InitComponent(this);
    Init();                              // try/catch，异常只打日志 "窗口初始化出错"
}

public void AsyncLoad(Action callback = null)
{
    // resPath 为空 → 抛 "窗口资源不存在"
    // UFluxUtils.AsyncLoad<GameObject>(...)
    callback?.Invoke();                  // ★ 异常路径也会调用
}
```

!!! danger "`AsyncLoad` 的回调在失败路径也会执行"
    回调里**必须检查 `IsLoad`**，否则会对着 `null` 的 `Transform` 操作。

### `Destroy()`

```csharp
public void Destroy()
{
    UFluxUtils.Destroy(go);
    Transform = null;
    UFluxUtils.Unload(resPath);
    IsDestroy = true;
}
```

只 `Close()` 不会释放资源——需要 `UnLoadWindow` / `Destroy()`。

## 消息机制

```csharp
public abstract class UIMsgData
{
    public T GetMsg<T>() { return (T)this; }
}
```

!!! danger "`UIMsgData` 是 `abstract`"
    必须定义具体子类。**匹配 key 是运行时类型**（`uiMsg.GetType()`），不做基类匹配。

### 注册

`AWindow<TP>` 构造函数末尾 `RegisterUIMessages()`：

```text
反射本类（含继承）的 Instance | Public | NonPublic 方法
取带 [UIMessageListener] 的
形参必须恰好 1 个
以参数类型为 key 存入 msgCallbackMap
```

### 派发

```csharp
public void SendMessage(UIMsgData uiMsg)
{
    // ① 查 msgCallbackMap 派发给自己
    if (msgCallbackMap.TryGetValue(uiMsg.GetType(), out var method))
        method.Invoke(this, new object[] { uiMsg });

    // ② 递归转发给所有子窗口
    foreach (var sub in subWindowsMap.Values)
        sub.SendMessage(uiMsg);
}
```

**未注册则静默忽略**（无日志）。转发方向是**父 → 子单向**。

### 消息缓存

```csharp
private Dictionary<int, List<UIMsgData>> uiDataCacheMap;   // 窗口未加载时的缓存
private void PushCaheData(int uiIdx);                       // 加载后回放
```

!!! danger "缓存没有上限"
    `uiDataCacheMap` 是 `List<UIMsgData>`，**无条数限制**。对未加载窗口高频 `SendMessage` 会导致内存持续增长。高频数据请走 `AStatusListener`。

## 窗口状态消息

```csharp
namespace BDFramework.UFlux.WindowStatus

public class OnWindowOpen  { }
public class OnWindowClose { }
public class OnWindowFocus { }
public class OnWindowBlur  { }
```

每个窗口的 `State` 上的这 4 个事件会被 `CreateWindow` 转发到 `UIManager.Status`：

```csharp
UIManager.Inst.Status.AddListener<OnWindowOpen>(e => { /* 任意窗口打开 */ });
```

## 子窗口

```csharp
public void RegisterSubWindow(IWindow subwin)
{
    subWindowsMap[subwin.GetHashCode()] = subwin;              // ★ key 是对象哈希
    subwin.Root   = this.Root != null ? this.Root : this;
    subwin.Parent = this;
    (subwin as IComponent).Init();                             // ★ 注册时立刻 Init
}

public T1 GetSubWindow<T1>() where T1 : class                  // 线性遍历取第一个 is T1
```

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | 窗口缺 `(string)` 构造函数 |
| 2 | `UIRoot` 下缺 `Bottom`/`Center`/`Top` |
| 3 | `AsyncLoadWindow` 恒挂 `Bottom`，无法指定层级 |
| 4 | `Init()` 在 `Load()` 阶段执行，不在 `Open()` |
| 5 | `AddComponent` 不调 `Init()` |
| 6 | `AsyncLoad` 回调在失败路径也执行，需自查 `IsLoad` |
| 7 | `SendMessage` 未注册监听时静默忽略 |
| 8 | `uiDataCacheMap` 无上限 |
| 9 | `[UIMessageListener]` 形参必须恰好 1 个 |
| 10 | 重复 `LoadWindow` 不重建（只打日志） |
| 11 | 只 `CloseWindow` 不释放资源，需 `UnLoadWindow` |
| 12 | `Require` 必须 `public` 且形参 > 0 |
| 13 | `AddTransient<T>(T obj)` 丢弃实例 |
| 14 | `GetService` 用 `o.GetType() == type` 精确比较 |
