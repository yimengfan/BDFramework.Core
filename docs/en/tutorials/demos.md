# Demo Walkthrough

Every sample under `Assets/Code/Game@hotfix/`, described one by one: what it teaches and which APIs it calls.

!!! note "The demos are business code"
    These files live under `Assets/Code/` (**no asmdef**), so they land in `Assembly-CSharp` and are **shipped with the hotfix**. They are samples, but they are also a living verification of the hotfix pipeline.

## Directory overview

```text
Assets/Code/Game@hotfix/
├── Window_DemoMain.cs          框架总览入口窗口
├── WinEnum.cs                  窗口 ID 枚举（13 个）
├── HotfixCheck.cs              热更状态检查
├── ScreenView/                 屏幕导航
├── demo1/                      最小窗口 + 导航返回
├── demo5/                      图集窗口
├── demo6_UFlux/                UFlux 全功能演示 ★
├── demo_EventManager/          管理器范式（ManagerBase）
└── demo_StatusListener/        事件总线
```

## `Window_DemoMain` — framework overview

`[UI((int)WinEnum.Win_Main, "Windows/window_demoMain")]`

| Button | API demonstrated |
|--------|------------------|
| `btn_UnitTest` | `ScriptLoder.IsRunning` branch → `TestRunner.RunHotfixUnitTest()` / `RunMonoCLRUnitTest()` |
| `btn_1` | `ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1)` |
| `btn_4` | `UIManager.Inst.AsyncLoadWindows(list, (i, j) => …)` async batch loading + progress callback |
| `btn_5` | **The five SQLite query forms** (see below) |
| `btn_6` | `BResources.Load<GameObject>("AssetTest/Cube")` synchronous + asynchronous + object pool |

`btn_5` is the best quick reference for SQLite queries (real code):

```csharp
// ① 单条件
var hero = SqliteHelper.DB.GetTableRuntime().Where("id = {0}", 1).FromAll<Hero>();

// ② And 追加
var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").And.Where("id < 3").FromAll<Hero>();

// ③ Or 追加
ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").Or.Where("id = 3").FromAll<Hero>();

// ④ 同字段多值（WhereAnd）
ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).FromAll<Hero>();

// ⑤ 同字段多值（WhereOr）
ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).FromAll<Hero>();
```

## `ScreenView/` — screen navigation

| File | Contents |
|------|----------|
| `ScreenViewEnum.cs` | `Main = 0, Demo1, Demo2, Demo3, Demo4` |
| `ScreenView_Main.cs` | The main screen (tag 0, the default startup page) |
| `ScreenView_Demo1_Screenview.cs` | The minimal `IScreenView` implementation |

```csharp
[ScreenView((int)ScreenViewEnum.Demo1)]
public class ScreenView_Demo1_Screenview : IScreenView
{
    public int  Name   { get; set; }
    public bool IsLoad { get; private set; }

    public void BeginInit()
    {
        this.IsLoad = true;                                          // ★ 必须设 true
        UIManager.Inst.LoadWindow(WinEnum.Win_Demo1);
        UIManager.Inst.ShowWindow(WinEnum.Win_Demo1);
    }

    public void BeginExit()
    {
        this.IsLoad = false;                                         // ★ 必须设 false
    }
}
```

→ See [Screen Navigation](../api/screen-navigation.md) for details.

## `demo1/` — minimal window

`Window_Demo1.cs`: a window with a back button, demonstrating "window + navigation back".

```csharp
[ButtonOnclick("btn_01")]
private void OnClickBack()
    => ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
```

## `demo5/` — sprite atlas window

`Window_Demo5.cs`: a window that loads a `SpriteAtlas` asset and only has a close button. Demonstrates "loading atlas assets from an AB".

## `demo6_UFlux/` — full UFlux feature demo ★

Entry point: `Window_FluxDemoMain` (`[UI((int)WinEnum.Win_UFlux, "Windows/UFlux/Window_FluxMain")]`); every button opens one sub-demo.

