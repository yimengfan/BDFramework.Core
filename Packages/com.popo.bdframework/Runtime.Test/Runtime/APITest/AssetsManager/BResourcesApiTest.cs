using BDFramework.RuntimeTests.Contracts;

namespace BDFramework.RuntimeTests.ApiTest.AssetsManager
{
    /// <summary>
    /// BResources 公开契约的 Runtime 测试主体。
    /// Runtime test body for the BResources public contracts.
    /// 该类型把资源路径拼接、资源组缓存与空列表异步加载保护固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套资源主链路断言。
    /// This type fixes resource-path composition, asset-group caching, and empty-list async-load guards inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same resource-mainline assertions.
    /// </summary>
    public sealed class BResourcesApiTest
    {
        /// <summary>
        /// 输出统一日志，记录 BResources API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the BResources API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(BResourcesApiTest) : testName,
                "验证 BResources 的核心静态契约不会因为资源链路调整而回归。",
                "通过直接调用 FrameworkContractAssertions 的资源路径、资源组缓存与空列表异步加载断言，并校验兼容分支与保护结果。"
            );
        }

        /// <summary>
        /// 验证服务器版控文件路径会稳定拼接平台目录和固定文件名。
        /// Verify that the server version-info path consistently appends the platform directory and fixed file name.
        /// </summary>
        public void GetServerAssetsVersionInfoPath_AppendsPlatformDirectoryAndFileName()
        {
            FrameworkContractAssertions.VerifyServerAssetsVersionInfoPathAppendsPlatformDirectoryAndFileName();
        }

        /// <summary>
        /// 验证资源信息路径的两个重载会分别走根目录和平台目录规则。
        /// Verify that the two resource-info path overloads follow the root-only and platform-directory rules respectively.
        /// </summary>
        public void GetAssetsInfoPath_OverloadsUseExpectedRules()
        {
            FrameworkContractAssertions.VerifyAssetsInfoPathOverloadsUseExpectedRules();
        }

        /// <summary>
        /// 验证旧版分包命名会直接按原文件名拼接。
        /// Verify that legacy sub-package names are appended directly without reformatting.
        /// </summary>
        public void GetAssetsSubPackageInfoPath_KeepsLegacyFileNameUnchanged()
        {
            FrameworkContractAssertions.VerifyLegacySubPackagePathPreserved();
        }

        /// <summary>
        /// 验证新版分包名会被格式化为既定规则。
        /// Verify that modern sub-package names are formatted into the expected rule.
        /// </summary>
        public void GetAssetsSubPackageInfoPath_FormatsModernSubPackageName()
        {
            FrameworkContractAssertions.VerifyModernSubPackagePathFormatted();
        }

        /// <summary>
        /// 验证资源组缓存会保留写入顺序，并且清理后读取为空。
        /// Verify that the asset-group cache preserves insertion order and returns empty after cleanup.
        /// </summary>
        public void AddAssetsPathToGroup_StoresOrderAndClearRemovesEntries()
        {
            FrameworkContractAssertions.VerifyAssetGroupStoresOrderAndClearRemovesEntries();
        }

        /// <summary>
        /// 验证空资源列表异步加载会直接回调空结果，并且不要求事先初始化 ResLoader。
        /// Verify that async loading with an empty asset list immediately returns an empty result and does not require ResLoader initialization.
        /// </summary>
        public void AsyncLoad_WithEmptyAssetList_ReturnsEmptyAndInvokesCallbackWithoutLoader()
        {
            FrameworkContractAssertions.VerifyAsyncLoadWithEmptyListReturnsEmptyAndInvokesCallbackWithoutLoader();
        }
    }
}