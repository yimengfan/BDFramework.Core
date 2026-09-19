# Testing Overview

## How the two systems divide the work

| Dimension | NUnit test suite | In-house lightweight unit tests |
|------|---------------|-------------|
| Location | `Packages/com.popo.bdframework/Runtime.Test/` | `Assets/Code/BDFramework.UnitTest/` |
| Assembly | `BDFramework.Test` (Runtime) / `BDFramework.EditorTest` (Editor) | `Assembly-CSharp` (ships with the hotfix) |
| Framework | Unity Test Framework (NUnit) | In-house `UnitTestAttribute` |
| Can test hotfix code | ✗ | ✓ |
| Can run on a real device | Requires the Test Runner | ✓ |
| Can run in Release | ✗ (excluded from Release) | Depends on configuration |

!!! note "Why keep an in-house suite as well"
    The original design motivation was that in the ILRuntime era NUnit could not execute in the hotfix layer. After v4 moved to HybridCLR that restriction eased, but the in-house suite is still kept — its test code itself **ships with the hotfix**, so hotfix logic can be verified on a real device.

## What is worth testing

| Category | Worth testing |
|------|-----------|
| Validation logic at trust boundaries (download manifest, CDN response parsing) | ✓ |
| Asset loading degradation / fallback paths | ✓ |
| Build pipeline config parsing and validation | ✓ |
| State machine / navigation stack transitions | ✓ |
| Business logic with branches or computation | ✓ |
| Logic-free properties on plain POCOs / DTOs | ✗ |
| Unity serialisation behaviour itself | ✗ |
| The behaviour of third-party frameworks themselves | ✗ |
| Plain getters / setters | ✗ |

!!! warning "Tests must cover concrete behaviour and failure paths"
    A test that only asserts "does not throw" has no value. At minimum there must be:
    - **Return value / state assertions** for the happy path
    - **Error handling assertions** for failure paths
    - **Boundary values** (empty, null, out of range)

## Where tests are placed

| Test type | Placement |
|---------|---------|
| Runtime API contracts | `Runtime.Test/Runtime/APITest/<模块>/` |
| Editor-only functionality | `Runtime.Test/Editor/<模块>/` |
| E2E flows | `Runtime.Test/Runtime/E2E/` |
| Contract assertion helpers | `Runtime.Test/Runtime/Contracts/` |
| Business-side hotfix tests | `Assets/Code/BDFramework.UnitTest/Runtime/<模块>@hotfix/` |
| CI build pipeline verification | `Runtime.Test/Editor/DevOps/` |

## Assembly configuration

### `BDFramework.Test` (Runtime)

```json
{
  "name": "BDFramework.Test",
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "autoReferenced": true,
  "noEngineReferences": false
}
```

**Note: there is no `optionalUnityReferences: ["TestAssemblies"]`** — this assembly does not depend on NUnit and only performs pure-logic assertions (so it can run in environments without the Test Runner).

### `BDFramework.EditorTest` (Editor)

```json
{
  "name": "BDFramework.EditorTest",
  "optionalUnityReferences": ["TestAssemblies"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"]
}
```

Depends on NUnit; Editor platform only.

!!! danger "Test assemblies are only effective in Debug builds"
    `BDFramework.Test` / `BDFramework.EditorTest` must be **excluded from Release / Profiler builds**.

    - `HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput` checks both the `.dll.bytes` and `.zlua.bytes` extensions
    - Finding a test assembly in Release **throws** and aborts the build
    - `IsCurrentBuildDebug()` precedence: `-buildMode` → `-buildDebug` → `EditorUserBuildSettings.development`

## In-house unit test framework

```csharp
// Assets/Code/BDFramework.UnitTest/Runtime/Attribute@hotfix/
public class UnitTestAttribute : UnitTestBaseAttribute { }
public class HotfixOnlyUnitTestAttribute : UnitTestBaseAttribute { }
public class UnitTestBaseAttribute : Attribute { }
```

```csharp
public class TestRunner
{
    // Assets/Code/BDFramework.UnitTest/Runtime/TestRunner@hotfix/TestRunner@hotfix.cs
}
```

Menu entry points:

| Menu | Purpose |
|------|------|
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | Opens the test window |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-DLL` | Runs the DLL-side tests |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-ILRuntime` | Runs the hotfix-side tests (legacy naming) |
| `BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)` | Rebuilds the DLL first, then runs |

## Logging convention

!!! danger "`TestContext.WriteLine` throws an NRE under BatchMode"
    NUnit's `TestContext` depends on test runner infrastructure that Unity BatchMode does not provide. **You must use `UnityEngine.Debug.Log`**.

    ```csharp
    // ✗ BatchMode 下抛 NullReferenceException
    TestContext.WriteLine("...");

    // ✓ 所有模式下都可用，输出到 -logFile
    Debug.Log("...");
    ```

    `using NUnit.Framework;` must still be kept (`Assert` / `Test` / `TestFixture` all live there).

### Mandatory log format

The logs of automated tests and BatchMode entry points **must** contain:

```text
测试目的=<做什么>
实现手段=<怎么做>
```

```csharp
Debug.Log($"测试目的=验证 local.db 只读打开后可正常查询; 实现手段=加载 DB 后执行 select count(*)");
```

## Related pages

- [Runtime Unit Tests](runtime-tests.md)
- [BatchMode & CI Tests](batchmode-tests.md)
- [E2E (Talos)](e2e.md)
- [Hotfix Code (HybridCLR)](../pipeline/build-hotfix-dll.md) — test assembly isolation
