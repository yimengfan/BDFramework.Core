using System;
using System.Collections;
using System.Reflection;
using BDFramework;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.HostE2E
{
    /// <summary>
    /// 宿主侧基础启动流程 E2E 测试套件。
    /// Host-owned base launch-flow E2E suite.
    /// 该套件只依赖 AOT 启动器和 Talos 运行时契约，通过宿主可见信号验证热更程序集已经装载并可被枚举，
    /// 避免为 step_01 把引用热更层的 Runtime.Test 程序集再次根引用进母包。
    /// This suite depends only on the AOT launcher and Talos runtime contracts and verifies through host-visible signals that hotfix assemblies are loaded and enumerable,
    /// avoiding the need to root the Runtime.Test assembly that references the hotfix layer back into the base package for step_01.
    /// </summary>
    [Preserve]
    public static class LaunchFlowHostTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string HotfixScriptLoaderTypeName = "BDFramework.ScriptLoder";

        /// <summary>
        /// 验证宿主进程已经装载核心热更程序集。
        /// Verify that the host process has already loaded the core hotfix assembly.
        /// 该检查直接使用当前 AppDomain 的程序集列表，不对热更程序集建立编译期引用，
        /// 从而避免 Android stripped-AOT 临时工程因为静态依赖而再次解析热更程序集。
        /// This check uses the current AppDomain assembly list directly and avoids a compile-time reference to the hotfix assembly,
        /// so the Android stripped-AOT temp project does not try to resolve the hotfix assembly again via a static dependency.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 1, des: "验证宿主已装载核心热更程序集")]
        public static void HotfixFrameworkAssemblyLoaded()
        {
            var hotfixAssembly = FindLoadedAssembly(HotfixFrameworkAssemblyName);
            if (hotfixAssembly == null)
            {
                throw new Exception($"未发现热更程序集: {HotfixFrameworkAssemblyName}");
            }

            Debug.Log($"[E2E] 宿主已装载热更程序集: {hotfixAssembly.GetName().Name}");
        }

        /// <summary>
        /// 验证宿主能够通过热更脚本加载器枚举托管类型。
        /// Verify that the host can enumerate hosted types through the hotfix script loader.
        /// 这里使用轻量反射跨越 AOT 与热更程序集边界，只读取公开静态入口，
        /// 既验证热更脚本加载器可见，又避免把热更类型静态链接进宿主测试程序集。
        /// This uses lightweight reflection across the AOT and hotfix-assembly boundary and reads only the public static entry,
        /// verifying that the hotfix script loader is visible while avoiding a static link from the host test assembly into hotfix types.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 2, des: "验证宿主可枚举热更托管类型")]
        public static void HostedTypesDiscoverableFromScriptLoader()
        {
            var hotfixAssembly = FindLoadedAssembly(HotfixFrameworkAssemblyName);
            if (hotfixAssembly == null)
            {
                throw new Exception($"未发现热更程序集: {HotfixFrameworkAssemblyName}");
            }

            var scriptLoaderType = hotfixAssembly.GetType(HotfixScriptLoaderTypeName);
            if (scriptLoaderType == null)
            {
                throw new Exception($"未发现热更脚本加载器类型: {HotfixScriptLoaderTypeName}");
            }

            var getHostingTypesMethod = scriptLoaderType.GetMethod(
                "GetAppDomainHostingTypes",
                BindingFlags.Public | BindingFlags.Static);
            if (getHostingTypesMethod == null)
            {
                throw new Exception("未发现 ScriptLoder.GetAppDomainHostingTypes 公开静态入口");
            }

            var hostingTypes = getHostingTypesMethod.Invoke(null, null) as IEnumerable;
            var hostingTypeCount = CountItems(hostingTypes);
            if (hostingTypeCount <= 0)
            {
                throw new Exception("热更托管类型枚举为空");
            }

            Debug.Log($"[E2E] 宿主已枚举热更托管类型: count={hostingTypeCount}");
        }

        /// <summary>
        /// 验证宿主启动器实例和框架版本号可读。
        /// Verify that the host launcher instance and framework version are readable.
        /// 该检查确认启动场景的 AOT 启动器已经完成实例注册，并提供稳定的宿主侧版本信号，
        /// 以覆盖 step_01 对基础启动链路完成态的最小要求。
        /// This check confirms that the AOT launcher in the startup scene has finished instance registration and exposes a stable host-side version signal,
        /// covering the minimal step_01 requirement that the base startup chain has completed.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 3, des: "验证宿主启动器版本信号可读")]
        public static void LauncherVersionSignalReady()
        {
            if (BDLauncher.Inst == null)
            {
                throw new Exception("BDLauncher.Inst 为空");
            }

            var frameworkVersion = BDLauncher.FrameworkVersion;
            if (string.IsNullOrWhiteSpace(frameworkVersion))
            {
                throw new Exception("框架版本号为空");
            }

            Debug.Log($"[E2E] 宿主启动器已就绪: frameworkVersion={frameworkVersion} clientVersion={BDLauncher.Inst.ClientVersion}");
        }

        /// <summary>
        /// 从当前 AppDomain 中查找指定短名称的已装载程序集。
        /// Find a loaded assembly with the specified short name from the current AppDomain.
        /// </summary>
        /// <param name="assemblyName">目标程序集短名称。</param>
        /// <param name="assemblyName">Target short assembly name.</param>
        /// <returns>命中时返回程序集，否则返回 null。</returns>
        /// <returns>Returns the assembly when found; otherwise returns null.</returns>
        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 统计非泛型枚举中的元素数量。
        /// Count the number of items in a non-generic enumerable.
        /// 该辅助方法让宿主测试能够消费反射返回的枚举结果，而不把热更程序集的具体集合类型耦合进来。
        /// This helper lets the host test consume reflection-returned enumerables without coupling the concrete collection type from the hotfix assembly.
        /// </summary>
        /// <param name="items">要统计的枚举结果。</param>
        /// <param name="items">Enumerable result to count.</param>
        /// <returns>枚举元素数量。</returns>
        /// <returns>Number of items in the enumerable.</returns>
        private static int CountItems(IEnumerable items)
        {
            if (items == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var _ in items)
            {
                count++;
            }

            return count;
        }
    }
}