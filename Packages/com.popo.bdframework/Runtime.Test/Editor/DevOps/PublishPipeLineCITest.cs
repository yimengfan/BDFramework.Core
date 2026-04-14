using System;
using System.IO;
using System.Text;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.DevOps;
using BDFramework.Editor.Environment;
using BDFramework.ResourceMgr;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// 对应文件服务器 BatchMode 验证请求 owner 的纯参数装配测试。
    /// 这些断言只覆盖 AssetsVersionController 文件服务器验证请求的构造与参数校验，不依赖真实 Unity 下载或 TeamCity 环境。
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
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的文件服务器 BatchMode 参数装配与错误契约。",
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
                "验证文件服务器 BatchMode 参数装配相关断言在 batchmode 下可稳定执行。",
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
                (nameof(BatchModeCommandLine_GetArg_ReturnsValueForCaseInsensitiveArgName),
                    testInstance.BatchModeCommandLine_GetArg_ReturnsValueForCaseInsensitiveArgName),
                (nameof(BatchModeCommandLine_GetArg_ReturnsNullWhenValueMissingOrArgAbsent),
                    testInstance.BatchModeCommandLine_GetArg_ReturnsNullWhenValueMissingOrArgAbsent),
                (nameof(BatchModeCommandLine_GetBoolArg_RecognizesSupportedBooleanValues),
                    testInstance.BatchModeCommandLine_GetBoolArg_RecognizesSupportedBooleanValues),
                (nameof(BatchModeCommandLine_GetBoolArg_ReturnsDefaultWhenValueMissingOrInvalid),
                    testInstance.BatchModeCommandLine_GetBoolArg_ReturnsDefaultWhenValueMissingOrInvalid),
                (nameof(PublishPipeLineCI_IsDebugBuildRequested_ResolvesSharedFlag),
                    testInstance.PublishPipeLineCI_IsDebugBuildRequested_ResolvesSharedFlag),
                (nameof(PublishPipeLineCI_ResolveClientPackageBuildModeForBatchMode_MapsToExpectedMode),
                    testInstance.PublishPipeLineCI_ResolveClientPackageBuildModeForBatchMode_MapsToExpectedMode),
                (nameof(AndroidExternalToolsBatchResolver_IsValidJdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidJdkPath_RecognizesExpectedLayout),
                (nameof(AndroidExternalToolsBatchResolver_IsValidAndroidSdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidAndroidSdkPath_RecognizesExpectedLayout),
                (nameof(AndroidExternalToolsBatchResolver_IsValidAndroidNdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidAndroidNdkPath_RecognizesExpectedLayout),
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
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
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
            var request = AssetsVersionController.CreateFileServerBatchVerificationRequest(
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
                AssetsVersionController.CreateFileServerBatchVerificationRequest(
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
                AssetsVersionController.CreateFileServerBatchVerificationRequest(
                    "http://127.0.0.1:20001",
                    "101",
                    " ",
                    "303"));

            Assert.That(exception?.ParamName, Is.EqualTo("-expectedAssetbundleVersion"));
        }

        /// <summary>
        /// 验证 BatchMode 命令行工具会忽略参数名大小写，并返回紧随其后的参数值。
        /// 这样 TeamCity 或本地脚本在参数大小写不完全一致时，owner 仍能稳定拿到统一值。
        /// </summary>
        [Test]
        public void BatchModeCommandLine_GetArg_ReturnsValueForCaseInsensitiveArgName()
        {
            var args = new[]
            {
                "-clientVersion",
                "1.2.3",
                "-ciOutputRoot",
                "/tmp/output"
            };

            var value = BatchModeCommandLine.GetArg(args, "-CIOUTPUTROOT");

            Assert.That(value, Is.EqualTo("/tmp/output"));
        }

        /// <summary>
        /// 验证当参数不存在、值缺失或输入列表为空时，BatchMode 命令行工具会显式返回空值。
        /// 这样上层 owner 可以用统一的 fallback 规则处理，而不是误读越界参数。
        /// </summary>
        [Test]
        public void BatchModeCommandLine_GetArg_ReturnsNullWhenValueMissingOrArgAbsent()
        {
            Assert.That(BatchModeCommandLine.GetArg((string[])null, "-ciOutputRoot"), Is.Null);
            Assert.That(BatchModeCommandLine.GetArg(new[] { "-ciOutputRoot" }, "-ciOutputRoot"), Is.Null);
            Assert.That(BatchModeCommandLine.GetArg(new[] { "-clientVersion", "1.2.3" }, "-ciOutputRoot"), Is.Null);
        }

        /// <summary>
        /// 验证 BatchMode 布尔参数会识别常见真假字面量，避免不同脚本层重复做值归一化。
        /// </summary>
        [Test]
        public void BatchModeCommandLine_GetBoolArg_RecognizesSupportedBooleanValues()
        {
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "true" }, "-buildDebug"), Is.True);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "1" }, "-buildDebug"), Is.True);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "YES" }, "-buildDebug"), Is.True);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "off" }, "-buildDebug", true), Is.False);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "0" }, "-buildDebug", true), Is.False);
        }

        /// <summary>
        /// 验证当 BatchMode 布尔参数缺失、无值或值非法时，会稳定回退到默认值。
        /// 这样上层 owner 可以只声明默认策略，而不用处理额外异常分支。
        /// </summary>
        [Test]
        public void BatchModeCommandLine_GetBoolArg_ReturnsDefaultWhenValueMissingOrInvalid()
        {
            Assert.That(BatchModeCommandLine.GetBoolArg((string[])null, "-buildDebug", true), Is.True);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug" }, "-buildDebug", true), Is.True);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-buildDebug", "maybe" }, "-buildDebug"), Is.False);
            Assert.That(BatchModeCommandLine.GetBoolArg(new[] { "-clientVersion", "1.2.3" }, "-buildDebug", true), Is.True);
        }

        /// <summary>
        /// 验证 PublishPipeLineCI 会统一复用共享的 <c>-buildDebug</c> 参数约定。
        /// 这样 TeamCity、Python wrapper 与 owner 层可以围绕同一布尔开关协作。
        /// </summary>
        [Test]
        public void PublishPipeLineCI_IsDebugBuildRequested_ResolvesSharedFlag()
        {
            Assert.That(PublishPipeLineCI.IsDebugBuildRequested(new[] { "-buildDebug", "true" }), Is.True);
            Assert.That(PublishPipeLineCI.IsDebugBuildRequested(new[] { "-buildDebug", "off" }), Is.False);
            Assert.That(PublishPipeLineCI.IsDebugBuildRequested(new[] { "-clientVersion", "0.1.1" }), Is.False);
        }

        /// <summary>
        /// 验证母包 BatchMode 构建模式会根据共享布尔参数稳定映射到 Debug 或 Release。
        /// 这确保新增的可选 debug 参数不会破坏现有默认 Release 行为。
        /// </summary>
        [Test]
        public void PublishPipeLineCI_ResolveClientPackageBuildModeForBatchMode_MapsToExpectedMode()
        {
            Assert.That(
                PublishPipeLineCI.ResolveClientPackageBuildModeForBatchMode(new[] { "-buildDebug", "true" }),
                Is.EqualTo(BuildTools_ClientPackage.BuildMode.Debug));
            Assert.That(
                PublishPipeLineCI.ResolveClientPackageBuildModeForBatchMode(new[] { "-buildDebug", "false" }),
                Is.EqualTo(BuildTools_ClientPackage.BuildMode.Release));
            Assert.That(
                PublishPipeLineCI.ResolveClientPackageBuildModeForBatchMode(new[] { "-clientVersion", "0.1.1" }),
                Is.EqualTo(BuildTools_ClientPackage.BuildMode.Release));
        }

        /// <summary>
        /// 验证 Android JDK 路径探测只要命中约定的 javac 布局，就会被识别成可用目录。
        /// 这覆盖了本次从 CI wrapper 下沉出的纯文件系统判断逻辑。
        /// </summary>
        [Test]
        public void AndroidExternalToolsBatchResolver_IsValidJdkPath_RecognizesExpectedLayout()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var jdkRoot = Path.Combine(tempRoot, "jdk");
                Directory.CreateDirectory(Path.Combine(jdkRoot, "bin"));
