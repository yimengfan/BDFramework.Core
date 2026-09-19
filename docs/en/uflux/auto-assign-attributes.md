# Auto-Assign Attributes

The `AutoAssignAttribute` family lets the framework **automatically wire nodes and events onto fields** when a window or component initialises, saving you from writing `transform.Find(...)` and `btn.onClick.AddListener(...)` by hand.

## Base class and execution timing

```csharp
abstract public class AutoAssignAttribute : Attribute
{
    public virtual void AutoSetField(IComponent com, FieldInfo fieldInfo)       { }
    public virtual void AutoSetProperty(IComponent com, PropertyInfo propInfo)  { }
    public virtual void AutoSetMethod(IComponent com, MethodInfo methodInfo)    { }
}
```

Execution order of `UFluxUtils.InitComponent(IComponent)`:

```text
① 以 comType.FullName 为键查 ComponentClassCacheMap
   未命中 → 反射收集带 AutoAssignAttribute 的 FieldInfo[] / PropertyInfo[] / MethodInfo[]
             （BindingFlags.NonPublic | Instance | Public，GetCustomAttributes(false)）并缓存

② 三趟顺序执行：
   foreach field    → attr.AutoSetField(component, field)
   foreach property → attr.AutoSetProperty(component, prop)
   foreach method   → attr.AutoSetMethod(component, method)
```

!!! note "Fields → properties → methods, strictly three passes"
    Not in declaration order. Fields always come before properties, and properties always before methods.

!!! warning "Exception behaviour differs between the Editor and non-Editor"
    In the Editor each member assignment is wrapped in `try/catch` (`#if UNITY_EDITOR`); **outside the Editor it is not caught**. So when one field assignment throws, in the Editor you only see one error log and the remaining fields keep being assigned; on a device the whole `InitComponent` is aborted.

**A single member can carry several `AutoAssignAttribute`s** — `GetCustomAttributes(false)` returns them all and each is executed in turn.

## Built-in attributes at a glance

| Class | Constructor | Target | Semantics |
|------|---------|------|------|
| `TransformPathAttribute` | `(string path)` | Field / Property | Finds a node by path and assigns a `Transform` or `GetComponent(field type)`; supports arrays / `List<>` |
| `UfluxComponentPathAttribute` | `(string path)` | Field / Property | Creates an `IComponent` sub-component at the path and calls `AddComponent`; supports `List<>` |
| `ButtonOnclickAttribute` | `(string path, bool isTriggerThisOnly = true)` | **Method only** | Finds the `Button` and registers `onClick` |
| `SubWindowAttribute` | `(string path)` | Field / Property | Creates a SubWindow and calls `RegisterSubWindow` |
| `ComponentAttribute` | `(string path, bool isAsyncLoad = false)` | **Class only** | Declares the component's asset path (not auto-assign, but part of the same family) |

!!! note "`ComponentPathAttribute` does not exist"
    The `ComponentPathAttribute` from older documentation is actually named **`UfluxComponentPathAttribute`** (with an extra `Uflux` prefix).

## `[TransformPath]`

```csharp
[TransformPath("bg/title")]        private Text      _title;      // 自动 GetComponent<Text>
[TransformPath("bg")]              private Transform _bg;         // 直接给 Transform
[TransformPath("list")]            private Transform[] _cells;    // 数组：遍历子节点
[TransformPath("list")]            private List<Image> _icons;    // List：同上
```

Behaviour:

1. Finds the node with `Transform.Find(path)`
2. If the field type is `Transform` → assign directly
3. Otherwise `GetComponent(field type)`
4. Array / `List<>` → iterate all child nodes and take a value from each; **the element type must be a subclass of `UnityEngine.Object`**, otherwise it throws
5. **When the node is not found it only calls `BDebug.LogError` and returns `null`** — it does not throw

## `[UfluxComponentPath]`

```csharp
[UfluxComponentPath("panel/info")]
private Component_Info _info;

[UfluxComponentPath("list")]
private List<Component_ItemCell> _cells;
```

Behaviour:

```text
Transform.Find(path)
 → Activator.CreateInstance(fieldType, new object[]{ transform })     // 走 (Transform) 构造
 → SetValue(字段)
 → instance.Init()
 → window.AddComponent(instance)
```

The generic (`List<>`) branch creates the list with `ScriptLoder.CreateHotfixInstance(fieldType)` and does the same for every child node.

!!! danger "It throws when the node does not exist"
    Unlike `[TransformPath]`, `[UfluxComponentPath]` **throws when the node is not found** (because it must have a `Transform` to construct the component).

## `[ButtonOnclick]`

```csharp
[ButtonOnclick("btnClose")]
private void OnClickClose() { … }

[ButtonOnclick("btnBuy", isTriggerThisOnly: false)]   // 保留已有监听
private void OnClickBuy() { … }
```

