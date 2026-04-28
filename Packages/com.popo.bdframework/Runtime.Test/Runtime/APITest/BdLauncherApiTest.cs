using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.RuntimeTests.Contracts;
using UnityEngine;

namespace BDFramework.RuntimeTests.ApiTest
{
    /// <summary>
    /// 启动器公开契约的 Runtime 测试主体。
    /// Runtime test body for the launcher public contracts.
    /// 该类型把启动器反射入口、AOT 启动 StreamingAssets 读取顺序、热更程序集装载顺序、缺失 BDebug 补挂策略、默认执行顺序与 E2E 自动检测运行时可达规则固定在 Runtime.Test 的 APITest 层，
    /// 让 Editor 包装、BatchMode 与真机 Talos 套件共享同一套启动器契约断言。
    /// This type fixes launcher reflection entry points, AOT-startup StreamingAssets read order, hotfix-assembly load order, missing-BDebug restoration, default execution order, and the runtime-reachability rule for the E2E auto-detection bridge inside the Runtime.Test APITest layer,
    /// allowing editor wrappers, BatchMode, and packaged Talos suites to share the same launcher contract assertions.
    /// </summary>
    public sealed class BdLauncherApiTest
    {
        private static readonly string[] BApplicationPathStateFieldNames =
        {
            "hasInitializedPathState",
            "<ProjectRoot>k__BackingField",
            "<BDWorkSpace>k__BackingField",
            "<Library>k__BackingField",
            "<Package>k__BackingField",
            "<RuntimeResourceLoadPath>k__BackingField",
            "<EditorResourcePath>k__BackingField",
            "<EditorResourceRuntimePath>k__BackingField",
            "<DevOpsPath>k__BackingField",
            "<DevOpsCodePath>k__BackingField",
            "<DevOpsPublishAssetsPath>k__BackingField",
            "<DevOpsPublishClientPackagePath>k__BackingField",
            "<DevOpsConfigPath>k__BackingField",
            "<DevOpsCIPath>k__BackingField",
            "<BDEditorCachePath>k__BackingField",
            "<persistentDataPath>k__BackingField",
            "<streamingAssetsPath>k__BackingField"
        };

