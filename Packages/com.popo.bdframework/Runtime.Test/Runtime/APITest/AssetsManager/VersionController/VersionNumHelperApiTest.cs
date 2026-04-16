using BDFramework.ResourceMgr;

namespace BDFramework.RuntimeTests.ApiTest.AssetsManager.VersionController
{
    /// <summary>
    /// 版本号辅助器公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the version-number helper.
    /// 该类型把版本解析、补零、递增与比较规则固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor 包装和真机 Talos 套件共享同一套版本号契约断言。
    /// This type fixes version parsing, zero-filling, incrementing, and comparison rules inside the Runtime.Test APITest layer,
    /// allowing editor wrappers and packaged Talos suites to share the same version-number contract assertions.
    /// </summary>
    public sealed class VersionNumHelperApiTest
    {
        /// <summary>
        /// 输出统一日志，记录版本号 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the version-number API test.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(VersionNumHelperApiTest) : testName,
                "验证版本号辅助 API 的解析、递增与比较规则保持稳定。",
                "通过直接调用 VersionNumHelper 的公开方法，并断言输出版本字符串与比较结果。"
            );
        }

        /// <summary>
        /// 验证同主次版本下，较低的目标自增号会回退为在旧版本基础上继续递增。
        /// Verify that with the same major and minor version, a lower requested additive value falls back to incrementing from the previous version.
        /// </summary>
        public void AddVersionNum_WithSameMajorMinor_IncrementsFromPreviousAdditive()
        {
            var version = VersionNumHelper.AddVersionNum("1.2.3", "1.2.1");

            ApiTestAssert.AreEqual("1.2.4", version, "同主次版本下的自增号应基于旧版本继续递增。");
        }

        /// <summary>
        /// 验证主次版本提升后，自增号会被重置为零。
        /// Verify that when the major or minor version increases, the additive value resets to zero.
        /// </summary>
        public void AddVersionNum_WithHigherMinor_ResetsAdditiveToZero()
        {
            var version = VersionNumHelper.AddVersionNum("1.2.9", "1.3.0");

            ApiTestAssert.AreEqual("1.3.0", version, "主次版本提升后自增号应重置为零。");
        }

        /// <summary>
        /// 验证缺失小版本或自增段时，会自动补零。
        /// Verify that missing minor or additive segments are automatically zero-filled.
        /// </summary>
        public void ParseVersion_WithMissingSegments_FillsMissingValuesWithZero()
        {
            var parsed = VersionNumHelper.ParseVersion("7.8");

            ApiTestAssert.AreEqual(7, parsed.bigNum, "ParseVersion 应保留主版本号。");
            ApiTestAssert.AreEqual(8, parsed.smallNum, "ParseVersion 应保留次版本号。");
            ApiTestAssert.AreEqual(0, parsed.additiveNum, "ParseVersion 对缺失的自增段应补零。");
        }

        /// <summary>
        /// 验证大版本更高时，即便小版本更低，也应视为更大或相等。
        /// Verify that a higher major version is still treated as greater-or-equal even if the minor version is lower.
        /// </summary>
        public void GT_WithHigherMajor_ReturnsTrueAcrossMinorBoundary()
        {
            var isGreaterOrEqual = VersionNumHelper.GT("2.0.0", "1.99.99");

            ApiTestAssert.IsTrue(isGreaterOrEqual, "更高的大版本应被视为大于等于较低大版本的目标版本。");
        }
    }
}