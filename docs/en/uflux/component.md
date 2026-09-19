# Component

A Component is UFlux's **smallest UI unit**: a screen fragment that can be loaded and refreshed on its own. A window is itself a special kind of component (`AWindow<TP> : ATComponent<TP>`).

## Type hierarchy

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

!!! note "`NoRenderData` is the 'no render data' placeholder type"
    Both `AWindow` (the non-generic version) and `AComponent` are based on `NoRenderData`. Only reach for `AWindow<TP>` / `ATComponent<TP>` when you need data-driven refresh.

## Declaring the asset path: `[Component]`

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

!!! warning "`[Component]` only takes effect in the `ATComponent(bool)` constructor"
    `ATComponent(bool isLoadAsset = true)` reads the `[Component]` attribute; if `IsAsyncLoad == false` it calls **`Load()` immediately in the constructor**. Components built through the `(Transform)` or `(string)` constructor do not read this attribute.

## Three constructor semantics

| Constructor | Loads the asset? | Calls `Init()` immediately | Typical scenario |
|------|-------------|--------------|---------|
| `ATComponent(bool isLoadAsset = true)` | Decided by `[Component]` | Called inside `Load()` | Self-loading components |
| `ATComponent(Transform trans)` | ✗ takes over an existing node | ✓ `InitComponent(this)` at the end of the constructor | Binding a node that already exists in the scene |
| `ATComponent(string resPath)` | ✗ only records the path | ✗ | Deferred loading |

## Lifecycle

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

!!! danger "`AsyncLoad`'s callback also runs on the failure path"
    `callback?.Invoke()` is also called on the exception path. The callback **must check `IsLoad`**, otherwise it will operate on a `null` `Transform`.

## Attaching to a window

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

`AWindow<TP>.AddComponent`:

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

!!! warning "`AddComponent` does not call `Init()`"
    It only sets `Parent` and adds to the list. `Init()` has to be triggered by the component itself, either in the constructor (the `Transform` version) or in `Load()`.

## Custom component example

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

→ For value-binding details see [RenderData](render-data.md).

## Differences from a window

| Dimension | `AWindow<TP>` | `ATComponent<T>` |
|------|--------------|------------------|
| Mounted to a `UILayer` | ✓ | ✗ (follows its parent node) |
| Enters `UIManager.windowMap` | ✓ | ✗ |
| Has `ServiceContainer` / `State` | ✓ | ✗ |
| Responds to `[UIMessageListener]` | ✓ | ✗ |
| Loads its own asset | ✓ | ✓ |
| Has `RenderData` and value binding | ✓ | ✓ |

**Components have no message mechanism.** Communication between components goes through `Parent.SendMessage(...)` or a direct method call.

## An unused interface: `ISubComponent`

```csharp
// Runtime/UI/View/Component/ISubComponent.cs
public interface ISubComponent
{
    void RegisterSubComponent(string name, IComponent component);
}
```

**No implementation in the repository uses it.** The sub-component mechanism is actually provided by `[UfluxComponentPath]` + `IWindow.AddComponent`. See the [Refactor Backlog](../architecture/refactor-backlog.md#ref-11-empty-types).

## Common failures

| Symptom | Root cause |
|------|------|
| The component's `Transform` is `null` | Built through the `(string)` constructor without calling `Load()`; or the `AsyncLoad` callback ran even though it failed |
| `Init()` is never called | `AddComponent` does not call `Init()`; only `Load()` and the `(Transform)` constructor do |
| Node reference is null | Wrong `[TransformPath]` path (**only logs an error**) |
| The component is `Init()`ed twice | The `(Transform)` constructor is combined with a manual `Init()` |
| The asset is never released | Only `Destroy()` calls `UFluxUtils.Unload(resPath)`; `Close()` alone does not |

## Related pages

- [Window](window.md)
- [RenderData](render-data.md)
- [Auto-Assign Attributes](auto-assign-attributes.md)
