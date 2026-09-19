# Demo 解读

`Assets/Code/Game@hotfix/` 下的全部示例，逐个说明它教什么、调用了哪些 API。

!!! note "Demo 属于业务代码"
    这些文件在 `Assets/Code/` 下（**无 asmdef**），因此落在 `Assembly-CSharp`，**随热更下发**。它们既是示例，也是热更链路的活体验证。

## 目录总览

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

## `Window_DemoMain` —— 框架总览

`[UI((int)WinEnum.Win_Main, "Windows/window_demoMain")]`

| 按钮 | 演示的 API |
|------|-----------|
| `btn_UnitTest` | `ScriptLoder.IsRunning` 分支 → `TestRunner.RunHotfixUnitTest()` / `RunMonoCLRUnitTest()` |
| `btn_1` | `ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Demo1)` |
| `btn_4` | `UIManager.Inst.AsyncLoadWindows(list, (i, j) => …)` 异步批量加载 + 进度回调 |
| `btn_5` | **SQLite 五种查询写法**（见下） |
| `btn_6` | `BResources.Load<GameObject>("AssetTest/Cube")` 同步 + 异步 + 对象池 |

`btn_5` 是 SQLite 查询的最佳速查（真实代码）：

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

## `ScreenView/` —— 屏幕导航

| 文件 | 内容 |
|------|------|
| `ScreenViewEnum.cs` | `Main = 0, Demo1, Demo2, Demo3, Demo4` |
| `ScreenView_Main.cs` | 主界面（tag 0，默认启动页） |
| `ScreenView_Demo1_Screenview.cs` | `IScreenView` 的最小实现 |

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

→ 详见 [屏幕导航 ScreenView](../api/screen-navigation.md)。

## `demo1/` —— 最小窗口

`Window_Demo1.cs`：一个带返回按钮的窗口，演示"窗口 + 导航返回"。

```csharp
[ButtonOnclick("btn_01")]
private void OnClickBack()
    => ScreenViewManager.Inst.MainLayer.BeginNavTo(ScreenViewEnum.Main);
```

## `demo5/` —— 图集窗口

`Window_Demo5.cs`：加载 `SpriteAtlas` 资源的窗口，只有关闭按钮。演示"图集资源从 AB 加载"。

## `demo6_UFlux/` —— UFlux 全功能演示 ★

入口：`Window_FluxDemoMain`（`[UI((int)WinEnum.Win_UFlux, "Windows/UFlux/Window_FluxMain")]`），每个按钮打开一个子 Demo。

| 目录 | 文件 | 教什么 |
|------|------|-------|
| `00.State` | `Server_HeroData.cs` | State 数据类定义（Reducer 的 State 类型） |
| `01.Component/01` | `Component_Test001.cs` | 最小组件（`[Component]` + `[TransformPath]`） |
| `01.Component/02` | `Component_Test002.cs` | 组件 + 值绑定 |
| `01.Component/03` | `RD_Item.cs`、`Window_Test003.cs` | 组件 + RenderData + `PropsList` 列表 |
| `02.CustomComponentBindAdator/01` | `Window_CustomCponentBind.cs`、`ComponentBindAdaptorScrollRect.cs`、`ScrollRectAdaptor.cs`、`item/Component_ItemTest002.cs` | **自定义绑定适配器**（`ScrollRectAdaptor` 演示增/改/删） |
| `02.CustomComponentBindAdator/02` | `Window_CustomLogicBind.cs` | 自定义逻辑绑定 |
| `04.SimpleWindow` | `Window_SimpleDemo004.cs`、`SubWindow_Demo004.cs` | 最小窗口 + **子窗口** |
| `05.Window_Props` | `Window_PropsDemo05.cs` | **RenderData 驱动刷新** |
| `06.Window_Reducer` | `Window_Demo06.cs`、`Reducer_Demo06.cs`、`Reducer_Demo06Copy.cs`、`Com_HeroData.cs` | **Reducer / Store / 多 Store** |
| `07.Windows_DI` | `Widnow_DI.cs`、`ITestService.cs`、`Test1Service.cs`、`Test2Service.cs` | **依赖注入** |

!!! note "编号跳号"
    `00 / 01 / 02 / 04 / 05 / 06 / 07` —— **缺 `03`**。编号沿用了历史版本，实际不影响阅读。

### `04.SimpleWindow` —— 最小窗口 + 子窗口

```csharp
[UI((int)WinEnum.Win_UFlux_Test004, "Windows/UFlux/demo004/Window_SimpleDemo")]
public class Window_SimpleDemo004 : AWindow
{
    [SubWindow("SubWindow")]                       // 自动创建并注册子窗口
    private SubWindow_Demo004 subWindow;
}
```

### `05.Window_Props` —— RenderData 驱动刷新

**最小可运行的 RenderData 示例**：

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

