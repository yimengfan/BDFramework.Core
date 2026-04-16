using System;
using System.IO;
using System.Text;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.RuntimeTests.ApiTest.AssetsManager.VersionController
{
    /// <summary>
    /// 客户端资源路径与构建信息公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public APIs of client-asset paths and build info.
    /// 该类型把版本归一化、多寻址路径拼接、构建信息反序列化和文件回退规则固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor 包装与真机 Talos 套件共享同一套资源入口工具契约断言。
    /// This type fixes version normalization, multi-address path composition, build-info deserialization, and file-fallback rules inside the Runtime.Test APITest layer,
    /// allowing editor wrappers and packaged Talos suites to share the same resource-entry helper contract assertions.
    /// </summary>
    public sealed class ClientAssetsUtilsApiTest
    {
        private string temporaryRootDirectory;

        /// <summary>
        /// 为每个测试创建独立的临时目录并输出统一日志。
        /// Create an isolated temporary directory for each test and emit a unified log.
        /// </summary>
        public void SetUp(string testName)
        {
            temporaryRootDirectory = Path.Combine(Path.GetTempPath(), "bdframework-client-assets-utils-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRootDirectory);

            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(ClientAssetsUtilsApiTest) : testName,
                "验证客户端资源路径解析、构建信息读取与多寻址回退规则保持稳定。",
                "通过临时目录构造 package_build.info 和测试文件，直接调用 ClientAssetsUtils 公开 API 并断言结果。"
            );
        }

        /// <summary>
        /// 清理每个测试创建的临时目录，避免磁盘副作用污染后续验证。
        /// Clean up the temporary directory created by each test so disk side effects do not leak into later verifications.
        /// </summary>
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(temporaryRootDirectory) && Directory.Exists(temporaryRootDirectory))
            {
                Directory.Delete(temporaryRootDirectory, true);
            }

            temporaryRootDirectory = null;
        }

        /// <summary>
        /// 验证多寻址路径会把版本归一化为大版本，并同步更新静态首选与备用目录。
        /// Verify that the multi-address path normalizes the version to the major line and also updates the static primary and secondary directories.
        /// </summary>
        public void GetMultiAssetsLoadPath_NormalizesVersionAndUpdatesStaticDirectories()
        {
            var platformPath = BApplication.GetPlatformLoadPath(RuntimePlatform.Android);
            var (firstLoadDirectory, secondLoadDirectory) = ClientAssetsUtils.GetMultiAssetsLoadPath(
                RuntimePlatform.Android,
                "1.2.3");

            ApiTestAssert.AreEqual(
                IPath.Combine(BApplication.persistentDataPath, "1.0.0", platformPath),
                firstLoadDirectory,
                "多寻址首选目录应把版本归一化到大版本线路。"
            );
            ApiTestAssert.AreEqual(
                IPath.Combine(BApplication.streamingAssetsPath, platformPath),
                secondLoadDirectory,
                "多寻址备用目录应保持为 StreamingAssets 对应平台目录。"
            );
            ApiTestAssert.AreEqual(
                IPath.Combine(firstLoadDirectory, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                ClientAssetsUtils.GetPersistentAssetPath(ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                "首选静态目录应同步到持久化资源路径。"
            );
            ApiTestAssert.AreEqual(
                IPath.Combine(secondLoadDirectory, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                ClientAssetsUtils.GetStreamingAssetPath(ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                "备用静态目录应同步到 StreamingAssets 资源路径。"
            );
        }

        /// <summary>
        /// 验证 package_build.info 存在时，会完整反序列化关键版本字段。
        /// Verify that when package_build.info exists, the key version fields are fully deserialized.
        /// </summary>
        public void GetPackageBuildInfo_WithExistingFile_ParsesSerializedFields()
        {
            var outputDirectory = Path.Combine(temporaryRootDirectory, "publish");
            var platformPath = BApplication.GetPlatformLoadPath(RuntimePlatform.Android);
            var platformDirectory = Path.Combine(outputDirectory, platformPath);
            Directory.CreateDirectory(platformDirectory);
            File.WriteAllText(
                Path.Combine(platformDirectory, ClientAssetsUtils.PACKAGE_BUILD_INFO_PATH),
                "{\"BuildTime\":123,\"Version\":\"5.6.7\",\"BasePckScriptSVCVersion\":\"11\",\"HotfixScriptSVCVersion\":\"22\",\"AssetBundleSVCVersion\":\"33\",\"TableSVCVersion\":\"44\"}",
                Encoding.UTF8);

            var buildInfo = ClientAssetsUtils.GetPackageBuildInfo(outputDirectory, RuntimePlatform.Android);

            ApiTestAssert.AreEqual(123L, buildInfo.BuildTime, "BuildTime 反序列化结果不匹配。");
            ApiTestAssert.AreEqual("5.6.7", buildInfo.Version, "Version 反序列化结果不匹配。");
            ApiTestAssert.AreEqual("11", buildInfo.BasePckScriptSVCVersion, "BasePckScriptSVCVersion 反序列化结果不匹配。");
            ApiTestAssert.AreEqual("22", buildInfo.HotfixScriptSVCVersion, "HotfixScriptSVCVersion 反序列化结果不匹配。");
            ApiTestAssert.AreEqual("33", buildInfo.AssetBundleSVCVersion, "AssetBundleSVCVersion 反序列化结果不匹配。");
            ApiTestAssert.AreEqual("44", buildInfo.TableSVCVersion, "TableSVCVersion 反序列化结果不匹配。");
        }

        /// <summary>
        /// 验证 package_build.info 缺失时，会返回稳定的默认构建信息对象。
        /// Verify that when package_build.info is missing, a stable default build-info object is returned.
        /// </summary>
        public void GetPackageBuildInfo_WithMissingFile_ReturnsDefaultBuildInfo()
        {
            var buildInfo = ClientAssetsUtils.GetPackageBuildInfo(temporaryRootDirectory, RuntimePlatform.OSXPlayer);

            ApiTestAssert.AreEqual(0L, buildInfo.BuildTime, "缺失 package_build.info 时 BuildTime 应保持为默认值。");
            ApiTestAssert.AreEqual("0.0.0", buildInfo.Version, "缺失 package_build.info 时 Version 应保持为默认值。");
            ApiTestAssert.AreEqual("none", buildInfo.BasePckScriptSVCVersion, "缺失 package_build.info 时 BasePckScriptSVCVersion 应保持为默认值。");
            ApiTestAssert.AreEqual("none", buildInfo.HotfixScriptSVCVersion, "缺失 package_build.info 时 HotfixScriptSVCVersion 应保持为默认值。");
            ApiTestAssert.AreEqual("none", buildInfo.AssetBundleSVCVersion, "缺失 package_build.info 时 AssetBundleSVCVersion 应保持为默认值。");
            ApiTestAssert.AreEqual("none", buildInfo.TableSVCVersion, "缺失 package_build.info 时 TableSVCVersion 应保持为默认值。");
        }

        /// <summary>
        /// 验证首选目录缺失文件时，会正确回退到备用目录读取字节内容。
        /// Verify that when the primary directory misses a file, the helper falls back to the secondary directory for bytes.
        /// </summary>
        public void ReadAllBytes_WhenPrimaryFileIsMissing_FallsBackToSecondaryDirectory()
        {
            var firstDirectory = Path.Combine(temporaryRootDirectory, "first");
            var secondDirectory = Path.Combine(temporaryRootDirectory, "second");
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            var expected = Encoding.UTF8.GetBytes("secondary-bytes");
            File.WriteAllBytes(Path.Combine(secondDirectory, "payload.bytes"), expected);

            var actual = ClientAssetsUtils.ReadAllBytes(firstDirectory, secondDirectory, "payload.bytes");

            ApiTestAssert.SequenceEqual(expected, actual, "首选目录缺失文件时，应从备用目录读取字节内容。");
        }

        /// <summary>
        /// 验证当首选与备用目录都存在同名文本时，会优先读取首选目录内容。
        /// Verify that when both primary and secondary directories contain the same text file, the primary content is preferred.
        /// </summary>
        public void ReadAllText_WhenBothDirectoriesContainFile_PrefersPrimaryDirectory()
        {
            var firstDirectory = Path.Combine(temporaryRootDirectory, "first");
            var secondDirectory = Path.Combine(temporaryRootDirectory, "second");
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            var utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(firstDirectory, "payload.txt"), "primary", utf8WithoutBom);
            File.WriteAllText(Path.Combine(secondDirectory, "payload.txt"), "secondary", utf8WithoutBom);

            var actual = ClientAssetsUtils.ReadAllText(firstDirectory, secondDirectory, "payload.txt");

            ApiTestAssert.AreEqual("primary", actual, "首选目录存在文件时，应优先读取首选目录内容。");
        }
    }
}