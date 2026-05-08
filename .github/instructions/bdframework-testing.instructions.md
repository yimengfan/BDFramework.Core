---
description: "编辑 BDFramework 测试代码时使用。涵盖 Runtime.Test、Unity Test Framework、host E2E suite、测试放置和编写规范。"
applyTo: "Packages/com.popo.bdframework/Runtime.Test/**"
---

# 测试编码规范

## 测试放置

- 框架 Runtime 测试（优先）：`Runtime.Test/Runtime/APITest/` — 可在 Player 和 Editor 中运行，支持集成测试
- 框架 E2E 测试：`Runtime.Test/Runtime/E2E/`
- 框架 Editor-only 测试（仅限无法在 Runtime 测试的 Editor 专属逻辑）：`Runtime.Test/Editor/`
- 业务测试：`Assets/Code/<Module>/Tests/` 或 `Assets/Code/<Module>@hotfix/Tests/`
- 业务自有 E2E：`Assets/Code/<Module>/E2E/`

### Runtime 优先原则

被测代码属于 Runtime 程序集时，测试必须优先放在 `Runtime.Test/Runtime/APITest/`（`BDFramework.Test` asmdef），而非 `Runtime.Test/Editor/`（`BDFramework.EditorTest` asmdef）。

理由：Runtime 测试可在 Editor、Player、BatchMode 三种环境执行，能做集成测试验证；Editor 测试只能在 Unity Editor 内运行，无法覆盖 Player 行为差异。

只有在以下情况才使用 Editor 测试：
- 被测代码本身是 Editor-only（如 EditorPipeline、BuildHotfix、BuildTable）
- 测试需要 `UnityEditor` 命名空间且无法用条件编译隔离

## 编写规则

- 有明确被测源码时，测试文件遵循 `source-file-name + Test.cs`
- 遵循 copilot-instructions.md §2 测试策略：覆盖具体行为和失败路径，不能只验证"不抛异常"
- 需要在打包 player/device 上运行的 runtime-facing 测试必须放在 `Runtime.Test/Runtime/`（`BDFramework.Test` asmdef），不要堆积 editor wrapper
- 已有 editor 测试的 Runtime 模块，新增测试应放在 Runtime 测试目录，逐步迁移可迁移的 editor 测试
- 磁盘 IO 测试必须使用临时路径并清理
- 面向自动化和 BatchMode 的测试日志遵循 copilot-instructions.md §2 命名规则：包含中文 `测试目的=` 和 `实现手段=` 开始标记
- 测试 asmdef 引用保持最小，并与被测程序集边界一致

## 测试策略归属

本模块是测试框架本身，不归入测试金字塔层。修改测试框架代码时，用框架自带的测试验证（Runtime.Test 内自测）。

## 包通用规则

同时生效：`bdframework.instructions.md`（Editor/Runtime 隔离、asmdef 约定、命名、反射规则）。

## 验证

- 可行时运行目标 Unity 测试程序集
- BatchMode bridge 或 host E2E 变化时，运行文档记录的 BatchMode 入口
- 如果测试会在 debug package 验证中注入热更程序集，验证 debug-only 注入路径，并确认 release build 不包含测试程序集

## E2E 测试组织架构

### 模块归属与套件命名

所有 E2E 测试套件必须归属于一个模块，套件命名遵循 `<module>[-tier]` 模式：

| 模块 | 套件前缀 | 覆盖范围 |
|------|---------|---------|
| sqlite | `sqlite`, `sqlite-contract`, `sqlite-business`, `sqlite-integration` | SQLite 数据存储 |
| asset | `asset-load`, `asset-business`, `asset-traversal`, `version-controller-api`, `version-business`, `download-prep`, `download-update` | 资源加载与版本控制 |
| framework | `framework-contract`, `framework-core-business`, `framework-integration` | 框架核心启动与配置 |
| service-store | `service-store-api` | 服务容器与依赖注入 |
| utility | `utility-api`, `object-pool-api`, `logs-contract`, `csv-contract` | 工具函数与基础设施 |
| launch | `launch`, `host-launch`, `host-asset-load`, `host-framework-integration` | 启动流程与宿主集成 |
| ui | `window-preconfig` | UI 窗口预配置 |
| meta | `module-integration` | 模块集成入口与目录验证 |

测试层级后缀约定：`-contract`（API 契约）、`-business`（业务逻辑）、`-integration`（跨模块集成）、`-api`（公共 API 接口）。无后缀表示基础操作或综合测试。