Behaviour:

```text
Transform.Find(path).GetComponent<Button>()
if (isTriggerThisOnly) button.onClick.RemoveAllListeners();     // 默认清除已有监听
button.onClick.AddListener(() => methodInfo.Invoke(com, new object[]{}));
```

| Parameter | Default | Notes |
|------|------|------|
| `path` | — | Node path |
| `isTriggerThisOnly` | `true` | When `true` it calls `RemoveAllListeners()` first, which **wipes listeners attached by artists or other code** |

**It throws when no `Button` is found**: `未找到Btn:<path>`.

!!! warning "It can only mark methods, and they must take no arguments"
    `ButtonOnclickAttribute` only takes effect in `AutoSetMethod`. The method must take **no parameters** (`Invoke(com, new object[]{})`).

## `[SubWindow]`

```csharp
[SubWindow("panel/topBar")]
private SubWindow_TopBar _topBar;
```

```text
Transform.Find(path)
 → Activator.CreateInstance(uiType, new object[]{ transform }) as IWindow
 → 赋值
 → (window as IWindow).RegisterSubWindow(subWindow)
```

**When the node is not found it only logs an error and returns `null`.**

→ See [SubWindow](sub-window.md) for details.

## Custom `AutoAssignAttribute`

Derive from `AutoAssignAttribute` and override the matching method:

```csharp
// 业务侧：把 Action<bool> 字段绑到 Toggle
public class ToggleClickBindAttribute : AutoAssignAttribute
{
    private string path;
    public ToggleClickBindAttribute(string path) => this.path = path;

    public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
    {
        var toggle = com.Transform.Find(path)?.GetComponent<Toggle>();
        if (toggle == null) return;

        // 首次回调时才从字段取 Action —— 兼容热更下的延迟赋值
        toggle.onValueChanged.AddListener(isOn =>
        {
            var action = fieldInfo.GetValue(com) as Action<bool>;
            action?.Invoke(isOn);
        });
    }
}

// 方法版本
public class ToggleValueChangeBindAttribute : AutoAssignAttribute
{
    private string path;
    public ToggleValueChangeBindAttribute(string path) => this.path = path;

    public override void AutoSetMethod(IComponent com, MethodInfo methodInfo)
    {
        var toggle = com.Transform.Find(path)?.GetComponent<Toggle>();
        toggle?.onValueChanged.AddListener(isOn =>
            methodInfo.Invoke(com, new object[] { isOn }));
    }
}
```

The real implementations in this repository live under `Assets/Code/BDFramework.Game/Uflux@hotfix/AutoInitComponentAttribute/`.

!!! tip "The deferred-read pattern under hotfix"
    `ToggleClickBindAttribute`'s approach is worth copying: **it does not read the field value at bind time but only on the first callback**, because in a hotfix environment the field may be assigned only after `InitComponent`.

## Usage example

```csharp
[UI((int)WinEnum.Shop, "Windows/Window_Shop")]
public class Window_Shop : AWindow
{
    // ── 节点 ──
    [TransformPath("bg/title")]  private Text      _title;
    [TransformPath("bg/close")]  private Transform _closeBtn;

    // ── 子组件 ──
    [UfluxComponentPath("panel/info")] private Component_Info       _info;
    [UfluxComponentPath("list")]       private List<Component_Cell> _cells;

    // ── 子窗口 ──
    [SubWindow("panel/topBar")] private SubWindow_TopBar _topBar;

    // ── 事件 ──
    [ButtonOnclick("bg/close")]
    private void OnClickClose() => UIManager.Inst.CloseWindow(WinEnum.Shop);

    [ButtonOnclick("panel/btnBuy", isTriggerThisOnly: false)]
    private void OnClickBuy() { /* … */ }
}
```

## Common failures

| Symptom | Root cause |
|------|------|
| The field is `null` | Wrong `[TransformPath]` path (**only logs an error**) |
| `未找到Btn:<path>` exception | There is no `Button` component at the `[ButtonOnclick]` path (**it throws**) |
| `[UfluxComponentPath]` throws | The node does not exist (**it throws**) |
| Existing listeners get wiped | `[ButtonOnclick]` defaults to `isTriggerThisOnly: true`, which calls `RemoveAllListeners()` |
| The method is never called | The method marked with `[ButtonOnclick]` takes parameters |
| Fine in the Editor, crashes on device | A field assignment throws; the Editor swallows it with `try/catch`, a device does not |
| An array field gets no values | The element type is not a subclass of `UnityEngine.Object` |
| The attribute has no effect | When several `AutoAssign` attributes sit on one member they all run; check whether a later one overwrites the earlier one |

## Related pages

- [Window](window.md)
- [Component](component.md)
- [SubWindow](sub-window.md)
- [RenderData](render-data.md)
