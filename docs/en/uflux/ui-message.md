# Messages (UIMessage)

UFlux's inter-window communication mechanism. Message bodies derive from `UIMsgData`, receivers mark methods with `[UIMessageListener]`, and messages are matched by **exact parameter type**.

## Defining a message body

```csharp
// Runtime/UI/View/Windows/UIMsgData.cs
public abstract class UIMsgData
{
    public T GetMsg<T>() { return (T)this; }
}
```

```csharp
// 业务侧定义消息
public class Msg_OpenShop : UIMsgData
{
    public int ShopId;
}

public class Msg_RefreshItem : UIMsgData
{
    public int ItemId;
    public int Count;
}
```

!!! danger "`UIMsgData` is `abstract` and cannot be instantiated directly"
    Every message needs a concrete subclass. **The match key is the message's runtime type** (`uiMsg.GetType()`), so two different `UIMsgData` subclasses never interfere with each other even when their fields are identical.

## Receiving messages

```csharp
[UIMessageListener]
private void OnMsg_OpenShop(Msg_OpenShop msg)
{
    Refresh(msg.ShopId);
}
```

At the end of `AWindow<TP>`'s constructor, `RegisterUIMessages()` runs:

1. Reflects over this class (including the inheritance chain) for `Instance | Public | NonPublic` methods
2. Takes those carrying `[UIMessageListener]`
3. **There must be exactly one parameter**, otherwise it reports an error
4. Stores the method in `msgCallbackMap` keyed by the parameter type

!!! warning "There must be exactly one parameter"
    Methods with 0 or 2 parameters are skipped (not registered), and **only the Editor prints a hint**. Writing `OnMsg(Msg_A a, Msg_B b)` fails silently.

## Three ways to send

### ① Send explicitly

```csharp
UIManager.Inst.SendMessage(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

### ② Carry it along when opening a window

```csharp
UIManager.Inst.ShowWindow(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

The order inside `AWindow<TP>.Open` is: `SetActive(true)` → `SendMessage(uiMsg)` → `State.TriggerEvent<OnWindowOpen>()`.

**Note**: the message is dispatched when the window is **opened**, by which point `Init()` has already run (`Init()` happens during the `Load()` phase).

### ③ Forward from inside a window to its SubWindows

```csharp
// 在父窗口内
this.SendMessage(new Msg_Refresh());
// → 父窗口自己先处理，然后自动递归转发给所有子窗口
```

## Message routing rules

| Rule | Explanation |
|------|------|
| **Exact type match** | The key is `uiMsg.GetType()`; **no base-class or interface matching**. Sending `Msg_A` does not trigger `OnMsg(UIMsgData msg)` |
| **Unregistered messages are silently ignored** | When no matching method is found there is **no log at all** |
| **One-way parent → child broadcast** | A parent window recursively forwards to all its SubWindows after receiving a message |
| **Does not cross windows** | Window A's `SendMessage` does not affect window B unless you go through `UIManager.SendMessage(B, …)` |
| **Cached while unloaded** | While the target window is not loaded, messages go into `uiDataCacheMap` and are replayed by `PushCaheData` after it loads |

## Message cache mechanism

```csharp
// 窗口还没加载 —— 消息不会丢
UIManager.Inst.SendMessage(WinEnum.Detail, new Msg_ShowItem { ItemId = 1001 });

// 之后加载，缓存的消息会被回放
UIManager.Inst.LoadWindow(WinEnum.Detail);
```

Inside `UIManager`:

```csharp
private Dictionary<int, List<UIMsgData>> uiDataCacheMap;   // 窗口未加载时的消息缓存
private void PushCaheData(int uiIdx);                       // 加载完成后回放
```

!!! warning "The cache has no upper bound"
    `uiDataCacheMap` is a `List<UIMsgData>` with **no entry limit**. Do not use `UIManager.SendMessage` for high-frequency messages (such as per-frame pushes) — memory will keep growing. Route high-frequency data through `AStatusListener` instead.

## Division of labour with `AStatusListener`

Both are messaging mechanisms and are easy to confuse. How to choose:

| Dimension | `UIMsgData` + `SendMessage` | `AStatusListener` |
|------|---------------------------|-------------------|
| Target | **A specific window** (addressed by window ID) | **Global / named service** (whoever listens receives it) |
| Matching | Exact message type match | `Enum` / `string` key |
| Number of subscribers | A single window (+ its SubWindow tree) | Any number |
| Cache | An unbounded replay list | **Capped at 20 entries**, oldest dropped on overflow |
| Use case | Targeted instructions inside a screen | State broadcasts across modules |

```csharp
// 定向：告诉商店窗口打开某个商品
UIManager.Inst.SendMessage(WinEnum.Shop, new Msg_OpenItem { ItemId = 5 });

// 广播：玩家金币变了，所有关心的模块自己监听
StatusListenerServer.Create("Player").SetData(PlayerData.Gold, 100);
```

→ See [Event Bus](../api/event-bus.md) for details.

## Full example

```csharp
// ── 消息定义 ──
public class Msg_OpenItem : UIMsgData { public int ItemId; }
public class Msg_ClosePanel : UIMsgData { }

// ── 发送方 ──
public class Window_Bag : AWindow
{
    [ButtonOnclick("item/btnUse")]
    private void OnClickUse()
    {
        // 打开详情窗口并携带参数
        UIManager.Inst.ShowWindow(WinEnum.ItemDetail, new Msg_OpenItem { ItemId = 5 });
    }
}

// ── 接收方 ──
[UI((int)WinEnum.ItemDetail, "Windows/Window_ItemDetail")]
public class Window_ItemDetail : AWindow
{
    [TransformPath("txtName")] private Text _name;

    [UIMessageListener]
    private void OnMsg_OpenItem(Msg_OpenItem msg)
    {
        var rows = SqliteHelper.DB.GetTableRuntime()
            .Where("id = {0}", msg.ItemId).FromAll<Item>();
        if (rows.Count > 0) _name.text = rows[0].Name;
    }
}
```

## Common failures

| Symptom | Root cause |
|------|------|
| The message is not handled | The method is missing `[UIMessageListener]`; or it does not take exactly one parameter; or the message type is not an exact match |
| A SubWindow does not receive messages | The SubWindow was registered after `SendMessage`; or you sent with `SendMessage(sub, msg)` instead of targeting the parent window |
| Memory keeps growing | High-frequency `SendMessage` to unloaded windows — `uiDataCacheMap` accumulates without a cap |
| The data is stale when the window opens | `Open` calls `SetActive` and only then `SendMessage`; if `Init()` also refreshed once, watch the overwrite order |

## Related pages

- [Window](window.md)
- [SubWindow](sub-window.md)
- [Event Bus](../api/event-bus.md)
