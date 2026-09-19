# 渲染数据 RenderData

RenderData（旧称 **Props**）描述"界面现在该长什么样"。它是**纯数据**，由 `AComponentBindAdaptor` 消费后写入 UI 控件。

!!! info "术语变更"
    旧文档中的 `APropsBase` / `props` / `CommitProps` 在 v3 起已改名：

    | 旧名 | 现名 |
    |------|------|
    | `APropsBase` | `ARenderDataBase` |
    | `props` | `RenderData` |
    | `CommitProps()` | `CommitRenderData()` |
    | `AutoInitComponentAttribute` | `AutoAssignAttribute` |

## 基类

```csharp
// Runtime/UI/View/Props/ARenderDataBase.cs
abstract public class ARenderDataBase : AStateBase
{
    public Type ComponentType { get; set; }        // 由框架回填，标识所属组件类型
    public Transform Transform { get; private set; }
}
```

`ARenderDataBase` 继承 `AStateBase`，因此**天然带脏标记能力**（`SetPropertyChange` / `GetChangedPropertise`）。

```csharp
public class RenderData_Item : ARenderDataBase
{
    public string Name;
    public int    Count;
    public string Icon;
}
```

## 值绑定声明：`[ComponentValueBind]`

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

!!! danger "构造时立刻解析 `uiType`"
    `ComponentValueBindAttribute` 的构造函数里就调 `ComponentBindAdaptorManager.Inst.GetBindComponentType(uiType.FullName)`。**解析失败会 `LogError` 且 `Type == null`**，运行时该字段静默不绑定。

## 适配器机制

### 抽象基类

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

### 注册适配器

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

`ComponentBindAdaptorManager`（`ManagerBase<ComponentBindAdaptorManager, ComponentBindAdaptorAttribute>`）在 `Init()` 时遍历所有 `ClassData` 建实例，并起一条 30 秒的协程清理已销毁的 `Transform` 缓存。

### 内置适配器（业务侧，`Assets/Code/BDFramework.Game/Uflux@hotfix/ComponentBindAdaptor/`）

| 适配器 | `BindType` | 注册的字段 key |
|--------|-----------|---------------|
| `CBA_Button` | `Button` | `Button.onClick`（替换）、`Button.onClick.AddListener`（追加）、`Button.interactable` |
| `CBA_IButton` | `IButton` | `IButton.onClick`、`IButton.onClick.AddListener` |
| `CBA_Image` | `Image` | `Image.sprite`、`Image.overrideSprite`、`Image.color`、`Image.fillAmount` |
| `CBA_Text` | `Text` | `Text.text`、`Text.color` |
| `CBA_Toggle` | `Toggle` | `Toggle.group`、`Toggle.onValueChanged`、`Toggle.interactable`、`Toggle.isOn` |
| `CBA_TransformHelper` | `TransformHelper` | `TransformHelper.ShowHideChildByNumber`（自定义逻辑示范） |
| `CBA_UFluxBindLogic` | `UFluxBindLogic` | `UFluxBindLogic.BindChild`、`BindChildren` |
| 基类默认 | — | `UIBehaviour.enabled`、`UIBehaviour.gameObject.active` |

!!! note "`TransformHelper` / `UFluxBindLogic` 是占位类"
    它们**方法体为空**，存在的唯一目的是给 `[ComponentValueBind(..., typeof(X), nameof(X.Method))]` 提供编译期符号（`nameof` 需要真实类型）。实际逻辑在 `CBA_TransformHelper` / `CBA_UFluxBindLogic` 里。

`CBA_Image` 的 `Image.sprite` 支持传 string：会自动 `UFluxUtils.Load<Sprite>(string)`。

## 差异刷新机制

调用 `SetRenderData(props)` → `CommitRenderData()` → `UFluxUtils.SetComponentRenderData` → `ComponentBindAdaptorManager.SetTransformRenderData`。

