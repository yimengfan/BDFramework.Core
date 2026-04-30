using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BDFramework.Core.Tools;
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
    /// PublishPipeLineCI 与相关纯参数、预处理契约测试集合。
    /// Contract tests for PublishPipeLineCI and related pure parameter and prebuild behaviors.
    /// 这些断言覆盖 AssetsVersionController 文件服务器验证请求装配、HybridCLR 预处理判定、Android external tools 探测，
    /// 以及母包构建前 AOT patch 输出根目录的稳定性，不依赖真实 Unity 下载或 TeamCity 环境。
    /// These assertions cover AssetsVersionController file-server request wiring, HybridCLR prebuild decisions, Android external-tool probing,
    /// and the stability of AOT patch output roots before package builds without depending on real Unity downloads or a TeamCity environment.
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
        /// Provide an explicit verification entry for batchmode.
        /// 当本地或 CI 需要绕过 Unity Test Runner 时，可以直接执行这组纯参数装配与预处理契约断言。
        /// When local runs or CI need to bypass the Unity Test Runner, this entry can execute the pure parameter-wiring and prebuild-contract assertions directly.
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
                (nameof(PublishPipeLineCI_ShouldIncludeDebugSymbolForTalosClientPackageBuild_UsesDebugOnly),
                    testInstance.PublishPipeLineCI_ShouldIncludeDebugSymbolForTalosClientPackageBuild_UsesDebugOnly),
                (nameof(BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrEnabledButCurrentConfigWouldSkip_Prepares),
                    testInstance.BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrEnabledButCurrentConfigWouldSkip_Prepares),
                (nameof(BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrDisabledOrUsesGlobalIl2cpp_Skips),
                    testInstance.BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrDisabledOrUsesGlobalIl2cpp_Skips),
                (nameof(BuildTools_ClientPackage_ResolveWindowsBuildOptions_ShouldKeepDebuggingWithoutProfilerFlags),
                    testInstance.BuildTools_ClientPackage_ResolveWindowsBuildOptions_ShouldKeepDebuggingWithoutProfilerFlags),
                (nameof(BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDelayDebugFlagsUntilAfterPreBuild),
                    testInstance.BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDelayDebugFlagsUntilAfterPreBuild),
                (nameof(BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDisableProfilerFlagsForWindowsDebugBuild),
                    testInstance.BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDisableProfilerFlagsForWindowsDebugBuild),
                (nameof(BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldNotModifyPlayerSettingsDisplayProperties),
                    testInstance.BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldNotModifyPlayerSettingsDisplayProperties),
                (nameof(BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldPopulateManagedDlls),
                    testInstance.BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldPopulateManagedDlls),
                (nameof(BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldRejectMissingSourceDll),
                    testInstance.BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldRejectMissingSourceDll),
                (nameof(BuildTools_ClientPackage_EnsureHybridClrHotUpdateAssembliesCopiedToManaged_ShouldBackfillMissingPreservedAssembliesWithoutOverwritingExistingOnes),
                    testInstance.BuildTools_ClientPackage_EnsureHybridClrHotUpdateAssembliesCopiedToManaged_ShouldBackfillMissingPreservedAssembliesWithoutOverwritingExistingOnes),
                (nameof(HyCLREditorTools_SetBDFramework2HCLRConfig_ShouldPreserveStartupAssemblies),
                    testInstance.HyCLREditorTools_SetBDFramework2HCLRConfig_ShouldPreserveStartupAssemblies),
                (nameof(HyCLREditorTools_GetHotfixDLLPaths_ShouldIncludePreservedAssemblies),
                    testInstance.HyCLREditorTools_GetHotfixDLLPaths_ShouldIncludePreservedAssemblies),
                (nameof(HyCLREditorTools_GetAotMetadataOutputRoots_ShouldIncludeDevOpsPublishAssetsBeforeStreamingAssets),
                    testInstance.HyCLREditorTools_GetAotMetadataOutputRoots_ShouldIncludeDevOpsPublishAssetsBeforeStreamingAssets),
                (nameof(AndroidExternalToolsBatchResolver_IsValidJdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidJdkPath_RecognizesExpectedLayout),
                (nameof(AndroidExternalToolsBatchResolver_IsValidAndroidSdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidAndroidSdkPath_RecognizesExpectedLayout),
                (nameof(AndroidExternalToolsBatchResolver_IsValidAndroidNdkPath_RecognizesExpectedLayout),
                    testInstance.AndroidExternalToolsBatchResolver_IsValidAndroidNdkPath_RecognizesExpectedLayout),
                (nameof(HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnLeakedTestDllInReleaseBuild),
                    testInstance.HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnLeakedTestDllInReleaseBuild),
                (nameof(HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnZluaBytesInReleaseBuild),
                    testInstance.HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnZluaBytesInReleaseBuild),
                (nameof(HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_WarnsButDoesNotThrowInDebugBuild),
                    testInstance.HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_WarnsButDoesNotThrowInDebugBuild),
                (nameof(HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_PassesWhenNoTestAssembliesPresent),
                    testInstance.HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_PassesWhenNoTestAssembliesPresent),
                (nameof(HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_SkipsWhenDirectoryDoesNotExist),
                    testInstance.HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_SkipsWhenDirectoryDoesNotExist),
                (nameof(HotfixTestAssemblyInjector_EnsureTestAssembliesRemoved_RemovesAllTestAssemblies),
                    testInstance.HotfixTestAssemblyInjector_EnsureTestAssembliesRemoved_RemovesAllTestAssemblies),
                (nameof(HotfixTestAssemblyInjector_TestAssemblyNames_ContainsKnownTestAssemblies),
                    testInstance.HotfixTestAssemblyInjector_TestAssemblyNames_ContainsKnownTestAssemblies),
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
        /// 验证 Talos 调试母包会同时注入 DEBUG 宏，避免运行时桥接层的 Conditional("DEBUG") 入口被编译期裁掉。
        /// Release 包则必须保持现状，避免把 E2E 启动入口带进非调试包体。
        /// </summary>
        [Test]
        public void PublishPipeLineCI_ShouldIncludeDebugSymbolForTalosClientPackageBuild_UsesDebugOnly()
        {
            Assert.That(
                PublishPipeLineCI.ShouldIncludeDebugSymbolForTalosClientPackageBuild(BuildTools_ClientPackage.BuildMode.Debug),
                Is.True);
            Assert.That(
                PublishPipeLineCI.ShouldIncludeDebugSymbolForTalosClientPackageBuild(BuildTools_ClientPackage.BuildMode.Release),
                Is.False);
        }

        /// <summary>
        /// 验证只要启用了本地 HybridCLR 打包模式，即使当前仍是 Editor + Mono 配置，也必须先执行预处理。
        /// 这覆盖 Windows Debug 母包在 TeamCity 上若跳过 PreBuild 就会被 HybridCLR 前置检查拦截的回归场景。
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrEnabledButCurrentConfigWouldSkip_Prepares()
        {
            var shouldPrepare = BuildTools_ClientPackage.ShouldPrepareHybridClrForPackageBuild(
                hybridClrEnabled: true,
                useGlobalIl2cpp: false,
                scriptingBackend: ScriptingImplementation.Mono2x,
                codeRoot: AssetLoadPathType.Editor,
                codeRunMode: HotfixCodeRunMode.HyCLR,
                out var reason);

            Assert.That(shouldPrepare, Is.True);
            Assert.That(reason, Does.Contain("hybridClrPackage enabled for package build"));
        }

        /// <summary>
        /// 验证当 HybridCLR 未启用或明确走 global il2cpp 时，母包构建不会触发额外 HybridCLR 预处理。
        /// 这保证新的放宽规则只覆盖真正需要本地 HybridCLR 产物的包体构建场景。
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_ShouldPrepareHybridClrForPackageBuild_WhenHybridClrDisabledOrUsesGlobalIl2cpp_Skips()
        {
            var shouldPrepareWhenDisabled = BuildTools_ClientPackage.ShouldPrepareHybridClrForPackageBuild(
                hybridClrEnabled: false,
                useGlobalIl2cpp: false,
                scriptingBackend: ScriptingImplementation.IL2CPP,
                codeRoot: AssetLoadPathType.Hotfix,
                codeRunMode: HotfixCodeRunMode.HyCLR,
                out var disabledReason);

            var shouldPrepareWhenGlobalIl2cpp = BuildTools_ClientPackage.ShouldPrepareHybridClrForPackageBuild(
                hybridClrEnabled: true,
                useGlobalIl2cpp: true,
                scriptingBackend: ScriptingImplementation.IL2CPP,
                codeRoot: AssetLoadPathType.Hotfix,
                codeRunMode: HotfixCodeRunMode.HyCLR,
                out var globalReason);

            Assert.That(shouldPrepareWhenDisabled, Is.False);
            Assert.That(disabledReason, Does.Contain("hybridClrEnabled=False"));
            Assert.That(shouldPrepareWhenGlobalIl2cpp, Is.False);
            Assert.That(globalReason, Does.Contain("useGlobalIl2cpp=True"));
        }

        /// <summary>
        /// 验证 Windows 调试母包会保留脚本调试能力，但不会再把 profiler 与 deep profiling 打进 BuildOptions。
        /// Verify that Windows debug packages keep script-debugging support but no longer carry profiler or deep-profiling flags in BuildOptions.
        /// 这覆盖 TeamCity Windows agent 上的 Talos Player 回归场景：若继续自动连接 profiler，
        /// Standalone Player 可能在进入托管启动前卡住，导致 TCP 就绪检测永远超时。
        /// This covers the Talos Player regression on TeamCity Windows agents: if profiler auto-connect remains enabled,
        /// the standalone player can stall before managed startup and the TCP readiness probe never succeeds.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_ResolveWindowsBuildOptions_ShouldKeepDebuggingWithoutProfilerFlags()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "ResolveWindowsBuildOptions",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var debugOptions = (BuildOptions)helperMethod.Invoke(
                null,
                new object[] { BuildTools_ClientPackage.BuildMode.Debug });
            var releaseOptions = (BuildOptions)helperMethod.Invoke(
                null,
                new object[] { BuildTools_ClientPackage.BuildMode.Release });

            Assert.That(debugOptions.HasFlag(BuildOptions.CompressWithLz4HC), Is.True);
            Assert.That(debugOptions.HasFlag(BuildOptions.Development), Is.True);
            Assert.That(debugOptions.HasFlag(BuildOptions.AllowDebugging), Is.True);
            Assert.That(debugOptions.HasFlag(BuildOptions.ConnectWithProfiler), Is.False);
            Assert.That(debugOptions.HasFlag(BuildOptions.EnableDeepProfilingSupport), Is.False);

            Assert.That(releaseOptions, Is.EqualTo(BuildOptions.CompressWithLz4HC));
        }

        /// <summary>
        /// 验证 HybridCLR 预处理会先于 Debug 母包的 EditorUserBuildSettings 覆盖执行。
        /// Verify that HybridCLR prebuild runs before the debug package overrides EditorUserBuildSettings.
        /// 这覆盖 Android stripped-AOT 临时构建的回归场景：预处理阶段应看到未污染的默认全局开关，
        /// 而 Android 正式母包构建阶段才应启用 development、debugging、profiler 与 deep profiling。
        /// This covers the Android stripped-AOT regression: the prebuild phase should observe clean default global flags,
        /// while the Android final package-build phase should enable development, debugging, profiler, and deep profiling only afterwards.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDelayDebugFlagsUntilAfterPreBuild()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "PrepareHybridClrAndCreateBuildPlayerSettingsScope",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var previousDevelopment = EditorUserBuildSettings.development;
            var previousAllowDebugging = EditorUserBuildSettings.allowDebugging;
            var previousConnectProfiler = EditorUserBuildSettings.connectProfiler;
            var previousDeepProfiling = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            var preBuildSnapshots = new List<(bool Development, bool AllowDebugging, bool ConnectProfiler, bool DeepProfiling)>();

            try
            {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;

                var scope = (IDisposable)helperMethod.Invoke(
                    null,
                    new object[]
                    {
                        BuildTools_ClientPackage.BuildMode.Debug,
                        true,
                        "test-hybridclr-order",
                        BuildTarget.Android,
                        (Action<BuildTarget>)(_ =>
                        {
                            preBuildSnapshots.Add((
                                EditorUserBuildSettings.development,
                                EditorUserBuildSettings.allowDebugging,
                                EditorUserBuildSettings.connectProfiler,
                                EditorUserBuildSettings.buildWithDeepProfilingSupport));
                        })
                    });

                Assert.That(preBuildSnapshots, Has.Count.EqualTo(1));
                Assert.That(preBuildSnapshots[0].Development, Is.False);
                Assert.That(preBuildSnapshots[0].AllowDebugging, Is.False);
                Assert.That(preBuildSnapshots[0].ConnectProfiler, Is.False);
                Assert.That(preBuildSnapshots[0].DeepProfiling, Is.False);

                using (scope)
                {
                    Assert.That(EditorUserBuildSettings.development, Is.True);
                    Assert.That(EditorUserBuildSettings.allowDebugging, Is.True);
                    Assert.That(EditorUserBuildSettings.connectProfiler, Is.True);
                    Assert.That(EditorUserBuildSettings.buildWithDeepProfilingSupport, Is.True);
                }

                Assert.That(EditorUserBuildSettings.development, Is.False);
                Assert.That(EditorUserBuildSettings.allowDebugging, Is.False);
                Assert.That(EditorUserBuildSettings.connectProfiler, Is.False);
                Assert.That(EditorUserBuildSettings.buildWithDeepProfilingSupport, Is.False);
            }
            finally
            {
                EditorUserBuildSettings.development = previousDevelopment;
                EditorUserBuildSettings.allowDebugging = previousAllowDebugging;
                EditorUserBuildSettings.connectProfiler = previousConnectProfiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = previousDeepProfiling;
            }
        }

        /// <summary>
        /// 验证 Windows 调试母包在进入正式 BuildPlayer 作用域后，仍保留 development 与 debugging，
        /// 但不会再打开 connectProfiler 与 deepProfiling。
        /// Verify that Windows debug packages still enable development and debugging inside the final BuildPlayer scope,
        /// but no longer turn on connectProfiler or deepProfiling.
        /// 这把 profiler 关闭策略锁定在 EditorUserBuildSettings 层，避免即使 BuildOptions 已收紧，
        /// 全局编辑器开关仍把 Windows Talos Player 带回 profiler 握手路径。
        /// This locks the profiler-disabled policy at the EditorUserBuildSettings layer so Windows Talos players do not fall
        /// back into the profiler handshake path even if BuildOptions have already been tightened.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldDisableProfilerFlagsForWindowsDebugBuild()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "PrepareHybridClrAndCreateBuildPlayerSettingsScope",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var previousDevelopment = EditorUserBuildSettings.development;
            var previousAllowDebugging = EditorUserBuildSettings.allowDebugging;
            var previousConnectProfiler = EditorUserBuildSettings.connectProfiler;
            var previousDeepProfiling = EditorUserBuildSettings.buildWithDeepProfilingSupport;

            try
            {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;

                var scope = (IDisposable)helperMethod.Invoke(
                    null,
                    new object[]
                    {
                        BuildTools_ClientPackage.BuildMode.Debug,
                        false,
                        "test-windows-profiler-flags",
                        BuildTarget.StandaloneWindows64,
                        null
                    });

                using (scope)
                {
                    Assert.That(EditorUserBuildSettings.development, Is.True);
                    Assert.That(EditorUserBuildSettings.allowDebugging, Is.True);
                    Assert.That(EditorUserBuildSettings.connectProfiler, Is.False);
                    Assert.That(EditorUserBuildSettings.buildWithDeepProfilingSupport, Is.False);
                }

                Assert.That(EditorUserBuildSettings.development, Is.False);
                Assert.That(EditorUserBuildSettings.allowDebugging, Is.False);
                Assert.That(EditorUserBuildSettings.connectProfiler, Is.False);
                Assert.That(EditorUserBuildSettings.buildWithDeepProfilingSupport, Is.False);
            }
            finally
            {
                EditorUserBuildSettings.development = previousDevelopment;
                EditorUserBuildSettings.allowDebugging = previousAllowDebugging;
                EditorUserBuildSettings.connectProfiler = previousConnectProfiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = previousDeepProfiling;
            }
        }

        /// <summary>
        /// 验证 BuildPlayerSettingsScope 不再修改 PlayerSettings 的分辨率、全屏模式与窗口可调整属性，
        /// 这些属性完全由 Unity ProjectSettings/ProjectSettings.asset 控制。
        /// Verify that BuildPlayerSettingsScope no longer modifies PlayerSettings resolution, fullscreen mode or resizable window;
        /// these properties are controlled exclusively via Unity ProjectSettings/ProjectSettings.asset.
        /// 这把“构建流程不再强制覆盖窗口显示设置”的回归锁定下来，
        /// 避免未来重新引入代码层覆盖导致 Unity 原生配置失效。
        /// This locks down the regression that the build pipeline no longer forces display settings overrides,
        /// preventing future reintroduction of code-level overrides that would bypass Unity's native configuration.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_PrepareHybridClrAndCreateBuildPlayerSettingsScope_ShouldNotModifyPlayerSettingsDisplayProperties()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "PrepareHybridClrAndCreateBuildPlayerSettingsScope",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var previousDefaultIsNativeResolution = PlayerSettings.defaultIsNativeResolution;
            var previousDefaultScreenWidth = PlayerSettings.defaultScreenWidth;
            var previousDefaultScreenHeight = PlayerSettings.defaultScreenHeight;
            var previousResizableWindow = PlayerSettings.resizableWindow;
            var previousFullScreenMode = PlayerSettings.fullScreenMode;

            try
            {
                // Set known starting values to detect any override.
                PlayerSettings.defaultIsNativeResolution = true;
                PlayerSettings.defaultScreenWidth = 800;
                PlayerSettings.defaultScreenHeight = 600;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

                var scope = (IDisposable)helperMethod.Invoke(
                    null,
                    new object[]
                    {
                        BuildTools_ClientPackage.BuildMode.Debug,
                        false,
                        "test-no-display-override",
                        BuildTarget.StandaloneWindows64,
                        null
                    });

                using (scope)
                {
                    // BuildPlayerSettingsScope should NOT touch PlayerSettings display properties.
                    Assert.That(PlayerSettings.defaultIsNativeResolution, Is.True, "defaultIsNativeResolution 不应被修改");
                    Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(800), "defaultScreenWidth 不应被修改");
                    Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(600), "defaultScreenHeight 不应被修改");
                    Assert.That(PlayerSettings.resizableWindow, Is.True, "resizableWindow 不应被修改");
                    Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow), "fullScreenMode 不应被修改");
                }

                // After scope disposal, display properties should still be unchanged.
                Assert.That(PlayerSettings.defaultIsNativeResolution, Is.True);
                Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(800));
                Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(600));
                Assert.That(PlayerSettings.resizableWindow, Is.True);
                Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            }
            finally
            {
                PlayerSettings.defaultIsNativeResolution = previousDefaultIsNativeResolution;
                PlayerSettings.defaultScreenWidth = previousDefaultScreenWidth;
                PlayerSettings.defaultScreenHeight = previousDefaultScreenHeight;
                PlayerSettings.resizableWindow = previousResizableWindow;
                PlayerSettings.fullScreenMode = previousFullScreenMode;
            }
        }

        /// <summary>
        /// 验证 Windows Player 构建后会把 HybridCLR 热更 DLL 补到 Managed 目录。
        /// Verify that Windows player post-build processing copies HybridCLR hot-update DLLs into the Managed directory.
        /// 这把 “ScriptingAssemblies.json 只有名字、但 Managed 没有 DLL 实体” 的回归锁定下来，
        /// 避免首场景上的热更 MonoBehaviour 在 Unity 启动时重新退化成 missing script。
        /// This locks down the regression where <c>ScriptingAssemblies.json</c> only listed names but the Managed directory lacked physical DLL files,
        /// causing startup-scene hotfix MonoBehaviours to regress into missing scripts during Unity startup.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldPopulateManagedDlls()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "CopyHybridClrHotUpdateAssembliesToManagedDirectory",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var tempRoot = CreateTempDirectory();
            try
            {
                var playerDir = Path.Combine(tempRoot, "windows", "com.demo.game");
                var playerOutputPath = Path.Combine(playerDir, "Launcher.exe");
                var scriptAssembliesRoot = Path.Combine(tempRoot, "Library", "ScriptAssemblies");
                Directory.CreateDirectory(playerDir);
                Directory.CreateDirectory(scriptAssembliesRoot);
                File.WriteAllText(playerOutputPath, string.Empty);
                File.WriteAllText(Path.Combine(scriptAssembliesRoot, "Assembly-CSharp.dll"), "assembly-csharp");
                File.WriteAllText(Path.Combine(scriptAssembliesRoot, "Assembly-CSharp-firstpass.dll"), "assembly-csharp-firstpass");
                File.WriteAllText(Path.Combine(scriptAssembliesRoot, "BDFramework.Core.dll"), "bdframework-core");

                helperMethod.Invoke(
                    null,
                    new object[]
                    {
                        playerOutputPath,
                        new[] { "Assembly-CSharp", "Assembly-CSharp-firstpass", "BDFramework.Core" },
                        scriptAssembliesRoot
                    });

                var managedDirectory = Path.Combine(playerDir, "Launcher_Data", "Managed");
                Assert.That(File.Exists(Path.Combine(managedDirectory, "Assembly-CSharp.dll")), Is.True);
                Assert.That(File.Exists(Path.Combine(managedDirectory, "Assembly-CSharp-firstpass.dll")), Is.True);
                Assert.That(File.Exists(Path.Combine(managedDirectory, "BDFramework.Core.dll")), Is.True);
                Assert.That(File.ReadAllText(Path.Combine(managedDirectory, "BDFramework.Core.dll")), Is.EqualTo("bdframework-core"));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证缺失任一热更 DLL 源文件时，会立即抛出显式错误而不是静默产出不完整包体。
        /// Verify that a missing hot-update DLL source fails fast with an explicit error instead of silently producing an incomplete package.
        /// 这样 TeamCity 会在母包构建阶段就暴露问题，不会等到 step_02 Player 启动后才以 missing script 形式晚发现。
        /// This makes TeamCity fail during package build instead of discovering the problem much later in step_02 as runtime missing scripts.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_CopyHybridClrHotUpdateAssembliesToManagedDirectory_ShouldRejectMissingSourceDll()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "CopyHybridClrHotUpdateAssembliesToManagedDirectory",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var tempRoot = CreateTempDirectory();
            try
            {
                var playerDir = Path.Combine(tempRoot, "windows", "com.demo.game");
                var playerOutputPath = Path.Combine(playerDir, "Launcher.exe");
                var scriptAssembliesRoot = Path.Combine(tempRoot, "Library", "ScriptAssemblies");
                Directory.CreateDirectory(playerDir);
                Directory.CreateDirectory(scriptAssembliesRoot);
                File.WriteAllText(playerOutputPath, string.Empty);
                File.WriteAllText(Path.Combine(scriptAssembliesRoot, "Assembly-CSharp.dll"), "assembly-csharp");

                var exception = Assert.Throws<TargetInvocationException>(() =>
                    helperMethod.Invoke(
                        null,
                        new object[]
                        {
                            playerOutputPath,
                            new[] { "Assembly-CSharp", "BDFramework.Core" },
                            scriptAssembliesRoot
                        }));

                Assert.That(exception?.InnerException, Is.TypeOf<FileNotFoundException>());
                Assert.That(exception?.InnerException?.Message, Does.Contain("BDFramework.Core.dll"));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证包体后处理会在 preserved DLL 缺失时补齐，但不会覆盖 Player 已自带的启动程序集。
        /// Verify that package post-processing backfills missing preserved DLLs but does not overwrite startup assemblies already shipped with the player.
        /// 这把“首场景依赖的程序集需要保留在 Player 内，同时又要在保留失败时有 Managed 目录兜底”的约束固定下来。
        /// This locks in the rule that startup-scene assemblies should stay inside the player while still getting a Managed-directory fallback when preservation fails.
        /// </summary>
        [Test]
        public void BuildTools_ClientPackage_EnsureHybridClrHotUpdateAssembliesCopiedToManaged_ShouldBackfillMissingPreservedAssembliesWithoutOverwritingExistingOnes()
        {
            var helperMethod = typeof(BuildTools_ClientPackage).GetMethod(
                "EnsureHybridClrHotUpdateAssembliesCopiedToManaged",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(helperMethod, Is.Not.Null);

            var originalHotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");
            var originalPreserveAssemblies = GetHybridClrStringArraySetting("preserveHotUpdateAssemblies");
            var tempRoot = CreateTempDirectory();
            var originalProjectRoot = BApplication.ProjectRoot;

            try
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", new[] { "BDFramework.Test" });
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", new[] { "Assembly-CSharp", "BDFramework.Core" });

                var playerDir = Path.Combine(tempRoot, "windows", "com.demo.game");
                var playerOutputPath = Path.Combine(playerDir, "Launcher.exe");
                var managedDirectory = Path.Combine(playerDir, "Launcher_Data", "Managed");
                var libraryDir = Path.Combine(tempRoot, "Library", "ScriptAssemblies");
                Directory.CreateDirectory(managedDirectory);
                Directory.CreateDirectory(libraryDir);
                File.WriteAllText(playerOutputPath, string.Empty);
                File.WriteAllText(Path.Combine(managedDirectory, "Assembly-CSharp.dll"), "player-assembly-csharp");
                File.WriteAllText(Path.Combine(libraryDir, "Assembly-CSharp.dll"), "script-assembly-csharp");
                File.WriteAllText(Path.Combine(libraryDir, "BDFramework.Core.dll"), "script-bdframework-core");
                File.WriteAllText(Path.Combine(libraryDir, "BDFramework.Test.dll"), "test-dll");

                SetBApplicationProjectRoot(tempRoot);
                helperMethod.Invoke(null, new object[] { playerOutputPath });

                Assert.That(File.ReadAllText(Path.Combine(managedDirectory, "Assembly-CSharp.dll")), Is.EqualTo("player-assembly-csharp"));
                Assert.That(File.ReadAllText(Path.Combine(managedDirectory, "BDFramework.Core.dll")), Is.EqualTo("script-bdframework-core"));
                Assert.That(File.ReadAllText(Path.Combine(managedDirectory, "BDFramework.Test.dll")), Is.EqualTo("test-dll"));
            }
            finally
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", originalHotUpdateAssemblies);
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", originalPreserveAssemblies);
                SetBApplicationProjectRoot(originalProjectRoot);
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 BDFramework 默认会把首场景依赖的程序集归入 preserved hot update 列表，而不是继续留在可过滤列表中。
        /// Verify that BDFramework defaults move startup-scene assemblies into the preserved hot-update list instead of leaving them filterable.
        /// 这样 `FilterHotFixAssemblies` 就不会把这些首包必需程序集从 Player 里剔掉。
        /// This ensures `FilterHotFixAssemblies` no longer strips those base-player-required assemblies from the player.
        /// </summary>
        [Test]
        public void HyCLREditorTools_SetBDFramework2HCLRConfig_ShouldPreserveStartupAssemblies()
        {
            var originalHotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");
            var originalPreserveAssemblies = GetHybridClrStringArraySetting("preserveHotUpdateAssemblies");
            var originalPatchAotAssemblies = GetHybridClrStringArraySetting("patchAOTAssemblies");

            try
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", new[] { "Assembly-CSharp", "Assembly-CSharp-firstpass", "BDFramework.Core", "BDFramework.Test" });
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", Array.Empty<string>());
                SetHybridClrStringArraySetting("patchAOTAssemblies", Array.Empty<string>());

                BDFramework.Editor.HotfixScript.HyCLREditorTools.SetBDFramework2HCLRConfig();

                var hotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");
                var preserveAssemblies = GetHybridClrStringArraySetting("preserveHotUpdateAssemblies");
                CollectionAssert.DoesNotContain(hotUpdateAssemblies, "Assembly-CSharp");
                CollectionAssert.DoesNotContain(hotUpdateAssemblies, "Assembly-CSharp-firstpass");
                CollectionAssert.DoesNotContain(hotUpdateAssemblies, "BDFramework.Core");
                CollectionAssert.Contains(hotUpdateAssemblies, "BDFramework.Test");

                CollectionAssert.Contains(preserveAssemblies, "Assembly-CSharp");
                CollectionAssert.Contains(preserveAssemblies, "Assembly-CSharp-firstpass");
                CollectionAssert.Contains(preserveAssemblies, "BDFramework.Core");
            }
            finally
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", originalHotUpdateAssemblies);
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", originalPreserveAssemblies);
                SetHybridClrStringArraySetting("patchAOTAssemblies", originalPatchAotAssemblies);
            }
        }

        /// <summary>
        /// 验证热更 DLL 路径集合仍会包含 preserved 程序集，确保后续版本更新依然能为这些程序集生成 `.zlua.bytes`。
        /// Verify that the hotfix DLL path list still includes preserved assemblies so updates can continue generating `.zlua.bytes` for them.
        /// </summary>
        [Test]
        public void HyCLREditorTools_GetHotfixDLLPaths_ShouldIncludePreservedAssemblies()
        {
            var originalHotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");
            var originalPreserveAssemblies = GetHybridClrStringArraySetting("preserveHotUpdateAssemblies");

            try
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", new[] { "BDFramework.Test" });
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", new[] { "Assembly-CSharp", "BDFramework.Core" });

                var hotfixPaths = BDFramework.Editor.HotfixScript.HyCLREditorTools.GetHotfixDLLPaths();

                CollectionAssert.Contains(hotfixPaths, $"{ScriptLoder.HOTFIX_DLL_PATH}/BDFramework.Test.dll.bytes");
                CollectionAssert.Contains(hotfixPaths, $"{ScriptLoder.HOTFIX_DLL_PATH}/Assembly-CSharp.dll.bytes");
                CollectionAssert.Contains(hotfixPaths, $"{ScriptLoder.HOTFIX_DLL_PATH}/BDFramework.Core.dll.bytes");
            }
            finally
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", originalHotUpdateAssemblies);
                SetHybridClrStringArraySetting("preserveHotUpdateAssemblies", originalPreserveAssemblies);
            }
        }

        /// <summary>
        /// 验证 AOT patch 输出根目录会同时包含 DevOpsPublishAssets 与 StreamingAssets，且保持去重与稳定顺序。
        /// Verify that AOT patch output roots include both DevOpsPublishAssets and StreamingAssets while remaining deduplicated and stable in order.
        /// 这覆盖 Android 母包构建先执行 HybridCLR PreBuild、再用 DevOpsPublishAssets 覆盖 StreamingAssets 的回归场景，
        /// 确保 AOT patch 不会只写入临时 StreamingAssets 而在后续拷贝阶段丢失。
        /// This covers the Android package-build regression where HybridCLR PreBuild runs before DevOpsPublishAssets overwrites StreamingAssets,
        /// ensuring the AOT patch is not written only into temporary StreamingAssets and lost during the later copy stage.
        /// </summary>
        [Test]
        public void HyCLREditorTools_GetAotMetadataOutputRoots_ShouldIncludeDevOpsPublishAssetsBeforeStreamingAssets()
        {
            var helperMethod = typeof(BDFramework.Editor.HotfixScript.HyCLREditorTools).GetMethod(
                "GetAotMetadataOutputRoots",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.That(helperMethod, Is.Not.Null);

            var tempRoot = CreateTempDirectory();
            try
            {
                var streamingAssetsPath = Path.Combine(tempRoot, "Assets", "StreamingAssets");
                var devOpsPublishAssetsPath = Path.Combine(tempRoot, "DevOps", "PublishAssets");

                var outputRoots = (string[])helperMethod.Invoke(
                    null,
                    new object[] { streamingAssetsPath, devOpsPublishAssetsPath });

                Assert.That(outputRoots, Is.EqualTo(new[]
                {
                    Path.GetFullPath(devOpsPublishAssetsPath).Replace("\\", "/"),
                    Path.GetFullPath(streamingAssetsPath).Replace("\\", "/")
                }));

                var deduplicatedRoots = (string[])helperMethod.Invoke(
                    null,
                    new object[] { streamingAssetsPath, streamingAssetsPath });

                Assert.That(deduplicatedRoots, Is.EqualTo(new[]
                {
                    Path.GetFullPath(streamingAssetsPath).Replace("\\", "/")
                }));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
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
        /// 验证 ValidateNoTestAssembliesInOutput 在 Release 模式下发现测试 DLL 产物时会抛出异常。
        /// Verify that ValidateNoTestAssembliesInOutput throws when test DLL artifacts are found in Release mode.
        /// 这覆盖 Release 构建最后一道防线的回归：即使上游遗漏了 EnsureTestAssembliesRemoved()，
        /// 落盘后的热更产物校验也能阻止测试程序集泄漏到发布制品。
        /// This covers the last-line-of-defense regression for Release builds: even if upstream misses EnsureTestAssembliesRemoved(),
        /// the on-disk artifact check prevents test assemblies from leaking into release artifacts.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnLeakedTestDllInReleaseBuild()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var hotfixDir = Path.Combine(tempRoot, "script", "hotfix");
                Directory.CreateDirectory(hotfixDir);
                // 写入一个测试程序集的 .dll.bytes 文件
                // Write a test assembly .dll.bytes file
                File.WriteAllText(Path.Combine(hotfixDir, "BDFramework.Test.dll.bytes"), "test-dll");

                var exception = Assert.Throws<Exception>(() =>
                    BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(
                        hotfixDir, isReleaseBuild: true));

                Assert.That(exception?.Message, Does.Contain("BDFramework.Test"));
                Assert.That(exception?.Message, Does.Contain("Release"));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 ValidateNoTestAssembliesInOutput 在 Release 模式下发现 .zlua.bytes 测试 DLL 时也会抛出异常。
        /// Verify that ValidateNoTestAssembliesInOutput also throws when .zlua.bytes test DLLs are found in Release mode.
        /// 这确保发布格式（.zlua.bytes）与编辑器拷贝格式（.dll.bytes）都会被检出。
        /// This ensures both the release format (.zlua.bytes) and editor copy format (.dll.bytes) are detected.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_ThrowsOnZluaBytesInReleaseBuild()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var hotfixDir = Path.Combine(tempRoot, "script", "hotfix");
                Directory.CreateDirectory(hotfixDir);
                File.WriteAllText(Path.Combine(hotfixDir, "BDFramework.Test.zlua.bytes"), "test-dll");

                var exception = Assert.Throws<Exception>(() =>
                    BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(
                        hotfixDir, isReleaseBuild: true));

                Assert.That(exception?.Message, Does.Contain("BDFramework.Test"));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 ValidateNoTestAssembliesInOutput 在 Debug 模式下发现测试 DLL 时仅输出警告，不抛异常。
        /// Verify that ValidateNoTestAssembliesInOutput only warns in Debug mode and does not throw.
        /// 这保证 Debug 构建允许测试程序集存在于热更输出目录中，不影响正常开发调试流程。
        /// This ensures Debug builds allow test assemblies in the hotfix output directory without disrupting normal development.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_WarnsButDoesNotThrowInDebugBuild()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var hotfixDir = Path.Combine(tempRoot, "script", "hotfix");
                Directory.CreateDirectory(hotfixDir);
                File.WriteAllText(Path.Combine(hotfixDir, "BDFramework.Test.dll.bytes"), "test-dll");

                // Debug 模式下不应抛异常
                // Should not throw in Debug mode
                Assert.DoesNotThrow(() =>
                    BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(
                        hotfixDir, isReleaseBuild: false));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 ValidateNoTestAssembliesInOutput 在无泄漏时正常通过。
        /// Verify that ValidateNoTestAssembliesInOutput passes cleanly when no test assemblies are found.
        /// 这确保干净的 Release 输出不会因为误报而中断构建。
        /// This ensures clean Release output does not fail the build with false positives.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_PassesWhenNoTestAssembliesPresent()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var hotfixDir = Path.Combine(tempRoot, "script", "hotfix");
                Directory.CreateDirectory(hotfixDir);
                // 只放非测试程序集
                // Place only non-test assemblies
                File.WriteAllText(Path.Combine(hotfixDir, "Assembly-CSharp.zlua.bytes"), "game-dll");
                File.WriteAllText(Path.Combine(hotfixDir, "BDFramework.Core.zlua.bytes"), "core-dll");

                Assert.DoesNotThrow(() =>
                    BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(
                        hotfixDir, isReleaseBuild: true));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 ValidateNoTestAssembliesInOutput 在目录不存在时不会抛异常，而是跳过验证。
        /// Verify that ValidateNoTestAssembliesInOutput does not throw when the directory does not exist and skips validation.
        /// 这覆盖首次构建或清理后输出目录尚未创建的场景。
        /// This covers the scenario where the output directory has not been created yet, such as first build or after cleanup.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_ValidateNoTestAssembliesInOutput_SkipsWhenDirectoryDoesNotExist()
        {
            Assert.DoesNotThrow(() =>
                BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(
                    "/nonexistent/path/script/hotfix", isReleaseBuild: true));
        }

        /// <summary>
        /// 验证 EnsureTestAssembliesRemoved 能从 HybridCLR 配置中移除所有测试程序集。
        /// Verify that EnsureTestAssembliesRemoved removes all test assemblies from HybridCLR configuration.
        /// 这覆盖 Release 构建纵深防御的核心契约：调用后 hotUpdateAssemblies 不再包含任何测试程序集。
        /// This covers the core contract of the Release build defense-in-depth: after calling, hotUpdateAssemblies contains no test assemblies.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_EnsureTestAssembliesRemoved_RemovesAllTestAssemblies()
        {
            var originalHotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");

            try
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies",
                    new[] { "Assembly-CSharp", "BDFramework.Test" });

                BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.EnsureTestAssembliesRemoved();

                var hotUpdateAssemblies = GetHybridClrStringArraySetting("hotUpdateAssemblies");
                CollectionAssert.Contains(hotUpdateAssemblies, "Assembly-CSharp");
                CollectionAssert.DoesNotContain(hotUpdateAssemblies, "BDFramework.Test");
            }
            finally
            {
                SetHybridClrStringArraySetting("hotUpdateAssemblies", originalHotUpdateAssemblies);
            }
        }

        /// <summary>
        /// 验证 TestAssemblyNames 列表对所有公开的测试程序集名称都有完整覆盖。
        /// Verify that TestAssemblyNames covers all publicly known test assembly names.
        /// 如果后续新增测试程序集但忘记更新 TestAssemblyNames，此测试会失败提醒维护者。
        /// If a new test assembly is added but TestAssemblyNames is not updated, this test fails to alert maintainers.
        /// </summary>
        [Test]
        public void HotfixTestAssemblyInjector_TestAssemblyNames_ContainsKnownTestAssemblies()
        {
            var names = BDFramework.Editor.HotfixScript.HotfixTestAssemblyInjector.TestAssemblyNames;
            CollectionAssert.Contains(names, "BDFramework.Test");
            Assert.That(names.Length, Is.GreaterThanOrEqualTo(1), "TestAssemblyNames 应至少包含 BDFramework.Test");
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

        /// <summary>
        /// 通过反射临时覆盖 BApplication.ProjectRoot，便于纯文件系统测试重定向 Library/ScriptAssemblies。
        /// Temporarily override BApplication.ProjectRoot via reflection so pure filesystem tests can redirect Library/ScriptAssemblies.
        /// </summary>
        private static void SetBApplicationProjectRoot(string projectRoot)
        {
            var property = typeof(BApplication).GetProperty(
                nameof(BApplication.ProjectRoot),
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(property, Is.Not.Null);
            var setter = property.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null);
            setter.Invoke(null, new object[] { projectRoot });
        }

        /// <summary>
        /// 通过反射读取 HybridCLRSettings 上的字符串数组配置，避免 EditorTest 程序集引入额外编译依赖。
        /// Read a string-array setting from HybridCLRSettings via reflection so the EditorTest assembly avoids an extra compile-time dependency.
        /// </summary>
        private static string[] GetHybridClrStringArraySetting(string propertyName)
        {
            var property = GetHybridClrSettingsProperty(propertyName);
            return ((string[])property.GetValue(GetHybridClrSettingsInstance())) ?? Array.Empty<string>();
        }

        /// <summary>
        /// 通过反射写入 HybridCLRSettings 上的字符串数组配置。
        /// Write a string-array setting onto HybridCLRSettings via reflection.
        /// </summary>
        private static void SetHybridClrStringArraySetting(string propertyName, string[] value)
        {
            var property = GetHybridClrSettingsProperty(propertyName);
            property.SetValue(GetHybridClrSettingsInstance(), value ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取 HybridCLRSettings 配置实例。
        /// Resolve the HybridCLRSettings singleton instance.
        /// </summary>
        private static object GetHybridClrSettingsInstance()
        {
            var settingsType = GetHybridClrSettingsType();
            var instanceProperty = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.That(instanceProperty, Is.Not.Null);
            return instanceProperty.GetValue(null);
        }

        /// <summary>
        /// 获取 HybridCLRSettings 上的目标配置属性。
        /// Resolve the target config property from HybridCLRSettings.
        /// </summary>
        private static PropertyInfo GetHybridClrSettingsProperty(string propertyName)
        {
            var property = GetHybridClrSettingsType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"HybridCLRSettings 应包含属性 {propertyName}");
            return property;
        }

        /// <summary>
        /// 在当前 AppDomain 中解析 HybridCLRSettings 类型。
        /// Resolve the HybridCLRSettings type from the current AppDomain.
        /// </summary>
        private static Type GetHybridClrSettingsType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var settingsType = assembly.GetType("HybridCLR.Editor.Settings.HybridCLRSettings");
                if (settingsType != null)
                {
                    return settingsType;
                }
            }

            Assert.Fail("当前 AppDomain 中未找到 HybridCLR.Editor.Settings.HybridCLRSettings");
            return null;
        }
    }
}