| Directory | File | What it teaches |
|-----------|------|-----------------|
| `00.State` | `Server_HeroData.cs` | Defining the State data class (the State type of a Reducer) |
| `01.Component/01` | `Component_Test001.cs` | Minimal component (`[Component]` + `[TransformPath]`) |
| `01.Component/02` | `Component_Test002.cs` | Component + value binding |
| `01.Component/03` | `RD_Item.cs`, `Window_Test003.cs` | Component + RenderData + `PropsList` list |
| `02.CustomComponentBindAdator/01` | `Window_CustomCponentBind.cs`, `ComponentBindAdaptorScrollRect.cs`, `ScrollRectAdaptor.cs`, `item/Component_ItemTest002.cs` | **Custom binding adaptor** (`ScrollRectAdaptor` demonstrates add / update / remove) |
| `02.CustomComponentBindAdator/02` | `Window_CustomLogicBind.cs` | Custom logic binding |
| `04.SimpleWindow` | `Window_SimpleDemo004.cs`, `SubWindow_Demo004.cs` | Minimal window + **subwindow** |
| `05.Window_Props` | `Window_PropsDemo05.cs` | **RenderData-driven refresh** |
| `06.Window_Reducer` | `Window_Demo06.cs`, `Reducer_Demo06.cs`, `Reducer_Demo06Copy.cs`, `Com_HeroData.cs` | **Reducer / Store / multiple Stores** |
| `07.Windows_DI` | `Widnow_DI.cs`, `ITestService.cs`, `Test1Service.cs`, `Test2Service.cs` | **Dependency injection** |

!!! note "The numbering skips"
    `00 / 01 / 02 / 04 / 05 / 06 / 07` — **`03` is missing**. The numbering carries over from historical versions and does not affect reading.

### `04.SimpleWindow` — minimal window + subwindow

```csharp
[UI((int)WinEnum.Win_UFlux_Test004, "Windows/UFlux/demo004/Window_SimpleDemo")]
public class Window_SimpleDemo004 : AWindow
{
    [SubWindow("SubWindow")]                       // 自动创建并注册子窗口
    private SubWindow_Demo004 subWindow;
}
```

### `05.Window_Props` — RenderData-driven refresh

**The smallest runnable RenderData example**:

```csharp
public class RD_HeroData : ARenderDataBase
{
    [ComponentValueBind("Hero/Content/t_Name",  typeof(Text), nameof(Text.text))]
    public string Name;

    [ComponentValueBind("Hero/Content/t_Hp",    typeof(Text), nameof(Text.text))]
    public int Hp;

    [ComponentValueBind("Hero/Content/t_MaxHp", typeof(Text), nameof(Text.text))]
    public int MaxHp;

    // 同一个节点可以绑到不同的 UI 属性
    [ComponentValueBind("Hero/Content/t_Hp",    typeof(Text), nameof(Text.color))]
    public Color HpColor;
}

[UI((int)WinEnum.Win_UFlux_Test005, "Windows/UFlux/demo005/Window_PropsDemo")]
public class Window_PropsDemo05 : AWindow<RD_HeroData>
{
    [ButtonOnclick("btn_TestWindowProps")]
    private void btn_TestWindowProps()
    {
        this.RenderData.Name   = "吕布";
        this.RenderData.Hp     = Random.Range(1, 100);
        this.RenderData.MaxHp  = 100;
        this.RenderData.HpColor = this.RenderData.Hp < 50 ? Color.red : Color.blue;

        this.CommitRenderData();          // ★ 提交才会触发差异刷新
    }
}
```

!!! tip "Note that `t_Hp` is bound twice"
    The same node, `Hero/Content/t_Hp`, is bound to `Text.text` and to `Text.color` separately. This is a normal use of `[ComponentValueBind]` — **one RenderData field maps to one UI property**, and a single node can carry several bindings.

### `06.Window_Reducer` — Reducer / Store

Three capabilities are demonstrated:

```csharp
public override void Init()
{
    base.Init();

    // ① 单 Store + 全量订阅
    store = StoreFactory.CreateStore(new Reducer_Demo06());
    store.Subscribe(newState => StateToRenderData(newState));
    store.ScanThisSubscribe(this);              // 扫描 [Subscribe] 方法

    // ② 多 Store（StoreWrapper）
    storeWrapper = StoreFactory.CreateStore(new Reducer_Demo06(), new Reducer_Demo06Copy());
    storeWrapper.Subscribe<S_HeroDataDemo6Copy>(s => State2ToRenderData2(s));
    storeWrapper.Subscribe<Server_HeroData>(s => StateToRenderData(s));
}
```

The Reducer registers three kinds — **synchronous / synchronous with a parameter / asynchronous**:

```csharp
public class Reducer_Demo06 : AReducers<Server_HeroData>
{
    public enum Reducer06 { InvokeSyncTest, InvokeAsyncTest }

    readonly public string url = "https://.../DemoForUFlux/";

    // 同步，无形参
    [Reducer((int)Reducer06.InvokeSyncTest)]
    private Server_HeroData RequestServer(Server_HeroData old)
    {
        var ret = new WebClient().DownloadString(url + "api/bdframework/getherodata");
        return JsonMapper.ToObject<Server_HeroData>(ret);
    }

    // 同步，带 int 形参（★ 类型必须精确匹配）
    [Reducer((int)Reducer06.InvokeSyncTest)]
    private Server_HeroData RequestServer2(Server_HeroData old, int @params) { … }

    // 异步
    [Reducer((int)Reducer06.InvokeAsyncTest)]
    async private Task<Server_HeroData> RequestServerByAsync(Server_HeroData old)
    {
        var ret = await new WebClient().DownloadStringTaskAsync(url + "api/...");
        return JsonMapper.ToObject<Server_HeroData>(ret);
    }
}
```

!!! warning "`[Reducer]` parameter types must match exactly"
    `RequestServer` and `RequestServer2` carry **the same tag**; the framework tells them apart by parameter count plus `methodParams[1].ParameterType == @params.GetType()`.
    Passing an `int` only hits `RequestServer2`; passing no argument only hits `RequestServer`.

!!! tip "The State → RenderData mapping is hand-written"
    A comment in the source states it plainly: "**do not refresh the whole page, only refresh the values that actually changed**". State and RenderData are not one-to-one, so the business code has to work it out (for example, `HpColor` is derived from `Hp`).

Automatic subscription:

```csharp
[Subscribe((int)Reducer_Demo06.Reducer06.InvokeAsyncTest)]
private void SubscribeInvokeAsyncTest(Server_HeroData state)
    => BDebug.Log($"订阅InvokeAsyncTest 返回成功!:{JsonMapper.ToJson(state, true)}");
```

→ See [State Management (Reducer/Store)](../uflux/state-management.md) for details.

### `07.Windows_DI` — dependency injection

```csharp
[UI((int)WinEnum.Win_UFlux_Test007_DI, "Windows/UFlux/demo007/Window_DI")]
public class Widnow_DI : AWindow
{
    private Test1Service testService1;
    private Test2Service testService2;

    /// <summary>
    /// 这里是DI请求对象的接口
    /// </summary>
    public void Require(Test1Service service1, Test2Service service2)
    {
        testService1 = service1;
        testService2 = service2;
    }
}
```

!!! warning "`Require` must be `public` and take more than zero parameters"
    The framework looks it up with `type.GetMethod("Require")`, and injection only runs when `if (@params.Length > 0)`.

→ See [Dependency Injection](../uflux/dependency-injection.md) for details.

## `demo_EventManager/` — the manager pattern

**Not part of UFlux**; it demonstrates the "attribute-driven automatic registration" pattern of `ManagerBase<T, V>`.

