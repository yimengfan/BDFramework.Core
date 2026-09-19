# 自动赋值属性

`AutoAssignAttribute` 体系让框架在窗口/组件初始化时**自动把节点和事件装配到字段上**，省掉手写 `transform.Find(...)` 与 `btn.onClick.AddListener(...)`。

## 基类与执行时机

```csharp
abstract public class AutoAssignAttribute : Attribute
{
    public virtual void AutoSetField(IComponent com, FieldInfo fieldInfo)       { }
    public virtual void AutoSetProperty(IComponent com, PropertyInfo propInfo)  { }
    public virtual void AutoSetMethod(IComponent com, MethodInfo methodInfo)    { }
}
```

`UFluxUtils.InitComponent(IComponent)` 的执行顺序：

```text
① 以 comType.FullName 为键查 ComponentClassCacheMap
   未命中 → 反射收集带 AutoAssignAttribute 的 FieldInfo[] / PropertyInfo[] / MethodInfo[]
             （BindingFlags.NonPublic | Instance | Public，GetCustomAttributes(false)）并缓存

② 三趟顺序执行：
   foreach field    → attr.AutoSetField(component, field)
   foreach property → attr.AutoSetProperty(component, prop)
   foreach method   → attr.AutoSetMethod(component, method)
```

!!! note "字段 → 属性 → 方法，严格三趟"
    不是按声明顺序。字段一定先于属性，属性一定先于方法。

!!! warning "Editor 与非 Editor 的异常行为不同"
    Editor 下每个成员的赋值被 `try/catch` 包裹（`#if UNITY_EDITOR`）；**非 Editor 下不捕获**。也就是说某个字段赋值抛异常时，Editor 里只看到一条错误日志、其余字段继续赋值；真机上会中断整个 `InitComponent`。

**同一个成员可以挂多个 `AutoAssignAttribute`** —— `GetCustomAttributes(false)` 会全部取出并逐个执行。

## 内置属性一览

| 类名 | 构造参数 | 目标 | 语义 |
|------|---------|------|------|
| `TransformPathAttribute` | `(string path)` | Field / Property | 按路径找节点，赋 `Transform` 或 `GetComponent(字段类型)`；支持数组 / `List<>` |
| `UfluxComponentPathAttribute` | `(string path)` | Field / Property | 按路径创建 `IComponent` 子组件并 `AddComponent`；支持 `List<>` |
| `ButtonOnclickAttribute` | `(string path, bool isTriggerThisOnly = true)` | **Method only** | 找 `Button` 并注册 `onClick` |
| `SubWindowAttribute` | `(string path)` | Field / Property | 创建子窗口并 `RegisterSubWindow` |
| `ComponentAttribute` | `(string path, bool isAsyncLoad = false)` | **Class only** | 声明组件资源路径（不是自动赋值，但同族） |

!!! note "`ComponentPathAttribute` 不存在"
    旧文档里的 `ComponentPathAttribute` 实际类名是 **`UfluxComponentPathAttribute`**（多一个 `Uflux` 前缀）。

## `[TransformPath]`

```csharp
[TransformPath("bg/title")]        private Text      _title;      // 自动 GetComponent<Text>
[TransformPath("bg")]              private Transform _bg;         // 直接给 Transform
[TransformPath("list")]            private Transform[] _cells;    // 数组：遍历子节点
[TransformPath("list")]            private List<Image> _icons;    // List：同上
```

行为：

1. `Transform.Find(path)` 找节点
2. 字段类型是 `Transform` → 直接赋值
3. 否则 `GetComponent(字段类型)`
4. 数组 / `List<>` → 遍历所有子节点逐个取值，**元素类型必须是 `UnityEngine.Object` 子类**，否则抛异常
5. **节点找不到时只 `BDebug.LogError` 并返回 `null`，不抛异常**

## `[UfluxComponentPath]`

```csharp
[UfluxComponentPath("panel/info")]
private Component_Info _info;

[UfluxComponentPath("list")]
private List<Component_ItemCell> _cells;
```

行为：

```text
Transform.Find(path)
 → Activator.CreateInstance(fieldType, new object[]{ transform })     // 走 (Transform) 构造
 → SetValue(字段)
 → instance.Init()
 → window.AddComponent(instance)
```

泛型（`List<>`）分支用 `ScriptLoder.CreateHotfixInstance(fieldType)` 创建 List，对每个子节点做同样处理。

!!! danger "节点不存在时会抛异常"
    与 `[TransformPath]` 不同，`[UfluxComponentPath]` **节点找不到时抛异常**（因为它必须拿到 `Transform` 才能构造组件）。

## `[ButtonOnclick]`

```csharp
[ButtonOnclick("btnClose")]
private void OnClickClose() { … }

[ButtonOnclick("btnBuy", isTriggerThisOnly: false)]   // 保留已有监听
private void OnClickBuy() { … }
```

行为：

```text
Transform.Find(path).GetComponent<Button>()
if (isTriggerThisOnly) button.onClick.RemoveAllListeners();     // 默认清除已有监听
button.onClick.AddListener(() => methodInfo.Invoke(com, new object[]{}));
```

| 参数 | 默认 | 说明 |
|------|------|------|
| `path` | — | 节点路径 |
| `isTriggerThisOnly` | `true` | `true` 时先 `RemoveAllListeners()`，会**清掉美术/其他代码挂的监听** |

**找不到 `Button` 时抛异常**：`未找到Btn:<path>`。

!!! warning "只能标记方法，且必须无参"
    `ButtonOnclickAttribute` 只在 `AutoSetMethod` 里生效。方法必须**无参**（`Invoke(com, new object[]{})`）。

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

**节点找不到时只报 error 并返回 `null`。**

→ 详见[子窗口 SubWindow](sub-window.md)。

## 自定义 `AutoAssignAttribute`

继承 `AutoAssignAttribute` 并覆写对应方法：

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

仓库中的真实实现位于 `Assets/Code/BDFramework.Game/Uflux@hotfix/AutoInitComponentAttribute/`。

!!! tip "热更下的延迟取值模式"
    `ToggleClickBindAttribute` 的做法值得借鉴：**不在绑定时取字段值，而在首次回调时才取**。因为热更环境下字段可能在 `InitComponent` 之后才被赋值。

## 使用示例

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

## 常见故障

| 现象 | 根因 |
|------|------|
| 字段为 `null` | `[TransformPath]` 路径错误（**只 LogError**） |
| `未找到Btn:<path>` 异常 | `[ButtonOnclick]` 的路径下没有 `Button` 组件（**抛异常**） |
| `[UfluxComponentPath]` 抛异常 | 节点不存在（**抛异常**） |
| 已有监听被清掉 | `[ButtonOnclick]` 默认 `isTriggerThisOnly: true` 会 `RemoveAllListeners()` |
| 方法没被调用 | `[ButtonOnclick]` 标记的方法有参数 |
| Editor 正常、真机崩 | 某字段赋值抛异常，Editor 下被 `try/catch` 吞掉，真机不吞 |
| 数组字段拿不到值 | 元素类型不是 `UnityEngine.Object` 子类 |
| 属性没生效 | 同一成员挂了多个 `AutoAssign` 时全部执行；检查是否被后面的覆盖 |

## 相关页面

- [窗口 Window](window.md)
- [组件 Component](component.md)
- [子窗口 SubWindow](sub-window.md)
- [渲染数据 RenderData](render-data.md)
