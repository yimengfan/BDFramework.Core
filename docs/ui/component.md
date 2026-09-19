# 组件 Component

Component 是 UFlux 的**最小 UI 单元**：一个可独立加载、独立刷新的界面片段。窗口本身就是一种特殊组件（`AWindow<TP> : ATComponent<TP>`）。

## 类型体系

```csharp
public interface IComponent
{
    IWindow Parent { get; set; }
    Transform Transform { get; }
    bool IsLoad { get; }
    bool IsOpen { get; }
    bool IsDestroy { get; }
    void Load();
    void AsyncLoad(Action callback);
    void Init();
    void Open(UIMsgData uiMsg = null);
    void Close();
    void Destroy();
    void SetRenderData(ARenderDataBase renderDataBase);
}

abstract public class ATComponent<T> : IComponent where T : ARenderDataBase, new()
{
    public T RenderData { get; private set; }
    public IWindow Parent { get; set; }
    public Transform Transform { get; private set; }
    public bool IsLoad { get; private set; }      // Load 成功后 true
    public bool IsOpen { get; private set; }
    public bool IsDestroy { get; private set; }

    public ATComponent(bool isLoadAsset = true);  // 读 [Component] → 同步则立即 Load()
    public ATComponent(Transform trans);          // 绑定既有节点 → InitComponent(this)
    public ATComponent(string resPath);           // 只记路径，不加载

    public void Load();
    public void AsyncLoad(Action callback = null);
    public void SetRenderData(T props);
    public void SetRenderData(ARenderDataBase renderDataBase);
    protected void CommitRenderData(Transform transform = null);
    virtual public void Init();
    virtual public void Open(UIMsgData uiMsg = null);
    virtual public void Close();
    virtual public void Destroy();
}

abstract public class AComponent : ATComponent<NoRenderData>
{
    protected AComponent(Transform trans);
    protected AComponent(bool isLoadAsset = true);
    protected AComponent(string resPath);
}

public class NoRenderData : ARenderDataBase { }   // 声明在 AComponent.cs
```

!!! note "`NoRenderData` 是"无渲染数据"的占位类型"
    `AWindow`（无泛型版本）与 `AComponent` 都基于 `NoRenderData`。需要数据驱动刷新时才用 `AWindow<TP>` / `ATComponent<TP>`。

## 资源路径声明：`[Component]`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute : Attribute
{
    public string Path { get; }
    public bool IsAsyncLoad { get; }

    public ComponentAttribute(string path, bool isAsyncLoad = false);
}
```

```csharp
[Component("Components/ItemCell", isAsyncLoad: false)]
public class Component_ItemCell : AComponent
{
    [TransformPath("txtName")] private Text _name;
}
```

!!! warning "`[Component]` 只在 `ATComponent(bool)` 构造里生效"
    `ATComponent(bool isLoadAsset = true)` 会读 `[Component]` 属性；若 `IsAsyncLoad == false` 则**构造时立即 `Load()`**。走 `(Transform)` 或 `(string)` 构造的组件不会读这个属性。

## 三种构造语义

| 构造 | 是否加载资源 | 立即 `Init()` | 典型场景 |
|------|-------------|--------------|---------|
| `ATComponent(bool isLoadAsset = true)` | 按 `[Component]` 决定 | `Load()` 内部调 | 自加载型组件 |
| `ATComponent(Transform trans)` | ✗ 接管已有节点 | ✓ 构造末尾 `InitComponent(this)` | 绑定场景中已存在的节点 |
| `ATComponent(string resPath)` | ✗ 只记路径 | ✗ | 延迟加载 |

## 生命周期

```text
构造
 ├─ (bool)   → 读 [Component]，同步时立刻 Load()
 ├─ (Transform) → InitComponent(this)   ← 节点赋值在这里完成
 └─ (string) → 只存 resPath

Load()
 ├─ resPath == null → 直接 return
 ├─ UFluxUtils.Load<GameObject>(resPath) → Instantiate
 ├─ IsLoad = true
 ├─ UFluxUtils.InitComponent(this)      ← 执行所有 AutoAssign 属性
 └─ Init()                              ← try/catch，异常只打日志 "窗口初始化出错"

AsyncLoad(callback)
 ├─ UFluxUtils.AsyncLoad<GameObject>(resPath)
 ├─ resPath 为空 → 抛 "窗口资源不存在"
 └─ callback?.Invoke()                  ← ★ 异常路径也会调用

SetRenderData(T props) → 赋值 + CommitRenderData()
CommitRenderData(transform = null)
 └─ UFluxUtils.SetComponentRenderData(transform ?? this.Transform, RenderData)

