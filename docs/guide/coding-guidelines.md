# 编码规范

本文是**面向工程实践**的规范（人写代码时遵守）。面向 AI Agent 的强制规则（工作链路、质量门禁、文档治理）维护在 `.github/copilot-instructions.md`，不在此重复。

## 1. 热更边界

这是框架里约束最强、破坏性最大的一条。

| 规则 | 原因 |
|------|------|
| **主工程代码不得直接 `new` / `typeof` 热更类型** | AOT 程序集在构建期就固定了元数据，引用热更类型会导致 IL2CPP 剪裁或运行时类型解析失败 |
| 主工程与热更层的通信只走**事件总线**或**管理器属性** | `StatusListenerServer` / `GameServiceStore` 是唯一稳定的跨层通道 |
| 减少跨层继承 | 热更类继承 AOT 类可行，反向不可行；继承链越长，AOT 元数据要求越多 |
| **热更代码禁止使用宏** | 宏在编译热更 DLL 时才求值，会造成"AOT 与热更对同一段代码有不同理解"的诡异问题 |
| 主工程代码放独立 `asmdef` | 避免被 `ScriptLoder.GetAppDomainHostingTypes()` 的 `"Assembly-CSharp,"` 前缀误收集为热更类型 |

```csharp
// ✗ 错误：主工程直接引用热更类型
public class MainClient
{
    void Start() => new GameHotfixEntry().Run();   // GameHotfixEntry 在 Assembly-CSharp
}

// ✓ 正确：通过事件总线解耦
public class MainClient
{
    void Start()
        => StatusListenerServer.Create("Client").TriggerEvent("ClientReady");
}
```

## 2. MonoBehaviour 的使用边界

| 场景 | 是否允许继承 `MonoBehaviour` |
|------|---------------------------|
| UI 逻辑（窗口/组件） | **禁止**。必须用 `AWindow<T>` / `AComponent`，靠 `[TransformPath]` 等属性拿节点 |
| 框架基础设施（`BDebug`、`IEnumeratorTool`、`Singleton<T>`） | 允许 |
| 场景中的独立 MonoBehaviour 脚本 | 允许，但要评估是否应该改成 UFlux 组件 |

```csharp
// ✗ 错误：UI 逻辑继承 MonoBehaviour
public class MyPanel : MonoBehaviour
{
    public Button btn;
    void Update() { /* 轮询状态 */ }
}

// ✓ 正确：UFlux 窗口 + 事件驱动
[UI((int)WinEnum.MyPanel, "Windows/MyPanel")]
public class Window_MyPanel : AWindow
{
    [TransformPath("btnClose")]
    private Button _btnClose;

    [ButtonOnclick("btnClose")]
    private void OnClickClose() => UIManager.Inst.CloseWindow(WinEnum.MyPanel);
}
```

## 3. 事件驱动优先于 Update 轮询

用 `Update()` 轮询状态是 UI 层性能与可维护性的主要腐化源。

```csharp
// ✗ 错误：每帧轮询
void Update()
{
    if (Hp != _lastHp) { RefreshHp(); _lastHp = Hp; }
}

// ✓ 正确：变化时推送
window.State.AddListener<OnHpChanged>(e => RefreshHp(e.Hp));
```

## 4. 禁止用字符串当 key

字符串 key 无法被编译器校验，重命名后静默失效。

```csharp
// ✗ 错误
window.State.SetData("Hp", 100);
var hp = window.State.GetData<int>("Hp");

// ✓ 正确
window.State.SetData(PlayerState.Hp, 100);
var hp = window.State.GetData<int>(PlayerState.Hp);
```

`StatusListenerServer.Create(nameof(MyEnum))` 这种 `nameof` 写法也是可接受的——它在编译期求值，重命名会同步更新。

## 5. 日志

统一使用 `BDebug`，不要直接用 `UnityEngine.Debug`：

```csharp
BDebug.Log("普通日志");
BDebug.Log("Tag", "带标签，可被 DisableLog 过滤");
BDebug.LogError("Tag", "错误");
BDebug.LogWatchBegin("LoadWindow");
// ...
BDebug.LogWatchEnd("LoadWindow");
```

!!! warning "`BDebug` 没有 `LogWarning` / `Assert`"
    仓库中**不存在** `BDebug.LogWarning`、`BDebug.Assert`、`BDebug.LogErrorAndThrow`。需要 warning 直接用 `UnityEngine.Debug.LogWarning`。

    另外所有 `BDebug.*` 都带 `[Conditional("ENABLE_BDEBUG")]`，宏关闭时**连参数求值都会被消除**——不要把有副作用的表达式写进日志调用。

详见[服务容器与日志](../api/utils.md)。

## 6. 注释与命名

| 项 | 约定 |
|----|------|
| 代码注释 / docstring | **中英双语，中文在前** |
| 文件名与目录名 | ASCII English |
| C# 标识符、枚举值、参数名 | 英文 |
| 面向开发者的运行时日志 | 可用中文 |
| 自动化测试 / BatchMode 入口日志 | **必须**包含中文 `测试目的=` 与 `实现手段=` |

## 7. 异步与线程

| 规则 | 说明 |
|------|------|
| 协程统一走 `IEnumeratorTool.StartCoroutine()` | 它做了热更环境适配；**它只是入队**，真正的驱动在 `IEnumeratorTool.Update()` |
| 不要在无 `IEnumeratorTool` 的场景发起 AB 异步加载 | 会**静默不推进**，不报错 |
| 从后台线程访问 `BApplication` 路径要小心 | 静态构造在 loading thread 触发时 `Application.dataPath` 会抛 "main thread only"；框架已用 `TryInitializePathState` 兜底，但业务侧应避免 |
| 版本控制回调已在主线程 | `AssetsVersionController.UpdateAssets` 内部 `UniTask.SwitchToMainThread()` 后才回调 |

## 8. 序列化与数据类

- 表类**必须**用 `Game.Data.*` 命名空间（`BuildTools_Excel2SQLite.CollectTableTypes()` 靠此前缀收集）。
- 表名 = 类名（`TableQueryForILRuntime` 用 `type.Name` 生成 SQL），**不同命名空间下的同名类会撞表**。
- Props / RenderData 的复合成员类型必须是 `ARenderDataBase` 派生（或 `List<ARenderDataBase>`），嵌套**不要超过 2 层**。

## 相关页面

- [目录结构与约定](project-structure.md)
- [热更代码 HybridCLR](../pipeline/build-hotfix-dll.md)
- [UI（UFlux）](../uflux/index.md)
- [服务容器与日志](../api/utils.md)
