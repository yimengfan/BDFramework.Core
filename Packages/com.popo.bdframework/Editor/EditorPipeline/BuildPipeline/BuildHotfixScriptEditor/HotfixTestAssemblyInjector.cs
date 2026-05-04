using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 热更测试程序集注入器。
    /// Hotfix test assembly injector.
    /// 该类型负责在需要测试的构建模式（Debug、ReleaseForTest）下，
    /// 将测试程序集动态注入 HybridCLR 的 hotUpdateAssemblies 列表。
    /// This type is responsible for dynamically injecting test assemblies into HybridCLR's hotUpdateAssemblies list
    /// under build modes that require tests (Debug, ReleaseForTest).
    /// 
    /// 构建模式与测试程序集矩阵：
    /// Build mode vs test assembly matrix:
    /// - Debug: ✓ 注入 / injected
    /// - DebugForProfiler: ✗ 不注入 / not injected
    /// - Release: ✗ 不注入 / not injected
    /// - ReleaseForTest: ✓ 注入 / injected
    /// 
    /// 使用方式：
    /// Usage:
    /// - 自动模式：构建时自动根据 Debug/Release 决定是否注入。
    /// - Automatic mode: injection is decided by Debug/Release during build.
    /// - 手动模式：通过菜单 "BDFrameWork工具箱/Hotfix/注入测试程序集" 手动控制。
    /// - Manual mode: use menu "BDFrameWork工具箱/Hotfix/注入测试程序集" to control manually.
    /// </summary>
    static public class HotfixTestAssemblyInjector
    {
        /// <summary>
        /// 测试程序集名称列表。
        /// Test assembly name list.
        /// 这些程序集将在 Debug 构建时被注入 hotUpdateAssemblies。
        /// These assemblies will be injected into hotUpdateAssemblies during Debug builds.
        /// Release 构建必须确保此列表中的程序集不出现在 hotUpdateAssemblies、preserveHotUpdateAssemblies
        /// 以及热更 DLL 输出目录中。
        /// Release builds must ensure assemblies in this list are absent from hotUpdateAssemblies, preserveHotUpdateAssemblies,
        /// and the hotfix DLL output directory.
        /// </summary>
        static public readonly string[] TestAssemblyNames = new string[]
        {
            "BDFramework.Test",
        };

        /// <summary>
        /// 注入状态标记，防止重复注入。
        /// Injection state flag to prevent duplicate injection.
        /// </summary>
        private static bool _hasInjected;

        /// <summary>
        /// 注入测试程序集到 HybridCLR 配置（用于 Debug 构建）。
        /// Inject test assemblies into HybridCLR configuration (for Debug builds).
        /// </summary>
        static public void InjectTestAssemblies()
        {
            if (_hasInjected)
            {
                Debug.Log("[HotfixTestInjector] 测试程序集已注入，跳过重复注入");
                return;
            }

            try
            {
                var hotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>();
                var list = new List<string>(hotUpdateAssemblies);
                var injected = new List<string>();

                foreach (var testAssembly in TestAssemblyNames)
                {
                    if (!list.Contains(testAssembly))
                    {
                        list.Add(testAssembly);
                        injected.Add(testAssembly);
                    }
                }

                if (injected.Count > 0)
                {
                    HybridCLRSettings.Instance.hotUpdateAssemblies = list.ToArray();
                    HybridCLRSettings.Save();
                    Debug.Log($"[HotfixTestInjector] 已注入测试程序集: {string.Join(", ", injected)}");
                }
                else
                {
                    Debug.Log("[HotfixTestInjector] 测试程序集已存在于配置中，无需注入");
                }

                _hasInjected = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[HotfixTestInjector] 注入测试程序集失败: {e.Message}");
            }
        }

        /// <summary>
        /// 确保测试程序集已从 HybridCLR 配置中移除（用于 Release 构建）。
        /// Ensure test assemblies are removed from HybridCLR configuration (for Release builds).
        /// </summary>
        static public void EnsureTestAssembliesRemoved()
        {
            try
            {
                var hotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>();
                var list = new List<string>(hotUpdateAssemblies);
                var removed = new List<string>();

                foreach (var testAssembly in TestAssemblyNames)
                {
                    if (list.Remove(testAssembly))
                    {
                        removed.Add(testAssembly);
                    }
                }

                if (removed.Count > 0)
                {
                    HybridCLRSettings.Instance.hotUpdateAssemblies = list.ToArray();
                    HybridCLRSettings.Save();
                    Debug.Log($"[HotfixTestInjector] 已移除测试程序集: {string.Join(", ", removed)}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HotfixTestInjector] 移除测试程序集失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取当前配置的测试程序集列表。
        /// Get the currently configured test assembly list.
        /// </summary>
        /// <returns>测试程序集名称数组。Array of test assembly names.</returns>
        static public string[] GetConfiguredTestAssemblies()
        {
            var hotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>();
            return TestAssemblyNames.Where(ta => hotUpdateAssemblies.Contains(ta)).ToArray();
        }

        /// <summary>
        /// 检查测试程序集是否已注入。
        /// Check whether test assemblies have been injected.
        /// </summary>
        /// <returns>如果已注入返回 true。Returns true if injected.</returns>
        static public bool AreTestAssembliesInjected()
        {
            var hotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>();
            return TestAssemblyNames.All(ta => hotUpdateAssemblies.Contains(ta));
        }

        /// <summary>
        /// 重置注入状态。
        /// Reset the injection state.
        /// </summary>
        static public void ResetInjectionState()
        {
            _hasInjected = false;
        }

        /// <summary>
        /// 检测当前构建是否为需要测试程序集的构建模式（Debug 或 ReleaseForTest）。
        /// Detect whether the current build requires test assemblies (Debug or ReleaseForTest).
        /// 优先检查 <c>-buildMode</c> 参数，回退到 <c>-buildDebug</c> 参数兼容旧 CI，
        /// 最后检查 EditorUserBuildSettings.development。
        /// Prioritizes the <c>-buildMode</c> parameter, falls back to <c>-buildDebug</c> for legacy CI compatibility,
        /// then checks EditorUserBuildSettings.development.
        /// </summary>
        /// <returns>如果是需要测试程序集的构建模式返回 true。Returns true if the build mode requires test assemblies.</returns>
        static public bool IsCurrentBuildDebug()
        {
            // 优先检查 -buildMode 参数
            // 使用完全限定名避免与 BDFramework.Core.Tools.Environment 命名冲突。
            // Use fully qualified name to avoid collision with BDFramework.Core.Tools.Environment.
            var commandLineArgs = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (string.Equals(commandLineArgs[i], "-buildMode", StringComparison.OrdinalIgnoreCase))
                {
                    var value = commandLineArgs[i + 1];
                    if (Enum.TryParse<BuildTools_ClientPackage.BuildMode>(value, true, out var parsed))
                    {
                        return BuildTools_ClientPackage.ShouldInjectTestAssemblies(parsed);
                    }
                }
            }

            // 回退到 -buildDebug 兼容旧 CI
            // Fall back to -buildDebug for legacy CI compatibility
            for (int i = 0; i < commandLineArgs.Length - 1; i++)
            {
                if (string.Equals(commandLineArgs[i], "-buildDebug", StringComparison.OrdinalIgnoreCase))
                {
                    var value = commandLineArgs[i + 1];
                    if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 检查 EditorUserBuildSettings
            if (EditorUserBuildSettings.development)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 输出当前 HybridCLR 热更程序集配置信息。
        /// Print current HybridCLR hotfix assembly configuration info.
        /// </summary>
        [MenuItem("BDFrameWork工具箱/Hotfix/查看热更程序集配置")]
        static public void PrintHotfixAssemblyConfig()
        {
            var hotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies ?? Array.Empty<string>();
            Debug.Log($"[HotfixTestInjector] 当前热更程序集配置 ({hotUpdateAssemblies.Length} 个):\n{string.Join("\n", hotUpdateAssemblies)}");

            var configuredTests = GetConfiguredTestAssemblies();
            Debug.Log($"[HotfixTestInjector] 已配置的测试程序集 ({configuredTests.Length} 个):\n{string.Join("\n", configuredTests)}");

            var isInjected = AreTestAssembliesInjected();
            Debug.Log($"[HotfixTestInjector] 测试程序集注入状态: {(isInjected ? "已注入" : "未注入")}");
        }

        /// <summary>
        /// 手动触发注入测试程序集（仅用于调试）。
        /// Manually trigger test assembly injection (for debugging only).
        /// </summary>
        [MenuItem("BDFrameWork工具箱/Hotfix/注入测试程序集 (Debug)")]
        static public void ManualInjectTestAssemblies()
        {
            ResetInjectionState();
            InjectTestAssemblies();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 手动触发移除测试程序集（仅用于调试）。
        /// Manually trigger test assembly removal (for debugging only).
        /// </summary>
        [MenuItem("BDFrameWork工具箱/Hotfix/移除测试程序集 (Release)")]
        static public void ManualRemoveTestAssemblies()
        {
            EnsureTestAssembliesRemoved();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 验证热更 DLL 输出目录中不包含测试程序集。
        /// Validate that the hotfix DLL output directory does not contain test assemblies.
        /// 这是 Release 构建的最后一道防线：即使上游遗漏了 EnsureTestAssembliesRemoved() 调用，
        /// 此方法也能在热更 DLL 已落盘后检出泄漏，阻止包含测试代码的制品流入发布流程。
        /// This is the last line of defense for Release builds: even if upstream misses an EnsureTestAssembliesRemoved() call,
        /// this method can detect leaks after hotfix DLLs have been written to disk and prevent test-contaminated artifacts from entering the release pipeline.
        /// </summary>
        /// <param name="hotfixDllOutputRoot">热更 DLL 输出根目录，通常为 DevOpsPublishAssetsPath 或 StreamingAssets 下的平台子目录。</param>
        /// <param name="hotfixDllOutputRoot">Hotfix DLL output root directory, typically the platform subdirectory under DevOpsPublishAssetsPath or StreamingAssets.</param>
        /// <param name="isReleaseBuild">是否为 Release 构建。为 true 时发现泄漏会抛出异常；为 false 时仅输出告警。</param>
        /// <param name="isReleaseBuild">Whether this is a Release build. When true, a leak throws an exception; when false, only a warning is logged.</param>
        /// <exception cref="Exception">当 isReleaseBuild 为 true 且输出目录中存在测试程序集文件时抛出。</exception>
        /// <exception cref="Exception">Thrown when isReleaseBuild is true and test assembly files are found in the output directory.</exception>
        static public void ValidateNoTestAssembliesInOutput(string hotfixDllOutputRoot, bool isReleaseBuild)
        {
            if (string.IsNullOrWhiteSpace(hotfixDllOutputRoot) || !Directory.Exists(hotfixDllOutputRoot))
            {
                Debug.Log("[HotfixTestInjector] 热更输出目录不存在，跳过产物验证");
                return;
            }

            var leakedAssemblies = new List<string>();
            foreach (var testAssemblyName in TestAssemblyNames)
            {
                // 检查 .dll.bytes（编辑器内拷贝格式）与 .zlua.bytes（发布格式）
                // Check both .dll.bytes (editor copy format) and .zlua.bytes (release format)
                var dllBytesPath = Path.Combine(hotfixDllOutputRoot, testAssemblyName + ".dll.bytes");
                var zluaBytesPath = Path.Combine(hotfixDllOutputRoot, testAssemblyName + ".zlua.bytes");

                if (File.Exists(dllBytesPath) || File.Exists(zluaBytesPath))
                {
                    leakedAssemblies.Add(testAssemblyName);
                }
            }

            if (leakedAssemblies.Count == 0)
            {
                Debug.Log("[HotfixTestInjector] 产物验证通过：热更输出目录不含测试程序集");
                return;
            }

            var leakedNames = string.Join(", ", leakedAssemblies);
            var message = $"[HotfixTestInjector] 检测到热更输出目录包含测试程序集: {leakedNames}，目录: {hotfixDllOutputRoot}";

            if (isReleaseBuild)
            {
                throw new Exception($"{message} — Release 构建禁止输出测试程序集，请检查构建流程是否正确调用了 EnsureTestAssembliesRemoved()");
            }

            Debug.LogWarning($"{message} — Debug 构建允许测试程序集存在，但如果这不是 Debug 构建请排查");
        }

        /// <summary>
        /// 根据当前构建模式自动注入或移除测试程序集。
        /// Automatically inject or remove test assemblies based on current build mode.
        /// </summary>
        [MenuItem("BDFrameWork工具箱/Hotfix/自动配置测试程序集")]
        static public void AutoConfigureTestAssemblies()
        {
            ResetInjectionState();
            
            if (IsCurrentBuildDebug())
            {
                Debug.Log("[HotfixTestInjector] 检测到 Debug 构建，注入测试程序集");
                InjectTestAssemblies();
            }
            else
            {
                Debug.Log("[HotfixTestInjector] 检测到 Release 构建，移除测试程序集");
                EnsureTestAssembliesRemoved();
            }
            
            AssetDatabase.Refresh();
        }
    }
}