!!! tip "注意 `t_Hp` 被绑了两次"
    同一节点 `Hero/Content/t_Hp` 分别绑到 `Text.text` 和 `Text.color`。这是 `[ComponentValueBind]` 的常规用法——**一个 RenderData 字段对应一个 UI 属性**，同一节点可以有多个绑定。

### `06.Window_Reducer` —— Reducer / Store

演示三种能力：

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

Reducer 里注册了**同步 / 同步带参 / 异步**三种：

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

!!! warning "`[Reducer]` 的参数类型必须精确相等"
    `RequestServer` 与 `RequestServer2` 挂了**同一个 tag**，框架靠形参数量 + `methodParams[1].ParameterType == @params.GetType()` 区分。
    传 `int` 只会命中 `RequestServer2`；不传参数只命中 `RequestServer`。

!!! tip "State → RenderData 的映射是手写的"
    源码注释明确："**不要刷新整个页面，只要刷新部分更新的数值即可**"。State 与 RenderData 不是一一对应，需要业务自己算（例：`HpColor` 由 `Hp` 推导）。

自动订阅：

```csharp
[Subscribe((int)Reducer_Demo06.Reducer06.InvokeAsyncTest)]
private void SubscribeInvokeAsyncTest(Server_HeroData state)
    => BDebug.Log($"订阅InvokeAsyncTest 返回成功!:{JsonMapper.ToJson(state, true)}");
```

→ 详见 [状态管理 Reducer/Store](../uflux/state-management.md)。

### `07.Windows_DI` —— 依赖注入

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

!!! warning "`Require` 必须是 `public` 且形参 > 0"
    框架用 `type.GetMethod("Require")` 查找，且 `if (@params.Length > 0)` 才执行注入。

→ 详见 [依赖注入](../uflux/dependency-injection.md)。

## `demo_EventManager/` —— 管理器范式

**不是 UFlux 的一部分**，演示的是 `ManagerBase<T, V>` 的"属性驱动自动注册"范式。

| 文件 | 内容 |
|------|------|
| `DemoEventAttribute.cs` | `: ManagerAttribute`，tag 是事件枚举 |
| `DemoEventEnum.cs` | 事件枚举 |
| `interface/IDemoEvent.cs` | `void Do()` |
| `Event_demo1.cs` / `Event_demo2.cs` | `[DemoEvent((int)DemoEventEnum.TestEvent1)]` 标记的实现 |
| `DemoEventManager.cs` | 管理器本体 |

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

**零注册代码**——挂上属性就会被 `ManagerInstHelper.LoadManager` 自动发现。

→ 详见 [管理器体系 ManagerBase](../api/manager-base.md)。

## `demo_StatusListener/` —— 事件总线

| 文件 | 内容 |
|------|------|
| `Window_StatusListener.cs` | `StatusListenerServer` / `AStatusListener` 的完整用法 |
| `HofixData.cs` | 热更侧数据类 |

覆盖的 API：

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

参数类型版本（推荐，避免拆箱）：

```csharp
var s2 = StatusListenerServer.Create(nameof(Msg_Test001.Msg2));
s2.AddListener<Msg_ParamTest>(nameof(Msg_Test001.Msg2), triggerNum: 10,
    action: o => { /* o.test1 */ });
```

→ 详见 [事件总线 EventBus](../api/event-bus.md)。

## 自定义 Attribute 与绑定适配器

`Assets/Code/BDFramework.Game/Uflux@hotfix/`：

### `ComponentBindAdaptor/`

| 文件 | 内容 |
|------|------|
| `CBA_Button.cs` | `Button` 适配器（onClick / onClick.AddListener / interactable） |
| `CBA_IButton.cs` | 自研 `IButton` 适配器 |
| `CBA_Image.cs` | `Image`（sprite / overrideSprite / color / fillAmount） |
| `CBA_Text.cs` | `Text`（text / color） |
| `CBA_Toggle.cs` | `Toggle`（group / onValueChanged / interactable / isOn） |
| `CBA_TransformHelper.cs` | **自定义逻辑适配器示范** |
| `CBA_UFluxBindLogic.cs` | **嵌套绑定示范**（`BindChild` / `BindChildren`） |

### `AutoInitComponentAttribute/`

| 文件 | 内容 |
|------|------|
| `ToggleClickBindAttribute.cs` | 字段为 `Action<bool>`，绑到 `Toggle.onValueChanged` |
| `ToggleValueChangeBindAttribute.cs` | 方法 `(bool isOn)`，绑到 `Toggle.onValueChanged` |

这两个是**自定义 `AutoAssignAttribute` 的参考实现**，其中 `ToggleClickBindAttribute` 演示了"**首次回调时才从字段取值**"的热更兼容模式。

→ 详见 [自动赋值属性](../uflux/auto-assign-attributes.md)。

## 相关页面

- [UI（UFlux）](../uflux/index.md)
- [窗口 Window](../uflux/window.md)
- [渲染数据 RenderData](../uflux/render-data.md)
- [状态管理 Reducer/Store](../uflux/state-management.md)
- [表格 SQLite](../api/sqlite.md)
