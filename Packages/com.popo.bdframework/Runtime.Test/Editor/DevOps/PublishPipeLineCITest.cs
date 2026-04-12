using System;
using System.IO;
using System.Text;
using BDFramework.Editor.DevOps;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// 对应 PublishPipeLineCI.cs 的纯参数装配测试。
    /// 这些断言只覆盖 BatchMode 文件服务器验证请求的构造与参数校验，不依赖真实 Unity 下载或 TeamCity 环境。
    /// </summary>
    public class PublishPipeLineCITest
    {
        /// <summary>
        /// 在每个 NUnit 测试入口开始时输出统一的测试目的与实现手段日志。
        /// 这样无论是 Unity Test Runner 还是 TeamCity 收集控制台输出，都能直接看到当前参数装配测试的验证目标。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 PublishPipeLineCI 参数装配与错误契约。",
                "执行显式参数装配断言，并校验构造结果与异常参数名。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// 当本地或 CI 需要绕过 Unity Test Runner 时，可以直接执行这组纯参数装配断言。
        /// </summary>
        public static void RunBatchVerification()
        {
            // Phase 1: 顺序执行这组纯参数断言，并把每个结果写入统一报告。
            LogTestPurposeAndMeans(nameof(PublishPipeLineCITest),
                "验证 PublishPipeLineCI BatchMode 参数装配相关断言在 batchmode 下可稳定执行。",
                "顺序执行参数装配与错误分支断言、写出批验证报告，并用显式退出码反馈结果。");
            Debug.Log("[测试进度] suite=PublishPipeLineCITest stage=start");
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var testInstance = new PublishPipeLineCITest();
            var checks = new (string Name, Action Action)[]
            {
                (nameof(CreateFileServerBatchVerificationRequest_BuildsExpectedRequest),
                    testInstance.CreateFileServerBatchVerificationRequest_BuildsExpectedRequest),
                (nameof(CreateFileServerBatchVerificationRequest_RejectsMissingServerUrl),
                    testInstance.CreateFileServerBatchVerificationRequest_RejectsMissingServerUrl),
                (nameof(CreateFileServerBatchVerificationRequest_RejectsMissingComponentVersion),
                    testInstance.CreateFileServerBatchVerificationRequest_RejectsMissingComponentVersion),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            // Phase 2: 把结果写到 Library，方便 CI 直接收集日志和失败明细。
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "PublishPipeLineCIBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total=3 passed={3 - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            // Phase 3: 用显式退出码把 batchmode 结果反馈给宿主 CI。
            if (failedCount > 0)
            {
                Debug.LogError($"PublishPipeLineCI batch 验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"PublishPipeLineCI batch 验证通过，报告: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 验证 TeamCity 透传的 serverUrl 与三段 build.number 会被稳定收敛成文件服务器验证请求。
        /// </summary>
        [Test]
        public void CreateFileServerBatchVerificationRequest_BuildsExpectedRequest()
        {
            var request = PublishPipeLineCI.CreateFileServerBatchVerificationRequest(
                " http://127.0.0.1:20001/ ",
                " 101 ",
                " 202 ",
                " 303 ");

            Assert.That(request.ServerUrl, Is.EqualTo("http://127.0.0.1:20001/"));
            Assert.That(request.ExpectedVersionInfo.CodeVersion, Is.EqualTo("101"));
            Assert.That(request.ExpectedVersionInfo.AssetBundleVersion, Is.EqualTo("202"));
            Assert.That(request.ExpectedVersionInfo.TableVersion, Is.EqualTo("303"));
            Assert.That(request.ExpectedVersionInfo.RawValue, Is.EqualTo("101.202.303"));
            Assert.That(request.ResetLocalStateBeforeVerify, Is.True);
        }

        /// <summary>
        /// 验证文件服务器地址缺失时会直接抛出显式参数错误，而不是把空值带入后续下载流程。
        /// </summary>
        [Test]
        public void CreateFileServerBatchVerificationRequest_RejectsMissingServerUrl()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                PublishPipeLineCI.CreateFileServerBatchVerificationRequest(
                    string.Empty,
                    "101",
                    "202",
                    "303"));

            Assert.That(exception?.ParamName, Is.EqualTo("-fileServerUrl"));
        }

        /// <summary>
        /// 验证任一组件期望版本缺失时，同样会被视为 TeamCity 参数错误并立刻中止。
        /// </summary>
        [Test]
        public void CreateFileServerBatchVerificationRequest_RejectsMissingComponentVersion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                PublishPipeLineCI.CreateFileServerBatchVerificationRequest(
                    "http://127.0.0.1:20001",
                    "101",
                    " ",
                    "303"));

            Assert.That(exception?.ParamName, Is.EqualTo("-expectedAssetbundleVersion"));
        }

        /// <summary>
        /// 执行单个纯逻辑断言，并把结果统一写入 batch 验证报告。
        /// </summary>
        private static void Execute(int currentIndex,
            int totalCount,
            string testName,
            Action testAction,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            LogTestPurposeAndMeans(testName,
                $"验证 {testName} 对应的 PublishPipeLineCI 参数装配与错误契约。",
                "执行纯参数装配断言，并校验请求字段或异常参数名。");
            Debug.Log($"[测试进度] suite=PublishPipeLineCITest current={currentIndex}/{totalCount} name={testName}");
            try
            {
                testAction();
                reportBuilder.AppendLine($"PASS {testName}");
                Debug.Log(
                    $"[测试进度] suite=PublishPipeLineCITest current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                Debug.LogError(
                    $"[测试进度] suite=PublishPipeLineCITest current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}