        /// <summary>
        /// 输出统一日志，记录启动器 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the launcher API tests.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(BdLauncherApiTest) : testName,
                "验证启动器反射契约、AOT 启动 StreamingAssets 读取与热更装载顺序规则、缺失 BDebug 补挂策略、默认执行顺序与 E2E 自动检测入口运行时可达规则保持稳定。",
                "通过直接调用 FrameworkContractAssertions 的启动器断言，并校验反射发现、StreamingAssets 初始化顺序、热更程序集装载顺序、缺失 BDebug 补挂、执行顺序与运行时可达规则。"
            );
        }

        /// <summary>
        /// 验证热更脚本加载入口仍然是可发现的静态方法。
        /// Verify that the hotfix script loading entry remains a discoverable static method.
        /// </summary>
        public void FindScriptLoderInitMethod_ShouldResolveStaticMethod()
        {
            FrameworkContractAssertions.VerifyScriptLoaderInitMethodCanBeResolved();
        }

        /// <summary>
        /// 验证 AOT 启动阶段读取 StreamingAssets DLL 列表时，会先初始化索引，并在可选目录缺失时返回空集合。
        /// Verify that AOT startup initializes the index before reading StreamingAssets DLL lists and returns an empty set when an optional directory is missing.
        /// </summary>
        public void GetStreamingAssetFiles_ShouldInitializeIndexAndSkipMissingOptionalDirectory()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTStreamingAssetsReadContract();
        }

        /// <summary>
        /// 验证 AOT 启动阶段装载热更程序集时，会先装载框架与 firstpass，再装载 Assembly-CSharp，并在单个热更文件缺失时继续后续装载。
        /// Verify that AOT startup loads the framework and firstpass assemblies before Assembly-CSharp and continues with remaining assemblies when one hotfix file is missing.
        /// </summary>
        public void LoadHotfixAssemblies_ShouldRespectKnownDependencyOrder()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTHotfixAssemblyLoadOrderContract();
        }

        /// <summary>
        /// 验证基础配置处理器会在缺失时补挂 BDebug 组件。
        /// Verify that the base-config processor restores the BDebug component when it is missing.
        /// </summary>
        public void EnsureDebugComponent_ShouldRestoreMissingBDebug()
        {
            FrameworkContractAssertions.VerifyGameBaseConfigProcessorRestoresMissingBDebugComponent();
        }

        /// <summary>
        /// 验证启动器声明了极小的默认执行顺序。
        /// Verify that the launcher declares the minimum default execution order.
        /// </summary>
        public void BDLauncher_ShouldDeclareMinimumDefaultExecutionOrder()
        {
            FrameworkContractAssertions.VerifyLauncherDefaultExecutionOrder();
        }

        /// <summary>
        /// 验证 AOT 热更预加载同时保留更早钩子与 BeforeSceneLoad 兜底。
        /// Verify that AOT hotfix preloading keeps both the earlier hook and the BeforeSceneLoad fallback.
        /// </summary>
        public void ScriptLoderAOT_ShouldKeepEarlyPreloadHooks()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTEarlyPreloadHooks();
        }

        /// <summary>
        /// 验证 AOT 运行时加载器会跳过已由 Player 预加载的 preserved 热更程序集。
        /// Verify that the AOT runtime loader skips preserved hot-update assemblies already loaded by the player.
        /// </summary>
        public void ScriptLoderAOT_ShouldSkipAssembliesAlreadyLoadedByPlayer()
        {
            FrameworkContractAssertions.VerifyScriptLoderAOTSkipsAssembliesAlreadyLoadedByPlayer();
        }

        /// <summary>
        /// 验证 WindowPreconfig 宿主测试可以反射解析继承的 GameConfigManager.Inst 静态属性。
        /// Verify that the WindowPreconfig host test can resolve the inherited GameConfigManager.Inst static property through reflection.
        /// </summary>
        public void WindowPreconfigHostTest_ShouldResolveInheritedGameConfigManagerInstProperty()
        {
            FrameworkContractAssertions.VerifyWindowPreconfigHostReflectionCanResolveGameConfigManagerInst();
        }

        /// <summary>
        /// 验证 E2E 自动检测入口在 Player 中保持运行时可达。
        /// Verify that the E2E auto-detection entry stays runtime-reachable in player builds.
        /// </summary>
        public void BDLauncher_ShouldOwnDebugTalosStartupBridge()
        {
            FrameworkContractAssertions.VerifyBDLauncherOwnsDebugTalosStartupBridge();
        }

        /// <summary>
        /// 验证 BDLauncher 持有无条件的 IL2CPP 保活引用，确保 Talos.E2E.Runtime 程序集在所有构建配置中都被包含在原生二进制。
        /// Verify that BDLauncher holds an unconditional IL2CPP keep-alive reference, ensuring Talos.E2E.Runtime is included in the native binary across all build configurations.
        /// </summary>
        public void BDLauncher_ShouldPreserveE2EAssemblyForIL2CPP()
        {
            FrameworkContractAssertions.VerifyBDLauncherPreservesE2EAssemblyForIL2CPP();
        }

        /// <summary>
        /// 验证 BApplication 在 loading thread 读取 Unity 路径失败时不会毒化静态类型，并可在后续安全线程重试成功。
        /// Verify that BApplication does not poison its static type when Unity path reads fail on the loading thread and can retry successfully on a later safe thread.
        /// </summary>
        public void BApplication_ShouldDeferUnsafeUnityPathInitializationUntilRetry()
        {
            var tryInitializeMethod = typeof(BApplication).GetMethod(
                "TryInitializePathState",
                BindingFlags.NonPublic | BindingFlags.Static);

            ApiTestAssert.IsNotNull(tryInitializeMethod, "未找到 BApplication.TryInitializePathState 反射入口。");

            var snapshot = CaptureBApplicationPathState();
            try
            {
                ClearBApplicationPathState();

                var warnings = new List<string>();
                var deferred = (bool) tryInitializeMethod.Invoke(
                    null,
                    new object[]
                    {
                        new Func<string>(() => throw new UnityException("get_dataPath can only be called from the main thread.")),
                        new Func<string>(() => "/Sandbox/Persistent"),
                        new Func<string>(() => "/Sandbox/Streaming"),
                        new Action<string>(warnings.Add),
                        "RuntimeTest"
                    });

                ApiTestAssert.IsFalse(deferred, "loading thread 的 Unity 路径异常应被延迟重试逻辑吞掉。");
                ApiTestAssert.AreEqual(1, warnings.Count, "loading thread 失败后应记录一次延迟初始化告警。");
                ApiTestAssert.IsTrue(
                    warnings[0].Contains("等待主线程重试"),
                    "延迟初始化告警应明确说明会等待主线程重试。");
                ApiTestAssert.IsNull(BApplication.ProjectRoot, "延迟初始化失败后不应提前写入 ProjectRoot。");

                warnings.Clear();
                var retried = (bool) tryInitializeMethod.Invoke(
                    null,
                    new object[]
                    {
                        new Func<string>(() => "/Project/Assets"),
                        new Func<string>(() => "/Sandbox/Persistent"),
                        new Func<string>(() => "/Sandbox/Streaming"),
                        new Action<string>(warnings.Add),
                        "RuntimeTestRetry"
                    });

                ApiTestAssert.IsTrue(retried, "安全线程重试应成功完成 BApplication 路径初始化。");
                ApiTestAssert.AreEqual("/Project", BApplication.ProjectRoot, "ProjectRoot 计算结果不匹配。");
                ApiTestAssert.IsTrue(
                    !string.IsNullOrEmpty(BApplication.persistentDataPath),
                    "重试成功后 persistentDataPath 不应为空。");
                ApiTestAssert.IsTrue(
                    !string.IsNullOrEmpty(BApplication.streamingAssetsPath),
                    "重试成功后 streamingAssetsPath 不应为空。");
                ApiTestAssert.AreEqual(0, warnings.Count, "安全线程重试成功后不应继续输出延迟初始化告警。");
            }
            finally
            {
                RestoreBApplicationPathState(snapshot);
            }
        }

        /// <summary>
        /// 快照 BApplication 的静态路径状态，供测试后恢复。
        /// Snapshot BApplication static path state so the test can restore it afterwards.
        /// </summary>
        private static Dictionary<string, object> CaptureBApplicationPathState()
        {
            var snapshot = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var fieldName in BApplicationPathStateFieldNames)
            {
                snapshot[fieldName] = GetRequiredBApplicationField(fieldName).GetValue(null);
            }

            return snapshot;
        }

        /// <summary>
        /// 清空 BApplication 的静态路径状态，模拟首次初始化前的冷启动状态。
        /// Clear BApplication static path state to simulate a cold-start state before first initialization.
        /// </summary>
        private static void ClearBApplicationPathState()
        {
            foreach (var fieldName in BApplicationPathStateFieldNames)
            {
                var field = GetRequiredBApplicationField(fieldName);
                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(null, false);
                    continue;
                }

                field.SetValue(null, null);
            }
        }

        /// <summary>
        /// 恢复 BApplication 的静态路径状态，避免当前测试污染后续运行时用例。
        /// Restore BApplication static path state so this test does not pollute later runtime cases.
        /// </summary>
        private static void RestoreBApplicationPathState(Dictionary<string, object> snapshot)
        {
            foreach (var entry in snapshot)
            {
                GetRequiredBApplicationField(entry.Key).SetValue(null, entry.Value);
            }
        }

        /// <summary>
        /// 获取必须存在的 BApplication 静态字段。
        /// Get a required BApplication static field.
        /// </summary>
        private static FieldInfo GetRequiredBApplicationField(string fieldName)
        {
            var field = typeof(BApplication).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            ApiTestAssert.IsNotNull(field, $"未找到 BApplication 字段: {fieldName}");
            return field;
        }

    }
}
