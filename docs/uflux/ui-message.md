# 消息 UIMessage

UFlux 的窗口间通信机制。消息体派生自 `UIMsgData`，接收方用 `[UIMessageListener]` 标记方法，按**参数类型精确匹配**。

## 消息体定义

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

!!! danger "`UIMsgData` 是 `abstract`，不能直接实例化"
    每个消息都必须有具体子类。**匹配 key 是消息的运行时类型**（`uiMsg.GetType()`），因此两个不同的 `UIMsgData` 子类即使字段相同也互不干扰。

## 接收消息

```csharp
[UIMessageListener]
private void OnMsg_OpenShop(Msg_OpenShop msg)
{
    Refresh(msg.ShopId);
}
```

`AWindow<TP>` 的构造函数末尾会调 `RegisterUIMessages()`：

1. 反射本类（含继承链）的 `Instance | Public | NonPublic` 方法
2. 取带 `[UIMessageListener]` 的
3. **形参必须恰好 1 个**，否则报错
4. 以参数类型为 key 存入 `msgCallbackMap`

!!! warning "形参数量必须恰好 1"
    0 个或 2 个形参的方法会被跳过（不注册），且**只在 Editor 下有提示**。写成 `OnMsg(Msg_A a, Msg_B b)` 会静默失效。

## 三种发送方式

### ① 主动发送

```csharp
UIManager.Inst.SendMessage(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

### ② 随开窗携带

```csharp
UIManager.Inst.ShowWindow(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

`AWindow<TP>.Open` 的顺序是：`SetActive(true)` → `SendMessage(uiMsg)` → `State.TriggerEvent<OnWindowOpen>()`。

**注意**：消息在窗口**打开时**派发，此时 `Init()` 已经执行过（`Init()` 在 `Load()` 阶段）。

### ③ 窗口内转发给子窗口

```csharp
// 在父窗口内
this.SendMessage(new Msg_Refresh());
// → 父窗口自己先处理，然后自动递归转发给所有子窗口
```

## 消息路由规则

| 规则 | 说明 |
|------|------|
| **类型精确匹配** | key 是 `uiMsg.GetType()`，**不做基类/接口匹配**。发 `Msg_A` 不会触发 `OnMsg(UIMsgData msg)` |
| **未注册则静默忽略** | 找不到对应方法时**没有任何日志** |
| **父 → 子单向广播** | 父窗口收到消息后会递归转发给所有子窗口 |
| **不跨窗口** | 窗口 A 的 `SendMessage` 不会影响窗口 B，除非通过 `UIManager.SendMessage(B, …)` |
| **未加载时缓存** | 目标窗口未加载时消息进 `uiDataCacheMap`，加载后由 `PushCaheData` 回放 |

## 消息缓存机制

```csharp
// 窗口还没加载 —— 消息不会丢
UIManager.Inst.SendMessage(WinEnum.Detail, new Msg_ShowItem { ItemId = 1001 });

// 之后加载，缓存的消息会被回放
UIManager.Inst.LoadWindow(WinEnum.Detail);
```

`UIManager` 内部：

```csharp
private Dictionary<int, List<UIMsgData>> uiDataCacheMap;   // 窗口未加载时的消息缓存
private void PushCaheData(int uiIdx);                       // 加载完成后回放
```

!!! warning "缓存没有上限"
    `uiDataCacheMap` 是 `List<UIMsgData>`，**没有条数限制**。对高频消息（如每帧推送）不要用 `UIManager.SendMessage`，会导致内存持续增长。高频数据请走 `AStatusListener`。

## 与 `AStatusListener` 的分工

两者都是消息机制，容易混用。选择依据：

| 维度 | `UIMsgData` + `SendMessage` | `AStatusListener` |
|------|---------------------------|-------------------|
| 目标 | **特定窗口**（按窗口 ID 定向） | **全局/具名服务**（谁监听谁收到） |
| 匹配 | 消息类型精确匹配 | `Enum` / `string` key |
| 订阅方数量 | 单窗口（+ 其子窗口树） | 任意多个 |
| 缓存 | 无上限的待回放列表 | **上限 20 条**，超出丢最旧 |
| 适用 | 界面内部的定向指令 | 跨模块的状态广播 |

```csharp
// 定向：告诉商店窗口打开某个商品
UIManager.Inst.SendMessage(WinEnum.Shop, new Msg_OpenItem { ItemId = 5 });

// 广播：玩家金币变了，所有关心的模块自己监听
StatusListenerServer.Create("Player").SetData(PlayerData.Gold, 100);
```

→ 详见[事件总线 EventBus](../api/event-bus.md)。

## 完整示例

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

## 常见故障

| 现象 | 根因 |
|------|------|
| 消息没被处理 | 方法缺 `[UIMessageListener]`；或形参不是 1 个；或消息类型不完全匹配 |
| 子窗口收不到消息 | 子窗口注册晚于 `SendMessage`；或发消息时用的是 `SendMessage(sub, msg)` 而不是父窗口 |
| 内存持续增长 | 对未加载窗口高频 `SendMessage`，`uiDataCacheMap` 无上限累积 |
| 窗口打开时数据是旧的 | `Open` 里先 `SetActive` 再 `SendMessage`；若 `Init()` 里也刷了一遍，注意覆盖顺序 |

## 相关页面

- [窗口 Window](window.md)
- [子窗口 SubWindow](sub-window.md)
- [事件总线 EventBus](../api/event-bus.md)
