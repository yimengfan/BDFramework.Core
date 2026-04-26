using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 热更测试程序集注入器。
    /// Hotfix test assembly injector.
    /// 该类型负责在 Debug 构建时，将测试程序集动态注入 HybridCLR 的 hotUpdateAssemblies 列表。
    /// This type is responsible for dynamically injecting test assemblies into HybridCLR's hotUpdateAssemblies list during Debug builds.
    /// 
    /// 设计目的：
    /// Design purpose:
    /// - Test DLLs must be hotfix assemblies to run on packaged players.
    /// - 测试 DLL 必须是热更程序集才能在打包后的 Player 上运行。
    /// - Release builds must NOT include test assemblies for security and performance.
    /// - Release 构建不得包含测试程序集，以保证安全性和性能。
    /// - Debug builds automatically include test assemblies for validation.
    /// - Debug 构建自动包含测试程序集以进行验证。
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
        /// </summary>
        private static readonly string[] TestAssemblyNames = new string[]
        {
            "BDFramework.Test",
            "BDFramework.HostE2E",
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
        /// 检测当前构建是否为 Debug 构建。
        /// Detect whether the current build is a Debug build.
        /// </summary>
        /// <returns>如果是 Debug 构建返回 true。Returns true if this is a Debug build.</returns>
        static public bool IsCurrentBuildDebug()
        {
            // 检查命令行参数
            // 使用完全限定名避免与 BDFramework.Core.Tools.Environment 命名冲突。
            // Use fully qualified name to avoid collision with BDFramework.Core.Tools.Environment.
            var commandLineArgs = System.Environment.GetCommandLineArgs();
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
