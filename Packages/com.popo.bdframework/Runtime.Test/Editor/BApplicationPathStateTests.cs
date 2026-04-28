using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.RuntimeTests.ApiTest;
using NUnit.Framework;
using UnityEngine;

namespace Runtime.Test.Editor
{
    /// <summary>
    /// 覆盖 BApplication 路径延迟初始化契约的编辑器测试。
    /// Editor tests that cover the deferred BApplication path-initialization contract.
    /// 这些断言只验证 loading thread 首次失败不毒化静态类型、后续安全线程可重试成功，
    /// 并明确把验证逻辑留在 EditorTest 程序集，避免 Android Player 包体因测试辅助代码继续膨胀。
    /// These assertions only verify that a first loading-thread failure does not poison the static type and that a later safe-thread retry succeeds,
    /// while deliberately keeping the verification logic inside the EditorTest assembly so Android player packages do not keep growing from test helpers.
    /// </summary>
    public class BApplicationPathStateTests
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
        /// 在每个测试开始时输出统一的测试目的与实现手段日志。
        /// Emit a unified purpose-and-means log at the start of each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            ApiTestLog.LogTestPurposeAndMeans(
                TestContext.CurrentContext.Test.Name,
                "验证 BApplication 在 loading thread 首次读取 Unity 路径失败时不会毒化静态类型，并且后续安全线程可以完成路径重试初始化。",
                "通过反射调用 BApplication 的私有延迟初始化入口，模拟 loading thread 异常与安全线程重试，并快照/恢复静态字段以避免污染其他 Editor NUnit 用例。"
            );
        }

        /// <summary>
        /// 验证 loading thread 首次失败会延迟到安全线程重试，而不是把静态类型永久标记为失败。
        /// Verify that a first loading-thread failure is deferred to a safe-thread retry instead of permanently poisoning the static type.
        /// </summary>
        [Test]
        public void DeferUnsafeUnityPathInitializationUntilRetry()
        {
            var tryInitializeMethod = typeof(BApplication).GetMethod(
                "TryInitializePathState",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(tryInitializeMethod, Is.Not.Null, "应能找到 BApplication.TryInitializePathState 私有静态方法。");

            var snapshot = CaptureBApplicationPathState();
            try
            {
                ClearBApplicationPathState();

                var warnings = new List<string>();
                var deferred = (bool) tryInitializeMethod!.Invoke(
                    null,
                    new object[]
                    {
                        new Func<string>(() => throw new UnityException("get_dataPath can only be called from the main thread.")),
                        new Func<string>(() => "/Sandbox/Persistent"),
                        new Func<string>(() => "/Sandbox/Streaming"),
                        new Action<string>(warnings.Add),
                        nameof(DeferUnsafeUnityPathInitializationUntilRetry)
                    });

                Assert.That(deferred, Is.False, "loading thread 的 Unity 路径异常应被延迟重试逻辑吞掉。");
                Assert.That(warnings, Has.Count.EqualTo(1), "loading thread 失败后应记录一次延迟初始化告警。");
                Assert.That(warnings[0], Does.Contain("等待主线程重试"), "延迟初始化告警应明确说明会等待主线程重试。");
                Assert.That(BApplication.ProjectRoot, Is.Null, "延迟初始化失败后不应提前写入 ProjectRoot。");

                warnings.Clear();
                var retried = (bool) tryInitializeMethod.Invoke(
                    null,
                    new object[]
                    {
                        new Func<string>(() => "/Project/Assets"),
                        new Func<string>(() => "/Sandbox/Persistent"),
                        new Func<string>(() => "/Sandbox/Streaming"),
                        new Action<string>(warnings.Add),
                        "EditorRetry"
                    });

                Assert.That(retried, Is.True, "安全线程重试应成功完成 BApplication 路径初始化。");
                Assert.That(BApplication.ProjectRoot, Is.EqualTo("/Project"), "ProjectRoot 计算结果不匹配。");
                Assert.That(BApplication.persistentDataPath, Is.Not.Empty, "重试成功后 persistentDataPath 不应为空。");
                Assert.That(BApplication.streamingAssetsPath, Is.Not.Empty, "重试成功后 streamingAssetsPath 不应为空。");
                Assert.That(warnings, Is.Empty, "安全线程重试成功后不应继续输出延迟初始化告警。");
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
        /// 恢复 BApplication 的静态路径状态，避免当前测试污染后续 Editor NUnit 用例。
        /// Restore BApplication static path state so this test does not pollute later Editor NUnit cases.
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
            Assert.That(field, Is.Not.Null, $"应能找到 BApplication 字段: {fieldName}");
            return field!;
        }
    }
}