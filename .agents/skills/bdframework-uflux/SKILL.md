---
name: bdframework-uflux
description: 'BDFramework UFlux UI 框架开发技能。使用场景：写/改 Unity 窗口（AWindow）、组件（AComponent/ATComponent）、子窗口（SubWindow）、渲染数据（ARenderDataBase/ComponentValueBind/PropsList）、元素自动赋值（TransformPath/ButtonOnclick/UfluxComponentPath/SubWindowAttribute）、窗口消息（UIMsgData/UIMessageListener）、状态管理（AReducers/Store/StoreFactory/Reducer/Subscribe）、UFlux 依赖注入（Require/AddSingleton/AddTransient）。关键字：UFlux、AWindow、ATComponent、ARenderDataBase、Props、RenderData、ComponentValueBind、ComponentBindAdaptor、CBA_、UIMsgData、UIMessageListener、Reducer、Store、StoreFactory、StateBase、PropsList、Require、绑定不刷新、窗口不显示。'
---

# BDFramework UFlux UI 框架技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 新增/修改窗口、组件、子窗口
- 界面数据不刷新、节点引用为 null、按钮点击无响应
- 需要窗口间通信、跨界面共享状态
- 扩展元素自动赋值属性或值绑定适配器

不适用：屏幕级导航（用 `bdframework-navigation`）、纯 UI 布局与美术（框架不涉及）。

## 2. 铁律（先读这 6 条）

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | **窗口/组件不是 `MonoBehaviour`** | 写 `Awake`/`Update` 无效；拿节点必须用 `[TransformPath]` |
| 2 | **窗口必须有 `(string path)` 构造函数** | `UIManager.CreateWindow` 用 `Activator.CreateInstance(type, new object[]{resPath})`，缺了会运行时报错 |
| 3 | **`CommitRenderData()` 才会刷新** | 只改 `RenderData` 字段不会触发 UI 更新 |
| 4 | **`ShowWindow` 前必须已 `Load`** | 否则 `UI处于[unload,lock,open]状态之一` |
| 5 | **`[Reducer]` 方法的第 2 个形参类型必须与传入 `@params` 精确相等** | 用 `==` 比较类型，不做基类/接口匹配，不匹配就静默不触发 |
| 6 | **`PropsList` 的 `GetNewDatas()` 等是读取即消费** | 调用两次第二次返回空数组 |

## 3. 类型速查

### 命名空间（高频踩坑）

| 类型 | 命名空间 |
|------|---------|
| `AWindow<T>` / `ATComponent<T>` / `AComponent` / `ARenderDataBase` / `AStateBase` / `StateBase` | `BDFramework.UFlux` |
| `AReducers<T>` / `UFluxAction` / `ReducerAttribute` | `BDFramework.UFlux.Reducer` |
| `Store<S>` / `IStore` / `StoreFactory` / `StoreWrapper` / `SubscribeAttribute` | `BDFramework.UFlux.Contains` |
| `PropsList<T>` / `IPropsList` | `BDFramework.UFlux.Collections` |
| `UIMsgData` / `IWindow` / `UILayer` | `BDFramework.UFlux` |
| `OnWindowOpen` 等窗口状态消息 | `BDFramework.UFlux.WindowStatus` |

!!! danger "`Store<S>` 不在 `BDFramework.UFlux` 下"
    它在 **`BDFramework.UFlux.Contains`**。`StoreFactory` / `IStore` / `StoreWrapper` / `SubscribeAttribute` 同。写代码时最容易漏 `using`。

### 继承关系

```text
IComponent
 └─ ATComponent<T> where T : ARenderDataBase, new()
     ├─ AComponent : ATComponent<NoRenderData>
     └─ AWindow<TP> : ATComponent<TP>, IWindow, IUIMessage
         └─ AWindow : AWindow<NoRenderData>

ARenderDataBase : AStateBase
 └─ NoRenderData

AStateBase : IState, IPropertyChange
 └─ StateBase
```

## 4. 标准工作流

### 4.1 写一个窗口

```csharp
[UI((int)WinEnum.Shop, "Windows/Window_Shop")]
public class Window_Shop : AWindow
{
    [TransformPath("bg/title")]  private Text      _title;
    [TransformPath("list")]      private Transform _listRoot;

    [UfluxComponentPath("list/cell")] private Component_Cell _cell;
    [SubWindow("panel/topBar")]       private SubWindow_TopBar _topBar;

    [ButtonOnclick("bg/btnClose")]
    private void OnClickClose() => UIManager.Inst.CloseWindow(WinEnum.Shop);

    [UIMessageListener]
    private void OnMsg_Open(Msg_OpenShop msg) => Refresh(msg.ShopId);

    public Window_Shop(string path) : base(path) { }      // ★ 必需

    public override void Init()                            // Load 阶段调用
    {
        base.Init();
        _title.text = "商店";
    }
}
```

调用：

```csharp
UIManager.Inst.LoadWindow(WinEnum.Shop, UILayer.Center);
UIManager.Inst.ShowWindow(WinEnum.Shop, new Msg_OpenShop { ShopId = 1 });
```

### 4.2 数据驱动刷新

```csharp
public class RD_Hero : ARenderDataBase
{
    [ComponentValueBind("Hero/Name", typeof(Text), nameof(Text.text))]
    public string Name;

    [ComponentValueBind("Hero/Hp", typeof(Text), nameof(Text.text))]
    public int Hp;
}

public class Window_Hero : AWindow<RD_Hero>
{
    [ButtonOnclick("btnRefresh")]
    private void OnClickRefresh()
    {
        RenderData.Name = "吕布";
        RenderData.Hp   = Random.Range(1, 100);
        CommitRenderData();                // ★ 必须提交
    }
}
```

