using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest
{
    /// <summary>
    /// 启动器公开契约的 Runtime 测试主体。
    /// Runtime test body for the launcher public contracts.
    /// 该类型把启动器反射入口、AOT 启动 StreamingAssets 读取顺序、热更程序集装载顺序、缺失 BDebug 补挂策略、默认执行顺序与 E2E 场景自启动契约固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套启动器契约断言。
    /// E2E 启动已从 BDLauncher 解耦——现由 Talos.E2E.E2ESceneAutoStarter MonoBehaviour 场景挂载自行激活。
    /// This type fixes launcher reflection entry points, AOT-startup StreamingAssets read order, hotfix-assembly load order, missing-BDebug restoration, default execution order, and the E2E scene-auto-start contract inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same launcher contract assertions.
    /// E2E startup has been decoupled from BDLauncher — now handled by Talos.E2E.E2ESceneAutoStarter MonoBehaviour via scene attachment.
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
        /// 验证 AOT 运行时加载器会跳过已由 Player 预加载的 preserved 热更程序集。
        /// Verify that the AOT runtime loader skips preserved hot-update assemblies already loaded by the player.
        /// </summary>
        public void ScriptLoderAOT_ShouldSkipAssembliesAlreadyLoadedByPlayer()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTSkipsAssembliesAlreadyLoadedByPlayer();
        }

        /// <summary>
        /// 验证 WindowPreconfig 宿主测试可以反射解析继承的 GameConfigManager.Inst 静态属性。
        /// Verify that the WindowPreconfig host test can resolve the inherited GameConfigManager.Inst static property through reflection.
        /// </summary>
        public void WindowPreconfigHostTest_ShouldResolveInheritedGameConfigManagerInstProperty()
        {
            FrameworkContractAssertions.VerifyWindowPreconfigHostReflectionCanResolveGameConfigManagerInst();
        }

        /// <summary>
        /// 验证 E2E 场景自启动组件在 Talos.E2E.Runtime 中存在且具备 IL2CPP 保活机制。
        /// Verify that the E2E scene auto-starter exists in Talos.E2E.Runtime with IL2CPP keep-alive mechanism.
        /// 原 TryLaunchTalosE2EInDebugBuild 已从 BDLauncher 移除，E2E 启动现由 E2ESceneAutoStarter 负责。
        /// </summary>
        public void BDLauncher_ShouldOwnDebugTalosStartupBridge()
        {
            FrameworkContractAssertions.VerifyBDLauncherOwnsDebugTalosStartupBridge();
        }

        /// <summary>
        /// 验证 E2ESceneAutoStarter 持有 IL2CPP 保活引用，确保 Talos.E2E.Runtime 在 Debug 构建中不被裁剪。
        /// Verify that E2ESceneAutoStarter holds an IL2CPP keep-alive reference, ensuring Talos.E2E.Runtime is not stripped in Debug builds.
        /// 原 PreserveE2EAssemblyReferenceForIL2CPP 已从 BDLauncher 移除。
        /// </summary>
        public void BDLauncher_ShouldPreserveE2EAssemblyForIL2CPP()
        {
            FrameworkContractAssertions.VerifyBDLauncherPreservesE2EAssemblyForIL2CPP();
        }

    }
}
