# 屏幕导航 ScreenView

ScreenView 用"时间片"划分游戏阶段：同一时刻只有一个 `IScreenView` 处于活动状态（大厅 / 战斗 / 结算），由 `ScreenViewManager` 统一调度。

## 类型体系

```csharp
namespace BDFramework.ScreenView

public interface IScreenView
{
    int  Name { get; set; }      // 由 ScreenViewManager.Init 从 attribute.IntTag 赋值
    bool IsLoad { get; }         // 只读；★ 框架本身不读取它
    void BeginInit();            // 进入该界面
    void BeginExit();            // 退出该界面
}

public class ScreenViewAttribute : ManagerAttribute
{
    public ScreenViewAttribute(int intTag) : base(intTag);
}

[ManagerOrder(Order = 99999)]     // ★ 全仓库唯一的 ManagerOrder
public class ScreenViewManager : ManagerBase<ScreenViewManager, ScreenViewAttribute>
{
    public List<ScreenViewLayer> screenViewList = new List<ScreenViewLayer>();
    public ScreenViewLayer MainLayer { get; private set; }

    public override void Init();
    public override void Start();          // base.Start() → MainLayer.BeginNavTo(defaultScreenTag)
    public ScreenViewLayer AddLayer();
}

public class ScreenViewLayer
{
    public ScreenViewLayer(int layerid);
    public int layerid { get; private set; }

    public IScreenView GetScreenView(int svName);
    public void RegisterScreen(IScreenView view);
    public void BeginNavTo(Enum name);
    public void BeginNavTo(int name);
    public void BeginNavForward(string name);
    public void BeginNavBack();
}
```

