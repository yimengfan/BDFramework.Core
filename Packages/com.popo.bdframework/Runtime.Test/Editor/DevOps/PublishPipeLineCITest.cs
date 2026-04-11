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
        /// 提供给 batchmode 的显式验证入口。
        /// 当本地或 CI 需要绕过 Unity Test Runner 时，可以直接执行这组纯参数装配断言。
        /// </summary>
        public static void RunBatchVerification()
        {
            // Phase 1: 顺序执行这组纯参数断言，并把每个结果写入统一报告。
            var testInstance = new PublishPipeLineCITest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;

            Execute(nameof(CreateFileServerBatchVerificationRequest_BuildsExpectedRequest),
                testInstance.CreateFileServerBatchVerificationRequest_BuildsExpectedRequest, reportBuilder,
                ref failedCount);
            Execute(nameof(CreateFileServerBatchVerificationRequest_RejectsMissingServerUrl),
                testInstance.CreateFileServerBatchVerificationRequest_RejectsMissingServerUrl, reportBuilder,
                ref failedCount);
            Execute(nameof(CreateFileServerBatchVerificationRequest_RejectsMissingComponentVersion),
                testInstance.CreateFileServerBatchVerificationRequest_RejectsMissingComponentVersion,
                reportBuilder, ref failedCount);

            // Phase 2: 把结果写到 Library，方便 CI 直接收集日志和失败明细。
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "PublishPipeLineCIBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total=3 passed={3 - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            // Phase 3: 用显式退出码把 batchmode 结果反馈给宿主 CI。
            if (failedCount > 0)
            {
                Debug.LogError($"PublishPipeLineCI batch verification failed. Report: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"PublishPipeLineCI batch verification passed. Report: {outputPath}");
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
        private static void Execute(string testName, Action testAction, StringBuilder reportBuilder, ref int failedCount)
        {
            try
            {
                testAction();
                reportBuilder.AppendLine($"PASS {testName}");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
            }
        }
    }
}