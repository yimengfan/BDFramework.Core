# Runtime 单元测试

`Packages/com.popo.bdframework/Runtime.Test/` 共 **80 个 `.cs`**，分两个程序集。

## `BDFramework.Test`（Runtime，`Runtime.Test/Runtime/`）

| 目录 | 内容 |
|------|------|
| `APITest/` | 框架 API 契约测试 |
| `Contracts/` | 契约断言 helper |
| `E2E/` | 端到端流程测试 |
| `Sqlite/` | SQLite 专项 |
| `SqliteBenchmark/` | 性能基准 |

### `APITest/` 文件清单

| 文件 | 覆盖 |
|------|------|
| `ApiTestAssert.cs` | 断言 helper |
| `ApiTestLog.cs` | 日志 helper |
| `AssetsManager/BResourcesApiTest.cs` | `BResources` API |
| `AssetsManager/VersionController/ClientAssetsUtilsApiTest.cs` | 双寻址与 hash 校验 |
| `AssetsManager/VersionController/VersionNumHelperApiTest.cs` | 版本号运算 |
| `BdLauncherApiTest.cs` | 启动链路 |
| `Config/GameConfigLoderApiTest.cs` | 配置加载 |
| `Config/GameConfigManagerApiTest.cs` | 配置中心 |
| `ServiceStore/GameServiceStoreApiTest.cs` | 模块容器 |
| `ServiceStore/ServiceContainerApiTest.cs` | 服务容器 |
| `Utils/IO/FileHelperApiTest.cs` | 文件 IO |
| `Utils/IO/HashHelperApiTest.cs` | MD5 |
| `Utils/IO/PathApiTest.cs` | `IPath` |
| `Utils/Logs/LogCryptoAndReaderApiTest.cs` | 日志加解密 |
| `Utils/Logs/PersistenceApiTest.cs` | 日志持久化 |
| `Utils/ObjectPools/ObjectPoolApiTest.cs` | 对象池 |

### `Contracts/` —— 契约断言 helper

| 文件 | 作用 |
|------|------|
| `FrameworkContractAssertions.cs` | 框架级契约 |
| `CsvContractAssertions.cs` | CSV 读写契约 |
| `LogContractAssertions.cs` | 日志契约 |
| `SqliteContractAssertions.cs` | SQLite 契约 |

### `E2E/` —— 端到端流程

| 文件 | 覆盖 |
|------|------|
| `E2ESuiteCatalog.cs` | 套件目录 |
| `ModuleIntegrationEntry.cs` | 模块集成入口 |
| `BaseFlowHostRuntimeTests.cs` | 基础流程宿主 |
| `LaunchTests.cs` / `LaunchFlowHostTests.cs` | 启动流程 |
| `AssetLoadTests.cs` / `AssetTraversalTests.cs` | 资源加载与遍历 |
| `FrameworkContractTests.cs` / `FrameworkCoreBusinessTests.cs` / `FrameworkIntegrationTests.cs` | 框架契约与集成 |
| `CsvContractTests.cs` / `LogsContractTests.cs` / `SqliteContractTests.cs` | 各模块契约 |
| `ObjectPoolApiContractTests.cs` / `ServiceStoreApiContractTests.cs` / `UtilityApiContractTests.cs` / `VersionControllerApiContractTests.cs` | API 契约 |
| `SqliteTests.cs` / `SqliteIntegrationTests.cs` / `SqliteBusinessTests.cs` / `SqliteAotPreservation.cs` | SQLite 全链路（含 AOT 保留验证） |

!!! note "`SqliteAotPreservation.cs` 验证 IL2CPP 剪裁"
    专门验证 SQLite 相关类型在 IL2CPP 下未被剪裁掉。

## `BDFramework.EditorTest`（Editor，`Runtime.Test/Editor/`）

