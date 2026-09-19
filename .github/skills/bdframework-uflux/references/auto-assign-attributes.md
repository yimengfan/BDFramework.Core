# 元素自动赋值属性（AutoAssign）参考

> 来源：`Packages/com.popo.bdframework/Runtime/UI/UFluxUtils.cs` + `Runtime/UI/View/Attribute/AutoInitComponent/*`

## 基类

```csharp
namespace BDFramework.UFlux

abstract public class AutoAssignAttribute : Attribute
{
    public virtual void AutoSetField(IComponent com, FieldInfo fieldInfo)      { }
    public virtual void AutoSetProperty(IComponent com, PropertyInfo propInfo) { }
    public virtual void AutoSetMethod(IComponent com, MethodInfo methodInfo)   { }
}
```

!!! note "基类名是 `AutoAssignAttribute`，不是 `AutoInitComponentAttribute`"
    旧文档里的 `AutoInitComponentAttribute` 是 v3 之前的名字。

## 执行时机与顺序

`UFluxUtils.InitComponent(IComponent)`：

```text
① 缓存查找
   key = comType.FullName → ComponentClassCacheMap
   未命中则反射收集：
     BindingFlags.NonPublic | Instance | Public
     GetCustomAttributes(false)   ← ★ 不包含继承链上的特性

② 三趟顺序执行（严格）
   foreach FieldInfo    → attr.AutoSetField(component, field)
   foreach PropertyInfo → attr.AutoSetProperty(component, prop)
   foreach MethodInfo   → attr.AutoSetMethod(component, method)
```

**触发点**：

| 触发点 | 说明 |
|--------|------|
| `ATComponent(Transform trans)` 构造末尾 | 绑定既有节点时 |
| `Load()` 内部（`Instantiate` 之后） | 自加载组件/窗口 |

!!! danger "Editor 与非 Editor 的异常行为不同"
    ```csharp
    #if UNITY_EDITOR
        try { attr.AutoSetField(component, f); }
        catch (Exception e) { BDebug.LogError(...); }
    #else
        attr.AutoSetField(component, f);      // ← 不捕获
    #endif
    ```
    Editor 下单个字段失败只影响该字段；**真机上会中断整个 `InitComponent`**。

## 内置属性表

| 类名 | 构造参数 | 目标 | 找不到节点 |
|------|---------|------|-----------|
| `TransformPathAttribute` | `(string path)` | Field / Property | `LogError` + 返回 null |
| `UfluxComponentPathAttribute` | `(string path)` | Field / Property | **抛异常** |
| `ButtonOnclickAttribute` | `(string path, bool isTriggerThisOnly = true)` | **Method only** | **抛异常** |
| `SubWindowAttribute` | `(string path)` | Field / Property | `LogError` + 返回 null |

## `TransformPathAttribute`

```csharp
[TransformPath("bg/title")]  private Text      _title;      // GetComponent<Text>
[TransformPath("bg")]        private Transform _bg;         // 直接给 Transform
[TransformPath("list")]      private Transform[] _cells;    // 遍历子节点
[TransformPath("list")]      private List<Image> _icons;    // 同上
```

解析逻辑：

```text
Transform.Find(path)
 ├─ 未找到 → BDebug.LogError(...) 并返回 null          （不抛）
 ├─ 字段类型 == Transform → 直接赋值
 ├─ 数组 / List<T> → 遍历所有子节点逐个 GetComponent
 │    元素类型必须是 UnityEngine.Object 子类，否则抛异常
 └─ 其他 → GetComponent(字段类型)
```

!!! warning "字段类型决定取值方式"
    声明 `Text _title` 拿的是 `GetComponent<Text>()`，不是节点本身。要节点用 `Transform`。

## `UfluxComponentPathAttribute`

```csharp
[UfluxComponentPath("panel/info")] private Component_Info _info;
[UfluxComponentPath("list")]       private List<Component_Cell> _cells;
```

解析逻辑：

```text
Transform.Find(path)
 → Activator.CreateInstance(fieldType, new object[]{ transform })     // 走 (Transform) 构造
 → SetValue
 → instance.Init()
 → window.AddComponent(instance)                                       // 设 Parent + 加入 ComponentList
```

`List<>` 分支用 `ScriptLoder.CreateHotfixInstance(fieldType)` 创建 List，对每个子节点重复上述流程。

!!! danger "旧名 `ComponentPathAttribute`"
    实际类名是 **`UfluxComponentPathAttribute`**（`Uflux` 前缀）。

## `ButtonOnclickAttribute`

