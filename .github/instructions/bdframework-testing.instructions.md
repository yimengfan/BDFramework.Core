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

### 框架/业务分离原则

BDFramework 测试代码按范围分为两层：

| 范围 | 放置位置 | 命名空间 | 聚合入口 | 管理责任 |
|------|---------|---------|---------|---------|
| **框架测试 (framework)** | `Runtime.Test/Runtime/E2E/` | `BDFramework.Test.E2E` | `ModuleIntegrationEntry` (suite: `module-integration`) | BDFramework 维护者 |
| **业务测试 (business)** | `Assets/Code/BDFramework.UnitTest/Runtime/E2E/` | `BDFramework.Game.E2E` | `BusinessModuleIntegrationEntry` (suite: `business-integration`) | 业务方 |

**E2ESuiteCatalog** 通过 `SuiteDescriptor.Scope` 字段（`"framework"` | `"business"`）区分套件归属。

### 模块归属与套件命名

所有 E2E 测试套件必须归属于一个模块，套件命名遵循 `<module>[-tier]` 模式：

| 范围 | 模块 | 套件 | 覆盖范围 |
|------|------|------|---------|
| framework | sqlite | `sqlite`, `sqlite-contract`, `sqlite-business`, `sqlite-integration` | SQLite 数据存储 |
| framework | asset | `asset-load`, `asset-traversal`, `version-controller-api` | 资源加载与版本控制（框架层） |
| business | asset | `asset-business`, `version-business`, `download-prep`, `download-update` | 资源加载与版本控制（业务层） |
| framework | framework | `framework-contract`, `framework-core-business`, `framework-integration` | 框架核心启动与配置 |
| framework | service-store | `service-store-api` | 服务容器与依赖注入 |
| framework | utility | `utility-api`, `object-pool-api`, `logs-contract`, `csv-contract` | 工具函数与基础设施 |
| framework | launch | `launch`, `host-launch`, `host-asset-load`, `host-framework-integration` | 启动流程与宿主集成 |
| business | ui | `window-preconfig` | UI 窗口预配置 |
| framework | meta | `module-integration` | 框架模块集成测试入口 |
| business | meta | `business-integration` | 业务集成测试入口 |

测试层级后缀约定：`-contract`（API 契约）、`-business`（业务逻辑）、`-integration`（跨模块集成）、`-api`（公共 API 接口）。无后缀表示基础操作或综合测试。

### E2ESuiteCatalog 目录注册表

`Runtime.Test/Runtime/E2E/E2ESuiteCatalog.cs` 是所有 E2E 套件的中央注册表，提供：

- **声明式目录**：所有套件的模块归属、测试层级、范围（Scope）和描述集中维护在 `AllSuites` 数组
- **Scope 字段**：每个 `SuiteDescriptor` 包含 `Scope` 字段（`"framework"` | `"business"`），区分框架测试与业务测试
  - `GetFrameworkSuites()` / `GetBusinessSuites()` 按范围查询
  - `GetSuitesByScope("framework")` / `GetSuitesByScope("business")` 通用查询
- **运行时验证**：`VerifyCatalogIntegrity()` 通过反射扫描 `[E2ETest]` 属性，与目录双向比对
- **查询 API**：`GetSuitesByModule(module)`、`GetSuitesByTier(tier)`、`GetSuitesByScope(scope)`、`GetAllModules()`

**新增套件时必须**：在 `E2ESuiteCatalog.AllSuites` 中添加对应条目。`VerifyCatalogIntegrity()` 会在 E2E 执行时自动检测遗漏。

### 模块集成测试入口

框架侧 `ModuleIntegrationEntry`（`Runtime.Test/Runtime/E2E/ModuleIntegrationEntry.cs`）和业务侧 `BusinessModuleIntegrationEntry`（`Assets/Code/BDFramework.UnitTest/Runtime/E2E/BusinessModuleIntegrationEntry.cs`）分别按模块维度聚合的集成测试入口：

**框架侧 — `ModuleIntegrationEntry`**（suite: `module-integration`）：
- 仅聚合 `Scope = "framework"` 的套件
- 每个框架模块一个 `[E2ETest]` 方法，按 contract → business → integration 顺序
- `RunSubSuite(suiteName, displayName)` 验证子套件入口可达
- `AllModulesSummary()` 汇总所有框架模块的测试覆盖范围

**业务侧 — `BusinessModuleIntegrationEntry`**（suite: `business-integration`）：
- 仅聚合 `Scope = "business"` 的套件
- 每个业务模块一个 `[E2ETest]` 方法，结构与 `ModuleIntegrationEntry` 一致
- 放在 `Assets/Code/BDFramework.UnitTest/Runtime/E2E/` 由 Assembly-CSharp 编译

### E2E 封装原则（Playwright）

Playwright spec 只编排调用，不写测试逻辑：

