---
description: "编辑 BDFramework Editor Pipeline 构建管线代码时使用。涵盖 PublishPipeline、BuildHotfix、BuildAssetBundle、BuildTable、母包构建和 BatchMode CI bridge。"
applyTo: "Packages/com.popo.bdframework/Editor/EditorPipeline/**"
---

# Editor Pipeline 编码规范

## 规则

- Editor pipeline 代码可以使用 UnityEditor API；runtime 程序集不得依赖它们
- 重要构建流程必须锚定在明确 coordinator 或入口方法，方便 CI 日志映射到代码阶段
- BatchMode 入口必须非交互、确定性、独立于当前打开场景和手工 Inspector 状态
- `PublishPipeLineCI` 入口、`CI(Des)` 描述、BuildTools 脚本、TeamCity DSL、README 和测试必须保持同步
- TeamCity DSL 和 pipeline 层只负责任务调度、参数和依赖；业务构建逻辑属于 `Editor.DevOps~/BuildTools/**` 或一方 editor pipeline 代码
- Debug build 行为、热更测试程序集注入、母包制品和上传路径都是公开 CI 契约；行为变化时更新测试和文档

## 测试程序集分离

测试程序集（`BDFramework.Test`）只能在需要测试的构建模式中被注入 HybridCLR `hotUpdateAssemblies`；不包含测试的模式必须调用 `EnsureTestAssembliesRemoved()` 确保不包含测试 DLL。新增构建入口必须遵守此分离策略。

注入判断统一使用 `BuildTools_ClientPackage.ShouldInjectTestAssemblies(buildMode)`：
- **Debug**: ✅ 注入
- **DebugForProfiler**: ❌ 不注入（性能剖析不应引入测试噪声）
- **Release**: ❌ 不注入
- **ReleaseForTest**: ✅ 注入（自动化测试需要测试程序集）

具体执行点：
- **PublishPipeLineCI BatchMode 入口**：`BuildClientPackageForBatchMode` 已在构建前根据模式调用 `InjectTestAssemblies()` 或 `EnsureTestAssembliesRemoved()`
- **BuildTools_ClientPackage.Build() 纵深防御**：`Build()` 方法内部对不包含测试的模式自动调用 `EnsureTestAssembliesRemoved()`，即使上游遗漏也能拦截
- **BuildTools_ClientPackage.Build() 构建后验证**：不包含测试的母包构建完成后，调用 `ValidateNoTestAssembliesInOutput()` 对落盘产物做双重校验，发现泄漏立即抛异常中断
- **HyCLREditorTools.CopyHotfixDLLs() 拷贝后校验**：热更 DLL 复制完成后调用 `ValidateNoTestAssembliesInOutput()` 验证输出目录不含测试程序集
- **HotfixTestAssemblyInjector.TestAssemblyNames**：公开的测试程序集名称列表，新增测试程序集时必须同步更新此列表

### 行为矩阵

| 维度 | Debug | DebugForProfiler | Release | ReleaseForTest |
| --- | --- | --- | --- | --- |
| Development / AllowDebugging | ✓ / ✓ | ✓ / ✓ | ✗ / ✗ | ✗ / ✗ |
| ConnectProfiler / DeepProfiling | ✗ / ✗ | ✓ / ✓ | ✗ / ✗ | ✗ / ✗ |
| `ShouldInjectTestAssemblies()` | ✅ 注入 | ❌ 不注入 | ❌ 不注入 | ✅ 注入 |
| `IsDebugBuildMode()` | `true` | `true` | `false` | `false` |
| ProductName suffix | `.debug` | `.debugforprofiler` | (none) | `.releasefortest` |
| `ValidateNoTestAssembliesInOutput()` `isReleaseBuild` | `false` → LogWarning | `true` → 抛异常 | `true` → 抛异常 | `false` → LogWarning |
| 热更产物含测试 `.dll.bytes` | ⚠️ 警告继续 | ❌ 异常中断 | ❌ 异常中断 | ⚠️ 警告继续 |

### 验收条件

任何涉及测试程序集或构建模式的改动，必须同时满足：
1. Debug / ReleaseForTest 构建热更产物包含 `BDFramework.Test`（`.dll.bytes` 或 `.zlua.bytes` 形式）
2. Release / DebugForProfiler 构建热更产物不含 `BDFramework.Test`
3. Release / DebugForProfiler 检测到泄漏必须抛异常中断，不得降级为警告
4. Debug / ReleaseForTest 构建中泄漏只产生 `LogWarning`
5. 新增测试程序集时同步更新 `HotfixTestAssemblyInjector.TestAssemblyNames`
6. 新增构建入口时必须对不包含测试的模式调用 `EnsureTestAssembliesRemoved()` 和 `ValidateNoTestAssembliesInOutput()`
7. TeamCity 远端构建必须验证 Release 模式不含测试程序集

## 测试策略归属

归属集成层和门禁层：构建管线配置解析和验证（集成层）、Debug/Release 行为矩阵（门禁层）。单元测试覆盖配置解析逻辑；BatchMode bridge 覆盖端到端构建流程。

## 日志

- 阶段日志标明平台、build target、client version、输出路径、executeMethod 和关键阶段开始/完成

## 验证

- 运行最近的 editor pipeline 测试或 BatchMode bridge 验证
- 影响 BuildTools 脚本或上传布局时，运行目标模块 README 中的 pytest 和 dry-run
- 影响 CI 脚本或 TeamCity 契约时，详见 `ci.instructions.md` 和 `.github/skills/teamcity/SKILL.md`