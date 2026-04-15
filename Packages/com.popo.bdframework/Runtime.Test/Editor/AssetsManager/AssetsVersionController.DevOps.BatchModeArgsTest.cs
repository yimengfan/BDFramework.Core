using System;
using BDFramework.ResourceMgr;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.AssetsManager
{
    /// <summary>
    /// 覆盖文件服务器 BatchMode 参数桥接的纯逻辑测试。
    /// Pure logic tests covering the file-server BatchMode argument bridge.
    /// 这些断言只验证显式参数列表如何被收敛成请求对象，不依赖真实下载、TeamCity 或文件服务器。
    /// These assertions only verify how explicit argument lists are converted into request objects and do not depend on real downloads, TeamCity, or a file server.
    /// </summary>
    public class AssetsVersionControllerDevOpsBatchModeArgsTest
    {
        /// <summary>
        /// 在每个测试入口开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose and means log at the start of each test entry.
        /// 这样无论走 Unity Test Runner 还是 CI 控制台，都能直接看到当前参数桥接断言的目标。
        /// This makes the goal of the current argument-bridge assertion visible in both Unity Test Runner and CI console output.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            string testName;
            try
            {
                testName = TestContext.CurrentContext?.Test?.Name;
            }
            catch
            {
                testName = null;
            }

            Debug.Log(
                $"[测试开始] name={(string.IsNullOrEmpty(testName) ? nameof(AssetsVersionControllerDevOpsBatchModeArgsTest) : testName)} 测试目的=验证文件服务器 BatchMode 参数桥接会稳定构造请求对象。 实现手段=使用显式参数数组驱动请求构造，并断言版本、平台与失败契约。");
        }

        /// <summary>
        /// 验证显式参数列表会按约定参数名与大小写无关规则稳定构造请求对象。
        /// Verify that an explicit argument list creates the request object with the agreed argument names and case-insensitive matching.
        /// </summary>
        [Test]
        public void CreateFileServerBatchVerificationRequestFromArgs_UsesExpectedBatchArgumentNames()
        {
            var request = AssetsVersionController.CreateFileServerBatchVerificationRequestFromArgs(
                new[]
                {
                    "-FILESERVERURL", " http://127.0.0.1:20001/ ",
                    "-expectedCodeVersion", " 101 ",
                    "-EXPECTEDASSETBUNDLEVERSION", " 202 ",
                    "-expectedTableVersion", " 303 "
                },
                resetLocalStateBeforeVerify: false);

            Assert.That(request.ServerUrl, Is.EqualTo("http://127.0.0.1:20001/"));
            Assert.That(request.ExpectedVersionInfo.CodeVersion, Is.EqualTo("101"));
            Assert.That(request.ExpectedVersionInfo.AssetBundleVersion, Is.EqualTo("202"));
            Assert.That(request.ExpectedVersionInfo.TableVersion, Is.EqualTo("303"));
            Assert.That(request.ExpectedVersionInfo.RawValue, Is.EqualTo("101.202.303"));
            Assert.That(request.ResetLocalStateBeforeVerify, Is.False);
        }

        /// <summary>
        /// 验证缺失表格版本值时会抛出显式参数错误，而不是把空值继续传给后续校验。
        /// Verify that a missing table-version value throws an explicit argument error instead of passing an empty value to later verification.
        /// </summary>
        [Test]
        public void CreateFileServerBatchVerificationRequestFromArgs_RejectsMissingTableVersionValue()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                AssetsVersionController.CreateFileServerBatchVerificationRequestFromArgs(
                    new[]
                    {
                        "-fileServerUrl", "http://127.0.0.1:20001/",
                        "-expectedCodeVersion", "101",
                        "-expectedAssetbundleVersion", "202",
                        "-expectedTableVersion"
                    }));

            Assert.That(exception?.ParamName, Is.EqualTo("-expectedTableVersion"));
        }

        /// <summary>
        /// 验证显式参数与目标平台会共同生成可供运行时 owner 直接消费的完整请求对象。
        /// Verify that explicit arguments and the target platform together produce a complete request object ready for the runtime owner.
        /// </summary>
        [Test]
        public void VerifyFileServerAssetsForBatchMode_RequestBuilderKeepsExpectedFieldsReadyForRuntimeOwner()
        {
            var request = AssetsVersionController.CreateFileServerBatchVerificationRequestFromArgs(
                new[]
                {
                    "-fileServerUrl", "http://127.0.0.1:20001/",
                    "-expectedCodeVersion", "11",
                    "-expectedAssetbundleVersion", "22",
                    "-expectedTableVersion", "33"
                });

            request.TargetPlatform = RuntimePlatform.Android;

            Assert.That(request.TargetPlatform, Is.EqualTo(RuntimePlatform.Android));
            Assert.That(request.ExpectedVersionInfo.RawValue, Is.EqualTo("11.22.33"));
            Assert.That(request.ResetLocalStateBeforeVerify, Is.True);
        }
    }
}