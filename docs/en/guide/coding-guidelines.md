# Coding Guidelines

This page is the **engineering-practice** guideline (what a human follows when writing code). The hard rules aimed at AI agents (workflow, quality gates, documentation governance) are maintained in `.github/copilot-instructions.md` and are not duplicated here.

## 1. The hotfix boundary

This is the strongest and most destructive constraint in the framework.

| Rule | Why |
|------|-----|
| **Main-project code must not `new` or `typeof` a hotfix type** | An AOT assembly's metadata is fixed at build time; referencing hotfix types causes IL2CPP stripping or runtime type-resolution failures |
| Main project ↔ hotfix communication goes **only** through the **event bus** or **manager properties** | `StatusListenerServer` / `GameServiceStore` are the only stable cross-layer channels |
| Minimise cross-layer inheritance | A hotfix class inheriting an AOT class works; the reverse does not. The longer the inheritance chain, the more AOT metadata is required |
| **Hotfix code must not use macros** | Macros are evaluated only when compiling the hotfix DLL, producing bizarre cases where "AOT and hotfix disagree about the same piece of code" |
| Put main-project code in its own `asmdef` | Avoids being mistakenly collected as hotfix types by the `"Assembly-CSharp,"` prefix in `ScriptLoder.GetAppDomainHostingTypes()` |

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

## 2. Where `MonoBehaviour` is allowed

| Scenario | May inherit `MonoBehaviour`? |
|----------|------------------------------|
| UI logic (windows / components) | **No.** Use `AWindow<T>` / `AComponent` and obtain nodes via attributes such as `[TransformPath]` |
| Framework infrastructure (`BDebug`, `IEnumeratorTool`, `Singleton<T>`) | Yes |
| Standalone MonoBehaviour scripts in a scene | Yes, but evaluate whether it should become a UFlux component |

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

## 3. Prefer event-driven over `Update` polling

Polling state in `Update()` is the main source of rot in the UI layer, both for performance and maintainability.

```csharp
// ✗ 错误：每帧轮询
void Update()
{
    if (Hp != _lastHp) { RefreshHp(); _lastHp = Hp; }
}

// ✓ 正确：变化时推送
window.State.AddListener<OnHpChanged>(e => RefreshHp(e.Hp));
```

## 4. Never use strings as keys

String keys cannot be validated by the compiler and fail silently after a rename.

```csharp
// ✗ 错误
window.State.SetData("Hp", 100);
var hp = window.State.GetData<int>("Hp");

// ✓ 正确
window.State.SetData(PlayerState.Hp, 100);
var hp = window.State.GetData<int>(PlayerState.Hp);
```

The `StatusListenerServer.Create(nameof(MyEnum))` form is also acceptable — `nameof` is evaluated at compile time and follows renames.

## 5. Logging

Always use `BDebug`, not `UnityEngine.Debug` directly:

```csharp
BDebug.Log("普通日志");
BDebug.Log("Tag", "带标签，可被 DisableLog 过滤");
BDebug.LogError("Tag", "错误");
BDebug.LogWatchBegin("LoadWindow");
// ...
BDebug.LogWatchEnd("LoadWindow");
```

!!! warning "`BDebug` has no `LogWarning` / `Assert`"
    `BDebug.LogWarning`, `BDebug.Assert` and `BDebug.LogErrorAndThrow` **do not exist** in this repository. When you need a warning, use `UnityEngine.Debug.LogWarning` directly.

    Also, every `BDebug.*` method carries `[Conditional("ENABLE_BDEBUG")]`; when the macro is off **even argument evaluation is eliminated** — never put a side-effecting expression into a log call.

See [Service Container & Logging](../api/utils.md) for details.

## 6. Comments and naming

| Item | Convention |
|------|-----------|
| Code comments / docstrings | **Bilingual, Chinese first** |
| File and directory names | ASCII English |
| C# identifiers, enum values, parameter names | English |
| Developer-facing runtime logs | Chinese is fine |
| Automated test / BatchMode entry logs | **Must** contain the Chinese markers `测试目的=` and `实现手段=` |

## 7. Async and threading

| Rule | Notes |
|------|-------|
| Route coroutines through `IEnumeratorTool.StartCoroutine()` | It adapts to the hotfix environment; **it only enqueues** — the actual driving happens in `IEnumeratorTool.Update()` |
| Do not start AB async loads in a scene without `IEnumeratorTool` | They **silently never progress**, with no error |
| Be careful accessing `BApplication` paths from a background thread | If the static constructor is triggered on the loading thread, `Application.dataPath` throws "main thread only". The framework falls back via `TryInitializePathState`, but business code should avoid this |
| Version-control callbacks already run on the main thread | `AssetsVersionController.UpdateAssets` calls `UniTask.SwitchToMainThread()` before invoking callbacks |

## 8. Serialization and data classes

- Table classes **must** use the `Game.Data.*` namespace (`BuildTools_Excel2SQLite.CollectTableTypes()` collects by that prefix).
- Table name == class name (`TableQueryForILRuntime` generates SQL from `type.Name`), so **same-named classes in different namespaces collide on the same table**.
- Composite members of Props / RenderData must derive from `ARenderDataBase` (or be `List<ARenderDataBase>`), and nesting **must not exceed 2 levels**.

## Related pages

- [Project Layout](project-structure.md)
- [Hotfix Code (HybridCLR)](../pipeline/build-hotfix-dll.md)
- [UI (UFlux)](../uflux/index.md)
- [Service Container & Logging](../api/utils.md)
