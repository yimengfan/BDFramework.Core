# Runtime Unit Tests

`Packages/com.popo.bdframework/Runtime.Test/` contains **80 `.cs` files** across two assemblies.

## `BDFramework.Test` (Runtime, `Runtime.Test/Runtime/`)

| Directory | Contents |
|------|------|
| `APITest/` | Framework API contract tests |
| `Contracts/` | Contract assertion helpers |
| `E2E/` | End-to-end flow tests |
| `Sqlite/` | SQLite specifics |
| `SqliteBenchmark/` | Performance benchmarks |

### `APITest/` file list

| File | Coverage |
|------|------|
| `ApiTestAssert.cs` | Assertion helper |
| `ApiTestLog.cs` | Log helper |
| `AssetsManager/BResourcesApiTest.cs` | `BResources` API |
| `AssetsManager/VersionController/ClientAssetsUtilsApiTest.cs` | Dual addressing and hash verification |
| `AssetsManager/VersionController/VersionNumHelperApiTest.cs` | Version number arithmetic |
| `BdLauncherApiTest.cs` | Startup chain |
| `Config/GameConfigLoderApiTest.cs` | Config loading |
| `Config/GameConfigManagerApiTest.cs` | Config centre |
| `ServiceStore/GameServiceStoreApiTest.cs` | Module container |
| `ServiceStore/ServiceContainerApiTest.cs` | Service container |
| `Utils/IO/FileHelperApiTest.cs` | File IO |
| `Utils/IO/HashHelperApiTest.cs` | MD5 |
| `Utils/IO/PathApiTest.cs` | `IPath` |
| `Utils/Logs/LogCryptoAndReaderApiTest.cs` | Log encryption and decryption |
| `Utils/Logs/PersistenceApiTest.cs` | Log persistence |
| `Utils/ObjectPools/ObjectPoolApiTest.cs` | Object pool |

### `Contracts/` — contract assertion helpers

| File | Purpose |
|------|------|
| `FrameworkContractAssertions.cs` | Framework-level contracts |
| `CsvContractAssertions.cs` | CSV read/write contracts |
| `LogContractAssertions.cs` | Log contracts |
| `SqliteContractAssertions.cs` | SQLite contracts |

### `E2E/` — end-to-end flows

| File | Coverage |
|------|------|
| `E2ESuiteCatalog.cs` | Suite catalogue |
| `ModuleIntegrationEntry.cs` | Module integration entry |
| `BaseFlowHostRuntimeTests.cs` | Base flow host |
| `LaunchTests.cs` / `LaunchFlowHostTests.cs` | Launch flow |
| `AssetLoadTests.cs` / `AssetTraversalTests.cs` | Asset loading and traversal |
| `FrameworkContractTests.cs` / `FrameworkCoreBusinessTests.cs` / `FrameworkIntegrationTests.cs` | Framework contracts and integration |
| `CsvContractTests.cs` / `LogsContractTests.cs` / `SqliteContractTests.cs` | Per-module contracts |
| `ObjectPoolApiContractTests.cs` / `ServiceStoreApiContractTests.cs` / `UtilityApiContractTests.cs` / `VersionControllerApiContractTests.cs` | API contracts |
| `SqliteTests.cs` / `SqliteIntegrationTests.cs` / `SqliteBusinessTests.cs` / `SqliteAotPreservation.cs` | The full SQLite chain (including AOT preservation verification) |

!!! note "`SqliteAotPreservation.cs` verifies IL2CPP stripping"
    It specifically verifies that SQLite-related types are not stripped away under IL2CPP.

## `BDFramework.EditorTest` (Editor, `Runtime.Test/Editor/`)

