using System.IO;

namespace BDFramework.RuntimeTests.ApiTest.Utils.IO
{
    /// <summary>
    /// 路径辅助器公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the path helper.
    /// 该类型把分隔符合并与 Unity 资源路径归一化契约固定在 Runtime.Test 的 APITest 层内，
    /// 便于打包后继续校验不依赖编辑器 API 的路径规则。
    /// This type fixes separator-merging and Unity asset-path normalization contracts inside the Runtime.Test APITest layer,
    /// making it possible to keep validating path rules that do not depend on editor APIs after packaging.
    /// </summary>
    public sealed class PathApiTest
    {
        /// <summary>
        /// 输出统一日志，记录路径 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the path API test.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(PathApiTest) : testName,
                "验证路径辅助器的分隔符合并与路径规范化规则保持稳定。",
                "通过直接调用 IPath 公开 API，并断言输出路径字符串符合既有规则。"
            );
        }

        /// <summary>
        /// 验证边界两侧都带斜杠时，不会产生双分隔符。
        /// Verify that when both sides contain boundary slashes, no duplicate separator is produced.
        /// </summary>
        public void Combine_WithBoundarySlashes_RemovesDuplicateSeparator()
        {
            var combinedPath = IPath.Combine("root/", "/child");

            ApiTestAssert.AreEqual("root/child", combinedPath, "IPath.Combine 应移除重复分隔符。");
        }

        /// <summary>
        /// 验证任一侧为空时，会直接返回另一侧内容。
        /// Verify that when either side is empty, the other side is returned directly.
        /// </summary>
        public void Combine_WithEmptyInput_ReturnsOtherSide()
        {
            ApiTestAssert.AreEqual("child", IPath.Combine(string.Empty, "child"), "左侧为空时应直接返回右侧路径。");
            ApiTestAssert.AreEqual("root", IPath.Combine("root", string.Empty), "右侧为空时应直接返回左侧路径。");
        }

        /// <summary>
        /// 验证缺失尾部斜杠时，只会补齐一次。
        /// Verify that when the trailing slash is missing, it is appended only once.
        /// </summary>
        public void AddEndSymbol_WithMissingSlash_AppendsTrailingSlashOnce()
        {
            ApiTestAssert.AreEqual("root/path/", IPath.AddEndSymbol("root/path"), "缺失尾部斜杠时应补齐一次。");
            ApiTestAssert.AreEqual("root/path/", IPath.AddEndSymbol("root/path/"), "已存在尾部斜杠时不应重复追加。");
        }

        /// <summary>
        /// 验证绝对 Assets 路径会被规范化为 Unity 使用的相对路径格式。
        /// Verify that an absolute Assets path is normalized to the Unity-relative path format.
        /// </summary>
        public void FormatPathOnUnity3d_WithAbsoluteAssetsPath_ReturnsAssetsRelativeNormalizedPath()
        {
            var formattedPath = IPath.FormatPathOnUnity3d(@"C:\project\Assets\Resource\Config");

            ApiTestAssert.AreEqual("Assets/Resource/Config", formattedPath, "FormatPathOnUnity3d 应输出 Unity 相对路径。");
        }
    }
}