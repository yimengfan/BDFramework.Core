# Editor Pipeline 模块规则

作用域：`Packages/com.popo.bdframework/Editor/EditorPipeline/**`、PublishPipeline、DevOpsPipeline、BuildHotfix、BuildAssetBundle、BuildTable、母包构建入口和 BatchMode CI bridge。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. `Packages/com.popo.bdframework/AGENTS.md`
3. 本文件
4. 附近 editor pipeline 源码和测试
5. 影响 CI 脚本或 TeamCity 契约时读取 `.github/talos-docs/modules/devops-ci.md`

## 规则

- Editor pipeline 代码可以使用 UnityEditor API；runtime 程序集不得依赖它们。
- 重要构建流程必须锚定在明确 coordinator 或入口方法，方便 CI 日志映射到代码阶段。
- BatchMode 入口必须非交互、确定性、独立于当前打开场景和手工 Inspector 状态。
- `PublishPipeLineCI` 入口、`CI(Des)` 描述、BuildTools 脚本、TeamCity DSL、README 文本和测试必须保持同步。
- TeamCity DSL 和 pipeline 层只负责任务调度、参数和依赖；业务构建逻辑属于 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/**` 或一方 editor pipeline 代码。
- Debug build 行为、热更测试程序集注入、母包制品和上传路径都是公开 CI 契约；行为变化时更新测试和文档。
- 测试程序集（`BDFramework.Test`、`BDFramework.HostE2E`）只能在 Debug 构建中被注入 HybridCLR `hotUpdateAssemblies`；Release 构建必须调用 `EnsureTestAssembliesRemoved()` 确保不包含测试 DLL。新增构建入口必须遵守此分离策略。具体执行点：
  - **PublishPipeLineCI BatchMode 入口**：`BuildClientPackageForBatchMode` 已在构建前根据模式调用 `InjectTestAssemblies()` 或 `EnsureTestAssembliesRemoved()`。
  - **BuildTools_ClientPackage.Build() 纵深防御**：`Build()` 方法内部对 Release/Profiler 模式自动调用 `EnsureTestAssembliesRemoved()`，即使上游遗漏也能拦截。
  - **BuildTools_ClientPackage.Build() 构建后验证**：Release/Profiler 母包构建完成后，调用 `ValidateNoTestAssembliesInOutput()` 对落盘产物做双重校验，发现泄漏立即抛异常中断。
  - **HyCLREditorTools.CopyHotfixDLLs() 拷贝后校验**：热更 DLL 复制完成后调用 `ValidateNoTestAssembliesInOutput()` 验证输出目录不含测试程序集。
  - **HotfixTestAssemblyInjector.TestAssemblyNames**：公开的测试程序集名称列表，新增测试程序集时必须同步更新此列表。

### 测试程序集分离行为矩阵 / Test Assembly Separation Behavior Matrix

| 维度 | Debug | Release / Profiler |
| --- | --- | --- |
| `IsCurrentBuildDebug()` | `true` | `false` |
| `InjectTestAssemblies()` | ✅ 注入测试程序集到 hotUpdateAssemblies | ❌ 不调用 |
| `EnsureTestAssembliesRemoved()` | ❌ 不调用 | ✅ 构建前强制移除（纵深防御） |
| `ValidateNoTestAssembliesInOutput()` `isReleaseBuild` | `false` → 仅 LogWarning | `true` → 抛异常中断构建 |
| 热更产物含 `.dll.bytes` / `.zlua.bytes` | ⚠️ 警告继续 | ❌ 异常中断 |
| 热更产物不含测试程序集 | ✅ 通过 | ✅ 通过 |

### 测试程序集分离验收条件 / Test Assembly Separation Acceptance Criteria

任何涉及测试程序集或构建模式的改动，必须同时满足以下条件才能标记完成：

1. **Debug 构建热更产物包含 `BDFramework.Test` 和 `BDFramework.HostE2E`**（以 `.dll.bytes` 或 `.zlua.bytes` 形式存在于热更输出目录）。
2. **Release/Profiler 构建热更产物不含任何 `TestAssemblyNames` 中列出的程序集**（`.dll.bytes` 和 `.zlua.bytes` 均不得存在）。
3. **Release/Profiler 构建如果检测到测试程序集泄漏，必须抛异常中断构建**，不得降级为警告或静默跳过。
4. **Debug 构建中测试程序集泄漏只产生 `LogWarning`**，不影响构建继续。
5. **新增测试程序集时，必须同步更新 `HotfixTestAssemblyInjector.TestAssemblyNames` 列表和对应单元测试**。
6. **新增构建入口时，必须对 Release/Profiler 模式调用 `EnsureTestAssembliesRemoved()` 和 `ValidateNoTestAssembliesInOutput()`**。
7. **TeamCity 远端构建必须验证 Release 模式下热更产物不含测试程序集**。
- 阶段日志必须标明平台、build target、client version、输出路径、executeMethod 和关键阶段开始/完成。

## 验证

- 运行最近的 editor pipeline 测试或 BatchMode bridge 验证。
- 如果影响 BuildTools 脚本或上传布局，运行目标模块 README 中的 pytest 和 dry-run。
- 如果 TeamCity DSL、构建参数、输出布局、上传协议或 CI 日志变化，本地检查通过后执行受影响 TeamCity 验证。
