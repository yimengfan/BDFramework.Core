using BDFramework.Editor.DevOps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// 覆盖 PublishPipeLineCI BatchMode 请求桥接的编辑器测试。
    /// Editor tests covering the PublishPipeLineCI BatchMode request bridge.
    /// 这些断言验证 BuildTarget 到 RuntimePlatform 的映射与文件服务器参数桥接会走同一条正式生产路径。
    /// These assertions verify that BuildTarget-to-RuntimePlatform mapping and file-server argument bridging stay on the same production path.
    /// </summary>
    public class PublishPipeLineCIBatchModeBridgeTest
    {
        /// <summary>
        /// 在每个测试入口开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose and means log at the start of each test entry.
        /// 这样 Unity Test Runner 和 TeamCity 都能直接看见当前桥接断言验证的是哪条发布路由。
        /// This lets both Unity Test Runner and TeamCity show which publish-route bridge the current assertion is validating.
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
                $"[测试开始] name={(string.IsNullOrEmpty(testName) ? nameof(PublishPipeLineCIBatchModeBridgeTest) : testName)} 测试目的=验证 PublishPipeLineCI 会把 BuildTarget 与文件服务器参数稳定桥接成验证请求。 实现手段=构造显式参数数组并断言目标平台、版本号与重置标记。");
        }

        /// <summary>
        /// 验证 BuildTarget 会被映射成对应的运行时平台，并保留统一的文件服务器验证参数。
        /// Verify that BuildTarget is mapped to the corresponding runtime platform while preserving the shared file-server verification arguments.
        /// </summary>
        [TestCase(BuildTarget.Android, RuntimePlatform.Android)]
        [TestCase(BuildTarget.iOS, RuntimePlatform.IPhonePlayer)]
        [TestCase(BuildTarget.StandaloneWindows64, RuntimePlatform.WindowsPlayer)]
        public void CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform(
            BuildTarget buildTarget,
            RuntimePlatform expectedPlatform)
        {
            var request = PublishPipeLineCI.CreateVerifyClientResRequestForBatchMode(
                buildTarget,
                new[]
                {
                    "-fileServerUrl", " http://127.0.0.1:20001/ ",
                    "-expectedCodeVersion", " 71 ",
                    "-expectedAssetbundleVersion", " 82 ",
                    "-expectedTableVersion", " 93 "
                },
                resetLocalStateBeforeVerify: false);

            Assert.That(request.TargetPlatform, Is.EqualTo(expectedPlatform));
            Assert.That(request.ServerUrl, Is.EqualTo("http://127.0.0.1:20001/"));
            Assert.That(request.ExpectedVersionInfo.RawValue, Is.EqualTo("71.82.93"));
            Assert.That(request.ResetLocalStateBeforeVerify, Is.False);
        }
    }
}