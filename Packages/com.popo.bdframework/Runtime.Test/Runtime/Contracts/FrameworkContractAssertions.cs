using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using UnityEngine;
using Object = UnityEngine.Object;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace BDFramework.RuntimeTests.Contracts
{
    /// <summary>
    /// 框架可打包契约断言集合。
    /// Packaged framework contract assertion collection.
    /// 该类型集中承载不依赖 UnityEditor 的启动器、配置与资源契约校验，
    /// 让 Editor NUnit 包装与 Runtime Talos E2E 套件复用同一套断言实现，避免同一条契约在两处漂移。
    /// This type centralizes launcher, configuration, and resource contract checks that do not depend on UnityEditor,
    /// allowing the editor NUnit wrappers and runtime Talos E2E suites to reuse the same assertion implementation so the same contract does not drift in two places.
    /// </summary>
    public static class FrameworkContractAssertions
    {
        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// Emit a unified test-start log that always includes the purpose and means.
        /// </summary>
        /// <param name="testName">测试名称。</param>
        /// <param name="testName">The test name.</param>
        /// <param name="purpose">测试目的。</param>
        /// <param name="purpose">The test purpose.</param>
        /// <param name="means">实现手段。</param>
        /// <param name="means">The verification means.</param>
        public static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 验证热更脚本初始化入口仍然可以通过静态反射发现。
        /// Verify that the hotfix script initialization entry can still be discovered through static reflection.
        /// </summary>
        public static void VerifyScriptLoaderInitMethodCanBeResolved()
        {
            MethodInfo method = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("BDFramework.ScriptLoder");
                if (type == null)
                {
                    continue;
                }

                method = type.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    break;
                }
            }

            EnsureTrue(method != null, "应该能够找到 BDFramework.ScriptLoder 的静态 Init 方法。");
            EnsureTrue(method.IsStatic, "Init 入口必须保持静态，才能被启动器按静态方式调用。");
            EnsureTrue(method.GetParameters().Length == 0, "Init 入口应保持无参，避免启动阶段额外参数耦合。");
        }

        /// <summary>
        /// 验证启动器声明了极小默认执行顺序。
        /// Verify that the launcher declares the minimum default execution order.
        /// </summary>
        public static void VerifyLauncherDefaultExecutionOrder()
        {
            var attribute = (DefaultExecutionOrder)Attribute.GetCustomAttribute(
                typeof(BDFramework.BDLauncher),
                typeof(DefaultExecutionOrder));

            EnsureTrue(attribute != null, "BDLauncher 应声明 DefaultExecutionOrder，避免启动顺序依赖场景对象创建偶然性。");
            EnsureEqual(int.MinValue, attribute.order, "BDLauncher 应使用 int.MinValue 作为默认执行顺序，尽量早于其他普通脚本。");
        }

        /// <summary>
        /// 验证 AOT 热更预加载同时保留更早的程序集阶段钩子和 BeforeSceneLoad 兜底。
        /// Verify that AOT hotfix preloading keeps both the earlier assembly-stage hook and the BeforeSceneLoad fallback.
        /// 该契约用来固定这次 missing-script 回归的修复方向：
        /// 必须先争取在首场景反序列化前完成预加载，同时保留旧的 BeforeSceneLoad 路径作为兜底与兼容重试。
        /// This contract locks in the fix direction for the current missing-script regression:
        /// preload must try to complete before first-scene deserialization while still preserving the older BeforeSceneLoad path as a fallback and compatibility retry.
        /// </summary>
        public static void VerifyScriptLoderAOTEarlyPreloadHooks()
        {
            var launcherAfterAssembliesLoadedMethod = typeof(BDFramework.BDLauncher).GetMethod(
                "PreLoadHotfixAssembliesAfterAssembliesLoadedFromLauncher",
                BindingFlags.NonPublic | BindingFlags.Static);
            var launcherBeforeSceneLoadMethod = typeof(BDFramework.BDLauncher).GetMethod(
                "PreLoadHotfixAssembliesBeforeSceneLoadFromLauncher",
                BindingFlags.NonPublic | BindingFlags.Static);
            var afterAssembliesLoadedMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "PreLoadHotfixAssembliesAfterAssembliesLoaded",
                BindingFlags.NonPublic | BindingFlags.Static);
            var beforeSceneLoadMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "PreLoadHotfixAssembliesBeforeSceneLoad",
                BindingFlags.NonPublic | BindingFlags.Static);
            var sharedHelperMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "TryPreLoadHotfixAssembliesAtRuntime",
                BindingFlags.NonPublic | BindingFlags.Static);

            EnsureTrue(afterAssembliesLoadedMethod != null, "应该保留 AfterAssembliesLoaded 预加载入口，避免首场景热更组件在反序列化前变成 missing script。");
            EnsureTrue(beforeSceneLoadMethod != null, "应该保留 BeforeSceneLoad 预加载兜底入口，避免较晚启动路径丢失热更程序集重试。");
            EnsureTrue(sharedHelperMethod != null, "应该保留共享的热更预加载辅助方法，避免两个启动钩子的装载逻辑漂移。");
            EnsureTrue(launcherAfterAssembliesLoadedMethod != null, "BDLauncher 应保留 AfterAssembliesLoaded 代理入口，确保玩家构建能稳定保留并执行热更预加载钩子。");
            EnsureTrue(launcherBeforeSceneLoadMethod != null, "BDLauncher 应保留 BeforeSceneLoad 代理入口，确保玩家构建里仍有兜底预加载链路。");

            var afterAssembliesLoadedAttribute = (RuntimeInitializeOnLoadMethodAttribute)Attribute.GetCustomAttribute(
                afterAssembliesLoadedMethod,
                typeof(RuntimeInitializeOnLoadMethodAttribute));
            var beforeSceneLoadAttribute = (RuntimeInitializeOnLoadMethodAttribute)Attribute.GetCustomAttribute(
                beforeSceneLoadMethod,
                typeof(RuntimeInitializeOnLoadMethodAttribute));
            var launcherAfterAssembliesLoadedAttribute = (RuntimeInitializeOnLoadMethodAttribute)Attribute.GetCustomAttribute(
                launcherAfterAssembliesLoadedMethod,
                typeof(RuntimeInitializeOnLoadMethodAttribute));
            var launcherBeforeSceneLoadAttribute = (RuntimeInitializeOnLoadMethodAttribute)Attribute.GetCustomAttribute(
                launcherBeforeSceneLoadMethod,
                typeof(RuntimeInitializeOnLoadMethodAttribute));

            EnsureTrue(afterAssembliesLoadedAttribute != null, "AfterAssembliesLoaded 预加载入口必须声明 RuntimeInitializeOnLoadMethodAttribute。");
            EnsureEqual(
                RuntimeInitializeLoadType.AfterAssembliesLoaded,
                afterAssembliesLoadedAttribute.loadType,
                "热更预加载应优先挂在 AfterAssembliesLoaded，确保早于首场景组件恢复。");

            EnsureTrue(beforeSceneLoadAttribute != null, "BeforeSceneLoad 兜底入口必须声明 RuntimeInitializeOnLoadMethodAttribute。");
            EnsureEqual(
                RuntimeInitializeLoadType.BeforeSceneLoad,
                beforeSceneLoadAttribute.loadType,
                "热更预加载兜底入口应保持 BeforeSceneLoad，兼容现有启动链后备重试。");

            EnsureTrue(launcherAfterAssembliesLoadedAttribute != null, "BDLauncher 的 AfterAssembliesLoaded 代理入口必须声明 RuntimeInitializeOnLoadMethodAttribute。");
            EnsureEqual(
                RuntimeInitializeLoadType.AfterAssembliesLoaded,
                launcherAfterAssembliesLoadedAttribute.loadType,
                "BDLauncher 的热更预加载代理入口应保持 AfterAssembliesLoaded。");

            EnsureTrue(launcherBeforeSceneLoadAttribute != null, "BDLauncher 的 BeforeSceneLoad 代理入口必须声明 RuntimeInitializeOnLoadMethodAttribute。");
            EnsureEqual(
                RuntimeInitializeLoadType.BeforeSceneLoad,
                launcherBeforeSceneLoadAttribute.loadType,
                "BDLauncher 的热更预加载兜底代理入口应保持 BeforeSceneLoad。");
        }

        /// <summary>
        /// 验证 WindowPreconfig 宿主测试使用的 GameConfigManager.Inst 反射契约可以跨基类解析。
        /// Verify that the GameConfigManager.Inst reflection contract used by the WindowPreconfig host test resolves across the base type.
        /// 宿主测试通过反射访问 `GameConfigManager.Inst`，但该静态属性定义在泛型基类 `ManagerBase<T, V>` 上，
        /// 因此这里要固定 `FlattenHierarchy` 的反射契约，避免把继承静态属性误判成运行时缺失。
        /// The host test accesses `GameConfigManager.Inst` through reflection, but the static property is declared on the generic base type `ManagerBase<T, V>`,
        /// so this assertion locks in the `FlattenHierarchy` reflection contract and avoids misclassifying inherited static properties as runtime regressions.
        /// </summary>
        public static void VerifyWindowPreconfigHostReflectionCanResolveGameConfigManagerInst()
        {
            var instProperty = typeof(BDFramework.Configure.GameConfigManager).GetProperty(
                "Inst",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                var currentType = typeof(BDFramework.Configure.GameConfigManager);
                while (currentType != null && instProperty == null)
                {
                    instProperty = currentType
                        .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .FirstOrDefault(property => string.Equals(property.Name, "Inst", StringComparison.Ordinal));
                    currentType = currentType.BaseType;
                }
            }

            EnsureTrue(instProperty != null, "GameConfigManager.Inst 应可通过继承层级上的静态属性反射解析到。");
            EnsureTrue(instProperty.PropertyType == typeof(BDFramework.Configure.GameConfigManager), "GameConfigManager.Inst 反射返回的属性类型应保持为 GameConfigManager。");
        }

        /// <summary>
        /// 验证 E2E 自动检测入口在 Player 中保持运行时可达。
        /// Verify that the E2E auto-detection entry stays runtime-reachable in player builds.
        /// </summary>
        public static void VerifyBDLauncherOwnsDebugTalosStartupBridge()
        {
            var method = typeof(BDFramework.BDLauncher).GetMethod(
                "TryLaunchTalosE2EInDebugBuild",
                BindingFlags.NonPublic | BindingFlags.Instance);

            EnsureTrue(method != null, "应该能够找到 BDLauncher.TryLaunchTalosE2EInDebugBuild 私有实例方法。");
            EnsureTrue(
                method.GetCustomAttributes(typeof(ConditionalAttribute), false).Any(attribute => ((ConditionalAttribute)attribute).ConditionString == "DEBUG"),
                "BDLauncher 的 Talos 启动桥接必须只在 DEBUG 宏生效时参与编译。");
        }

        /// <summary>
        /// 验证 E2EAutoInit 具有 [RuntimeInitializeOnLoadMethod] 保活入口，确保 IL2CPP 构建中类型不会被裁剪。
        /// Verify that E2EAutoInit has a [RuntimeInitializeOnLoadMethod] keep-alive entrypoint so IL2CPP builds do not strip the type.
        /// 在 IL2CPP 构建中，Assembly.Load() 无法工作（没有托管 DLL），
        /// link.xml 只防止元数据裁剪但不保证代码被编译到原生二进制。
        /// [RuntimeInitializeOnLoadMethod] 让 Unity 引擎直接引用本类型，强制 IL2CPP 保留整个类。
        /// In IL2CPP builds, Assembly.Load() does not work (no managed DLLs),
        /// link.xml only prevents metadata stripping but does not guarantee code is compiled into the native binary.
        /// [RuntimeInitializeOnLoadMethod] causes Unity's engine to directly reference this type, forcing IL2CPP to preserve the entire class.
        /// </summary>
        public static void VerifyE2EAutoInitHasRuntimeInitializeKeepAliveForIL2CPP()
        {
            var keepAliveMethod = typeof(Talos.E2E.E2EAutoInit).GetMethod(
                "EnsureTypePreservedInIL2CPP",
                BindingFlags.NonPublic | BindingFlags.Static);

            EnsureTrue(keepAliveMethod != null, "应该能够找到 E2EAutoInit.EnsureTypePreservedInIL2CPP 保活方法。");
            EnsureTrue(
                keepAliveMethod.GetCustomAttributes(typeof(UnityEngine.RuntimeInitializeOnLoadMethodAttribute), false).Any(),
                "E2EAutoInit.EnsureTypePreservedInIL2CPP 必须带有 [RuntimeInitializeOnLoadMethod] 属性，确保 IL2CPP 构建中类型不被裁剪。");
            EnsureTrue(
                keepAliveMethod.GetCustomAttributes(typeof(UnityEngine.Scripting.PreserveAttribute), false).Any(),
                "E2EAutoInit.EnsureTypePreservedInIL2CPP 必须带有 [Preserve] 属性，防止 IL2CPP 链接器裁剪本方法。");
        }

        /// <summary>
        /// 验证 AOT 运行时加载器会跳过已由 Player 预加载的 preserved 热更程序集。
        /// Verify that the AOT runtime loader skips preserved hot-update assemblies already loaded by the player.
        /// 这覆盖“程序集既保留在首包里，又仍然存在 `.zlua.bytes` 更新产物”时的重复装载风险，
        /// 避免同名程序集被二次 `Assembly.Load(byte[])` 后在 AppDomain 中出现重复副本。
        /// This covers the duplicate-load risk when an assembly is both preserved in the base player and still published as a `.zlua.bytes` update payload,
        /// preventing a second `Assembly.Load(byte[])` from creating duplicate assembly copies inside the AppDomain.
        /// </summary>
        public static void VerifyScriptLoderAOTSkipsAssembliesAlreadyLoadedByPlayer()
        {
            var helperMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "ShouldSkipAlreadyLoadedHotfixAssembly",
                BindingFlags.NonPublic | BindingFlags.Static);

            EnsureTrue(helperMethod != null, "应该能够找到 ScriptLoderAOT 的重复装载跳过辅助方法。");

            var shouldSkipPreservedAssembly = (bool)helperMethod.Invoke(
                null,
                new object[]
                {
                    "android/script/hotfix/BDFramework.Core.dll.bytes",
                    new[] { typeof(BDFramework.ScriptLoder).Assembly }
                });
            var shouldSkipUnknownAssembly = (bool)helperMethod.Invoke(
                null,
                new object[]
                {
                    "android/script/hotfix/BDFramework.Test.dll.bytes",
                    new[] { typeof(BDFramework.ScriptLoder).Assembly }
                });

            EnsureTrue(shouldSkipPreservedAssembly, "已由 Player 预加载的程序集应跳过重复 Assembly.Load。");
            EnsureTrue(!shouldSkipUnknownAssembly, "未预加载的热更程序集不应被误判为已加载。");
        }

        /// <summary>
        /// 验证 AOT 启动阶段读取 StreamingAssets 时，会先初始化索引，并在可选目录缺失时回退为空集合。
        /// Verify that AOT startup reads initialize the StreamingAssets index first and fall back to an empty set when an optional directory is missing.
        /// </summary>
        public static void VerifyScriptLoderAOTStreamingAssetsReadContract()
        {
            var helperMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "GetStreamingAssetFiles",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(Action),
                    typeof(Func<string, bool>),
                    typeof(Func<string, string, string[]>)
                },
                null);

            EnsureTrue(helperMethod != null, "应该能够找到 ScriptLoderAOT 的 StreamingAssets 读取辅助方法。");

            var missingDirectoryCallOrder = new List<string>();
            var missingDirectoryResult = (string[])helperMethod.Invoke(
                null,
                new object[]
                {
                    "android/script/aot_patch",
                    "*.zlua.bytes",
                    (Action)(() => missingDirectoryCallOrder.Add("initialize")),
                    (Func<string, bool>)(path =>
                    {
                        missingDirectoryCallOrder.Add($"directory:{path}");
                        return false;
                    }),
                    (Func<string, string, string[]>)((path, pattern) =>
                    {
                        missingDirectoryCallOrder.Add($"files:{path}:{pattern}");
                        return new[] { "unexpected" };
                    })
                });

            EnsureSequenceEqual(
                new[] { "initialize", "directory:android/script/aot_patch" },
                missingDirectoryCallOrder,
                "缺失可选 StreamingAssets 目录时，应先初始化索引，再只探测目录而不继续枚举文件。");
            EnsureTrue(
                missingDirectoryResult != null && missingDirectoryResult.Length == 0,
                "缺失可选 StreamingAssets 目录时，应返回空集合而不是抛出异常或返回 null。");

            var existingDirectoryCallOrder = new List<string>();
            var expectedFiles = new[] { "android/script/hotfix/main.zlua.bytes" };
            var existingDirectoryResult = (string[])helperMethod.Invoke(
                null,
                new object[]
                {
                    "android/script/hotfix",
                    "*.zlua.bytes",
                    (Action)(() => existingDirectoryCallOrder.Add("initialize")),
                    (Func<string, bool>)(path =>
                    {
                        existingDirectoryCallOrder.Add($"directory:{path}");
                        return true;
                    }),
                    (Func<string, string, string[]>)((path, pattern) =>
                    {
                        existingDirectoryCallOrder.Add($"files:{path}:{pattern}");
                        return expectedFiles;
                    })
                });

            EnsureSequenceEqual(
                new[]
                {
                    "initialize",
                    "directory:android/script/hotfix",
                    "files:android/script/hotfix:*.zlua.bytes"
                },
                existingDirectoryCallOrder,
                "存在 StreamingAssets 目录时，应先初始化索引，再探测目录，最后枚举文件。");
            EnsureSequenceEqual(
                expectedFiles,
                existingDirectoryResult,
                "存在 StreamingAssets 目录时，应返回底层文件枚举结果。");
        }

        /// <summary>
        /// 验证 AOT 启动阶段会先装载框架与 firstpass 热更程序集，再装载 Assembly-CSharp，并在单个热更文件缺失时跳过告警继续后续装载。
        /// Verify that AOT startup loads the framework and firstpass hotfix assemblies before Assembly-CSharp and skips forward with a warning when a single hotfix file is missing.
        /// </summary>
        public static void VerifyScriptLoderAOTHotfixAssemblyLoadOrderContract()
        {
            var helperMethod = typeof(BDFramework.ScriptLoderAOT).GetMethod(
                "LoadHotfixAssemblies",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(string[]),
                    typeof(Func<string, byte[]>),
                    typeof(Action<string, byte[]>)
                },
                null);

            EnsureTrue(helperMethod != null, "应该能够找到 ScriptLoderAOT 的热更程序集装载辅助方法。");

            var loadOrder = new List<string>();
            helperMethod.Invoke(
                null,
                new object[]
                {
                    new[]
                    {
                        "android/script/hotfix/Assembly-CSharp.zlua.bytes",
                        "android/script/hotfix/BDFramework.Core.zlua.bytes",
                        "android/script/hotfix/Assembly-CSharp-firstpass.zlua.bytes",
                        "android/script/hotfix/Game.Hotfix.zlua.bytes"
                    },
                    (Func<string, byte[]>)(_ => new byte[] { 1 }),
                    (Action<string, byte[]>)((path, _) =>
                    {
                        loadOrder.Add(
                            Path.GetFileName(path).Replace(".zlua.bytes", string.Empty, StringComparison.OrdinalIgnoreCase));
                    })
                });

            EnsureSequenceEqual(
                new[] { "BDFramework.Core", "Assembly-CSharp-firstpass", "Assembly-CSharp", "Game.Hotfix" },
                loadOrder,
                "热更程序集应按稳定依赖顺序装载，避免 Assembly-CSharp 早于其依赖被装载。"
            );

            var loadOrderWithMissingFirstpass = new List<string>();
            helperMethod.Invoke(
                null,
                new object[]
                {
                    new[]
                    {
                        "android/script/hotfix/Assembly-CSharp.zlua.bytes",
                        "android/script/hotfix/BDFramework.Core.zlua.bytes",
                        "android/script/hotfix/Assembly-CSharp-firstpass.zlua.bytes",
                        "android/script/hotfix/Game.Hotfix.zlua.bytes"
                    },
                    (Func<string, byte[]>)(path =>
                    {
                        if (path.EndsWith("Assembly-CSharp-firstpass.zlua.bytes", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new DirectoryNotFoundException(path);
                        }

                        return new byte[] { 1 };
                    }),
                    (Action<string, byte[]>)((path, _) =>
                    {
                        loadOrderWithMissingFirstpass.Add(
                            Path.GetFileName(path).Replace(".zlua.bytes", string.Empty, StringComparison.OrdinalIgnoreCase));
                    })
                });

            EnsureSequenceEqual(
                new[] { "BDFramework.Core", "Assembly-CSharp", "Game.Hotfix" },
                loadOrderWithMissingFirstpass,
                "单个热更文件缺失时，应跳过该文件并继续后续程序集装载，避免把整条启动链直接打断。"
            );
        }

        /// <summary>
        /// 验证基础配置处理器会在缺失时补挂 BDebug 组件，避免启动场景里的 placeholder 缺脚本把配置装载链直接打断。
        /// Verify that the base-config processor restores the BDebug component when it is missing so placeholder startup-scene scripts do not break the configuration load chain.
        /// </summary>
        public static void VerifyGameBaseConfigProcessorRestoresMissingBDebugComponent()
        {
            var owner = new GameObject("BDebugOwner");
            try
            {
                var createdComponent = GameBaseConfigProcessor.EnsureDebugComponent(owner);
                EnsureTrue(createdComponent != null, "缺失 BDebug 组件时，应补挂一个新的运行时实例。");
                EnsureTrue(owner.GetComponent<BDebug>() == createdComponent, "补挂后的 BDebug 组件应驻留在传入的启动器物体上。");

                createdComponent.IsLog = false;
                var reusedComponent = GameBaseConfigProcessor.EnsureDebugComponent(owner);
                EnsureTrue(reusedComponent == createdComponent, "已有 BDebug 组件时，应直接复用现有实例而不是重复补挂。");
                EnsureTrue(!reusedComponent.IsLog, "复用已有 BDebug 组件时，不应意外重置其运行时状态。");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        /// <summary>
        /// 验证运行态 launcher 文本优先级最高。
        /// Verify that runtime launcher text keeps the highest priority.
        /// </summary>
        public static void VerifyRuntimeLauncherConfigTextPreferredWhenPlaying()
        {
            var plan = GameConfigStartupPureLogic.ResolveFrameworkConfigTextSource(
                isPlaying: true,
                hasRuntimeLauncherConfigText: true,
                runtimeLauncherConfigName: "RuntimeConfig.bytes",
                hasSceneLauncherConfigText: true,
                sceneLauncherConfigName: "SceneConfig.bytes",
                isEditor: true,
                defaultEditorConfigExists: true,
                defaultEditorConfigPath: "Assets/Scenes/Config/editor.bytes");

            EnsureEqual(
                GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.RuntimeLauncherTextAsset,
                plan.SourceKind,
                "运行态 launcher 文本优先级不匹配。");
            EnsureEqual("RuntimeConfig.bytes", plan.SourceIdentifier, "运行态 launcher 来源标识不匹配。");
            EnsureTrue(plan.ShouldLogSource, "运行态 launcher 来源应输出统一日志。");
        }

        /// <summary>
        /// 验证运行态来源缺失时会回退到场景 launcher 文本。
        /// Verify that the logic falls back to the scene launcher text when the runtime source is unavailable.
        /// </summary>
        public static void VerifySceneLauncherFallback()
        {
            var plan = GameConfigStartupPureLogic.ResolveFrameworkConfigTextSource(
                isPlaying: true,
                hasRuntimeLauncherConfigText: false,
                runtimeLauncherConfigName: string.Empty,
                hasSceneLauncherConfigText: true,
                sceneLauncherConfigName: "SceneConfig.bytes",
                isEditor: true,
                defaultEditorConfigExists: true,
                defaultEditorConfigPath: "Assets/Scenes/Config/editor.bytes");

            EnsureEqual(
                GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.SceneLauncherTextAsset,
                plan.SourceKind,
                "场景 launcher 回退来源不匹配。");
            EnsureEqual("SceneConfig.bytes", plan.SourceIdentifier, "场景 launcher 来源标识不匹配。");
            EnsureTrue(plan.ShouldLogSource, "场景 launcher 来源应输出统一日志。");
        }

        /// <summary>
        /// 验证所有 launcher 来源缺失时会回退到编辑器默认文件。
        /// Verify that the logic falls back to the editor default file when all launcher sources are missing.
        /// </summary>
        public static void VerifyEditorDefaultFileFallback()
        {
            var plan = GameConfigStartupPureLogic.ResolveFrameworkConfigTextSource(
                isPlaying: false,
                hasRuntimeLauncherConfigText: false,
                runtimeLauncherConfigName: string.Empty,
                hasSceneLauncherConfigText: false,
                sceneLauncherConfigName: string.Empty,
                isEditor: true,
                defaultEditorConfigExists: true,
                defaultEditorConfigPath: "Assets/Scenes/Config/editor.bytes");

            EnsureEqual(
                GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.EditorDefaultFile,
                plan.SourceKind,
                "编辑器默认文件回退来源不匹配。");
            EnsureEqual(
                "Assets/Scenes/Config/editor.bytes",
                plan.SourceIdentifier,
                "编辑器默认文件来源标识不匹配。");
            EnsureTrue(plan.ShouldLogSource, "编辑器默认文件来源应输出统一日志。");
        }

        /// <summary>
        /// 验证没有任何来源时会返回空决策。
        /// Verify that the logic returns an empty decision when no source exists.
        /// </summary>
        public static void VerifyNoConfigSourceReturnsNone()
        {
            var plan = GameConfigStartupPureLogic.ResolveFrameworkConfigTextSource(
                isPlaying: false,
                hasRuntimeLauncherConfigText: false,
                runtimeLauncherConfigName: string.Empty,
                hasSceneLauncherConfigText: false,
                sceneLauncherConfigName: string.Empty,
                isEditor: true,
                defaultEditorConfigExists: false,
                defaultEditorConfigPath: "Assets/Scenes/Config/editor.bytes");

            EnsureEqual(
                GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.None,
                plan.SourceKind,
                "空来源分支应返回 None。");
            EnsureEqual(string.Empty, plan.SourceIdentifier, "空来源分支不应携带来源标识。");
            EnsureTrue(!plan.ShouldLogSource, "空来源分支不应输出来源日志。");
        }

        /// <summary>
        /// 验证配置来源日志格式在空来源时会回退到占位符。
        /// Verify that the configuration-source log message falls back to a placeholder when the source is empty.
        /// </summary>
        public static void VerifyFormatFrameworkConfigSourceLogMessageFallback()
        {
            EnsureEqual(
                "GameConfig加载配置:RuntimeConfig.bytes",
                GameConfigStartupPureLogic.FormatFrameworkConfigSourceLogMessage("RuntimeConfig.bytes"),
                "配置来源日志文本不匹配。");
            EnsureEqual(
                "GameConfig加载配置:-",
                GameConfigStartupPureLogic.FormatFrameworkConfigSourceLogMessage("   "),
                "空配置来源日志文本应回退到占位符。");
        }

        /// <summary>
        /// 验证配置管理器存在与否会稳定映射到装载判断结果。
        /// Verify that manager presence is mapped deterministically to the loading decision.
        /// </summary>
        public static void VerifyShouldLoadFrameworkConfigManagerMatchesManagerPresence()
        {
            EnsureTrue(
                !GameConfigStartupPureLogic.ShouldLoadFrameworkConfigManager(false),
                "缺少配置管理器实例时不应触发正式配置装载。");
            EnsureTrue(
                GameConfigStartupPureLogic.ShouldLoadFrameworkConfigManager(true),
                "存在配置管理器实例时应允许触发正式配置装载。");
        }

        /// <summary>
        /// 验证服务器版控文件路径会稳定拼接平台目录和固定文件名。
        /// Verify that the server version-info path consistently appends the platform directory and fixed file name.
        /// </summary>
        public static void VerifyServerAssetsVersionInfoPathAppendsPlatformDirectoryAndFileName()
        {
            var rootPath = Path.Combine("Root", "Server");
            var expected = Path.Combine(
                rootPath,
                BApplication.GetPlatformLoadPath(RuntimePlatform.Android),
                BResources.SERVER_ASSETS_VERSION_INFO_PATH);

            EnsureEqual(
                expected,
                BResources.GetServerAssetsVersionInfoPath(rootPath, RuntimePlatform.Android),
                "服务器资源版控路径拼接结果不匹配。");
        }

        /// <summary>
        /// 验证资源信息路径的两个重载会分别走根目录和平台目录规则。
        /// Verify that the two resource-info path overloads follow the root-only and platform-directory rules respectively.
        /// </summary>
        public static void VerifyAssetsInfoPathOverloadsUseExpectedRules()
        {
            var rootPath = Path.Combine("Root", "Client");
            EnsureEqual(
                Path.Combine(rootPath, BResources.ASSETS_INFO_PATH),
                BResources.GetAssetsInfoPath(rootPath),
                "根目录资源信息路径不匹配。");
            EnsureEqual(
                Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.WindowsPlayer), BResources.ASSETS_INFO_PATH),
                BResources.GetAssetsInfoPath(rootPath, RuntimePlatform.WindowsPlayer),
                "平台资源信息路径不匹配。");
        }

        /// <summary>
        /// 验证旧版分包命名会直接按原文件名拼接。
        /// Verify that legacy sub-package names are appended directly without reformatting.
        /// </summary>
        public static void VerifyLegacySubPackagePathPreserved()
        {
            var rootPath = Path.Combine("Root", "Client");
            const string legacyName = "ServerAssetsSubPackage_demo.info";
            var expected = Path.Combine(rootPath, BApplication.GetPlatformLoadPath(RuntimePlatform.Android), legacyName);

            EnsureEqual(
                expected,
                BResources.GetAssetsSubPackageInfoPath(rootPath, RuntimePlatform.Android, legacyName),
                "旧版分包路径应保持原始文件名。");
        }

        /// <summary>
        /// 验证新版分包名会被格式化为既定规则。
        /// Verify that modern sub-package names are formatted into the expected rule.
        /// </summary>
        public static void VerifyModernSubPackagePathFormatted()
        {
            var rootPath = Path.Combine("Root", "Client");
            var expected = Path.Combine(
                rootPath,
                BApplication.GetPlatformLoadPath(RuntimePlatform.IPhonePlayer),
                string.Format(BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH, "demo"));

            EnsureEqual(
                expected,
                BResources.GetAssetsSubPackageInfoPath(rootPath, RuntimePlatform.IPhonePlayer, "demo"),
                "新版分包路径格式化结果不匹配。");
        }

        /// <summary>
        /// 验证资源组缓存会保留写入顺序，并且清理后读取为空。
        /// Verify that the asset-group cache preserves insertion order and returns empty after cleanup.
        /// </summary>
        public static void VerifyAssetGroupStoresOrderAndClearRemovesEntries()
        {
            var groupName = $"framework-contract-{Guid.NewGuid():N}";
            try
            {
                BResources.AddAssetsPathToGroup(groupName, "a.prefab", "b.mat");
                BResources.AddAssetsPathToGroup(groupName, "c.png");

                EnsureSequenceEqual(
                    new[] { "a.prefab", "b.mat", "c.png" },
                    BResources.GetAssetsPathByGroup(groupName),
                    "资源组缓存顺序不匹配。");
            }
            finally
            {
                BResources.ClearAssetGroup(groupName);
            }

            EnsureTrue(BResources.GetAssetsPathByGroup(groupName).Length == 0, "资源组清理后应返回空集合。");
        }

        /// <summary>
        /// 验证空资源列表异步加载会直接回调空结果，并且不要求事先初始化 ResLoader。
        /// Verify that async loading with an empty asset list immediately returns an empty result and does not require ResLoader initialization.
        /// </summary>
        public static void VerifyAsyncLoadWithEmptyListReturnsEmptyAndInvokesCallbackWithoutLoader()
        {
            IDictionary<string, Object> callbackResult = null;
            var callbackInvoked = false;

            var ids = BResources.AsyncLoad(new List<string>(), onLoadEnd: result =>
            {
                callbackInvoked = true;
                callbackResult = result;
            });

            EnsureTrue(ids.Count == 0, "空资源列表异步加载应直接返回空任务列表。");
            EnsureTrue(callbackInvoked, "空资源列表异步加载应立即触发完成回调。");
            EnsureTrue(callbackResult != null, "空资源列表异步加载应返回空字典而非 null。");
            EnsureEqual(0, callbackResult.Count, "空资源列表异步加载回调结果数量应为 0。");
        }

        /// <summary>
        /// 验证 ShaderLoder 在预热前的空缓存状态下执行查找不会抛出空引用。
        /// Verify that ShaderLoder does not throw a null reference when lookup happens before warmup populates the cache.
        /// </summary>
        public static void VerifyShaderLookupReturnsNullWithoutWarmupCache()
        {
            var shaderLoader = new ShaderLoder(null);
            var shader = shaderLoader.FindShader("__Framework_Contract_NonExistent_Shader__");

            EnsureTrue(shader == null, "未命中的 Shader 查询应返回 null，而不是抛出异常。");
        }

        /// <summary>
        /// 验证 BResources.FindShader 在 ResLoader 尚未初始化时返回 null，而不是抛出空引用。
        /// Verify that BResources.FindShader returns null instead of throwing a null reference when ResLoader has not been initialized yet.
        /// </summary>
        public static void VerifyFindShaderReturnsNullWithoutLoader()
        {
            var loaderField = typeof(BResources).GetField(
                "<ResLoader>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            EnsureTrue(loaderField != null, "ResLoader backing field should exist for the no-loader shader lookup contract test.");

            var originalLoader = loaderField.GetValue(null);
            try
            {
                loaderField.SetValue(null, null);
                var shader = BResources.FindShader("__Framework_Contract_NoLoader_Shader__");
                EnsureTrue(shader == null, "ResLoader 未初始化时，FindShader 应返回 null。");
            }
            finally
            {
                loaderField.SetValue(null, originalLoader);
            }
        }

        /// <summary>
        /// 统一的相等断言。
        /// Shared equality assertion helper.
        /// </summary>
        public static void EnsureEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} expected={expected} actual={actual}");
            }
        }

        /// <summary>
        /// 统一的布尔断言。
        /// Shared boolean assertion helper.
        /// </summary>
        public static void EnsureTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        /// <summary>
        /// 统一的顺序断言。
        /// Shared sequence assertion helper.
        /// </summary>
        private static void EnsureSequenceEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual, string message)
        {
            if (actual == null)
            {
                throw new Exception($"{message} actual=null");
            }

            if (expected.Count != actual.Count)
            {
                throw new Exception($"{message} expectedCount={expected.Count} actualCount={actual.Count}");
            }

            for (var index = 0; index < expected.Count; index++)
            {
                if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal))
                {
                    throw new Exception($"{message} index={index} expected={expected[index]} actual={actual[index]}");
                }
            }
        }
    }
}
