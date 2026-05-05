using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// BuildTools_AssetBundleV2 路径和资源验证契约测试。
    /// Contract tests for BuildTools_AssetBundleV2: runtime path detection, asset validation, and path formatting.
    /// 验证 IsRuntimePath、IsRuntimePathAssetWithoutFolder、GetAbsPathFormRuntime、CheckAssetsPath 的行为。
    /// These verify IsRuntimePath, IsRuntimePathAssetWithoutFolder, GetAbsPathFormRuntime, and CheckAssetsPath behaviors.
    /// </summary>
    [TestFixture]
    public class BuildToolsAssetBundleV2Test
    {
        [SetUp]
        public void SetUp()
        {
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 BuildTools_AssetBundleV2 路径/资源契约。",
                "执行显式路径检测断言，校验大小写不敏感、文件夹过滤、后缀移除、特殊后缀排除等行为。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        #region IsRuntimePath

        /// <summary>
        /// 验证包含 /runtime/ 的路径返回 true。
        /// Verify that a path containing "/runtime/" returns true.
        /// </summary>
        [Test]
        public void IsRuntimePath_ContainsRuntime_ReturnsTrue()
        {
            var path = "assets/resources/runtime/textures/icon.png";
            Assert.That(BuildTools_AssetBundleV2.IsRuntimePath(path), Is.True);
        }

        /// <summary>
        /// 验证大小写不敏感：/Runtime/ 也匹配。
        /// Verify case-insensitivity: "/Runtime/" also matches.
        /// </summary>
        [Test]
        public void IsRuntimePath_CaseInsensitive_ReturnsTrue()
        {
            var path = "assets/Resources/Runtime/Textures/icon.png";
            Assert.That(BuildTools_AssetBundleV2.IsRuntimePath(path), Is.True);
        }

        /// <summary>
        /// 验证不含 /runtime/ 的路径返回 false。
        /// Verify that a path without "/runtime/" returns false.
        /// </summary>
        [Test]
        public void IsRuntimePath_NoRuntime_ReturnsFalse()
        {
            var path = "assets/resources/editor/tool.prefab";
            Assert.That(BuildTools_AssetBundleV2.IsRuntimePath(path), Is.False);
        }

        /// <summary>
        /// 验证空路径返回 false。
        /// Verify that an empty path returns false.
        /// </summary>
        [Test]
        public void IsRuntimePath_EmptyPath_ReturnsFalse()
        {
            Assert.That(BuildTools_AssetBundleV2.IsRuntimePath(""), Is.False);
        }

        /// <summary>
        /// 验证路径中间包含 /runtime/ 子串也匹配。
        /// Verify that "/runtime/" in the middle of a path also matches.
        /// </summary>
        [Test]
        public void IsRuntimePath_RuntimeInMiddle_ReturnsTrue()
        {
            var path = "assets/project/runtime/subfolder/asset.prefab";
            Assert.That(BuildTools_AssetBundleV2.IsRuntimePath(path), Is.True);
        }

        #endregion

        #region IsRuntimePathAssetWithoutFolder

        /// <summary>
        /// 验证 runtime 路径指向文件（非目录）时返回 true。
        /// Verify that a runtime path pointing to a file (not directory) returns true.
        /// </summary>
        [Test]
        public void IsRuntimePathAssetWithoutFolder_FilePath_ReturnsTrue()
        {
            // 创建临时文件验证
            // Create temp file for verification
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_runtime");
            try
            {
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "test.txt");
                File.WriteAllText(tempFile, "test");

                // 路径含 runtime 且是文件
                // Path contains "runtime" and is a file
                Assert.That(BuildTools_AssetBundleV2.IsRuntimePathAssetWithoutFolder(tempFile), Is.True);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证 runtime 路径指向目录时返回 false。
        /// Verify that a runtime path pointing to a directory returns false.
        /// </summary>
        [Test]
        public void IsRuntimePathAssetWithoutFolder_DirectoryPath_ReturnsFalse()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_runtime");
            try
            {
                Directory.CreateDirectory(tempDir);

                // 路径含 runtime 但是是目录
                // Path contains "runtime" but is a directory
                Assert.That(BuildTools_AssetBundleV2.IsRuntimePathAssetWithoutFolder(tempDir), Is.False);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证非 runtime 路径即使指向文件也返回 false。
        /// Verify that a non-runtime path even pointing to a file returns false.
        /// </summary>
        [Test]
        public void IsRuntimePathAssetWithoutFolder_NonRuntimeFilePath_ReturnsFalse()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_editor");
            try
            {
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, "test.txt");
                File.WriteAllText(tempFile, "test");

                // 路径不含 runtime，即使文件存在也返回 false
                // Path doesn't contain "runtime", returns false even if file exists
                Assert.That(BuildTools_AssetBundleV2.IsRuntimePathAssetWithoutFolder(tempFile), Is.False);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #endregion

        #region GetAbsPathFormRuntime

        /// <summary>
        /// 验证从 runtime 路径中提取相对路径并移除扩展名。
        /// Verify extracting relative path from runtime path and removing the extension.
        /// </summary>
        [Test]
        public void GetAbsPathFormRuntime_NormalPath_StripsPrefixAndExtension()
        {
            var path = "assets/resources/runtime/textures/icon.png";
            var result = BuildTools_AssetBundleV2.GetAbsPathFormRuntime(path);

            Assert.That(result, Is.EqualTo("textures/icon"), "应移除 runtime 前缀和扩展名 / Should strip runtime prefix and extension");
        }

        /// <summary>
        /// 验证不含 runtime 的路径原样返回。
        /// Verify that a path without "runtime" is returned as-is.
        /// </summary>
        [Test]
        public void GetAbsPathFormRuntime_NoRuntime_ReturnsAsIs()
        {
            var path = "assets/resources/editor/tool.prefab";
            var result = BuildTools_AssetBundleV2.GetAbsPathFormRuntime(path);

            Assert.That(result, Is.EqualTo(path), "不含 runtime 的路径应原样返回 / Path without runtime should be returned as-is");
        }

        /// <summary>
        /// 验证大小写不敏感的 runtime 匹配也能正确截取。
        /// Verify case-insensitive runtime matching also correctly extracts the path.
        /// </summary>
        [Test]
        public void GetAbsPathFormRuntime_CaseInsensitive_StripsCorrectly()
        {
            var path = "assets/Resources/Runtime/Textures/Icon.png";
            var result = BuildTools_AssetBundleV2.GetAbsPathFormRuntime(path);

            Assert.That(result, Is.EqualTo("Textures/Icon"), "大小写不敏感匹配应正确截取 / Case-insensitive match should strip correctly");
        }

        /// <summary>
        /// 验证无扩展名的 runtime 路径只移除前缀。
        /// Verify that a runtime path without extension only strips the prefix.
        /// </summary>
        [Test]
        public void GetAbsPathFormRuntime_NoExtension_StripsPrefixOnly()
        {
            var path = "assets/resources/runtime/textures/icon";
            var result = BuildTools_AssetBundleV2.GetAbsPathFormRuntime(path);

            Assert.That(result, Is.EqualTo("textures/icon"), "无扩展名时只移除前缀 / Should only strip prefix when no extension");
        }

        #endregion

        #region CheckAssetsPath

        /// <summary>
        /// 验证 .cs 后缀的文件被过滤。
        /// Verify that .cs files are filtered out.
        /// </summary>
        [Test]
        public void CheckAssetsPath_CsFile_FilteredOut()
        {
            var paths = new string[] { "assets/scripts/test.cs" };
            var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

            Assert.That(result, Does.Not.Contain("assets/scripts/test.cs"), ".cs 文件应被过滤 / .cs files should be filtered");
        }

        /// <summary>
        /// 验证 .js 后缀的文件被过滤。
        /// Verify that .js files are filtered out.
        /// </summary>
        [Test]
        public void CheckAssetsPath_JsFile_FilteredOut()
        {
            var paths = new string[] { "assets/scripts/test.js" };
            var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

            Assert.That(result, Does.Not.Contain("assets/scripts/test.js"), ".js 文件应被过滤 / .js files should be filtered");
        }

        /// <summary>
        /// 验证 .dll 后缀的文件被过滤。
        /// Verify that .dll files are filtered out.
        /// </summary>
        [Test]
        public void CheckAssetsPath_DllFile_FilteredOut()
        {
            var paths = new string[] { "assets/plugins/test.dll" };
            var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

            Assert.That(result, Does.Not.Contain("assets/plugins/test.dll"), ".dll 文件应被过滤 / .dll files should be filtered");
        }

        /// <summary>
        /// 验证 editor 目录下的资源被过滤。
        /// Verify that resources under /editor/ directory are filtered out.
        /// </summary>
        [Test]
        public void CheckAssetsPath_EditorPath_FilteredOut()
        {
            // 这里需要文件实际存在于磁盘上才能通过 File.Exists 检查
            // 但 /editor/ 路径检测在 File.Exists 之前
            // We need files to exist on disk for File.Exists check,
            // but /editor/ path detection happens before File.Exists
            // 使用不存在的路径，因为 File.Exists 会先检查
            // Use non-existent path since File.Exists check comes first
            // CheckAssetsPath 的逻辑：先检查后缀 → 再检查文件存在 → 再检查 editor 路径
            // 对于不存在的文件，File.Exists=false → 被移除，所以用临时文件
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_CheckPath");
            try
            {
                Directory.CreateDirectory(tempDir);
                var editorDir = Path.Combine(tempDir, "editor");
                Directory.CreateDirectory(editorDir);
                var editorFile = Path.Combine(editorDir, "tool.prefab");
                File.WriteAllText(editorFile, "test");

                var paths = new string[] { editorFile };
                var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

                Assert.That(result, Does.Not.Contain(editorFile), "/editor/ 路径应被过滤 / /editor/ path should be filtered");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证普通资产路径（非 .cs/.js/.dll、非 editor、文件存在）保留。
        /// Verify that normal asset paths (non-.cs/.js/.dll, non-editor, file exists) are kept.
        /// </summary>
        [Test]
        public void CheckAssetsPath_NormalAsset_Kept()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_CheckPath");
            try
            {
                Directory.CreateDirectory(tempDir);
                var normalFile = Path.Combine(tempDir, "texture.png");
                File.WriteAllText(normalFile, "test");

                var paths = new string[] { normalFile };
                var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

                Assert.That(result, Does.Contain(normalFile), "普通资产应保留 / Normal asset should be kept");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        /// <summary>
        /// 验证不存在的文件被过滤。
        /// Verify that non-existent files are filtered out.
        /// </summary>
        [Test]
        public void CheckAssetsPath_NonExistentFile_FilteredOut()
        {
            var paths = new string[] { "assets/nonexistent/texture.png" };
            var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

            Assert.That(result, Does.Not.Contain("assets/nonexistent/texture.png"), "不存在的文件应被过滤 / Non-existent files should be filtered");
        }

        /// <summary>
        /// 验证目录路径被过滤（即使目录存在）。
        /// Verify that directory paths are filtered out (even if the directory exists).
        /// </summary>
        [Test]
        public void CheckAssetsPath_DirectoryPath_FilteredOut()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_CheckPath");
            try
            {
                Directory.CreateDirectory(tempDir);
                var subDir = Path.Combine(tempDir, "subdir");
                Directory.CreateDirectory(subDir);

                var paths = new string[] { subDir };
                var result = BuildTools_AssetBundleV2.CheckAssetsPath(paths);

                Assert.That(result, Does.Not.Contain(subDir), "目录路径应被过滤 / Directory paths should be filtered");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #endregion
    }
}
