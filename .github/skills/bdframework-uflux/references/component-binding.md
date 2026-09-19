# 组件值绑定与适配器参考

> 来源：`Runtime/UI/View/ComponentBindAdaptor/**`、`Runtime/UI/View/Props/ARenderDataBase.cs`、`Runtime/UI/Collections/PropsList.cs`

## `ARenderDataBase`

```csharp
namespace BDFramework.UFlux

abstract public class ARenderDataBase : AStateBase
{
    public Type      ComponentType { get; set; }     // 框架回填
    public Transform Transform     { get; private set; }
}

public class NoRenderData : ARenderDataBase { }      // 声明在 AComponent.cs
```

!!! note "术语：Props → RenderData"
    v3 起改名：`APropsBase` → `ARenderDataBase`；`props` → `RenderData`；`CommitProps()` → `CommitRenderData()`。

`ARenderDataBase` 继承 `AStateBase`，因此自带脏标记能力：

```csharp
public bool IsMunalMarkMode { get; private set; }     // 一旦手动标记过，永久 true
public void SetPropertyChange(string name);           // 进入手动模式 + 记录脏字段
public string[] GetChangedPropertise();               // ★ 返回并清空
public void SetAllPropertyChanged();
```

## `[ComponentValueBind]`

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ComponentValueBindAttribute : Attribute
{
    public ComponentValueBindAttribute(string transformPath, Type uiType, string functionName);
}
```

| 参数 | 含义 |
|------|------|
| `transformPath` | 节点路径（相对组件根） |
| `uiType` | 目标控件类型 —— 用于查适配器，**构造时立刻解析** |
| `functionName` | 适配器里注册的字段名（通常用 `nameof(控件属性)`） |

```csharp
[ComponentValueBind("Hero/t_Name", typeof(Text),  nameof(Text.text))]
public string BindName;      // 字段名随意，映射由第三个参数决定

[ComponentValueBind("Hero/t_Hp",   typeof(Text),  nameof(Text.color))]
public Color  BindHpColor;   // 同一节点可绑到不同 UI 属性
```

!!! danger "构造时立刻 `GetBindComponentType`"
    ```csharp
    public ComponentValueBindAttribute(string transformPath, Type uiType, string functionName)
    {
        // 解析失败 → BDebug.LogError 且 Type == null
        // 运行时该字段静默不绑定
    }
    ```
    用 `typeof(不存在类型)` 或未注册的适配器类型，只会在**构造时**报一次错。

## 适配器基类

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

未注册字段时：`BDebug.LogError("不存在赋值字段:" + propName)`。

### 注册

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ComponentBindAdaptorAttribute : ManagerAttribute
{
    public Type BindType { get; }
    public ComponentBindAdaptorAttribute(Type bindType) : base(bindType.FullName) { }
}
```

```csharp
[ComponentBindAdaptor(typeof(MyWidget))]
public class CBA_MyWidget : AComponentBindAdaptor
{
    public override void Init()
    {
        base.Init();      // ★ 保留 enabled / active 的默认注册
        base.setPropComponentBindMap.Add(nameof(MyWidget.Value), SetValue);
    }

    private void SetValue(UIBehaviour ui, object value)
        => (ui as MyWidget).Value = (float)value;
}
```

## 内置适配器映射表

| 适配器 | `BindType` | 注册的字段 key |
|--------|-----------|---------------|
| `CBA_Button` | `Button` | `Button.onClick`（替换）、`Button.onClick.AddListener`（追加）、`Button.interactable` |
| `CBA_IButton` | `IButton` | `IButton.onClick`、`IButton.onClick.AddListener` |
| `CBA_Image` | `Image` | `Image.sprite`、`Image.overrideSprite`、`Image.color`、`Image.fillAmount` |
| `CBA_Text` | `Text` | `Text.text`（`value.ToString()`）、`Text.color` |
| `CBA_Toggle` | `Toggle` | `Toggle.group`（Transform→ToggleGroup）、`Toggle.onValueChanged`、`Toggle.interactable`、`Toggle.isOn` |
| `CBA_TransformHelper` | `TransformHelper` | `TransformHelper.ShowHideChildByNumber` |
| `CBA_UFluxBindLogic` | `UFluxBindLogic` | `UFluxBindLogic.BindChild`（单节点）、`BindChildren`（配合 `PropsList`） |
| `ComponentBindAdaptorScrollRect`（demo 内） | `ScrollRectAdaptor` | `ScrollRectAdaptor.ContentMap`（增/改/删） |
| 基类默认 | — | `UIBehaviour.enabled`、`UIBehaviour.gameObject.active` |

!!! note "`TransformHelper` / `UFluxBindLogic` 是纯占位类"
    它们**方法体为空**，存在的唯一目的是给 `[ComponentValueBind(..., typeof(X), nameof(X.Method))]` 提供编译期符号（`nameof` 需要真实类型）。实际逻辑在 `CBA_TransformHelper` / `CBA_UFluxBindLogic` 里。

`CBA_Image` 的 `Image.sprite` 支持 string：自动 `UFluxUtils.Load<Sprite>(string)`。

## `ComponentBindAdaptorManager`

