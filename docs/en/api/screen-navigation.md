# Screen Navigation

ScreenView divides the game into "time slices" using phases: at any moment exactly one `IScreenView` is active (lobby / battle / result), and `ScreenViewManager` schedules them centrally.

## Type hierarchy

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

!!! note "The `ScreenViewCenter` type does not exist"
    The file `Runtime/Navigation/ScreenViewCenter.cs` contains only `ScreenViewLayer`. See [Refactor Backlog](../architecture/refactor-backlog.md#ref-7-filename-type-mismatch).

## Lifecycle order

```text
BeginNavTo(newName)
 ① newView.BeginInit()          ← ★ 先初始化新界面
 ② currentView.BeginExit()      ← ★ 后退出旧界面
 ③ currentView = newView
    currentViewIndex = -1
    navViews.Add(view)
 ④ if (navViews.Count > 10) navViews.RemoveAt(0);   ← 显示栈硬上限 10
```

!!! danger "The order is 'initialise the new one first, exit the old one after'"
    This is designed to **avoid a black screen**: the new screen is ready before the old one goes away. The side effect is that **the two screens briefly coexist**, so you have to handle any conflict between their assets / UI layers yourself.

!!! warning "The display stack is capped at 10; older entries are dropped"
    `navViews` records the screens that have been displayed (it is not a back stack); past 10 entries it does `RemoveAt(0)`.

## Boundary behaviour of `BeginNavTo`

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

!!! danger "Navigating to an unregistered tag fails silently"
    When `allViews` has no such name it **returns immediately without logging anything**. If a screen refuses to switch, first check whether the `[ScreenView]` attribute is attached and whether the tag matches.

## The out-of-range problem in `BeginNavBack()`

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

When `currentViewIndex == -1`, `-2 >= count` evaluates to `false`, so it runs `currentViewIndex--` and turns it into `-2`, after which reading `navViews[-2]` **throws**.

On top of that, the `name` parameter of `BeginNavForward(string name)` is **never used** (a dead parameter), and `BeginNavTo` resets `currentViewIndex` to `-1`, so calling `BeginNavForward` right after `BeginNavTo` is guaranteed to error.

→ See [Refactor Backlog](../architecture/refactor-backlog.md#ref-2-beginnavback).

## Implementing a ScreenView

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

!!! warning "`IsLoad` is yours to maintain"
    The framework **never reads `IsLoad`**; it exists for your own state checks. But the source comments stress:
    - `BeginInit` **must set it to `true`**, otherwise the screen counts as not loaded
    - `BeginExit` **must set it to `false`**, otherwise `BeginInit` will not be called the next time you enter

## Business enum

`Assets/Code/Game@hotfix/ScreenView/ScreenViewEnum.cs`:

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

`ScreenViewManager.Start()` calls `MainLayer.BeginNavTo(this.defaultScreenTag)`, and `defaultScreenTag` defaults to `0`.

## Switching screens

```csharp
ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1);
```

Real usage (`Assets/Code/Game@hotfix/demo1/Window_Demo1.cs`):

```csharp
[ButtonOnclick("btn_01")]
private void OnClickBack()
    => ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
```

## Registration mechanism

`ScreenViewManager.Init()`:

```text
遍历 GetAllClassDatas()
 → CreateInstance<IScreenView>(attr.IntTag)
 → sv.Name = attr.IntTag
 → MainLayer.RegisterScreen(sv)
```

!!! warning "In the Editor, `MainLayer != null` returns immediately"
    ```csharp
    public override void Init()
    {
    #if UNITY_EDITOR
        if (MainLayer != null) return;     // 退出 PlayMode 不重载程序集时的保护
    #endif
        // ...
    }
    ```
    **The side effect**: when you press Play twice in a row in the Editor, newly added `[ScreenView]` classes are **not re-registered**. After changing code you need Unity to recompile (which triggers an assembly reload).

## Relationship with UIManager

| Layer | Responsibility |
|-------|----------------|
| `ScreenViewManager` / `ScreenViewLayer` | **Phase-level** switching (lobby ↔ battle); one active screen at a time |
| `UIManager` | **Window-level** management (several screens inside one phase) |

The typical pattern: one `IScreenView` maps to one main window, and `BeginInit` does `LoadWindow + ShowWindow`.

```csharp
// 阶段切换
ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Battle);

// 阶段内部切窗口
UIManager.Inst.ShowWindow(WinEnum.BattleResult);
```

## Multiple layers (`AddLayer`)

```csharp
public ScreenViewLayer AddLayer();
```

`ScreenViewManager` supports several `ScreenViewLayer`s (for example a "main UI layer" and a "battle layer" navigating independently), but there is **no usage example in the repository** — `MainLayer` is the only layer actually used.

## Common failures

| Symptom | Root cause |
|---------|------------|
| `别闹，当前就是<N>` | Navigating to the screen you are already on |
| The screen does not switch and there is no log | The target tag is not registered (check the `[ScreenView]` attribute and the enum value) |
| `别闹，前方没有view` | `BeginNavBack` / `BeginNavForward` called with an invalid cursor |
| A newly added ScreenView has no effect | The `MainLayer != null` early return in the Editor; recompile to fix |
| A `navViews[-2]` out-of-range exception | `BeginNavBack` called while `currentViewIndex == -1` |
| Two screens are visible at once | `BeginInit` runs before `BeginExit` (by design) |

## Related pages

- [Window](../uflux/window.md)
- [Startup Sequence](../architecture/bootstrap.md) — `ManagerOrder` and when navigation starts
- [Runtime Module Map](../architecture/runtime-modules.md)