### E2ESuiteCatalog 目录注册表

`Runtime.Test/Runtime/E2E/E2ESuiteCatalog.cs` 是所有 E2E 套件的中央注册表，提供：

- **声明式目录**：所有套件的模块归属、测试层级和描述集中维护在 `AllSuites` 数组
- **运行时验证**：`VerifyCatalogIntegrity()` 通过反射扫描 `[E2ETest]` 属性，与目录双向比对
- **查询 API**：`GetSuitesByModule(module)`、`GetSuitesByTier(tier)`、`GetAllModules()`

**新增套件时必须**：在 `E2ESuiteCatalog.AllSuites` 中添加对应条目。`VerifyCatalogIntegrity()` 会在 E2E 执行时自动检测遗漏。

### 模块集成测试入口

`Runtime.Test/Runtime/E2E/ModuleIntegrationEntry.cs` 是按模块维度聚合的集成测试入口，套件名 `module-integration`：

- 每个模块一个 `[E2ETest]` 方法，按 contract → business → integration 顺序引用该模块的所有子套件
- `RunSubSuite(suiteName, displayName)` 验证子套件入口可达
- `AllModulesSummary()` 汇总所有模块的测试覆盖范围
- `E2ESuiteCatalog.VerifyCatalogIntegrity()` 验证目录与运行时套件同步

**Playwright 层双重覆盖**：`testModuleIntegration-e2e.spec.ts` 按模块维度执行每个子套件，确保独立子套件和模块聚合入口都得到验证。

### 新增 E2E 套件检查清单

1. 创建测试文件于 `Runtime.Test/Runtime/E2E/`，使用 `[E2ETest(suite, order, des)]` 属性
2. 在 `E2ESuiteCatalog.AllSuites` 添加条目（套件名、模块、层级、描述）
3. 在 `ModuleIntegrationEntry.cs` 对应模块方法中添加 `RunSubSuite()` 调用
4. 在 `testModuleIntegration-e2e.spec.ts` 对应模块 describe 块中添加 test case
5. 运行 E2E 验证新套件通过且 `VerifyCatalogIntegrity()` 不报遗漏

## 集成测试入口

各核心模块须提供统一的集成测试入口，按单元测试/性能测试分类输出报告。

### 通用规则

- 入口类放在模块测试目录根下，命名为 `<Module>IntegrationTestRunner.cs`
- 入口须支持两种调用方式：Editor Menu（`[MenuItem]`）和 BatchMode（`-executeMethod`）
- 测试分类至少包含：**单元测试**（功能契约和边界行为验证）和 **性能测试**（性能指标和基准门禁）
- 报告输出到 `Library/<Module>IntegrationTest/` 目录，按时间戳区分
- BatchMode 入口须通过 `EditorApplication.Exit(exitCode)` 返回非零退出码表示失败
- 入口类通过反射扫描 NUnit 属性（`[Test]`、`[SetUp]`、`[TearDown]`、`[OneTimeSetUp]`、`[OneTimeTearDown]`）执行各 Fixture，不依赖 Unity Test Runner

### SQLite 集成测试入口

- **入口类**: `Runtime.Test/Editor/Sqlite/SqliteIntegrationTestRunner.cs`
- **Editor Menu**: `BDFramework/测试/SQLite 集成测试`
- **BatchMode**: `-executeMethod BDFramework.EditorTest.SQLite.SqliteIntegrationTestRunner.RunBatch`
- **单元测试 Fixtures**: `SqliteLoderPipelineTest`、`SqliteTransactionAndMigrationTest`、`SqliteTableQueryBoundaryTest`、`SqliteUnitTest`、`SqliteFastJsonConvertOptimizationTest`
- **性能测试 Fixtures**: `SqlitePerformanceMonitorTest`、`SqliteBenchmarkGateTest`
- **报告路径**: `Library/SqliteIntegrationTest/Sqlite_UnitTest_<timestamp>.txt`、`Library/SqliteIntegrationTest/Sqlite_PerfTest_<timestamp>.txt`

### 完成前检查

每个 SQLite 代码变更完成后必须：
1. 在 Unity Editor 中通过菜单 `BDFramework/测试/SQLite 集成测试` 执行全部测试
2. 确认单元测试和性能测试分类报告均已生成且全部通过
3. 若无法在 Editor 中运行，通过 BatchMode 入口验证：`Unity -batchmode -executeMethod BDFramework.EditorTest.SQLite.SqliteIntegrationTestRunner.RunBatch -quit`