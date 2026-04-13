using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LitJson;
using UnityEngine;
using BDFramework;

namespace Talos.E2E
{
    /// <summary>
    /// 单条 E2E 测试用例的执行结果。
    /// 与 Playwright 端通过 JSON 交换。
    /// </summary>
    [Serializable]
    public class E2ETestResult
    {
        /// <summary>测试套件名称</summary>
        public string suite;

        /// <summary>测试类全名</summary>
        public string className;

        /// <summary>测试方法名</summary>
        public string methodName;

        /// <summary>测试描述</summary>
        public string description;

        /// <summary>是否通过</summary>
        public bool passed;

        /// <summary>失败消息，通过时为空</summary>
        public string errorMessage;

        /// <summary>执行耗时（毫秒）</summary>
        public long durationMs;

        /// <summary>执行时间戳（Unix 秒）</summary>
        public long timestamp;
    }

    /// <summary>
    /// E2E 测试描述符——描述一个被发现的测试用例的元信息。
    /// 用于响应 list_tests 请求。
    /// </summary>
    [Serializable]
    public class E2ETestDescriptor
    {
        /// <summary>测试套件名称</summary>
        public string suite;

        /// <summary>测试类全名</summary>
        public string className;

        /// <summary>测试方法名</summary>
        public string methodName;

        /// <summary>测试描述</summary>
        public string description;

        /// <summary>执行顺序</summary>
        public int order;

        /// <summary>超时时间（毫秒）</summary>
        public int timeout;
    }

    /// <summary>
    /// E2E 测试运行器——核心协调器。
    /// 
    /// 设计角色：
    /// - 从热更程序集中发现所有标记了 [E2ETest] 的静态方法。
    /// - 按套件和顺序组织测试用例。
    /// - 逐条执行测试并生成标准化的结果对象。
    /// - 通过 TalosTestServer 将结果实时推送给 Playwright 端。
    /// 
    /// 生命周期：
    /// 1. 在热更代码加载完成后，由 TalosE2EBootstrap 自动调用 Initialize()。
    /// 2. Playwright 端通过 TCP 发送 run_test / run_suite / run_all_tests 指令。
    /// 3. 运行器执行测试并通过 TCP 推送结果。
    /// 4. 全部完成后发送 all_tests_complete 通知。
    /// </summary>
    static public class E2ETestRunner
    {
        /// <summary>
        /// 已发现的测试用例列表，按 Order 排序。
        /// </summary>
        static private readonly List<E2ETestDescriptor> DiscoveredTests = new List<E2ETestDescriptor>();

        /// <summary>
        /// 测试用例方法缓存：methodName -> MethodInfo。
        /// </summary>
        static private readonly Dictionary<string, MethodInfo> MethodCache = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// 当前是否正在执行测试。
        /// </summary>
        static public bool IsRunning { get; private set; }