| 文件 | 覆盖 |
|------|------|
| `AssetsManager/BResourcesTest.cs` | Editor 侧资源加载 |
| `AssetsManager/AssetsVersionController.DevOpsTest.cs` | 文件服务器协议 |
| `AssetsManager/AssetsVersionController.DevOps.BatchModeArgsTest.cs` | 命令行参数解析 |
| `BApplicationPathStateTests.cs` | 路径状态机 |
| `BDLauncherTest.cs` | 启动器 |
| `Config/GameConfigLoderTest.cs` / `GameConfigManagerTest.cs` | 配置 |
| `DevOps/BuildToolsAssetBundleV2Test.cs` | AB 构建 |
| `DevOps/DevOpsEditorTasksTest.cs` | 编辑器任务 |
| `DevOps/ExcelEditorToolsTest.cs` | Excel 扫描 |
| `DevOps/PublishPipeLineCITest.cs` | **CI 入口验证（`RunBatchVerification`）** |
| `DevOps/PublishPipeLineCI.BatchModeBridgeTest.cs` / `.BatchModeBridgeBatchVerification.cs` | BatchMode bridge |
| `Event/AStatusListenerTest.cs` | 事件总线 |
| `ScreenNavigation/ScreenViewLayerTest.cs` | 导航层 |
| `Sqlite/*` | SQLite 全套（见下） |
| `SqliteBenchmark/*` | 基准测试 |
| `TalosE2EBatchBridgeTest.cs` | E2E bridge |
| `TalosRuntimeArgumentPolicyTests.cs` | Talos 参数策略 |
| `UI/State/AStateBaseTest.cs` | State 脏标记 |
| `Utils/Logs/*` | 日志加解密、持久化、批量验证 |
| `Utils/ReflectionExtensionTest.cs` | 反射扩展 |
| `LaunchFlowHostTestsLauncherSignalTests.cs` | 启动信号 |
| `BaseFlowHostRuntimeTests.SqliteProbePathTests.cs` | SQLite 探测路径 |

### SQLite 专项（`Editor/Sqlite/`）

| 文件 | 覆盖 |
|------|------|
| `SqliteUnitTest.cs` | 基础 CRUD |
| `SqliteIntegrationTestRunner.cs` | 集成测试运行器（菜单入口） |
| `SqliteLoderPipelineTest.cs` | 加载管线 |
| `SqliteTableQueryBoundaryTest.cs` | 查询边界（`Where` / `And` / `Or`） |
| `SqliteTransactionAndMigrationTest.cs` | 事务与迁移 |
| `SqlitePerformanceMonitorTest.cs` | 性能监控 |
| `SqliteFastJsonConvertTest.cs` | 快速 JSON 转换 |

## 业务侧热更测试

`Assets/Code/BDFramework.UnitTest/`：

| 目录 | 内容 |
|------|------|
| `Runtime/APITest@hotfix/` | `APITest_AssetsManager_AssetsBundle.cs`、`APITest_AssetsManager_DevResource.cs`、`APITest_DataListener.cs`、`APITest_LitJson.cs`、`APITest_Protobuf.cs`、`APITest_Sqlite.cs`、`UniTestSqlite_AllType.cs` |
| `Runtime/Attribute@hotfix/` | `UnitTestAttribute.cs`、`HotfixOnlyUnitTestAttribute.cs`、`UnitTestBaseAttribute.cs` |
| `Runtime/TestRunner@hotfix/` | `TestRunner@hotfix.cs`、`Assert@hotfix.cs` |
| `Runtime/E2E/` | `AssetBusinessTests.cs`、`VersionBusinessTests.cs`、`DownloadPrepTests.cs`、`DownloadUpdateTests.cs`、`WindowPreconfigHostTests.cs`、`BusinessModuleIntegrationEntry.cs` |
| `Editor/TestRunner/` | `TestRunnerEditor.cs` |

## 运行方式

### Editor 内

Unity 菜单 `Window → General → Test Runner`，或框架菜单：

| 菜单 | 作用 |
|------|------|
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | 打开自研 TestRunner |
| `BDFramework/测试/SQLite 集成测试` | SQLite 集成测试 |
| `BDFramework/测试/SQLite优化性能基准 ▶` | 性能基准 |

### BatchMode

```bash
"$UNITY_PATH" -batchmode -quit \
  -projectPath "<repo>" \
  -executeMethod BDFramework.EditorTest.AssetsManager.AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification \
  -logFile /tmp/verify.log
```

→ 更多入口见 [BatchMode 与 CI 测试](batchmode-tests.md)。

## 编写新测试的要求

1. **放置位置**按上表分类
2. **日志用 `UnityEngine.Debug.Log`**（`TestContext.WriteLine` 在 BatchMode 下抛 NRE）
3. **BatchMode 入口日志必须含** `测试目的=` 与 `实现手段=`
4. **覆盖失败路径**，不只测正常流程
5. **不要依赖 PlayMode**，除非确实需要（纯逻辑优先）

## 相关页面

- [测试体系总览](overview.md)
- [BatchMode 与 CI 测试](batchmode-tests.md)
- [E2E（Talos）](e2e.md)
