using System;
using BDFramework.RuntimeTests.ApiTest.AssetsManager.VersionController;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 版本控制 Runtime API 契约测试套件。
    /// Runtime API contract suite for the version-controller layer.
    /// 该套件把版本号辅助器与客户端资源工具的纯逻辑 API 断言迁移为可打包执行的 Talos E2E 用例，
    /// 让真机环境也能持续覆盖版本归一化、构建信息读取与多寻址回退规则。
    /// This suite moves the pure-logic API assertions for the version-number helper and client-asset utilities into packaged Talos E2E cases,
    /// allowing device runs to keep covering version normalization, build-info loading, and multi-address fallback rules.
    /// </summary>
    public static class VersionControllerApiContractTests
    {
        /// <summary>
        /// 验证同主次版本下，较低的目标自增号会回退为在旧版本基础上继续递增。
        /// Verify that with the same major and minor version, a lower requested additive value falls back to incrementing from the previous version.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 1, des: "version-add-same-major-minor")]
        public static void VersionAddSameMajorMinor()
        {
            var test = new VersionNumHelperApiTest();
            test.SetUp(nameof(VersionAddSameMajorMinor));
            test.AddVersionNum_WithSameMajorMinor_IncrementsFromPreviousAdditive();
        }

        /// <summary>
        /// 验证主次版本提升后，自增号会被重置为零。
        /// Verify that when the major or minor version increases, the additive value resets to zero.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 2, des: "version-add-higher-minor-reset")]
        public static void VersionAddHigherMinorReset()
        {
            var test = new VersionNumHelperApiTest();
            test.SetUp(nameof(VersionAddHigherMinorReset));
            test.AddVersionNum_WithHigherMinor_ResetsAdditiveToZero();
        }

        /// <summary>
        /// 验证缺失小版本或自增段时，会自动补零。
        /// Verify that missing minor or additive segments are automatically zero-filled.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 3, des: "version-parse-zero-fill")]
        public static void VersionParseZeroFill()
        {
            var test = new VersionNumHelperApiTest();
            test.SetUp(nameof(VersionParseZeroFill));
            test.ParseVersion_WithMissingSegments_FillsMissingValuesWithZero();
        }

        /// <summary>
        /// 验证大版本更高时，即便小版本更低，也应视为更大或相等。
        /// Verify that a higher major version is still treated as greater-or-equal even if the minor version is lower.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 4, des: "version-gt-major-boundary")]
        public static void VersionGtMajorBoundary()
        {
            var test = new VersionNumHelperApiTest();
            test.SetUp(nameof(VersionGtMajorBoundary));
            test.GT_WithHigherMajor_ReturnsTrueAcrossMinorBoundary();
        }

        /// <summary>
        /// 验证多寻址路径会把版本归一化为大版本，并同步更新静态首选与备用目录。
        /// Verify that the multi-address path normalizes the version to the major line and also updates the static primary and secondary directories.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 10, des: "client-assets-multi-path-normalize")]
        public static void ClientAssetsMultiPathNormalize()
        {
            var test = new ClientAssetsUtilsApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(ClientAssetsMultiPathNormalize)),
                test.GetMultiAssetsLoadPath_NormalizesVersionAndUpdatesStaticDirectories,
                test.TearDown);
        }

        /// <summary>
        /// 验证 package_build.info 存在时，会完整反序列化关键版本字段。
        /// Verify that when package_build.info exists, the key version fields are fully deserialized.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 11, des: "client-assets-build-info-existing")]
        public static void ClientAssetsBuildInfoExisting()
        {
            var test = new ClientAssetsUtilsApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(ClientAssetsBuildInfoExisting)),
                test.GetPackageBuildInfo_WithExistingFile_ParsesSerializedFields,
                test.TearDown);
        }

        /// <summary>
        /// 验证 package_build.info 缺失时，会返回稳定的默认构建信息对象。
        /// Verify that when package_build.info is missing, a stable default build-info object is returned.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 12, des: "client-assets-build-info-missing")]
        public static void ClientAssetsBuildInfoMissing()
        {
            var test = new ClientAssetsUtilsApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(ClientAssetsBuildInfoMissing)),
                test.GetPackageBuildInfo_WithMissingFile_ReturnsDefaultBuildInfo,
                test.TearDown);
        }

        /// <summary>
        /// 验证首选目录缺失文件时，会正确回退到备用目录读取字节内容。
        /// Verify that when the primary directory misses a file, the helper falls back to the secondary directory for bytes.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 13, des: "client-assets-read-bytes-fallback")]
        public static void ClientAssetsReadBytesFallback()
        {
            var test = new ClientAssetsUtilsApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(ClientAssetsReadBytesFallback)),
                test.ReadAllBytes_WhenPrimaryFileIsMissing_FallsBackToSecondaryDirectory,
                test.TearDown);
        }

        /// <summary>
        /// 验证当首选与备用目录都存在同名文本时，会优先读取首选目录内容。
        /// Verify that when both primary and secondary directories contain the same text file, the primary content is preferred.
        /// </summary>
        [E2ETest(suite: "version-controller-api", order: 14, des: "client-assets-read-text-primary")]
        public static void ClientAssetsReadTextPrimary()
        {
            var test = new ClientAssetsUtilsApiTest();
            ExecuteWithLifecycle(
                () => test.SetUp(nameof(ClientAssetsReadTextPrimary)),
                test.ReadAllText_WhenBothDirectoriesContainFile_PrefersPrimaryDirectory,
                test.TearDown);
        }

        /// <summary>
        /// 按生命周期顺序执行 Runtime API 测试。
        /// Execute a runtime API test in lifecycle order.
        /// </summary>
        private static void ExecuteWithLifecycle(Action setUp, Action action, Action tearDown)
        {
            setUp();
            try
            {
                action();
            }
            finally
            {
                tearDown?.Invoke();
            }
        }
    }
}