Destroy()
 └─ UFluxUtils.Destroy(go) + Transform = null + UFluxUtils.Unload(resPath) + IsDestroy = true
```

!!! danger "`AsyncLoad` 的回调在失败路径也会执行"
    `callback?.Invoke()` 在异常路径也会被调用。回调里**必须检查 `IsLoad`**，否则会对着 `null` 的 `Transform` 操作。

## 挂载到窗口

```csharp
// 方式一：[UfluxComponentPath] 自动创建（见「自动赋值属性」）
[UfluxComponentPath("list")]
private Component_ItemCell _cell;

// 方式二：手动挂载
public override void Init()
{
    base.Init();
    var cell = new Component_ItemCell(transform.Find("list"));
    this.AddComponent(cell);           // 会设置 Parent 并加入 ComponentList
}
```

`AWindow<TP>.AddComponent`：

```csharp
public void AddComponent(params IComponent[] coms)
{
    foreach (var com in coms)
    {
        com.Parent = this;
        ComponentList.Add(com);
    }
}
```

!!! warning "`AddComponent` 不调 `Init()`"
    它只设 `Parent` 并加入列表。`Init()` 需要组件自己在构造（`Transform` 版本）或 `Load()` 时触发。

## 自定义组件示例

```csharp
// 带 RenderData 的组件
public class RenderData_ItemCell : ARenderDataBase
{
    public string Name;
    public int Count;
    public string Icon;
}

[Component("Components/ItemCell")]
public class Component_ItemCell : ATComponent<RenderData_ItemCell>
{
    [TransformPath("txtName")]  private Text   _name;
    [TransformPath("txtCount")] private Text   _count;
    [TransformPath("imgIcon")]  private Image  _icon;

    // 用 ComponentValueBind 声明"字段 → UI 属性"的映射
    [ComponentValueBind("txtName",  typeof(Text),  nameof(Text.text))]
    public string BindName;

    [ComponentValueBind("txtCount", typeof(Text),  nameof(Text.text))]
    public string BindCount;

    [ComponentValueBind("imgIcon",  typeof(Image), nameof(Image.sprite))]
    public string BindIcon;

    public void SetItem(Item item)
    {
        SetRenderData(new RenderData_ItemCell
        {
            Name  = item.Name,
            Count = item.Count,
            Icon  = item.Icon,
        });
    }
}
```

```csharp
// 使用
var cell = new Component_ItemCell(transform.Find("list/item0"));
cell.SetItem(item);
```

→ 值绑定细节见[渲染数据 RenderData](render-data.md)。

## 与窗口的差异

| 维度 | `AWindow<TP>` | `ATComponent<T>` |
|------|--------------|------------------|
| 挂载到 `UILayer` | ✓ | ✗（跟随父节点） |
| 进入 `UIManager.windowMap` | ✓ | ✗ |
| 有 `ServiceContainer` / `State` | ✓ | ✗ |
| 响应 `[UIMessageListener]` | ✓ | ✗ |
| 独立加载资源 | ✓ | ✓ |
| 有 `RenderData` 与值绑定 | ✓ | ✓ |

**Component 没有消息机制**。组件间的通信要走 `Parent.SendMessage(...)` 或直接方法调用。

## 未使用的接口：`ISubComponent`

```csharp
// Runtime/UI/View/Component/ISubComponent.cs
public interface ISubComponent
{
    void RegisterSubComponent(string name, IComponent component);
}
```

**仓库中没有任何实现类使用它。** 子组件机制实际由 `[UfluxComponentPath]` + `IWindow.AddComponent` 完成。见[重构清单](../architecture/refactor-backlog.md#ref-11-empty-types)。

## 常见故障

| 现象 | 根因 |
|------|------|
| 组件 `Transform` 为 `null` | 走了 `(string)` 构造但没调 `Load()`；或 `AsyncLoad` 失败后回调仍执行 |
| `Init()` 没被调用 | `AddComponent` 不调 `Init()`；只有 `Load()` / `(Transform)` 构造会调 |
| 节点引用为 null | `[TransformPath]` 路径错误（只 `LogError`） |
| 组件重复 `Init()` | 同时用了 `(Transform)` 构造与手动 `Init()` |
| 资源没释放 | `Destroy()` 才会 `UFluxUtils.Unload(resPath)`；只 `Close()` 不会 |

## 相关页面

- [窗口 Window](window.md)
- [渲染数据 RenderData](render-data.md)
- [自动赋值属性](auto-assign-attributes.md)