#if UNITY_EDITOR_WIN
                File.WriteAllText(Path.Combine(jdkRoot, "bin", "javac.exe"), string.Empty);
#else
                File.WriteAllText(Path.Combine(jdkRoot, "bin", "javac"), string.Empty);
#endif

                Assert.That(AndroidExternalToolsBatchResolver.IsValidJdkPath(jdkRoot), Is.True);
                Assert.That(AndroidExternalToolsBatchResolver.IsValidJdkPath(Path.Combine(tempRoot, "missing-jdk")), Is.False);
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 Android SDK 路径探测会识别标准的 platform-tools/adb 布局。
        /// 这样无论路径来自环境变量还是自动探测，基础设施层都能一致地判定是否可用。
        /// </summary>
        [Test]
        public void AndroidExternalToolsBatchResolver_IsValidAndroidSdkPath_RecognizesExpectedLayout()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var sdkRoot = Path.Combine(tempRoot, "sdk");
                Directory.CreateDirectory(Path.Combine(sdkRoot, "platform-tools"));
#if UNITY_EDITOR_WIN
                File.WriteAllText(Path.Combine(sdkRoot, "platform-tools", "adb.exe"), string.Empty);
#else
                File.WriteAllText(Path.Combine(sdkRoot, "platform-tools", "adb"), string.Empty);
#endif

                Assert.That(AndroidExternalToolsBatchResolver.IsValidAndroidSdkPath(sdkRoot), Is.True);
                Assert.That(AndroidExternalToolsBatchResolver.IsValidAndroidSdkPath(Path.Combine(tempRoot, "missing-sdk")), Is.False);
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 Android NDK 路径探测会识别 source.properties 与 toolchains 组合布局。
        /// 这样 CI 在没有直接命中 ndk-build 可执行文件时，仍能通过目录结构判断合法性。
        /// </summary>
        [Test]
        public void AndroidExternalToolsBatchResolver_IsValidAndroidNdkPath_RecognizesExpectedLayout()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var ndkRoot = Path.Combine(tempRoot, "ndk");
                Directory.CreateDirectory(Path.Combine(ndkRoot, "toolchains"));
                File.WriteAllText(Path.Combine(ndkRoot, "source.properties"), "Pkg.Revision = 23.1.7779620");

                Assert.That(AndroidExternalToolsBatchResolver.IsValidAndroidNdkPath(ndkRoot), Is.True);
                Assert.That(AndroidExternalToolsBatchResolver.IsValidAndroidNdkPath(Path.Combine(tempRoot, "missing-ndk")), Is.False);
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
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
                $"验证 {testName} 对应的文件服务器 BatchMode 参数装配与错误契约。",
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

        /// <summary>
        /// 为纯文件系统测试创建独占临时目录，避免不同测试共享目录造成串扰。
        /// </summary>
        private static string CreateTempDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "BDFramework-PublishPipeLineCITest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        /// <summary>
        /// 清理测试使用的临时目录，避免本地和 CI 机器留下无用目录。
        /// </summary>
        private static void DeleteDirectoryIfExists(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }
}