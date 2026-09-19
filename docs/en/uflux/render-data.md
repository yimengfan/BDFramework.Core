# RenderData

RenderData (formerly known as **Props**) describes "what the screen should look like right now". It is **pure data**, consumed by `AComponentBindAdaptor` and written into UI controls.

!!! info "Terminology changes"
    `APropsBase` / `props` / `CommitProps` in older documentation were renamed starting with v3:

    | Old name | Current name |
    |------|------|
    | `APropsBase` | `ARenderDataBase` |
    | `props` | `RenderData` |
    | `CommitProps()` | `CommitRenderData()` |
    | `AutoInitComponentAttribute` | `AutoAssignAttribute` |

## Base class

```csharp
// Runtime/UI/View/Props/ARenderDataBase.cs
abstract public class ARenderDataBase : AStateBase
{
    public Type ComponentType { get; set; }        // 由框架回填，标识所属组件类型
    public Transform Transform { get; private set; }
}
```

`ARenderDataBase` inherits `AStateBase`, so it **comes with dirty-marking built in** (`SetPropertyChange` / `GetChangedPropertise`).

```csharp
public class RenderData_Item : ARenderDataBase
{
    public string Name;
    public int    Count;
    public string Icon;
}
```

## Declaring value bindings: `[ComponentValueBind]`

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ComponentValueBindAttribute : Attribute
{
    // transformPath : 节点路径
    // uiType        : 目标控件类型（用于查适配器）
    // functionName  : 适配器里注册的字段名
    public ComponentValueBindAttribute(string transformPath, Type uiType, string functionName);
}
```

```csharp
[Component("Components/ItemCell")]
public class Component_ItemCell : ATComponent<RenderData_Item>
{
    // 字段名随意（BindName），真正的映射由第三个参数决定
    [ComponentValueBind("txtName", typeof(Text), nameof(Text.text))]
    public string BindName;

    [ComponentValueBind("imgIcon", typeof(Image), nameof(Image.sprite))]
    public string BindIcon;
}
```

!!! danger "`uiType` is resolved immediately in the constructor"
    The constructor of `ComponentValueBindAttribute` calls `ComponentBindAdaptorManager.Inst.GetBindComponentType(uiType.FullName)`. **If resolution fails it logs an error and `Type == null`**, and at runtime that field is silently left unbound.

## The adapter mechanism

### Abstract base class

```csharp
abstract public class AComponentBindAdaptor
{
    public delegate void SetUIBehaviourDelegate(UIBehaviour ui, object value);
    public delegate void SetTransformDelegate(Transform transform, object value);

    protected Dictionary<string, SetUIBehaviourDelegate> setPropComponentBindMap;
    protected Dictionary<string, SetTransformDelegate>    setPropCustomLogicMap;

    public AComponentBindAdaptor();       // 内部调 Init()
    public virtual void Init();           // 默认注册 enabled / gameObject.active
    public virtual void SetData(UIBehaviour uiBehaviour, string propName, object propValue);
    public virtual void SetData(Transform transform,     string propName, object propValue);
}
```

### Registering an adapter

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ComponentBindAdaptorAttribute : ManagerAttribute
{
    public Type BindType { get; }
    public ComponentBindAdaptorAttribute(Type bindType) : base(bindType.FullName) { … }
}
```

```csharp
// 自定义适配器
[ComponentBindAdaptor(typeof(MyWidget))]
public class CBA_MyWidget : AComponentBindAdaptor
{
    public override void Init()
    {
        base.Init();     // 保留 enabled / active
        base.setPropComponentBindMap.Add(nameof(MyWidget.Value), SetValue);
    }

    private void SetValue(UIBehaviour ui, object value)
        => (ui as MyWidget).Value = (float)value;
}
```

`ComponentBindAdaptorManager` (`ManagerBase<ComponentBindAdaptorManager, ComponentBindAdaptorAttribute>`) instantiates every `ClassData` entry in `Init()`, and starts a 30-second coroutine that cleans up cached `Transform`s which have been destroyed.

### Built-in adapters (business side, `Assets/Code/BDFramework.Game/Uflux@hotfix/ComponentBindAdaptor/`)

| Adapter | `BindType` | Registered field keys |
|--------|-----------|---------------|
| `CBA_Button` | `Button` | `Button.onClick` (replace), `Button.onClick.AddListener` (append), `Button.interactable` |
| `CBA_IButton` | `IButton` | `IButton.onClick`, `IButton.onClick.AddListener` |
| `CBA_Image` | `Image` | `Image.sprite`, `Image.overrideSprite`, `Image.color`, `Image.fillAmount` |
| `CBA_Text` | `Text` | `Text.text`, `Text.color` |
| `CBA_Toggle` | `Toggle` | `Toggle.group`, `Toggle.onValueChanged`, `Toggle.interactable`, `Toggle.isOn` |
| `CBA_TransformHelper` | `TransformHelper` | `TransformHelper.ShowHideChildByNumber` (custom-logic example) |
| `CBA_UFluxBindLogic` | `UFluxBindLogic` | `UFluxBindLogic.BindChild`, `BindChildren` |
| Base-class default | — | `UIBehaviour.enabled`, `UIBehaviour.gameObject.active` |

