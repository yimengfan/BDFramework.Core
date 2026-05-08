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