        /// <summary>
        /// 初始化测试运行器，从 AOT 程序集和热更程序集中发现所有 E2E 测试用例。
        /// 必须在热更 DLL 加载完成后调用（热更类型在热更 DLL 加载后才可被发现）。
        /// 
        /// 扫描范围：
        /// 1. 当前程序集（Talos.E2E.Runtime）——AOT 预编译的内置测试用例
        /// 2. 热更程序集——业务层动态添加的测试用例
        /// 
        /// 热更程序集发现策略：
        /// - 优先通过 HotfixAssembliesHelper（旧版热更加载器维护的列表）
        /// - 回退到 AppDomain.CurrentDomain.GetAssemblies() 全域扫描
        ///   真机上 ScriptLoderAOT 使用 Assembly.Load 直接加载，
        ///   不经过 HotfixAssembliesHelper，因此需要 AppDomain 全扫描兜底。
        /// </summary>
        static public void Initialize()
        {
            DiscoveredTests.Clear();
            MethodCache.Clear();

            // Phase 1: 扫描当前 AOT 程序集（Talos.E2E.Runtime）
            // 内置测试用例（LaunchTests、AssetLoadTests 等）在预编译的程序集中
            var selfAssembly = typeof(E2ETestRunner).Assembly;
            ScanAssembly(selfAssembly);
            UnityEngine.Debug.Log($"[TalosE2E] AOT 程序集扫描完成，发现 {DiscoveredTests.Count} 个内置测试用例");

            // Phase 2: 扫描热更程序集
            // 业务层可通过热更 DLL 动态添加 E2E 测试用例
            ScanHotfixAssemblies();

            // Phase 3: 按 suite 和 order 排序
            DiscoveredTests.Sort((a, b) =>
            {
                int cmp = string.Compare(a.suite, b.suite, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                return a.order.CompareTo(b.order);
            });

            UnityEngine.Debug.Log($"[TalosE2E] 测试发现完成，共 {DiscoveredTests.Count} 个测试用例");
        }

        /// <summary>
        /// 扫描热更程序集中的 E2E 测试用例。
        /// 
        /// 真机上 ScriptLoderAOT 通过 Assembly.Load 直接加载热更 DLL，
        /// 不经过 HotfixAssembliesHelper，因此需要多策略扫描：
        /// 
        /// 策略 1：通过 HotfixAssembliesHelper.GetHotfixTypes()（如果已注册）
        /// 策略 2：通过 AppDomain 全扫描，过滤非系统程序集
        /// </summary>
        static private void ScanHotfixAssemblies()
        {
            var scannedAssemblies = new HashSet<string>();
            int beforeCount = DiscoveredTests.Count;

            // 策略 1: 通过 HotfixAssembliesHelper 扫描（旧版热更加载器）
            var hotfixTypes = ScriptLoder.GetHostingTypes();
            if (hotfixTypes != null && hotfixTypes.Count() > 0)
            {
                foreach (var type in hotfixTypes)
                {
                    ScanType(type);
                    if (type.Assembly != null)
                    {
                        scannedAssemblies.Add(type.Assembly.FullName);
                    }
                }
                UnityEngine.Debug.Log($"[TalosE2E] HotfixAssembliesHelper 扫描: 新增 {DiscoveredTests.Count - beforeCount} 个热更测试用例");
            }

            // 策略 2: AppDomain 全扫描兜底——覆盖 ScriptLoderAOT 直接 Assembly.Load 的场景
            // 真机上热更 DLL 通过 ScriptLoderAOT.LoadHotfixDLL() 用 Assembly.Load 加载，
            // 但不注册到 HotfixAssembliesHelper，所以需要通过 AppDomain 发现。
            int beforeAppDomain = DiscoveredTests.Count;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 跳过已扫描的程序集
                if (scannedAssemblies.Contains(assembly.FullName)) continue;

                var name = assembly.GetName().Name;

                // 只扫描可能是业务热更的程序集：
                // - 排除系统程序集（mscorlib、System.*、Unity.*、Mono.* 等）
                // - 排除当前 AOT 程序集（已扫描）
                // - 包含热更特征名称（Assembly-CSharp、Game.*、hotfix 等）
                if (IsSystemAssembly(name)) continue;
                if (name == selfAssemblyName) continue;

                ScanAssembly(assembly);
                scannedAssemblies.Add(assembly.FullName);
            }
            UnityEngine.Debug.Log($"[TalosE2E] AppDomain 全扫描: 新增 {DiscoveredTests.Count - beforeAppDomain} 个热更测试用例");

            if (DiscoveredTests.Count == beforeCount)
            {
                UnityEngine.Debug.Log("[TalosE2E] 无热更测试用例");
            }
        }

        /// <summary>
        /// 当前 AOT 程序集名称，用于在 AppDomain 扫描时跳过。
        /// </summary>
        static private readonly string selfAssemblyName = typeof(E2ETestRunner).Assembly.GetName().Name;

        /// <summary>
        /// 判断程序集名称是否为系统程序集，避免扫描 Unity 引擎和 .NET 运行时。
        /// </summary>
        /// <param name="assemblyName">程序集短名称。</param>
        /// <returns>如果是系统程序集返回 true。</returns>
        static private bool IsSystemAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return true;

            // 系统前缀过滤
            if (assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("Google.Protobuf", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("MessagePack", StringComparison.OrdinalIgnoreCase)) return true;
            if (assemblyName.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase)) return true;

            // 已知的框架/第三方程序集（非业务热更）
            var knownNonHotfix = new HashSet<string>()
            {
                "LitJson",
                "BDFramework.Runtime",
                "BDFramework.Editor",
                "BDFramework.Core",
                "BDFramework.AOT",
                "BDFramework.EditorTest",
                "BDFramework.Test",
                "BDFramework.core.test",
                "HybridCLR.Runtime",
                "HybridCLR.Editor",
                "UniTask",
                "UniTask.Addressables",
                "UniTask.DOTween",
                "UniTask.Editor",
                "UniTask.Linq",
                "UniTask.TextMeshPro",
                "DOTween",
                "DOTween.Ex",
                "ZString",
                "Sirenix.OdinInspector",
                "Obfuz.Runtime",
                "Obfuz.Editor",
                "Obfuz4HybridCLR.Editor",
                "NuGet",
                "com.logviewer.runtime",
                "com.logviewer.editor",
                "Unity.InternalAPIEngineBridge.010",
                "Unity.AssetGraph",
                "Talos.E2E.Runtime",
                "Talos.E2E.Editor",
                "EditorSetting",
                "GameClaw.Unity3d",
                "GameLogic", // 非热更的 AOT GameLogic
            };
            if (knownNonHotfix.Contains(assemblyName)) return true;

            return false;
        }

