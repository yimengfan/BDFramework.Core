---
name: bdframework-uimanager
description: 'BDFramework 屏幕导航与事件总线技能。使用场景：用 ScreenViewManager/ScreenViewLayer/IScreenView 做阶段级界面切换（大厅↔战斗）、排查 BeginNavTo/BeginNavBack 不生效、用 StatusListenerServer/AStatusListener/ADataListenerT 做跨模块状态广播与值监听、排查事件收不到或类型不匹配、用 ManagerBase 注册新管理器、用 GameServiceStore/ServiceContainer 做模块级服务容器。关键字：ScreenView、ScreenViewManager、ScreenViewLayer、IScreenView、BeginNavTo、BeginNavBack、MainLayer、StatusListenerServer、AStatusListener、ADataListenerT、AddListener、TriggerEvent、SetData、20条缓存、类型不匹配、ManagerBase、ManagerAttribute、ManagerOrder、ManagerInstHelper、ServiceContainer、GameServiceStore。'
---

# BDFramework 屏幕导航 / 事件总线 / 管理器技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 做**阶段级**界面切换（大厅 / 战斗 / 结算）
- 排查"事件收不到"、"监听只触发一次"、"设置失败,类型不匹配"
- 新增管理器（`ManagerBase` 派生类）
- 需要按模块隔离的服务容器

不适用：**窗口级**管理（用 `bdframework-uflux`）、UI 消息（`UIMsgData`，也在 `bdframework-uflux`）。

## 2. 三层选择指南

| 需求 | 用什么 | 匹配方式 |
|------|--------|---------|
| 特定窗口的定向指令 | `UIMsgData` + `UIManager.SendMessage` | 消息**类型**精确匹配 |
| 跨模块状态广播 | `AStatusListener` + `StatusListenerServer` | `Enum` / `string` / `Type.FullName` |
| 强类型值监听 | `ADataListenerT<T>` | `string` name |
| 阶段级界面切换 | `ScreenViewManager.MainLayer.BeginNavTo` | `int` tag |
| 模块级服务隔离 | `GameServiceStore.GetService("模块")` | 模块名字符串 |

## 3. 屏幕导航

### 3.1 铁律

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | `IScreenView.IsLoad` **必须自己维护** | 框架**不读取**它；不设 `false` 会导致下次不调 `BeginInit` |
| 2 | 导航到**未注册**的 tag **静默失败** | 无任何日志，界面不切换 |
| 3 | 顺序是 **先 `BeginInit()` 新的，再 `BeginExit()` 旧的** | 两个界面会短暂同时存在 |
| 4 | `navViews` 上限 **10** | 超出 `RemoveAt(0)` 丢最早的 |
| 5 | tag **0** 是默认启动页 | `ScreenViewManager.Start()` 会 `BeginNavTo(0)` |
| 6 | `ScreenViewManager` 有 `[ManagerOrder(99999)]` | 全仓库唯一，保证导航最后启动 |

### 3.2 实现一个 ScreenView

```csharp
public enum ScreenViewEnum { Main = 0, Battle, Result }

[ScreenView((int)ScreenViewEnum.Battle)]
public class ScreenView_Battle : IScreenView
{
    public int  Name   { get; set; }        // 框架从 attribute.IntTag 赋值
    public bool IsLoad { get; private set; }

    public void BeginInit()
    {
        IsLoad = true;                       // ★ 必须
        UIManager.Inst.LoadWindow(WinEnum.Battle);
        UIManager.Inst.ShowWindow(WinEnum.Battle);
    }

    public void BeginExit()
    {
        IsLoad = false;                      // ★ 必须
        UIManager.Inst.CloseWindow(WinEnum.Battle);
    }
}

// 切换
ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Battle);
```

### 3.3 `ScreenViewLayer` API

