using System;
using System.IO;
using System.Text;
using BDFramework.EditorTest.AssetsManager;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// PublishPipeLineCI BatchMode 桥接测试的显式批验证入口。
    /// Explicit batch verification entry for the PublishPipeLineCI BatchMode bridge tests.
    /// 当项目级初始化干扰 Unity 原生 <c>-runTests</c> 结果导出时，这个入口会直接执行新增桥接断言并写出稳定报告。
    /// When project-level initialization interferes with Unity's native <c>-runTests</c> result export, this entry runs the new bridge assertions directly and writes a stable report.
    /// </summary>
    public static class PublishPipeLineCIBatchModeBridgeBatchVerification
    {
        /// <summary>
        /// 顺序执行本次新增的桥接测试，并把结果写到 Library 报告文件。
        /// Execute the newly added bridge tests sequentially and write the results to a Library report file.
        /// </summary>
        public static void RunBatchVerification()
        {
            LogTestPurposeAndMeans(
                nameof(PublishPipeLineCIBatchModeBridgeBatchVerification),
                "验证 PublishPipeLineCI 与文件服务器 BatchMode 参数桥接会稳定生成目标请求对象。",
                "顺序执行新增的参数桥接与平台映射断言，输出批验证报告并使用显式退出码反馈结果。");
            Debug.Log("[测试进度] suite=PublishPipeLineCIBatchModeBridgeBatchVerification stage=start");

            var publishPipeLineTest = new PublishPipeLineCIBatchModeBridgeTest();
            var assetsVersionControllerTest = new AssetsVersionControllerDevOpsBatchModeArgsTest();
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var checks = new (string Name, Action Action)[]
            {
                (
                    nameof(AssetsVersionControllerDevOpsBatchModeArgsTest.CreateFileServerBatchVerificationRequestFromArgs_UsesExpectedBatchArgumentNames),
                    () => ExecuteWithSetUp(
                        assetsVersionControllerTest.SetUp,
                        assetsVersionControllerTest.CreateFileServerBatchVerificationRequestFromArgs_UsesExpectedBatchArgumentNames)),
                (
                    nameof(AssetsVersionControllerDevOpsBatchModeArgsTest.CreateFileServerBatchVerificationRequestFromArgs_RejectsMissingTableVersionValue),
                    () => ExecuteWithSetUp(
                        assetsVersionControllerTest.SetUp,
                        assetsVersionControllerTest.CreateFileServerBatchVerificationRequestFromArgs_RejectsMissingTableVersionValue)),
                (
                    nameof(AssetsVersionControllerDevOpsBatchModeArgsTest.VerifyFileServerAssetsForBatchMode_RequestBuilderKeepsExpectedFieldsReadyForRuntimeOwner),
                    () => ExecuteWithSetUp(
                        assetsVersionControllerTest.SetUp,
                        assetsVersionControllerTest.VerifyFileServerAssetsForBatchMode_RequestBuilderKeepsExpectedFieldsReadyForRuntimeOwner)),
                (
                    nameof(PublishPipeLineCIBatchModeBridgeTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform) + "_Android",
                    () => ExecuteWithSetUp(
                        publishPipeLineTest.SetUp,
                        () => publishPipeLineTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform(
                            UnityEditor.BuildTarget.Android,
                            RuntimePlatform.Android))),
                (
                    nameof(PublishPipeLineCIBatchModeBridgeTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform) + "_iOS",
                    () => ExecuteWithSetUp(
                        publishPipeLineTest.SetUp,
                        () => publishPipeLineTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform(
                            UnityEditor.BuildTarget.iOS,
                            RuntimePlatform.IPhonePlayer))),
                (
                    nameof(PublishPipeLineCIBatchModeBridgeTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform) + "_Windows",
                    () => ExecuteWithSetUp(
                        publishPipeLineTest.SetUp,
                        () => publishPipeLineTest.CreateVerifyClientResRequestForBatchMode_MapsBuildTargetToRuntimePlatform(
                            UnityEditor.BuildTarget.StandaloneWindows64,
                            RuntimePlatform.WindowsPlayer))),
            };

            for (var index = 0; index < checks.Length; index++)
            {
                var check = checks[index];
                Execute(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "PublishPipeLineCIBatchModeBridgeBatchVerification.txt");
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);

            if (failedCount > 0)
            {
                Debug.LogError($"PublishPipeLineCI BatchMode bridge 批验证失败，请查看报告: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"PublishPipeLineCI BatchMode bridge 批验证通过，报告: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 输出统一的测试开始日志，带出测试目的与实现手段。
        /// Emit a unified test-start log that includes purpose and means.
        /// </summary>
        private static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 先执行测试级 SetUp，再执行实际断言。
        /// Run the test-level SetUp first, then execute the actual assertion.
        /// </summary>
        private static void ExecuteWithSetUp(Action setUp, Action action)
        {
            setUp();
            action();
        }

        /// <summary>
        /// 执行单个断言并把结果写入统一批验证报告。
        /// Execute a single assertion and append the result to the shared batch verification report.
        /// </summary>
        private static void Execute(
            int currentIndex,
            int totalCount,
            string testName,
            Action action,
            StringBuilder reportBuilder,
            ref int failedCount)
        {
            Debug.Log($"[测试进度] suite=PublishPipeLineCIBatchModeBridgeBatchVerification current={currentIndex}/{totalCount} name={testName}");
            try
            {
                action();
                reportBuilder.AppendLine($"PASS {testName}");
                Debug.Log($"[测试进度] suite=PublishPipeLineCIBatchModeBridgeBatchVerification current={currentIndex}/{totalCount} name={testName} status=passed");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {testName}");
                reportBuilder.AppendLine(exception.ToString());
                Debug.LogError($"[测试进度] suite=PublishPipeLineCIBatchModeBridgeBatchVerification current={currentIndex}/{totalCount} name={testName} status=failed err={exception.Message}");
            }
        }
    }
}