        /// <summary>
        /// 扫描一个程序集中所有带 [E2ETest] 标记的静态方法。
        /// </summary>
        /// <param name="assembly">待扫描的程序集。</param>
        static private void ScanAssembly(Assembly assembly)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    ScanType(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 某些类型加载失败时，处理已成功加载的类型
                UnityEngine.Debug.LogWarning($"[TalosE2E] 程序集 {assembly.GetName().Name} 部分类型加载失败，跳过失败类型");
                if (ex.Types != null)
                {
                    foreach (var type in ex.Types)
                    {
                        if (type != null) ScanType(type);
                    }
                }
            }
        }

        /// <summary>
        /// 扫描一个类型中所有带 [E2ETest] 标记的静态方法。
        /// </summary>
        /// <param name="type">待扫描的类型。</param>
        static private void ScanType(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<E2ETestAttribute>();
                if (attr == null) continue;

                var desc = new E2ETestDescriptor
                {
                    suite = attr.Suite,
                    className = type.FullName,
                    methodName = method.Name,
                    description = attr.Des,
                    order = attr.Order,
                    timeout = attr.Timeout
                };

                DiscoveredTests.Add(desc);
                MethodCache[method.Name] = method;
            }
        }

        /// <summary>
        /// 获取所有已发现的测试描述符列表（用于 list_tests 响应）。
        /// </summary>
        static public List<E2ETestDescriptor> GetTestList()
        {
            return new List<E2ETestDescriptor>(DiscoveredTests);
        }

        /// <summary>
        /// 执行单个测试用例。
        /// </summary>
        /// <param name="methodName">测试方法名。</param>
        /// <returns>测试结果，找不到方法时返回 null。</returns>
        static public E2ETestResult RunSingle(string methodName)
        {
            MethodInfo method;
            if (!MethodCache.TryGetValue(methodName, out method))
            {
                return new E2ETestResult
                {
                    methodName = methodName,
                    passed = false,
                    errorMessage = $"未找到测试方法: {methodName}",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }

            return ExecuteTestMethod(method);
        }

        /// <summary>
        /// 执行指定套件中的所有测试用例。
        /// </summary>
        /// <param name="suite">套件名称。</param>
        /// <returns>该套件的测试结果列表。</returns>
        static public List<E2ETestResult> RunSuite(string suite)
        {
            var results = new List<E2ETestResult>();
            IsRunning = true;

            try
            {
                foreach (var desc in DiscoveredTests)
                {
                    if (desc.suite != suite) continue;
                    MethodInfo method;
                    if (MethodCache.TryGetValue(desc.methodName, out method))
                    {
                        results.Add(ExecuteTestMethod(method));
                    }
                }
            }
            finally
            {
                IsRunning = false;
            }

            return results;
        }

        /// <summary>
        /// 执行所有已发现的测试用例，按排序顺序依次执行。
        /// 每个测试执行完毕后通过 onTestComplete 回调推送结果。
        /// </summary>
        /// <param name="onTestComplete">单个测试完成时的回调，用于实时推送结果。</param>
        /// <returns>所有测试结果列表。</returns>
        static public List<E2ETestResult> RunAll(Action<E2ETestResult> onTestComplete = null)
        {
            var results = new List<E2ETestResult>();
            IsRunning = true;

            try
            {
                foreach (var desc in DiscoveredTests)
                {
                    MethodInfo method;
                    if (!MethodCache.TryGetValue(desc.methodName, out method)) continue;

                    var result = ExecuteTestMethod(method);
                    results.Add(result);
                    onTestComplete?.Invoke(result);
                }
            }
            finally
            {
                IsRunning = false;
            }

            return results;
        }

        /// <summary>
        /// 执行单个测试方法，捕获异常并测量耗时。
        /// </summary>
        private static E2ETestResult ExecuteTestMethod(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<E2ETestAttribute>();
            var stopwatch = Stopwatch.StartNew();
            var result = new E2ETestResult
            {
                suite = attr?.Suite ?? "未知",
                className = method.DeclaringType?.FullName ?? "",
                methodName = method.Name,
                description = attr?.Des ?? "",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            try
            {
                UnityEngine.Debug.Log($"[TalosE2E] 执行测试: {result.suite}/{result.methodName} - {result.description}");
                method.Invoke(null, null);
                result.passed = true;
            }
            catch (TargetInvocationException tie)
            {
                result.passed = false;
                result.errorMessage = tie.InnerException?.Message ?? tie.Message;
                UnityEngine.Debug.LogError($"[TalosE2E] 测试失败: {result.methodName} - {result.errorMessage}");
            }
            catch (Exception ex)
            {
                result.passed = false;
                result.errorMessage = ex.Message;
                UnityEngine.Debug.LogError($"[TalosE2E] 测试失败: {result.methodName} - {result.errorMessage}");
            }

            stopwatch.Stop();
            result.durationMs = stopwatch.ElapsedMilliseconds;

            // 超时检查
            if (attr != null && result.durationMs > attr.Timeout)
            {
                result.passed = false;
                result.errorMessage = $"测试超时: {result.durationMs}ms > {attr.Timeout}ms";
            }

            UnityEngine.Debug.Log($"[TalosE2E] 测试完成: {result.methodName} - {(result.passed ? "通过" : "失败")} ({result.durationMs}ms)");
            return result;
        }
    }
}