!!! note "`TransformHelper` / `UFluxBindLogic` are placeholder classes"
    Their **method bodies are empty**; their only reason to exist is to give `[ComponentValueBind(..., typeof(X), nameof(X.Method))]` a compile-time symbol (`nameof` needs a real type). The actual logic lives in `CBA_TransformHelper` / `CBA_UFluxBindLogic`.

`CBA_Image`'s `Image.sprite` accepts a string: it automatically calls `UFluxUtils.Load<Sprite>(string)`.

## Diff refresh mechanism

`SetRenderData(props)` → `CommitRenderData()` → `UFluxUtils.SetComponentRenderData` → `ComponentBindAdaptorManager.SetTransformRenderData`.

```text
SetTransformRenderData(transform, newRenderData)
 ├─ 首次：BindTransformRenderData 解析全部 [ComponentValueBind] 字段，固化 FieldCacheMap
 └─ 后续：AnalysisRenderDataChanged 只挑出变化的字段，逐个 SetData
```

The cache key is the **`Transform` instance**; `FieldCacheMap` is fixed on the first bind.

### Change-detection rules

| Field type | Decision |
|---------|------|
| `IsMunalMarkMode == true` | Only `GetChangedPropertise()` is used (driven by manual `SetPropertyChange`) |
| New value is `null` | **Skipped** (the UI is not updated) |
| `LastValue == null` | Treated as changed |
| Value type / `Namespace == "System"` | Compared with `Equals` |
| Nested `ARenderDataBase` | Analysed recursively (**the source comment warns not to nest more than 2 levels**) |
| `IPropsList` | Looks at `IsChanged` |
| Other types | `BDebug.LogError("可能不支持的Props字段类型")` |

### Manual marking mode

Once `SetPropertyChange` has been called, that RenderData **permanently enters manual mode** (`IsMunalMarkMode` never falls back once it is `true`):

```csharp
// 自动模式：框架逐字段比较
_renderData.Name = "新名字";
cell.SetRenderData(_renderData);

// 手动模式：只刷新显式标记过的字段
_renderData.Name = "新名字";
_renderData.SetPropertyChange(nameof(RenderData_Item.Name));
cell.SetRenderData(_renderData);
```

!!! tip "When to use manual mode"
    When there are many fields but only one or two change each time, manual mode avoids the cost of comparing every field. The price is that **a missed mark means no refresh**.

## `PropsList<T>`: list diffing

```csharp
namespace BDFramework.UFlux.Collections

public interface IPropsList
{
    int Count { get; }
    void Foreach(Action<int, ARenderDataBase> action);
    bool IsChanged { get; }
    ARenderDataBase[] GetNewDatas();
    ARenderDataBase[] GetRemovedDatas();
    ARenderDataBase[] GetChangedDatas();
    void ClearChangedData();
}

public class PropsList<T> : IPropsList where T : ARenderDataBase
{
    public List<T> BaseList = new List<T>();
    public bool IsChanged { get; set; }
    public T Get(int idx);
    new public void Add(T t);          // → newDataList
    public void SetChangedData(T t);   // → changedDataList
    new public void Remove(T t);
    public void RemoveAt(int idx);
    new public void Clear();
}
```

!!! danger "Reading consumes the data"
    `GetNewDatas()` / `GetRemovedDatas()` / `GetChangedDatas()` **drain the internal lists and reset `IsChanged = false`**. Therefore:
    - The three methods **must not be called twice** (the second call returns empty arrays)
    - **The order of consumption affects the result**, so consume only once per frame

```csharp
[ComponentValueBind("list", typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChildren))]
public PropsList<RenderData_ItemCell> Items = new PropsList<RenderData_ItemCell>();
```

The companion adapter `CBA_UFluxBindLogic.BindChildren` calls `GetNewDatas()` / `GetRemovedDatas()` / `GetChangedDatas()` to handle add/remove/change.

## The three-step refresh flow

```csharp
// ① 改值
_renderData.Hp = 80;

// ② 提交（自动模式可省略标记）
cell.SetRenderData(_renderData);
```

For `AWindow<TP>` there is also a shortcut — mutate `RenderData` directly and then commit:

```csharp
public class Window_Hp : AWindow<RenderData_Hp>
{
    public void SetHp(int hp)
    {
        RenderData.Hp = hp;                 // ATComponent<T>.RenderData
        CommitRenderData();                 // 触发差异刷新
    }
}
```

## Restrictions on complex member types

The comment on `ARenderDataBase` states the convention explicitly:

> Props' composite member types must be Props (or `List<Props>`); **do not nest more than 2 levels**.

Nesting deeper than 2 levels is not analysed recursively — it is treated as an "other type" and triggers an `LogError`.

## Common failures

| Symptom | Root cause |
|------|------|
| The screen does not refresh | The field type is not in the supported list (it logs `LogError("可能不支持的Props字段类型")`) |
| Some fields do not refresh | A `SetPropertyChange` call is missing in manual mode |
| A list refreshes twice / never refreshes | `PropsList`'s read-consumes behaviour was consumed more than once |
| `ComponentValueBind` has no effect | `typeof(X)` failed to resolve (`Type == null`, and it only logs an error during construction) |
| The value changed but the UI did not | The new value was `null` and got skipped; or `Equals` reported equality |
| Nested data does not refresh | Nesting exceeds 2 levels |

## Related pages

- [Component](component.md)
- [State Management (Reducer/Store)](state-management.md)
- [Demo Walkthrough](../tutorials/demos.md) — `demo6_UFlux/05.Window_Props`