```csharp
public class ScreenViewLayer
{
    public ScreenViewLayer(int layerid);
    public int layerid { get; private set; }

    public IScreenView GetScreenView(int svName);       // 先查 navViews，再查 allViews
    public void RegisterScreen(IScreenView view);        // allViews.Add(view.Name, view)
    public void BeginNavTo(Enum name);
    public void BeginNavTo(int name);
    public void BeginNavForward(string name);            // ★ name 参数未被使用
    public void BeginNavBack();
}
```

!!! danger "`BeginNavBack()` 有越界风险"
    ```csharp
    if (currentViewIndex == 0 || currentViewIndex - 1 >= navViews.Count) { LogError; return; }
    ```
    `currentViewIndex == -1` 时 `-2 >= count` 为 `false`，会执行 `currentViewIndex--` 变成 `-2`，随后 `navViews[-2]` **抛异常**。

    且 `BeginNavTo` 会把 `currentViewIndex` 重置为 `-1`，所以 `BeginNavTo` 之后立刻 `BeginNavForward` 一定报错。

### 3.4 故障对照

| 现象 | 根因 |
|------|------|
| `别闹，当前就是<N>` | 重复导航到当前界面 |
| 界面不切换且无日志 | 目标 tag 未注册（检查 `[ScreenView]` 属性与枚举值） |
| `别闹，前方没有view` | `BeginNavBack` / `BeginNavForward` 在游标不合法时调用 |
| 新增 ScreenView 不生效 | Editor 下 `Init()` 里 `if (MainLayer != null) return;` 的 early return，需重新编译 |
| `navViews[-2]` 越界异常 | `BeginNavBack` 在 `currentViewIndex == -1` 时调用 |
| 两个界面同时可见 | `BeginInit` 先于 `BeginExit`（设计如此） |

## 4. 事件总线

### 4.1 铁律

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | `AStatusListener` 值缓存**上限 20 条** | 超出丢最旧的 |
| 2 | **同一 key 不能改类型** | `SetData` 只 `LogError` 并**拒绝写入** |
| 3 | `StatusListenerServer` 有**两套独立字典** | `Create("X")` 与 `Create<T>("X")` 是两个不同服务 |
| 4 | `StatusListenerService.Name` **永远是 `null`** | 已知问题，无法从服务实例取名字 |
| 5 | `AStatusListener.AddListener<T>` 要求 `T : class` | 值类型无法直接监听（需装箱或用 `Action<object>`） |
| 6 | `ADataListenerT<T>` 的缓存**无上限** | 与 `AStatusListener` 的 20 条不同 |

### 4.2 推荐用法：按模块持有

```csharp
public static class PlayerEvents
{
    private static StatusListenerService _svc;
    public static StatusListenerService Svc
        => _svc ??= StatusListenerServer.Create(nameof(PlayerEvents));

    public static void SetGold(int gold)              => Svc.SetData(PlayerData.Gold, gold);
    public static void OnGoldChanged(Action<int> cb)  => Svc.AddListener<int>(PlayerData.Gold, cb);
}
```

### 4.3 四种 key 形式

| 形式 | API | 匹配 |
|------|-----|------|
| `Enum` | `ValueListenerEx.SetData(Enum, …)` | `enum.GetHashCode().ToString()` / `ToString()` |
| `string` | `ValueListenerEx.SetData(string, …)` | 原样 |
| 类型 | `EventListenerEx.TriggerEvent<T>(…)` | `typeof(T).FullName` |
| 泛型 listener | `StatusListenerServer.Create<T>(name)` | `string` name |

!!! danger "三种 key 形式不能混用"
    ```csharp
    svc.SetData(PlayerData.Gold, 100);                 // key = "Gold"（或 hash 字符串）
    svc.AddListener<int>("Gold", v => { });            // ✗ 不一定命中
    svc.AddListener<int>(PlayerData.Gold, v => { });   // ✓ 用同一个 Enum
    ```

### 4.4 `AStatusListener` API 要点

