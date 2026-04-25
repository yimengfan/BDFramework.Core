using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest
{
    /// <summary>
    /// 启动器公开契约的 Runtime 测试主体。
    /// Runtime test body for the launcher public contracts.
    /// 该类型把启动器反射入口、AOT 启动 StreamingAssets 读取顺序、热更程序集装载顺序、缺失 BDebug 补挂策略、默认执行顺序与 E2E 自动检测运行时可达规则固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套启动器契约断言。
    /// This type fixes launcher reflection entry points, AOT-startup StreamingAssets read order, hotfix-assembly load order, missing-BDebug restoration, default execution order, and the runtime-reachability rule for the E2E auto-detection bridge inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same launcher contract assertions.
    /// </summary>
    public sealed class BdLauncherApiTest
    {
        /// <summary>
        /// 输出统一日志，记录启动器 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the launcher API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(BdLauncherApiTest) : testName,
                "验证启动器反射契约、AOT 启动 StreamingAssets 读取与热更装载顺序规则、缺失 BDebug 补挂策略、默认执行顺序与 E2E 自动检测入口运行时可达规则保持稳定。",
                "通过直接调用 FrameworkContractAssertions 的启动器断言，并校验反射发现、StreamingAssets 初始化顺序、热更程序集装载顺序、缺失 BDebug 补挂、执行顺序与运行时可达规则。"
            );
        }

        /// <summary>
        /// 验证热更脚本加载入口仍然是可发现的静态方法。
        /// Verify that the hotfix script loading entry remains a discoverable static method.
        /// </summary>
        public void FindScriptLoderInitMethod_ShouldResolveStaticMethod()
        {
            FrameworkContractAssertions.VerifyScriptLoaderInitMethodCanBeResolved();
        }

        /// <summary>
        /// 验证 AOT 启动阶段读取 StreamingAssets DLL 列表时，会先初始化索引，并在可选目录缺失时返回空集合。
        /// Verify that AOT startup initializes the index before reading StreamingAssets DLL lists and returns an empty set when an optional directory is missing.
        /// </summary>
        public void GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTStreamingAssetsReadContract();
        }

        /// <summary>
        /// 验证 AOT 启动阶段装载热更程序集时，会先装载框架与 firstpass，再装载 Assembly-CSharp，并在单个热更文件缺失时继续后续装载。
        /// Verify that AOT startup loads the framework and firstpass assemblies before Assembly-CSharp and continues with remaining assemblies when one hotfix file is missing.
        /// </summary>
        public void LoadHotfixAssemblies_ShouldRespectKnownDependencyOrder()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTHotfixAssemblyLoadOrderContract();
        }

        /// <summary>
        /// 验证基础配置处理器会在缺失时补挂 BDebug 组件。
        /// Verify that the base-config processor restores the BDebug component when it is missing.
        /// </summary>
        public void EnsureDebugComponent_ShouldRestoreMissingBDebug()
        {
            FrameworkContractAssertions.VerifyGameBaseConfigProcessorRestoresMissingBDebugComponent();
        }

        /// <summary>
        /// 验证启动器声明了极小的默认执行顺序。
        /// Verify that the launcher declares the minimum default execution order.
        /// </summary>
        public void BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder()
        {
            FrameworkContractAssertions.VerifyLauncherDefaultExecutionOrder();
        }

        /// <summary>
        /// 验证 AOT 热更预加载同时保留更早钩子与 BeforeSceneLoad 兜底。
        /// Verify that AOT hotfix preloading keeps both the earlier hook and the BeforeSceneLoad fallback.
        /// </summary>
        public void ScriptLoderAOT_ShouldKeepEarlyPreloadHooks()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTEarlyPreloadHooks();
        }

        /// <summary>
        /// 验证 E2E 自动检测入口在 Player 中保持运行时可达。
        /// Verify that the E2E auto-detection entry stays runtime-reachable in player builds.
        /// </summary>
        public void TryStartE2EFramework_ShouldRemainRuntimeReachable()
        {
            FrameworkContractAssertions.VerifyTryStartE2EFrameworkRemainsRuntimeReachable();
        }

    }
}