!!! note "`ScreenViewCenter` 类型不存在"
    `Runtime/Navigation/ScreenViewCenter.cs` 这个文件里只有 `ScreenViewLayer`。见[重构清单](../architecture/refactor-backlog.md#ref-7-filename-type-mismatch)。

## 生命周期顺序

```text
BeginNavTo(newName)
 ① newView.BeginInit()          ← ★ 先初始化新界面
 ② currentView.BeginExit()      ← ★ 后退出旧界面
 ③ currentView = newView
    currentViewIndex = -1
    navViews.Add(view)
 ④ if (navViews.Count > 10) navViews.RemoveAt(0);   ← 显示栈硬上限 10
```

!!! danger "顺序是「先 Init 新的，再 Exit 旧的」"
    这样设计是为了**避免黑屏**：新界面先准备好，旧界面再退出。但副作用是**两个界面会短暂同时存在**，如果两者的资源/UI 层有冲突需要自己处理。

!!! warning "显示栈上限 10，超出丢最早的"
    `navViews` 是"显示过的界面"记录（不是返回栈），超过 10 个会 `RemoveAt(0)`。

## `BeginNavTo` 的边界行为

```csharp
public void BeginNavTo(int name)
{
    if (currentView.Name == name)
    {
        BDebug.LogError("别闹，当前就是" + name);
        return;
    }
    if (!allViews.ContainsKey(name))
    {
        return;        // ★ 静默什么都不做，无日志
    }
    // ...
}
```

!!! danger "导航到未注册的 tag 会静默失败"
    `allViews` 里没有该 name 时**直接 return，不打任何日志**。界面不切换时先检查 `[ScreenView]` 属性是否挂上、tag 是否一致。

## `BeginNavBack()` 的越界问题

```csharp
public void BeginNavBack()
{
    if (currentViewIndex == 0 || currentViewIndex - 1 >= navViews.Count)
    {
        BDebug.LogError("别闹，前方没有view");
        return;
    }
    // ...
}
```

`currentViewIndex == -1` 时 `-2 >= count` 为 `false`，会执行 `currentViewIndex--` 变成 `-2`，随后访问 `navViews[-2]` **抛异常**。

另外 `BeginNavForward(string name)` 的 `name` 参数**未被使用**（死参数），且 `BeginNavTo` 会把 `currentViewIndex` 重置为 `-1`，所以 `BeginNavTo` 之后立刻 `BeginNavForward` 一定报错。

→ 见[重构清单](../architecture/refactor-backlog.md#ref-2-beginnavback)。

## 实现一个 ScreenView

```csharp
[ScreenView((int)ScreenViewEnum.Demo1)]
public class ScreenView_Demo1 : IScreenView
{
    public int  Name    { get; set; }
    public bool IsLoad  { get; private set; }

    public void BeginInit()
    {
        // ★ 必须设为 true
        this.IsLoad = true;

        UIManager.Inst.LoadWindow(WinEnum.Win_Demo1);
        UIManager.Inst.ShowWindow(WinEnum.Win_Demo1);
    }

    public void BeginExit()
    {
        // ★ 必须设为 false，否则下次进入不会调用 BeginInit
        this.IsLoad = false;

        UIManager.Inst.CloseWindow(WinEnum.Win_Demo1);
    }
}
```

!!! warning "`IsLoad` 必须自己维护"
    框架**不读取 `IsLoad`**。它是给你自己的状态判断用的。但源码注释强调：
    - `BeginInit` 里**一定要设为 `true`**，否则当前是未加载状态
    - `BeginExit` 里**一定要设为 `false`**，否则下次进入不会调用 `BeginInit`

## 业务枚举

`Assets/Code/Game@hotfix/ScreenView/ScreenViewEnum.cs`：

```csharp
public enum ScreenViewEnum
{
    Main  = 0,     // ★ tag 0 是默认启动页
    Demo1,
    Demo2,
    Demo3,
    Demo4,
}
```

`ScreenViewManager.Start()` 会 `MainLayer.BeginNavTo(this.defaultScreenTag)`，`defaultScreenTag` 默认为 `0`。

## 切换界面

```csharp
ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1);
```

真实用法（`Assets/Code/Game@hotfix/demo1/Window_Demo1.cs`）：

```csharp
[ButtonOnclick("btn_01")]
private void OnClickBack()
    => ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
```

## 注册机制

`ScreenViewManager.Init()`：

```text
遍历 GetAllClassDatas()
 → CreateInstance<IScreenView>(attr.IntTag)
 → sv.Name = attr.IntTag
 → MainLayer.RegisterScreen(sv)
```

!!! warning "Editor 下 `MainLayer != null` 时会直接 return"
    ```csharp
    public override void Init()
    {
    #if UNITY_EDITOR
        if (MainLayer != null) return;     // 退出 PlayMode 不重载程序集时的保护
    #endif
        // ...
    }
    ```
    **副作用**：在 Editor 中连续两次 Play 时，新增的 `[ScreenView]` 类**不会被重新注册**。改完代码后需要让 Unity 重新编译（触发程序集重载）。

## 与 UIManager 的关系

| 层 | 职责 |
|----|------|
| `ScreenViewManager` / `ScreenViewLayer` | **阶段级**切换（大厅 ↔ 战斗），同一时刻一个活动 |
| `UIManager` | **窗口级**管理（一个阶段内的多个界面） |

典型模式：一个 `IScreenView` 对应一个主窗口，`BeginInit` 里 `LoadWindow + ShowWindow`。

```csharp
// 阶段切换
ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Battle);

// 阶段内部切窗口
UIManager.Inst.ShowWindow(WinEnum.BattleResult);
```

## 多层级（`AddLayer`）

```csharp
public ScreenViewLayer AddLayer();
```

`ScreenViewManager` 支持多个 `ScreenViewLayer`（例如"主界面层"与"战斗层"各自独立导航），但仓库内**没有使用示例**——`MainLayer` 是唯一实际使用的层。

## 常见故障

| 现象 | 根因 |
|------|------|
| `别闹，当前就是<N>` | 重复导航到当前界面 |
| 界面不切换且无日志 | 目标 tag 未注册（检查 `[ScreenView]` 属性与枚举值） |
| `别闹，前方没有view` | `BeginNavBack` / `BeginNavForward` 在游标不合法时调用 |
| 新增 ScreenView 不生效 | Editor 下 `MainLayer != null` 的 early return；需重新编译 |
| `navViews[-2]` 越界异常 | `BeginNavBack` 在 `currentViewIndex == -1` 时调用 |
| 两个界面同时可见 | `BeginInit` 先于 `BeginExit`（设计如此） |

## 相关页面

- [窗口 Window](../ui/window.md)
- [启动链路](../architecture/bootstrap.md) —— `ManagerOrder` 与导航启动时机
- [Runtime 模块地图](../architecture/runtime-modules.md)
