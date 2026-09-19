# SubWindow

A SubWindow is **isomorphic** to a regular window (both implement `IWindow`), with one extra relationship: `Parent`.

!!! note "There is no `SubWindow` class"
    **No type named `SubWindow` exists** in this repository. The SubWindow mechanism is put together from three parts:
    1. The `[SubWindow(path)]` attribute — creates and registers automatically
    2. `IWindow.RegisterSubWindow(IWindow)` / `GetSubWindow<T>()` — manual registration and lookup
    3. The parent window's `SendMessage` forwards downward automatically

## Two ways to create one

### Option 1: automatic creation via `[SubWindow]` (recommended)

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

Implementation details (`SubWindowAttribute.AutoSetField`):

```text
Transform.Find(path)
  → Activator.CreateInstance(uiType, new object[]{ transform }) as IWindow
  → 赋值给字段
  → (window as IWindow).RegisterSubWindow(subWindow)
```

**When the node is not found it only calls `BDebug.LogError` and returns `null`** — it does not throw.

### Option 2: manual registration (when you load the asset yourself)

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

`SubWindow_Xxx`'s two constructors come from `ATComponent<T>`:

```csharp
public ATComponent(Transform trans);        // 绑定既有节点 → InitComponent(this)
public ATComponent(string resPath);         // 只记路径，不加载
```

## What `RegisterSubWindow` does

```csharp
public void RegisterSubWindow(IWindow subwin)
{
    subWindowsMap[subwin.GetHashCode()] = subwin;     // ★ key 是对象哈希，不是枚举 ID
    subwin.Root = this.Root != null ? this.Root : this;
    subwin.Parent = this;
    (subwin as IComponent).Init();                    // 注册时立刻 Init
}
```

!!! warning "`subWindowsMap`'s key is `GetHashCode()`"
    It is not the window ID. This means:
    - Several SubWindow instances of the same type **do not overwrite each other** (different hashes);
    - But `GetSubWindow<T>()` **walks the map linearly and returns the first type match**, so with several SubWindows of the same type you only ever get the first one.

## Getting a SubWindow

```csharp
public T1 GetSubWindow<T1>() where T1 : class
```

```csharp
var topBar = this.GetSubWindow<SubWindow_TopBar>();
if (topBar != null) topBar.SetTitle("标题");
```

The `Root` property lets a SubWindow reach the root window directly:

```csharp
public override void Init()
{
    base.Init();
    // 拿到根窗口（可能是多级子窗口嵌套）
    var root = this.Root;
}
```

## Message forwarding: parent → child

After dispatching to itself, `AWindow<TP>.SendMessage` **recursively forwards to every SubWindow**:

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

**Forwarding is one-way: parent → child.** For a SubWindow to message its parent it has to call `this.Parent.SendMessage(...)` itself.

## Lifecycle coupling

| Event | Automatically passed to SubWindows? |
|------|-------------------|
| `SendMessage` | ✓ forwarded recursively |
| `Open()` | ✗ needs a manual `GetSubWindow<T>()?.Open()` |
| `Close()` | ✗ same as above |
| `Destroy()` | ✗ SubWindows sit in `ComponentList` as components and are destroyed together with the parent window's GameObject |

!!! tip "The parent window should be a pure container"
    The recommended pattern: the parent window is responsible only for **layout and assembling SubWindows**, with all business logic pushed down into the SubWindows. That way:
    - SubWindows are independently reusable (they also work under a different parent)
    - The parent window's code is tiny and needs almost no maintenance

## Full example

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

## Common failures

| Symptom | Root cause |
|------|------|
| The SubWindow field is `null` | The `[SubWindow]` path found no node (it only logs an error, it does not throw) |
| `GetSubWindow<T>()` returns `null` | Type mismatch, or `RegisterSubWindow` inside `Init()` has not run yet |
| The SubWindow does not respond to messages | Message type mismatch; or the SubWindow was registered only after `SendMessage` |
| The second SubWindow of the same type cannot be retrieved | `GetSubWindow<T>()` returns only the first type match |
| The SubWindow's `Init()` runs twice | `[SubWindow]` (which calls `Init()` on registration) is combined with a manual `new` (whose constructor may already have called `Init`) |

## Related pages

- [Window](window.md)
- [Messages (UIMessage)](ui-message.md)
- [Auto-Assign Attributes](auto-assign-attributes.md)
