using BDFramework.RuntimeTests.ApiTest;
using BDFramework.RuntimeTests.ApiTest.AssetsManager;
using BDFramework.RuntimeTests.ApiTest.Config;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架可打包契约测试套件。
    /// Packaged framework contract test suite.
    /// 该套件把原本主要停留在 Editor 下的启动器、配置与资源静态契约迁移为 Runtime Talos E2E 用例，
    /// 让打包后的母包也能持续验证这些无编辑器依赖的基础行为。
    /// This suite moves launcher, configuration, and resource static contracts that previously lived mainly under the editor into runtime Talos E2E cases,
    /// allowing packaged players to keep validating these foundational behaviors that do not depend on editor APIs.
    /// </summary>
    public static class FrameworkContractTests
    {
        /// <summary>
        /// 验证脚本加载器初始化入口可通过静态反射发现。
        /// Verify that the script-loader initialization entry can be discovered through static reflection.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 1, des: "script-loader-init-contract")]
        public static void ScriptLoaderInitContract()
        {
            var test = new BdLauncherApiTest();
            test.SetUp(nameof(ScriptLoaderInitContract));
            test.FindScriptLoderInitMethod_ShouldResolveStaticMethod();
        }

        /// <summary>
        /// 验证启动器默认执行顺序契约。
        /// Verify the launcher default execution-order contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 2, des: "launcher-order-contract")]
        public static void LauncherExecutionOrderContract()
        {
            var test = new BdLauncherApiTest();
            test.SetUp(nameof(LauncherExecutionOrderContract));
            test.BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder();
        }

        /// <summary>
        /// 验证 E2E 自动检测入口依赖编译期 DEBUG 裁剪。
        /// Verify that the E2E auto-detection entry depends on compile-time DEBUG stripping.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 3, des: "launcher-e2e-contract")]
        public static void LauncherE2EEntryContract()
        {
            var test = new BdLauncherApiTest();
            test.SetUp(nameof(LauncherE2EEntryContract));
            test.TryStartE2EFramework_ShouldUseConditionalDebugAttribute();
        }

        /// <summary>
        /// 验证 AOT 启动阶段的 StreamingAssets 读取契约。
        /// Verify the StreamingAssets read contract during the AOT startup phase.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 4, des: "aot-streaming-assets-contract")]
        public static void AOTStreamingAssetsContract()
        {
            var test = new BdLauncherApiTest();
            test.SetUp(nameof(AOTStreamingAssetsContract));
            test.GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory();
        }

        /// <summary>
        /// 验证 AOT 启动阶段的热更程序集依赖重试契约。
        /// Verify the hotfix-assembly dependency retry contract during the AOT startup phase.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 5, des: "aot-hotfix-retry-contract")]
        public static void AOTHotfixRetryContract()
        {
            var test = new BdLauncherApiTest();
            test.SetUp(nameof(AOTHotfixRetryContract));
            test.LoadHotfixAssemblies_ShouldRetryWhenDependenciesBecomeAvailableLater();
        }

        /// <summary>
        /// 验证运行态 launcher 配置文本优先级最高。
        /// Verify that runtime launcher config text keeps the highest priority.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 10, des: "config-runtime-source-priority")]
        public static void ConfigRuntimeSourcePriority()
        {
            var test = new GameConfigManagerApiTest();
            test.SetUp(nameof(ConfigRuntimeSourcePriority));
            test.ResolveFrameworkConfigTextSource_PrefersRuntimeLauncherTextWhenPlaying();
        }

        /// <summary>
        /// 验证场景 launcher 配置文本回退。
        /// Verify the scene launcher config-text fallback contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 11, des: "config-scene-fallback")]
        public static void ConfigSceneFallback()
        {
            var test = new GameConfigManagerApiTest();
            test.SetUp(nameof(ConfigSceneFallback));
            test.ResolveFrameworkConfigTextSource_FallsBackToSceneLauncherText();
        }

        /// <summary>
        /// 验证编辑器默认配置文件回退契约。
        /// Verify the editor default-file fallback contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 12, des: "config-editor-default-fallback")]
        public static void ConfigEditorDefaultFallback()
        {
            var test = new GameConfigManagerApiTest();
            test.SetUp(nameof(ConfigEditorDefaultFallback));
            test.ResolveFrameworkConfigTextSource_UsesEditorDefaultFileAfterLauncherFallbacks();
        }

        /// <summary>
        /// 验证空配置来源分支会返回 None。
        /// Verify that the empty configuration-source branch returns None.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 13, des: "config-none-fallback")]
        public static void ConfigNoneFallback()
        {
            var test = new GameConfigManagerApiTest();
            test.SetUp(nameof(ConfigNoneFallback));
            test.ResolveFrameworkConfigTextSource_ReturnsNoneWhenNoSourceExists();
        }

        /// <summary>
        /// 验证配置来源日志格式化契约。
        /// Verify the configuration-source log-format contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 14, des: "config-log-format-contract")]
        public static void ConfigLogFormatContract()
        {
            var test = new GameConfigManagerApiTest();
            test.SetUp(nameof(ConfigLogFormatContract));
            test.FormatFrameworkConfigSourceLogMessage_UsesFallbackMarkerForMissingSource();
        }

        /// <summary>
        /// 验证配置管理器装载前置条件契约。
        /// Verify the configuration-manager loading-precondition contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 15, des: "config-loader-precondition-contract")]
        public static void ConfigLoaderPreconditionContract()
        {
            var test = new GameConfigLoderApiTest();
            test.SetUp(nameof(ConfigLoaderPreconditionContract));
            test.ShouldLoadFrameworkConfigManager_MatchesManagerPresence();
        }

        /// <summary>
        /// 验证服务器资源版控路径拼接契约。
        /// Verify the server resource version-path contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 20, des: "resource-server-version-path-contract")]
        public static void ResourceServerVersionPathContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceServerVersionPathContract));
            test.GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName();
        }

        /// <summary>
        /// 验证资源信息路径重载契约。
        /// Verify the resource-info path overload contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 21, des: "resource-info-path-overload-contract")]
        public static void ResourceInfoPathOverloadContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceInfoPathOverloadContract));
            test.GetAssetsInfoPath_OverloadsUseExpectedRules();
        }

        /// <summary>
        /// 验证旧版分包命名兼容契约。
        /// Verify the legacy sub-package naming compatibility contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 22, des: "resource-legacy-subpackage-contract")]
        public static void ResourceLegacySubPackageContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceLegacySubPackageContract));
            test.GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged();
        }

        /// <summary>
        /// 验证新版分包命名格式化契约。
        /// Verify the modern sub-package formatting contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 23, des: "resource-modern-subpackage-contract")]
        public static void ResourceModernSubPackageContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceModernSubPackageContract));
            test.GetAssetsSubPackageInfoPath_FormatsModernSubPackageName();
        }

        /// <summary>
        /// 验证资源组缓存与清理契约。
        /// Verify the asset-group caching and cleanup contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 24, des: "resource-group-cache-contract")]
        public static void ResourceGroupCacheContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceGroupCacheContract));
            test.AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries();
        }

        /// <summary>
        /// 验证空资源列表异步加载保护契约。
        /// Verify the empty asset-list async-load guard contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 25, des: "resource-empty-async-load-contract")]
        public static void ResourceEmptyAsyncLoadContract()
        {
            var test = new BResourcesApiTest();
            test.SetUp(nameof(ResourceEmptyAsyncLoadContract));
            test.AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader();
        }
    }
}