| File | Contents |
|------|----------|
| `DemoEventAttribute.cs` | `: ManagerAttribute`, whose tag is the event enum |
| `DemoEventEnum.cs` | The event enum |
| `interface/IDemoEvent.cs` | `void Do()` |
| `Event_demo1.cs` / `Event_demo2.cs` | Implementations marked `[DemoEvent((int)DemoEventEnum.TestEvent1)]` |
| `DemoEventManager.cs` | The manager itself |

```csharp
public class DemoEventManager : ManagerBase<DemoEventManager, DemoEventAttribute>
{
    public void Do(DemoEventEnum @enum, object o = null)
    {
        // 每次 new 一个；实际项目可自行池化
        var @event = CreateInstance<IDemoEvent>((int)@enum);
        if (@event != null) @event.Do();
        else BDebug.Log("获取不到 event:" + @enum.ToString());
    }
}
```

**Zero registration code** — attach the attribute and `ManagerInstHelper.LoadManager` discovers it automatically.

→ See [Managers (ManagerBase)](../api/manager-base.md) for details.

## `demo_StatusListener/` — event bus

| File | Contents |
|------|----------|
| `Window_StatusListener.cs` | Complete usage of `StatusListenerServer` / `AStatusListener` |
| `HofixData.cs` | The hotfix-side data class |

The APIs it covers:

```csharp
var serviceEnum = StatusListenerServer.Create(nameof(StatusListenerEnum));

serviceEnum.SetData(StatusListenerEnum.Test, 1);
serviceEnum.GetData<int>(StatusListenerEnum.Test);
serviceEnum.AddListener(StatusListenerEnum.Test, o => Debug.Log("监听热更Enum :" + o));
serviceEnum.AddListenerOnce(StatusListenerEnum.Once, o => Debug.Log("监听热更Enum Once:" + o));
serviceEnum.TriggerEvent(StatusListenerEnum.Test, 2);
serviceEnum.RemoveListener(StatusListenerEnum.Test);
serviceEnum.ClearListener(StatusListenerEnum.Test);
```

The parameter-typed version (recommended, it avoids boxing):

```csharp
var s2 = StatusListenerServer.Create(nameof(Msg_Test001.Msg2));
s2.AddListener<Msg_ParamTest>(nameof(Msg_Test001.Msg2), triggerNum: 10,
    action: o => { /* o.test1 */ });
```

→ See [Event Bus](../api/event-bus.md) for details.

## Custom attributes and binding adaptors

`Assets/Code/BDFramework.Game/Uflux@hotfix/`:

### `ComponentBindAdaptor/`

| File | Contents |
|------|----------|
| `CBA_Button.cs` | The `Button` adaptor (onClick / onClick.AddListener / interactable) |
| `CBA_IButton.cs` | The in-house `IButton` adaptor |
| `CBA_Image.cs` | `Image` (sprite / overrideSprite / color / fillAmount) |
| `CBA_Text.cs` | `Text` (text / color) |
| `CBA_Toggle.cs` | `Toggle` (group / onValueChanged / interactable / isOn) |
| `CBA_TransformHelper.cs` | **A worked example of a custom logic adaptor** |
| `CBA_UFluxBindLogic.cs` | **A worked example of nested binding** (`BindChild` / `BindChildren`) |

### `AutoInitComponentAttribute/`

| File | Contents |
|------|----------|
| `ToggleClickBindAttribute.cs` | The field is an `Action<bool>`, bound to `Toggle.onValueChanged` |
| `ToggleValueChangeBindAttribute.cs` | A method `(bool isOn)`, bound to `Toggle.onValueChanged` |

These two are **the reference implementations for a custom `AutoAssignAttribute`**; `ToggleClickBindAttribute` in particular demonstrates the hotfix-compatible pattern of "**only read the field value on the first callback**".

→ See [Auto-Assign Attributes](../uflux/auto-assign-attributes.md) for details.

## Related pages

- [UI (UFlux)](../uflux/index.md)
- [Window](../uflux/window.md)
- [RenderData](../uflux/render-data.md)
- [State Management (Reducer/Store)](../uflux/state-management.md)
- [Tables (SQLite)](../api/sqlite.md)