| File | Coverage |
|------|------|
| `AssetsManager/BResourcesTest.cs` | Editor-side asset loading |
| `AssetsManager/AssetsVersionController.DevOpsTest.cs` | File server protocol |
| `AssetsManager/AssetsVersionController.DevOps.BatchModeArgsTest.cs` | Command line argument parsing |
| `BApplicationPathStateTests.cs` | Path state machine |
| `BDLauncherTest.cs` | Launcher |
| `Config/GameConfigLoderTest.cs` / `GameConfigManagerTest.cs` | Config |
| `DevOps/BuildToolsAssetBundleV2Test.cs` | AB build |
| `DevOps/DevOpsEditorTasksTest.cs` | Editor tasks |
| `DevOps/ExcelEditorToolsTest.cs` | Excel scanning |
| `DevOps/PublishPipeLineCITest.cs` | **CI entry point verification (`RunBatchVerification`)** |
| `DevOps/PublishPipeLineCI.BatchModeBridgeTest.cs` / `.BatchModeBridgeBatchVerification.cs` | BatchMode bridge |
| `Event/AStatusListenerTest.cs` | Event bus |
| `ScreenNavigation/ScreenViewLayerTest.cs` | Navigation layer |
| `Sqlite/*` | Full SQLite set (see below) |
| `SqliteBenchmark/*` | Benchmarks |
| `TalosE2EBatchBridgeTest.cs` | E2E bridge |
| `TalosRuntimeArgumentPolicyTests.cs` | Talos argument policy |
| `UI/State/AStateBaseTest.cs` | State dirty flags |
| `Utils/Logs/*` | Log encryption/decryption, persistence, batch verification |
| `Utils/ReflectionExtensionTest.cs` | Reflection extensions |
| `LaunchFlowHostTestsLauncherSignalTests.cs` | Launch signal |
| `BaseFlowHostRuntimeTests.SqliteProbePathTests.cs` | SQLite probe paths |

### SQLite specifics (`Editor/Sqlite/`)

| File | Coverage |
|------|------|
| `SqliteUnitTest.cs` | Basic CRUD |
| `SqliteIntegrationTestRunner.cs` | Integration test runner (menu entry) |
| `SqliteLoderPipelineTest.cs` | Load pipeline |
| `SqliteTableQueryBoundaryTest.cs` | Query boundaries (`Where` / `And` / `Or`) |
| `SqliteTransactionAndMigrationTest.cs` | Transactions and migration |
| `SqlitePerformanceMonitorTest.cs` | Performance monitoring |
| `SqliteFastJsonConvertTest.cs` | Fast JSON conversion |

## Business-side hotfix tests

`Assets/Code/BDFramework.UnitTest/`:

| Directory | Contents |
|------|------|
| `Runtime/APITest@hotfix/` | `APITest_AssetsManager_AssetsBundle.cs`, `APITest_AssetsManager_DevResource.cs`, `APITest_DataListener.cs`, `APITest_LitJson.cs`, `APITest_Protobuf.cs`, `APITest_Sqlite.cs`, `UniTestSqlite_AllType.cs` |
| `Runtime/Attribute@hotfix/` | `UnitTestAttribute.cs`, `HotfixOnlyUnitTestAttribute.cs`, `UnitTestBaseAttribute.cs` |
| `Runtime/TestRunner@hotfix/` | `TestRunner@hotfix.cs`, `Assert@hotfix.cs` |
| `Runtime/E2E/` | `AssetBusinessTests.cs`, `VersionBusinessTests.cs`, `DownloadPrepTests.cs`, `DownloadUpdateTests.cs`, `WindowPreconfigHostTests.cs`, `BusinessModuleIntegrationEntry.cs` |
| `Editor/TestRunner/` | `TestRunnerEditor.cs` |

## How to run

### Inside the Editor

Unity menu `Window → General → Test Runner`, or the framework menus:

| Menu | Purpose |
|------|------|
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | Opens the in-house TestRunner |
| `BDFramework/测试/SQLite 集成测试` | SQLite integration test |
| `BDFramework/测试/SQLite优化性能基准 ▶` | Performance benchmark |

### BatchMode

```bash
"$UNITY_PATH" -batchmode -quit \
  -projectPath "<repo>" \
  -executeMethod BDFramework.EditorTest.AssetsManager.AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification \
  -logFile /tmp/verify.log
```

→ For more entry points see [BatchMode & CI Tests](batchmode-tests.md).

## Requirements for writing new tests

1. **Placement** follows the classification tables above
2. **Use `UnityEngine.Debug.Log` for logs** (`TestContext.WriteLine` throws an NRE under BatchMode)
3. **BatchMode entry logs must contain** the Chinese markers `测试目的=` and `实现手段=`
4. **Cover failure paths**, not just the happy path
5. **Do not depend on PlayMode** unless it is genuinely required (prefer pure logic)

## Related pages

- [Testing Overview](overview.md)
- [BatchMode & CI Tests](batchmode-tests.md)
- [E2E (Talos)](e2e.md)