```csharp
virtual public void SetData(string name, object value, bool isTriggerCallback = true);
virtual public void TriggerEvent(string name, object value = null, bool isTriggerCallback = true);
virtual public T GetData<T>(string name);

virtual public void AddListener<T>(string name, Action<T> callback,
                                   int order = -1, int triggerNum = -1, bool isTriggerCacheData = false)
    where T : class;
virtual public void AddListenerOnce<T>(string name, Action<T> callback,
                                       int order = -1, bool isTriggerCacheData = false) where T : class;

virtual public void RemoveListener<T>(string name, Action<T> callback);
virtual public void RemoveListener(string name);
virtual public void ClearListener(string name);
public    void ClearAllListener();
```

| 参数 | 语义 |
|------|------|
| `order` | 插入顺序，`-1`（默认）排最前 |
| `triggerNum` | `-1` = 永久；`0` = 已耗尽（会被回收）；`> 0` = 剩余次数 |
| `isTriggerCacheData` | `true` 时把 `valueCacheMap` 全量重放后 `Clear()` |

**回调安全**：触发时复制一份 `_callbackList` 再遍历，**允许在回调里增删监听**。

### 4.5 缓存语义（20 条）

```text
SetData(name, value)
 ├─ 该 name 已有回调 → 直接派发，不写缓存
 └─ 该 name 无回调   → 写入 valueCacheMap
                       若 list.Count >= 20 → RemoveAt(0)   ← 保留最新 20 条
```

### 4.6 故障对照

| 现象 | 根因 |
|------|------|
| `设置失败,类型不匹配:<name> curType:X setType:Y` | 同一 key 前后类型不一致 |
| `暂时无数据,提前监听:<name>` | `AddListener` 时 `dataMap` 还没该 key（仍会注册，仅提醒） |
| 只收到一次回调 | 用了 `AddListenerOnce` |
| 只收到前 N 次 | `triggerNum: N` 限制 |
| 回调收不到 | key 不一致；或 `Create` / `Create<T>` 混名 |
| 缓存的历史值丢了 | `AStatusListener` 只保留最新 20 条 |
| 日志里 `Name` 是 `null` | 已知问题 |

## 5. 管理器体系

### 5.1 命名空间

!!! danger "`ManagerBase` 在 `BDFramework.Mgr`"
    ```csharp
    using BDFramework.Mgr;
    ```
    `ManagerBase<T,V>`、`ManagerAttribute`、`ManagerOrder`、`ManagerInstHelper`、`IMgr`、`ClassData` 全在这个命名空间。

### 5.2 实现契约

| # | 要求 | 违反后果 |
|---|------|---------|
| 1 | `: ManagerBase<自类型, 属性类型>`，属性类型派生自 `ManagerAttribute` | 不会被 `LoadManager` 识别 |
| 2 | 有无参构造函数（`new()` 约束） | 编译失败 |
| 3 | 业务类挂 `V` 派生属性（`IntTag` 或 `Tag` 至少一个） | `GetClassData` 取不到 |
| 4 | 可选 `[ManagerOrder(Order = n)]`，越小越先 | 默认 `0` |
| 5 | 覆写 `Start()` **必须调 `base.Start()`** | `IsStarted` 不置位 → **被重复调用** |

### 5.3 最小实现

```csharp
using BDFramework.Mgr;

public class MyAttribute : ManagerAttribute
{
    public MyAttribute(int tag) : base(tag) { }
}

public class MyManager : ManagerBase<MyManager, MyAttribute>
{
    public override void Init() { base.Init(); }        // 遍历 ClassData 做初始化

    public void Do(MyEnum e)
    {
        var inst = CreateInstance<IMyThing>((int)e);    // 每次 new
        inst?.Run();
    }
}

[My((int)MyEnum.A)]
public class MyThing_A : IMyThing { public void Run() { } }
```

**零注册代码** —— 挂上属性就会被自动发现。

### 5.4 启动顺序

```text
ManagerInstHelper.Start()
 ① 全部 Mgr.Init()
 ② 所有 !IsStarted 的 Mgr.Start()      ← 按 [ManagerOrder] 升序
```

仓库中的管理器：