### 4.3 状态管理

```csharp
public class State_Hero : StateBase { public int Hp; public string Name; }
public enum HeroAction { Refresh, LoadAsync }

public class Reducer_Hero : AReducers<State_Hero>
{
    [Reducer((int)HeroAction.Refresh)]
    public State_Hero OnRefresh(State_Hero state, int hp)   // ★ 形参类型必须精确为 int
    {
        state.Hp = hp;
        return state;
    }

    [Reducer((int)HeroAction.LoadAsync)]
    async public Task<State_Hero> OnLoad(State_Hero state)
    {
        state.Name = await FetchName();
        return state;
    }
}

// 使用
var store = StoreFactory.CreateStore(new Reducer_Hero());
store.Subscribe(s => RefreshUI(s));
store.ScanThisSubscribe(this);              // 扫描 [Subscribe] 方法
store.Dispatch(HeroAction.Refresh, 80);
```

### 4.4 依赖注入

```csharp
// 注册（业务启动时）
UIManager.Inst.AddSingleton<PlayerModel>();

// 消费
public class Window_Player : AWindow
{
    private PlayerModel _model;

    public void Require(PlayerModel model) { _model = model; }   // ★ public，形参 > 0
}
```

## 5. 常见故障对照

| 现象 | 根因 | 修复 |
|------|------|------|
| 节点字段为 `null` | `[TransformPath]` 路径错误 | 路径只 `LogError` 不抛异常，逐级核对 |
| `未找到Btn:<path>` | `[ButtonOnclick]` 路径下无 `Button` | **会抛异常**，检查节点类型 |
| `[UfluxComponentPath]` 抛异常 | 节点不存在 | 它**必须**拿到 `Transform`，不存在就抛 |
| 界面不刷新 | 未调 `CommitRenderData()` | 或字段类型不在支持列表（会 `LogError("可能不支持的Props字段类型")`） |
| 部分字段不刷新 | 手动模式漏了 `SetPropertyChange` | `IsMunalMarkMode` 一旦为 true 不会回落 |
| 已有按钮监听被清掉 | `[ButtonOnclick]` 默认 `isTriggerThisOnly: true` | 传 `false` 保留 |
| `UI处于[unload,lock,open]状态之一` | 未 `Load` 就 `Show`，或重复 `Show` | 先 `LoadWindow` |
| 窗口空白 | `UIRoot` 下缺 `Bottom`/`Center`/`Top` | `UIManager.Init` 靠 `GameObject.Find` 找 |
| `窗口初始化出错` | `Init()` 内抛异常 | `Load()` 里 `try/catch` 只打日志 |
| `未触发同步Reducer:<enum>` | action 枚举与注册 key 不一致 | 核对 `[Reducer((int)X)]` |
| `触发失败，参数不匹配!` | `[Reducer]` 第 2 形参类型 ≠ `@params` 类型 | 用精确类型 |
| 消息没被处理 | 缺 `[UIMessageListener]`，或形参不是 1 个，或类型不完全匹配 | 三者逐一核对 |
| `Require` 没被调用 | 方法非 `public`，或形参为 0 个 | 框架用 `GetMethod("Require")` + `Length > 0` |
| 注入的服务是 `null` | 服务未注册，或注册类型与形参类型不精确相等 | `GetService` 用 `o.GetType() == type` |
| Editor 正常、真机崩 | 某字段赋值抛异常，Editor 下被 `try/catch` 吞掉 | 非 Editor 下 `InitComponent` 不捕获异常 |
| 内存持续增长 | 对未加载窗口高频 `SendMessage` | `uiDataCacheMap` **无上限** |

## 6. 详细参考

按需加载（不要一次全读）：

| 文件 | 内容 |
|------|------|
| [references/auto-assign-attributes.md](./references/auto-assign-attributes.md) | 5 个内置 Attribute 的精确语义与执行顺序 |
| [references/component-binding.md](./references/component-binding.md) | 值绑定、适配器注册、差异刷新算法 |
| [references/state-store.md](./references/state-store.md) | Reducer/Store 完整 API 与执行模式 |
| [references/lifecycle.md](./references/lifecycle.md) | 窗口/组件完整生命周期与 `UIManager` API 表 |

在线文档（面向人，更详细）：

- [UI（UFlux）](https://yimengfan.github.io/BDFramework.Core/ui/index.md)
- [窗口 Window](https://yimengfan.github.io/BDFramework.Core/ui/window.md)
- [状态管理](https://yimengfan.github.io/BDFramework.Core/ui/state-management.md)

## 7. 改动前检查清单

- [ ] 窗口有 `(string path)` 构造函数
- [ ] 没有在 UI 类里继承 `MonoBehaviour`
- [ ] 没有用 `Update()` 轮询状态（改用事件/State 通知）
- [ ] `[Reducer]` 的参数类型与 `Dispatch` 传入类型**完全一致**
- [ ] `CommitRenderData()` 已在改值后调用
- [ ] `[ButtonOnclick]` 是否需要保留既有监听（`isTriggerThisOnly`）
- [ ] 字符串 key 已替换为 `Enum` / `nameof`
- [ ] 新增/修改的类与成员注释为中英双语（中文在前）
