# 测试体系总览

## 两套体系的分工

| 维度 | NUnit 测试套件 | 自研精简单测 |
|------|---------------|-------------|
| 位置 | `Packages/com.popo.bdframework/Runtime.Test/` | `Assets/Code/BDFramework.UnitTest/` |
| 程序集 | `BDFramework.Test`（Runtime）/ `BDFramework.EditorTest`（Editor） | `Assembly-CSharp`（随热更） |
| 框架 | Unity Test Framework (NUnit) | 自研 `UnitTestAttribute` |
| 能测热更代码 | ✗ | ✓ |
| 能跑在真机 | 需 Test Runner | ✓ |
| 能跑在 Release | ✗（Release 排除） | 视配置 |

!!! note "为什么还要自研一套"
    原始设计动机是 ILRuntime 时代 NUnit 无法在热更层执行。v4 用 HybridCLR 后这个限制已弱化，但自研套件仍然保留——它的测试代码本身**随热更下发**，可以在真机上验证热更逻辑。

## 值得测什么

| 类别 | 是否值得测 |
|------|-----------|
| 信任边界的校验逻辑（下载 manifest、CDN 响应解析） | ✓ |
| 资源加载降级/回退路径 | ✓ |
| 构建管线配置解析与验证 | ✓ |
| 状态机 / 导航栈转换 | ✓ |
| 有分支或计算的业务逻辑 | ✓ |
| 纯 POCO / DTO 的无逻辑属性 | ✗ |
| Unity 序列化行为本身 | ✗ |
| 第三方框架自身行为 | ✗ |
| 纯 getter / setter | ✗ |

!!! warning "测试必须覆盖具体行为和失败路径"
    只断言"不抛异常"的测试没有价值。至少要有：
    - **正常路径**的返回值/状态断言
    - **失败路径**的错误处理断言
    - **边界值**（空、null、超限）

## 测试放置规范

| 测试类型 | 放置位置 |
|---------|---------|
| Runtime API 契约 | `Runtime.Test/Runtime/APITest/<模块>/` |
| Editor-only 功能 | `Runtime.Test/Editor/<模块>/` |
| E2E 流程 | `Runtime.Test/Runtime/E2E/` |
| 契约断言 helper | `Runtime.Test/Runtime/Contracts/` |
| 业务侧热更测试 | `Assets/Code/BDFramework.UnitTest/Runtime/<模块>@hotfix/` |
| CI 构建管线验证 | `Runtime.Test/Editor/DevOps/` |

## 程序集配置

### `BDFramework.Test`（Runtime）

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

**注意：没有 `optionalUnityReferences: ["TestAssemblies"]`** —— 这个程序集不依赖 NUnit，只做纯逻辑断言（可跑在无 Test Runner 的环境）。

### `BDFramework.EditorTest`（Editor）

```json
{
  "name": "BDFramework.EditorTest",
  "optionalUnityReferences": ["TestAssemblies"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"]
}
```

依赖 NUnit，仅 Editor 平台。

!!! danger "测试程序集只在 Debug 构建生效"
    `BDFramework.Test` / `BDFramework.EditorTest` 必须在 **Release / Profiler 构建中被排除**。

    - `HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput` 检查 `.dll.bytes` 与 `.zlua.bytes` 两种扩展名
    - Release 下发现测试程序集**抛异常**终止构建
    - `IsCurrentBuildDebug()` 优先级：`-buildMode` → `-buildDebug` → `EditorUserBuildSettings.development`

## 自研单测框架

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

菜单入口：

| 菜单 | 作用 |
|------|------|
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | 打开测试窗口 |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-DLL` | 执行 DLL 侧测试 |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-ILRuntime` | 执行热更侧测试（命名遗留） |
| `BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)` | 重新构建 DLL 后执行 |

## 日志规范

!!! danger "BatchMode 下 `TestContext.WriteLine` 会抛 NRE"
    NUnit 的 `TestContext` 依赖 Unity BatchMode 不提供的测试运行器基础设施。**必须用 `UnityEngine.Debug.Log`**。

    ```csharp
    // ✗ BatchMode 下抛 NullReferenceException
    TestContext.WriteLine("...");

    // ✓ 所有模式下都可用，输出到 -logFile
    Debug.Log("...");
    ```

    `using NUnit.Framework;` 仍需要保留（`Assert` / `Test` / `TestFixture` 都在里面）。

### 强制日志格式

自动化测试和 BatchMode 入口的日志**必须**包含：

```text
测试目的=<做什么>
实现手段=<怎么做>
```

```csharp
Debug.Log($"测试目的=验证 local.db 只读打开后可正常查询; 实现手段=加载 DB 后执行 select count(*)");
```

## 相关页面

- [Runtime 单元测试](runtime-tests.md)
- [BatchMode 与 CI 测试](batchmode-tests.md)
- [E2E（Talos）](e2e.md)
- [热更代码 HybridCLR](../pipeline/build-hotfix-dll.md) —— 测试程序集隔离