- 每个 test case 只做两件事：`connector.runSuite('suite-name')` → `expect(summary.failed === 0)`
- 不检查单个 test method 的 `passed` 属性（assemblyLoadTest.passed、screenLoadTest.passed 等）
- 不实现重试逻辑、条件判断或业务断言
- 所有测试逻辑在 C# 端通过 `[E2ETest]` 方法表达
- 框架测试调用 `connector.runSuite('module-integration')`
- 业务测试调用 `connector.runSuite('business-integration')`

### 新增 E2E 套件检查清单

**场景 A：现有模块新增子套件**（如 sqlite 模块新增 `sqlite-perf` 套件）

1. 确定套件范围：框架套件 → `Runtime.Test/Runtime/E2E/`，业务套件 → `Assets/Code/BDFramework.UnitTest/Runtime/E2E/`
2. 创建测试文件，使用 `[E2ETest(suite, order, des)]` 属性
3. 在 `E2ESuiteCatalog.AllSuites` 添加条目（套件名、模块、层级、范围 Scope、描述）
4. 运行 E2E 验证：新套件通过 + `VerifyCatalogIntegrity()` 不报遗漏

**无需改动 `ModuleIntegrationEntry.cs` 或 `BusinessModuleIntegrationEntry.cs`**：`RunModuleIntegration()` 从 `E2ESuiteCatalog` 自动发现模块的所有子套件并按其 Scope 筛选，新增子套件只需更新目录即可。

**场景 B：新增模块**（如新增 `network` 框架模块）

1. 创建测试文件于 `Runtime.Test/Runtime/E2E/`，使用 `[E2ETest(suite, order, des)]` 属性，Scope = "framework"
2. 在 `E2ESuiteCatalog.AllSuites` 添加条目（套件名、模块=`network`、层级、Scope、描述）
3. 在 `ModuleIntegrationEntry.cs` 的 `AllModuleEntries` 数组中添加一行：`("network", "网络通信", "Network Communication", 7)`
4. 在 `ModuleIntegrationEntry.cs` 中添加入口方法（复制任一现有方法，修改 3 处：`[E2ETest]` 的 order/des、方法名、`RunModuleIntegration` 的 module/displayName）
5. 运行 E2E 验证：新模块套件通过 + `VerifyCatalogIntegrity()` 不报遗漏

**场景 C：删除套件/模块**

1. 删除测试文件
2. 从 `E2ESuiteCatalog.AllSuites` 移除对应条目
3. 从 `ModuleIntegrationEntry.cs` 或 `BusinessModuleIntegrationEntry.cs` 的入口数组中移除
4. 运行 E2E 验证 `VerifyCatalogIntegrity()` 不报过期套件

**场景 D：框架套件 → 业务套件**（范围迁移）

1. 移动测试文件到 `Assets/Code/BDFramework.UnitTest/Runtime/E2E/`
2. 更新命名空间从 `BDFramework.Test.E2E` 到 `BDFramework.Game.E2E`
3. 在 `E2ESuiteCatalog.AllSuites` 中修改 Scope 从 `"framework"` 到 `"business"`
4. 运行 E2E 验证：框架侧不再包含该套件 + 业务侧正常发现并执行

### 维护职责矩阵

| 维护动作 | E2ESuiteCatalog | ModuleIntegrationEntry | BusinessModuleIntegrationEntry | Playwright spec |
|---------|:-:|:-:|:-:|:-:|
| 现有模块新增框架子套件 | ✅ 必改 | ❌ 无需改 | ❌ 无需改 | ❌ 无需改 |
| 现有模块新增业务子套件 | ✅ 必改 | ❌ 无需改 | ❌ 无需改（自动发现） | ❌ 无需改 |
| 新增框架模块 | ✅ 必改 | ✅ 必改（2 处） | ❌ 无需改 | ❌ 无需改 |
| 新增业务模块 | ✅ 必改 | ❌ 无需改 | ✅ 必改（2 处） | ❌ 无需改 |
| 删除套件/模块 | ✅ 必改 | ✅ 必改 | ✅ 必改 | ❌ 无需改 |
| 修改套件名称 | ✅ 必改 | ❌ 无需改（自动发现） | ❌ 无需改（自动发现） | ❌ 无需改 |
| 框架→业务范围迁移 | ✅ 必改（改 Scope） | ❌ 无需改 | ❌ 无需改 | ❌ 无需改 |

**自动同步保障**：`E2ESuiteCatalog.VerifyCatalogIntegrity()` 在每次 E2E 运行时自动检测：
- 运行时有但目录中没有的套件 → **抛出异常**（遗漏的目录条目必须补上）
- 目录中有但运行时没有的套件 → **警告**（可能是过期的目录条目）

`ModuleIntegrationEntry.RunModuleIntegration()` 和 `BusinessModuleIntegrationEntry.RunModuleIntegration()` 分别从 `E2ESuiteCatalog` 按 Scope 读取模块的子套件列表并按层级排序执行，**新增框架/业务子套件不需要修改入口方法**。Playwright spec 简化为只调用 `module-integration` 或 `business-integration` 套件，新增/删除子套件不需要修改 Playwright 代码。

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