```csharp
public class ComponentBindAdaptorManager
    : ManagerBase<ComponentBindAdaptorManager, ComponentBindAdaptorAttribute>
{
    Dictionary<Type, AComponentBindAdaptor>      componentBindAdaptorMap;        // key = BindType
    Dictionary<Transform, TransformBindData>     globalTransformBindCacheMap;    // ★ key = Transform 实例

    public override void Init();                       // 建实例 + 起 30s 清缓存协程
    public Type GetBindComponentType(string name);     // 按 Type.FullName 线性匹配
    public void SetTransformRenderData(Transform transform, ARenderDataBase newRenderData);
}
```

```csharp
public class TransformBindData
{
    public class ComponentFieldCahce
    {
        UIBehaviour UIBehaviour;
        Transform   Transform;
        ComponentValueBindAttribute Attribute;
        object      LastValue;
    }

    public Transform Transform { get; set; }
    public ARenderDataBase RenderData { get; set; }
    public Dictionary<string, ComponentFieldCahce> FieldCacheMap;
}
```

!!! warning "缓存 key 是 `Transform` 实例"
    首次绑定即固化 `FieldCacheMap`，之后**只做差异比较，不重新解析**。`IE_ClearCache(30)` 每 30 秒清理已销毁的 `Transform`。

## 差异刷新流程

```text
SetRenderData(props)
 → CommitRenderData(transform = null)
     transform ??= this.Transform
 → UFluxUtils.SetComponentRenderData(transform, RenderData)
 → ComponentBindAdaptorManager.SetTransformRenderData(transform, newRenderData)
     ① 首次：BindTransformRenderData → 解析全部 [ComponentValueBind] 字段，固化 FieldCacheMap
     ② 后续：AnalysisRenderDataChanged → 只挑出变化字段，逐个 SetData
```

### 变化判定规则（`AnalysisRenderDataChanged`）

| 字段类型 | 判定 |
|---------|------|
| `IsMunalMarkMode == true` | 只取 `GetChangedPropertise()` |
| 新值为 `null` | **跳过**（不更新 UI） |
| `LastValue == null` | 视为变化 |
| 值类型 / `Namespace == "System"` | 调 `Equals` 比较 |
| `ARenderDataBase` 嵌套 | 递归分析（**注释提示嵌套不要超过 2 层**） |
| `IPropsList` | 看 `IsChanged` |
| 其他类型 | `BDebug.LogError("可能不支持的Props字段类型")` |

!!! danger "源码里的比较对象可疑"
    值类型分支写的是 `newRenderData.Equals(LastValue)` —— 比较的是 **RenderData 自身**而不是缓存的上一次值。已记录在[重构清单](https://yimengfan.github.io/BDFramework.Core/architecture/refactor-backlog.md)。

    当前实践：优先用**手动标记模式**（`SetPropertyChange`）规避这个问题。

### 手动标记模式

```csharp
// 一旦调用过 SetPropertyChange，该 RenderData 永久进入手动模式
_renderData.Hp = 80;
_renderData.SetPropertyChange(nameof(RD_Hero.Hp));
cell.SetRenderData(_renderData);
```

!!! warning "手动模式不可逆"
    `IsMunalMarkMode` 一旦为 `true` **不会回落**。混用自动/手动会导致部分字段不再自动比较。

## `PropsList<T>`

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

!!! danger "读取即消费"
    `GetNewDatas()` / `GetRemovedDatas()` / `GetChangedDatas()` 会**取空内部列表并重置 `IsChanged = false`**。

    - 三个方法**不能重复调用**（第二次返回空数组）
    - **消费顺序会影响结果**
    - 同一帧内只应消费一次

## 完整示例

```csharp
// ① RenderData
public class RD_ItemCell : ARenderDataBase
{
    [ComponentValueBind("txtName",  typeof(Text),  nameof(Text.text))]
    public string Name;

    [ComponentValueBind("txtCount", typeof(Text),  nameof(Text.text))]
    public string Count;

    [ComponentValueBind("imgIcon",  typeof(Image), nameof(Image.sprite))]
    public string Icon;
}

// ② 组件
[Component("Components/ItemCell")]
public class Component_ItemCell : ATComponent<RD_ItemCell>
{
    public void SetItem(Item item)
    {
        SetRenderData(new RD_ItemCell
        {
            Name  = item.Name,
            Count = item.Count.ToString(),
            Icon  = item.Icon,
        });
    }
}

// ③ 列表容器
public class RD_ItemList : ARenderDataBase
{
    [ComponentValueBind("list", typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChildren))]
    public PropsList<RD_ItemCell> Items = new PropsList<RD_ItemCell>();
}
```

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | 改值后忘调 `CommitRenderData()` |
| 2 | 字段类型不在支持列表（值类型/System/嵌套 RenderData/`IPropsList` 之外） |
| 3 | 新值为 `null` 被跳过（不会把 UI 清空） |
| 4 | 手动模式下漏 `SetPropertyChange` |
| 5 | `PropsList` 的读取即消费被多次调用 |
| 6 | 嵌套 RenderData 超过 2 层不再递归分析 |
| 7 | `[ComponentValueBind]` 的 `typeof(X)` 未注册适配器 → 构造时报错后静默不绑定 |
| 8 | `Clone()` 是浅拷贝 —— `Store` 用它生成 `oldState`，Reducer 改引用类型成员会污染历史状态 |
| 9 | 同一 `Transform` 的 `FieldCacheMap` 固化后不会因组件结构调整而更新 |
