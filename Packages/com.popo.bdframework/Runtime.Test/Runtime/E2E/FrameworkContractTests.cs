using BDFramework.RuntimeTests.Contracts;
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
            FrameworkContractAssertions.VerifyScriptLoaderInitMethodCanBeResolved();
        }

        /// <summary>
        /// 验证启动器默认执行顺序契约。
        /// Verify the launcher default execution-order contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 2, des: "launcher-order-contract")]
        public static void LauncherExecutionOrderContract()
        {
            FrameworkContractAssertions.VerifyLauncherDefaultExecutionOrder();
        }

        /// <summary>
        /// 验证 E2E 自动检测入口依赖编译期 DEBUG 裁剪。
        /// Verify that the E2E auto-detection entry depends on compile-time DEBUG stripping.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 3, des: "launcher-e2e-contract")]
        public static void LauncherE2EEntryContract()
        {
            FrameworkContractAssertions.VerifyTryStartE2EFrameworkUsesConditionalDebugAttribute();
        }

        /// <summary>
        /// 验证运行态 launcher 配置文本优先级最高。
        /// Verify that runtime launcher config text keeps the highest priority.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 10, des: "config-runtime-source-priority")]
        public static void ConfigRuntimeSourcePriority()
        {
            FrameworkContractAssertions.VerifyRuntimeLauncherConfigTextPreferredWhenPlaying();
        }

        /// <summary>
        /// 验证场景 launcher 配置文本回退。
        /// Verify the scene launcher config-text fallback contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 11, des: "config-scene-fallback")]
        public static void ConfigSceneFallback()
        {
            FrameworkContractAssertions.VerifySceneLauncherFallback();
        }

        /// <summary>
        /// 验证编辑器默认配置文件回退契约。
        /// Verify the editor default-file fallback contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 12, des: "config-editor-default-fallback")]
        public static void ConfigEditorDefaultFallback()
        {
            FrameworkContractAssertions.VerifyEditorDefaultFileFallback();
        }

        /// <summary>
        /// 验证空配置来源分支会返回 None。
        /// Verify that the empty configuration-source branch returns None.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 13, des: "config-none-fallback")]
        public static void ConfigNoneFallback()
        {
            FrameworkContractAssertions.VerifyNoConfigSourceReturnsNone();
        }

        /// <summary>
        /// 验证配置来源日志格式化契约。
        /// Verify the configuration-source log-format contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 14, des: "config-log-format-contract")]
        public static void ConfigLogFormatContract()
        {
            FrameworkContractAssertions.VerifyFormatFrameworkConfigSourceLogMessageFallback();
        }

        /// <summary>
        /// 验证配置管理器装载前置条件契约。
        /// Verify the configuration-manager loading-precondition contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 15, des: "config-loader-precondition-contract")]
        public static void ConfigLoaderPreconditionContract()
        {
            FrameworkContractAssertions.VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence();
        }

        /// <summary>
        /// 验证服务器资源版控路径拼接契约。
        /// Verify the server resource version-path contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 20, des: "resource-server-version-path-contract")]
        public static void ResourceServerVersionPathContract()
        {
            FrameworkContractAssertions.VerifyServerAssetsVersionInfoPathAppendsPlatformDirectoryAndFileName();
        }

        /// <summary>
        /// 验证资源信息路径重载契约。
        /// Verify the resource-info path overload contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 21, des: "resource-info-path-overload-contract")]
        public static void ResourceInfoPathOverloadContract()
        {
            FrameworkContractAssertions.VerifyAssetsInfoPathOverloadsUseExpectedRules();
        }

        /// <summary>
        /// 验证旧版分包命名兼容契约。
        /// Verify the legacy sub-package naming compatibility contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 22, des: "resource-legacy-subpackage-contract")]
        public static void ResourceLegacySubPackageContract()
        {
            FrameworkContractAssertions.VerifyLegacySubPackagePathPreserved();
        }

        /// <summary>
        /// 验证新版分包命名格式化契约。
        /// Verify the modern sub-package formatting contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 23, des: "resource-modern-subpackage-contract")]
        public static void ResourceModernSubPackageContract()
        {
            FrameworkContractAssertions.VerifyModernSubPackagePathFormatted();
        }

        /// <summary>
        /// 验证资源组缓存与清理契约。
        /// Verify the asset-group caching and cleanup contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 24, des: "resource-group-cache-contract")]
        public static void ResourceGroupCacheContract()
        {
            FrameworkContractAssertions.VerifyAssetGroupStoresOrderAndClearRemovesEntries();
        }

        /// <summary>
        /// 验证空资源列表异步加载保护契约。
        /// Verify the empty asset-list async-load guard contract.
        /// </summary>
        [E2ETest(suite: "framework-contract", order: 25, des: "resource-empty-async-load-contract")]
        public static void ResourceEmptyAsyncLoadContract()
        {
            FrameworkContractAssertions.VerifyAsyncLoadWithEmptyListReturnsEmptyAndInvokesCallbackWithoutLoader();
        }
    }
}