| 类型 | Order |
|------|-------|
| `GameConfigManager` | 0 |
| `UIManager` | 0 |
| `ComponentBindAdaptorManager` | 0 |
| `ScreenViewManager` | **99999** |

!!! warning "`GetAllClassDatas()` 不混排 int 与 string tag"
    `IntKey` 非空 → 按 int 升序；否则 `StringKey` 非空 → 按 string 升序；都为空返回空数组。**同时用两种 tag 形式只会返回其中一种。**

### 5.5 故障对照

| 现象 | 根因 |
|------|------|
| 管理器 `Init()` 没被调用 | 类没挂 `ManagerAttribute` 派生属性；或没实现 `IMgr` |
| `Start()` 被调用两次 | 覆写时漏了 `base.Start()` |
| `GetClassData(tag)` 返回 `null` | tag 类型（int/string）与注册时不一致 |
| 顺序不对 | 未加 `[ManagerOrder]` |
| `加载管理器失败,-<type>` | `Inst` 属性反射不到 |

## 6. 服务容器

### 6.1 三种容器

| 容器 | 作用域 | 注册入口 |
|------|--------|---------|
| `UIManager` 的 singleton/transient 列表 | 全局 | `UIManager.Inst.AddSingleton/AddTransient` |
| `IWindow.ServiceContainer` | 单窗口 | 窗口内部自行注册 |
| `GameServiceStore` | **按模块名** | `GameServiceStore.GetService("模块")` |

!!! danger "三者互不相通"
    `SetWindowDI` 只查 `UIManager` 自己的两个列表；`GameServiceStore` 是另一套。

### 6.2 `ServiceContainer`

```csharp
public class ServiceContainer
{
    public void AddSingleton<T>() where T : class;
    public void AddSingleton(object inst);                    // 同类型已存在 → LogError
    public void AddTransient<T>(T obj) where T : class;       // ★ 只用 obj 取类型
    public T GetService<T>() where T : class;
}
```

```csharp
var battle = GameServiceStore.GetService("Battle");
battle.AddSingleton<BattleContext>();
var ctx = battle.GetService<BattleContext>();

var lobby = GameServiceStore.GetService("Lobby");
lobby.AddSingleton<LobbyContext>();      // 与 Battle 互不干扰
```

!!! danger "两个陷阱"
    1. `AddTransient<T>(T obj)` **丢弃传入实例**，每次 `GetService` 都 `Activator.CreateInstance`（要求无参构造）
    2. `GetService` 用 `o.GetType() == type` **精确比较**，不做接口/基类匹配

## 7. 详细参考

| 文件 | 内容 |
|------|------|
| [references/event-bus-api.md](./references/event-bus-api.md) | `AStatusListener` / `ADataListenerT<T>` / 扩展方法完整 API |
| [references/manager-service.md](./references/manager-service.md) | `ManagerBase` / `ManagerInstHelper` / `ServiceContainer` 完整 API |

在线文档：

- [屏幕导航 ScreenView](https://yimengfan.github.io/BDFramework.Core/api/screen-navigation.md)
- [事件总线 EventBus](https://yimengfan.github.io/BDFramework.Core/api/event-bus.md)
- [管理器体系 ManagerBase](https://yimengfan.github.io/BDFramework.Core/api/manager-base.md)
- [服务容器与日志](https://yimengfan.github.io/BDFramework.Core/api/utils.md)

## 8. 改动前检查清单

- [ ] `IScreenView.IsLoad` 在 `BeginInit` / `BeginExit` 中正确维护
- [ ] 事件 key 用 `Enum` 而非裸字符串
- [ ] 同一 key 前后类型一致
- [ ] 需要"只触发一次"时用了 `AddListenerOnce` 或 `triggerNum`
- [ ] 新管理器的 `Start()` 调了 `base.Start()`
- [ ] 需要晚于 UI 启动的管理器加了 `[ManagerOrder]`
- [ ] `using BDFramework.Mgr;` 已添加