```text
SetTransformRenderData(transform, newRenderData)
 ├─ 首次：BindTransformRenderData 解析全部 [ComponentValueBind] 字段，固化 FieldCacheMap
 └─ 后续：AnalysisRenderDataChanged 只挑出变化的字段，逐个 SetData
```

缓存 key 是 **`Transform` 实例**，`FieldCacheMap` 在首次绑定时固化。

### 变化判定规则

| 字段类型 | 判定 |
|---------|------|
| `IsMunalMarkMode == true` | 只取 `GetChangedPropertise()`（手动 `SetPropertyChange` 驱动） |
| 新值为 `null` | **跳过**（不更新 UI） |
| `LastValue == null` | 视为变化 |
| 值类型 / `Namespace == "System"` | 调 `Equals` 比较 |
| `ARenderDataBase` 嵌套 | 递归分析（**注释提示嵌套不要超过 2 层**） |
| `IPropsList` | 看 `IsChanged` |
| 其他类型 | `BDebug.LogError("可能不支持的Props字段类型")` |

### 手动标记模式

一旦调用过 `SetPropertyChange`，该 RenderData 就**永久进入手动模式**（`IsMunalMarkMode` 一旦为 `true` 不再回落）：

```csharp
// 自动模式：框架逐字段比较
_renderData.Name = "新名字";
cell.SetRenderData(_renderData);

// 手动模式：只刷新显式标记过的字段
_renderData.Name = "新名字";
_renderData.SetPropertyChange(nameof(RenderData_Item.Name));
cell.SetRenderData(_renderData);
```

!!! tip "什么时候用手动模式"
    字段很多但每次只改一两个时，手动模式能避免全字段比较的开销。代价是**漏标记就不刷新**。

## `PropsList<T>`：列表差异

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
    `GetNewDatas()` / `GetRemovedDatas()` / `GetChangedDatas()` 会**取空内部列表并重置 `IsChanged = false`**。所以：
    - 三个方法**不能重复调用**（第二次返回空数组）
    - **消费顺序会影响结果**，同一帧内只应消费一次

```csharp
[ComponentValueBind("list", typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChildren))]
public PropsList<RenderData_ItemCell> Items = new PropsList<RenderData_ItemCell>();
```

配套适配器 `CBA_UFluxBindLogic.BindChildren` 会调 `GetNewDatas()` / `GetRemovedDatas()` / `GetChangedDatas()` 做增/删/改。

## 三步刷新流程

```csharp
// ① 改值
_renderData.Hp = 80;

// ② 提交（自动模式可省略标记）
cell.SetRenderData(_renderData);
```

对于 `AWindow<TP>`，还有一条便捷路径——直接改 `RenderData` 后提交：

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

## 复杂成员类型限制

`ARenderDataBase` 的注释明确约定：

> Props 的复合成员类型必须是 Props（或 `List<Props>`），**嵌套不要超过 2 层**。

超过 2 层的嵌套不会递归分析，会被当作"其他类型"并 `LogError`。

## 常见故障

| 现象 | 根因 |
|------|------|
| 界面不刷新 | 字段类型不在支持列表（会 `LogError("可能不支持的Props字段类型")`） |
| 部分字段不刷新 | 手动模式下漏了 `SetPropertyChange` |
| 列表重复刷新/不刷新 | `PropsList` 的"读取即消费"被多次消费 |
| `ComponentValueBind` 不生效 | `typeof(X)` 解析失败（`Type == null`，只在构造时 `LogError`） |
| 值改了但 UI 没变 | 新值为 `null` 时被跳过；或 `Equals` 判定为相等 |
| 嵌套数据不刷新 | 嵌套超过 2 层 |

## 相关页面

- [组件 Component](component.md)
- [状态管理 Reducer/Store](state-management.md)
- [Demo 解读](../tutorials/demos.md) —— `demo6_UFlux/05.Window_Props`