```csharp
[ButtonOnclick("btnClose")]
private void OnClickClose() { }

[ButtonOnclick("btnBuy", isTriggerThisOnly: false)]   // 保留已有监听
private void OnClickBuy() { }
```

解析逻辑：

```text
Transform.Find(path).GetComponent<Button>()
if (isTriggerThisOnly) button.onClick.RemoveAllListeners();      // 默认 true
button.onClick.AddListener(() => methodInfo.Invoke(com, new object[]{}));
```

| 约束 | 说明 |
|------|------|
| 只能标在**方法**上 | `AutoSetMethod` 里生效 |
| 方法必须**无参** | `Invoke(com, new object[]{})` |
| 找不到 `Button` | **抛异常** `未找到Btn:<path>` |
| `isTriggerThisOnly` 默认 `true` | 会清掉美术/其他代码挂的监听 |

## `SubWindowAttribute`

```csharp
[SubWindow("panel/topBar")] private SubWindow_TopBar _topBar;
```

解析逻辑：

```text
Transform.Find(path)
 → Activator.CreateInstance(uiType, new object[]{ transform }) as IWindow
 → 赋值
 → (window as IWindow).RegisterSubWindow(subWindow)
```

`RegisterSubWindow` 的副作用：

```csharp
subWindowsMap[subwin.GetHashCode()] = subwin;      // ★ key 是对象哈希，不是枚举 ID
subwin.Root   = this.Root != null ? this.Root : this;
subwin.Parent = this;
(subwin as IComponent).Init();                     // ★ 注册时立刻 Init
```

!!! warning "`GetSubWindow<T>()` 只返回第一个匹配类型"
    `subWindowsMap` 的 key 是 `GetHashCode()`，因此同类型多个子窗口不会互相覆盖，但 `GetSubWindow<T>()` 是**线性遍历取第一个 `is T1`**。

## `ComponentAttribute`（不是 AutoAssign，但同族）

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ComponentAttribute : Attribute
{
    public string Path { get; }
    public bool   IsAsyncLoad { get; }

    public ComponentAttribute(string path, bool isAsyncLoad = false);
}
```

**只在 `ATComponent(bool isLoadAsset = true)` 构造里读取**：`IsAsyncLoad == false` 时构造末尾立即 `Load()`。

走 `(Transform)` 或 `(string)` 构造的组件**不读这个属性**。

## 自定义 `AutoAssignAttribute`

```csharp
// 字段版：绑 Action<bool> 到 Toggle
public class ToggleClickBindAttribute : AutoAssignAttribute
{
    private string path;
    public ToggleClickBindAttribute(string path) => this.path = path;

    public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
    {
        var toggle = com.Transform.Find(path)?.GetComponent<Toggle>();
        if (toggle == null) return;

        // ★ 首次回调时才从字段取值 —— 兼容热更下的延迟赋值
        toggle.onValueChanged.AddListener(isOn =>
        {
            var action = fieldInfo.GetValue(com) as Action<bool>;
            action?.Invoke(isOn);
        });
    }
}

// 方法版
public class ToggleValueChangeBindAttribute : AutoAssignAttribute
{
    private string path;
    public ToggleValueChangeBindAttribute(string path) => this.path = path;

    public override void AutoSetMethod(IComponent com, MethodInfo methodInfo)
    {
        var toggle = com.Transform.Find(path)?.GetComponent<Toggle>();
        toggle?.onValueChanged.AddListener(isOn => methodInfo.Invoke(com, new object[] { isOn }));
    }
}
```

仓库中的真实实现：`Assets/Code/BDFramework.Game/Uflux@hotfix/AutoInitComponentAttribute/`。

!!! tip "热更兼容模式：延迟取值"
    `ToggleClickBindAttribute` 的做法值得推广——**不在绑定时取字段值，而在首次回调时才取**。热更环境下字段可能在 `InitComponent` 之后才被赋值。

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | `GetCustomAttributes(false)` **不返回继承来的特性** —— 基类上的 `[TransformPath]` 不会被处理 |
| 2 | 同一成员可挂**多个** `AutoAssignAttribute`，会全部执行（后面的可能覆盖前面的） |
| 3 | 三趟顺序是 字段 → 属性 → 方法，**不是声明顺序** |
| 4 | Editor 下异常被吞，真机不吞 —— 联调时务必在真机验证一次 |
| 5 | `[TransformPath]` 找不到节点只报 error，字段静默为 `null` |
| 6 | `[ButtonOnclick]` / `[UfluxComponentPath]` 找不到节点**抛异常** |
| 7 | 类级缓存 key 是 `comType.FullName`，热更后同名类型可能命中旧缓存 |
