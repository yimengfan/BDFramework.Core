# 子窗口 SubWindow

子窗口与普通窗口**同构**（都实现 `IWindow`），额外多一个 `Parent` 关系。

!!! note "没有 `SubWindow` 类"
    仓库中**不存在**名为 `SubWindow` 的类型。子窗口机制由三部分组合而成：
    1. `[SubWindow(path)]` 属性 —— 自动创建并注册
    2. `IWindow.RegisterSubWindow(IWindow)` / `GetSubWindow<T>()` —— 手动注册与获取
    3. 父窗口的 `SendMessage` 自动向下转发

## 两种创建方式

### 方式一：`[SubWindow]` 自动创建（推荐）

```csharp
[UI((int)WinEnum.Main, "Windows/Window_Main")]
public class Window_Main : AWindow
{
    // 框架会 new SubWindow_Xxx(transform) 并自动 RegisterSubWindow
    [SubWindow("panel/topBar")]
    private SubWindow_TopBar _topBar;

    [SubWindow("panel/content")]
    private SubWindow_Content _content;
}
```

实现细节（`SubWindowAttribute.AutoSetField`）：

```text
Transform.Find(path)
  → Activator.CreateInstance(uiType, new object[]{ transform }) as IWindow
  → 赋值给字段
  → (window as IWindow).RegisterSubWindow(subWindow)
```

**节点找不到时只 `BDebug.LogError` 并返回 `null`**，不抛异常。

### 方式二：手动注册（需要自行加载资源时）

```csharp
public class Window_Main : AWindow
{
    public override void Init()
    {
        base.Init();

        // 传 Transform：接管已有节点，不加载资源
        var sub = new SubWindow_Content(transform.Find("panel/content"));
        RegisterSubWindow(sub);

        // 传 path：自行加载资源
        var sub2 = new SubWindow_Other("Windows/Sub/Other");
        RegisterSubWindow(sub2);
    }
}
```

`SubWindow_Xxx` 的两种构造函数来自 `ATComponent<T>`：

```csharp
public ATComponent(Transform trans);        // 绑定既有节点 → InitComponent(this)
public ATComponent(string resPath);         // 只记路径，不加载
```

## `RegisterSubWindow` 的行为

```csharp
public void RegisterSubWindow(IWindow subwin)
{
    subWindowsMap[subwin.GetHashCode()] = subwin;     // ★ key 是对象哈希，不是枚举 ID
    subwin.Root = this.Root != null ? this.Root : this;
    subwin.Parent = this;
    (subwin as IComponent).Init();                    // 注册时立刻 Init
}
```

!!! warning "`subWindowsMap` 的 key 是 `GetHashCode()`"
    不是窗口 ID。这意味着：
    - 同一类型的多个子窗口实例**不会互相覆盖**（不同哈希）；
    - 但 `GetSubWindow<T>()` 是**线性遍历取第一个匹配类型**，多个同类型子窗口只能拿到第一个。

## 获取子窗口

```csharp
public T1 GetSubWindow<T1>() where T1 : class
```

```csharp
var topBar = this.GetSubWindow<SubWindow_TopBar>();
if (topBar != null) topBar.SetTitle("标题");
```

`Root` 属性让子窗口可以直接访问根窗口：

```csharp
public override void Init()
{
    base.Init();
    // 拿到根窗口（可能是多级子窗口嵌套）
    var root = this.Root;
}
```

## 消息转发：父 → 子

`AWindow<TP>.SendMessage` 在派发给自己之后，会**递归向所有子窗口转发**：

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

**转发是单向的：父 → 子。** 子窗口向父窗口发消息需要自己调 `this.Parent.SendMessage(...)`。

## 生命周期联动

| 事件 | 是否自动传递给子窗口 |
|------|-------------------|
| `SendMessage` | ✓ 自动递归转发 |
| `Open()` | ✗ 需手动 `GetSubWindow<T>()?.Open()` |
| `Close()` | ✗ 同上 |
| `Destroy()` | ✗ 子窗口作为组件挂在 `ComponentList` 里，随父窗口的 GameObject 一起销毁 |

!!! tip "父窗口宜作纯容器"
    推荐模式：父窗口只负责**布局与子窗口装配**，业务逻辑全部下沉到子窗口。这样：
    - 子窗口可以独立复用（换个父窗口也能用）
    - 父窗口代码极短，几乎不需要维护

## 完整示例

```csharp
// ── 父窗口：纯容器 ──
[UI((int)WinEnum.Player, "Windows/Window_Player")]
public class Window_Player : AWindow
{
    [SubWindow("top/bar")]   private SubWindow_TopBar  _topBar;
    [SubWindow("main/info")] private SubWindow_Info    _info;
    [SubWindow("main/bag")]  private SubWindow_Bag     _bag;

    [UIMessageListener]
    private void OnMsg_SelectTab(Msg_SelectTab msg)
        => _info?.SetPlayer(msg.PlayerId);
}

// ── 子窗口：独立可复用 ──
public class SubWindow_Info : AWindow
{
    [TransformPath("txtName")] private Text _name;

    public void SetPlayer(int playerId)
    {
        var hero = SqliteHelper.DB.GetTableRuntime()
            .Where("id = {0}", playerId).FromAll<Hero>();
        if (hero.Count > 0) _name.text = hero[0].Name;
    }
}
```

```csharp
// 使用时只需操作父窗口
UIManager.Inst.LoadWindow(WinEnum.Player);
UIManager.Inst.ShowWindow(WinEnum.Player);

// 消息会被自动转发到所有子窗口
UIManager.Inst.SendMessage(WinEnum.Player, new Msg_SelectTab { PlayerId = 1001 });
```

## 常见故障

| 现象 | 根因 |
|------|------|
| 子窗口字段为 `null` | `[SubWindow]` 的路径没找到节点（只报 error 不抛） |
| `GetSubWindow<T>()` 返回 `null` | 子窗口类型不匹配，或 `Init()` 里 `RegisterSubWindow` 还没执行 |
| 子窗口不响应消息 | 消息类型不匹配；或子窗口是在 `SendMessage` 之后才注册的 |
| 同类型子窗口拿不到第二个 | `GetSubWindow<T>()` 只返回第一个匹配类型 |
| 子窗口的 `Init()` 执行了两次 | 同时用了 `[SubWindow]`（注册时 `Init()`）与手动 `new`（构造时可能已 `Init`） |

## 相关页面

- [窗口 Window](window.md)
- [消息 UIMessage](ui-message.md)
- [自动赋值属性](auto-assign-attributes.md)
