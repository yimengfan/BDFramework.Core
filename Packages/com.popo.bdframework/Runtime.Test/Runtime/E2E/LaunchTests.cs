using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using Talos.E2E;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架启动流程 E2E 测试套件。
    /// Framework startup E2E test suite.
    /// 验证 BDFramework 从 DLL 加载到完全初始化的完整启动流程，并为 Android IL2CPP 反射发现保活 launch 套件。
    /// Verifies the end-to-end BDFramework startup path from DLL loading to full initialisation, and preserves the launch suite for Android IL2CPP reflection discovery.
    ///
    /// IL2CPP + HybridCLR 静态字段可见性说明：
    /// BDFramework.Test 是 AOT 程序集，直接引用 BDFramework.Core（热更程序集）。
    /// IL2CPP 会为 BDFramework.Core 中的类型生成独立的原生代码副本和静态字段存储，
    /// 而 HybridCLR 在运行时加载 BDFramework.Core 时会创建另一套独立的静态字段。
    /// AOT 代码直接访问 ScriptLoder.IsRunning 或 BApplication.IsPlaying 时，
    /// 读到的是 IL2CPP 编译版本的静态字段（始终为默认值 false），
    /// 而不是 HybridCLR 解释器维护的字段（已被 Init() 设为 true）。
    /// 因此，所有对热更程序集静态属性的访问必须通过 AppDomain 枚举 + 反射来进行，
    /// 以确保读到 HybridCLR 解释器中的实际值。
    ///
    /// IL2CPP + HybridCLR static field visibility note:
    /// BDFramework.Test is an AOT assembly that directly references BDFramework.Core (hotfix assembly).
    /// IL2CPP generates its own native code copy and static field storage for types in BDFramework.Core,
    /// while HybridCLR creates a separate set of static fields when it loads BDFramework.Core at runtime.
    /// When AOT code directly accesses ScriptLoder.IsRunning or BApplication.IsPlaying,
    /// it reads the IL2CPP-compiled version of the static fields (always the default value false),
    /// not the HybridCLR interpreter-maintained fields (which have been set to true by Init()).
    /// Therefore, all accesses to hotfix assembly static properties must go through AppDomain enumeration + reflection
    /// to ensure reading the actual values from the HybridCLR interpreter.
    /// </summary>
    [Preserve]
    static public class LaunchTests
    {
        /// <summary>
        /// 热更框架程序集名称，用于 AppDomain 枚举查找。
        /// Hotfix framework assembly name used for AppDomain enumeration lookup.
        /// </summary>
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";

        /// <summary>
        /// 验证热更 DLL 加载状态。
        /// Verify hotfix DLL loading state.
        /// Player 构建模式要求 ScriptLoder.IsRunning 为 true；Editor batchmode 直接编译热更代码，因此跳过运行态检查。
        /// Player builds require ScriptLoder.IsRunning to be true; Editor batchmode compiles hotfix code directly, so the runtime-only check is skipped there.
        ///
        /// IL2CPP 环境下必须通过反射读取热更程序集中的 ScriptLoder.IsRunning，
        /// 而不是直接访问 AOT 编译副本的静态字段。
        /// In IL2CPP environments, ScriptLoder.IsRunning must be read through reflection from the hotfix assembly,
        /// not through direct access to the AOT-compiled copy's static fields.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 1, des: "验证热更 DLL 加载完成")]
        static public void HotfixDllLoaded()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更代码直接由 Editor 编译，不通过 LoadHotfix()。
                // In Editor batchmode hotfix code is compiled directly by the Editor instead of going through LoadHotfix().
                Debug.Log("[E2E] Editor 模式：热更代码已由 Editor 直接编译，跳过 IsRunning 检查");
            }
            else
            {
                // Player 构建模式下，必须通过反射读取热更程序集中的 ScriptLoder.IsRunning，
                // 因为 AOT 编译副本与 HybridCLR 解释器维护的静态字段是独立的。
                // In player builds, ScriptLoder.IsRunning must be read through reflection from the hotfix assembly,
                // because the AOT-compiled copy and the HybridCLR interpreter maintain independent static fields.
                var isRunning = ReadHotfixStaticProperty<bool>(
                    "BDFramework.ScriptLoder", "IsRunning", false);
                if (!isRunning)
                {
                    throw new Exception("热更 DLL 未加载: ScriptLoder.IsRunning = false");
                }
                Debug.Log("[E2E] 热更 DLL 已成功加载（Player 模式）");
            }
        }

        /// <summary>
        /// 验证热更程序集中的类型可被枚举。
        /// Verify that hosted types from the hotfix assembly can be enumerated.
        /// Player 构建模式要求宿主类型枚举非空；Editor batchmode 中热更类型已直接并入 Editor 编译，因此跳过该检查。
        /// Player builds require a non-empty hosted-type enumeration; Editor batchmode already merges hotfix types into the Editor compilation, so the check is skipped there.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 2, des: "验证热更类型可被枚举")]
        static public void HotfixTypesDiscovered()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更类型直接在 Editor 程序集中。
                // In Editor batchmode the hotfix types live directly in the Editor-compiled assemblies.
                Debug.Log("[E2E] Editor 模式：热更类型已包含在 Editor 编译中，跳过 GetHotfixTypes 检查");
            }
            else
            {
                // 通过反射调用热更程序集中的 ScriptLoder.GetAppDomainHostingTypes()，
                // 确保 HybridCLR 解释器中的托管类型列表能被正确读取。
                // Call ScriptLoder.GetAppDomainHostingTypes() through reflection from the hotfix assembly,
                // ensuring the hosted type list maintained by the HybridCLR interpreter is read correctly.
                var types = InvokeHotfixStaticMethod<IEnumerable<Type>>(
                    "BDFramework.ScriptLoder", "GetAppDomainHostingTypes", null);
                
                if (types == null || types.Count() == 0)
                {
                    throw new Exception($"热更类型列表为空: count={types?.Count() ?? 0}");
                }
                Debug.Log($"[E2E] 发现 {types.Count()} 个热更类型（Player 模式）");
            }
        }

        /// <summary>
        /// 验证 BApplication 运行时标记已设置。
        /// Verify that the BApplication runtime flag has been set.
        ///
        /// IL2CPP 环境下必须通过反射读取热更程序集中的 BApplication.IsPlaying，
        /// 而不是直接访问 AOT 编译副本的静态字段。
        /// In IL2CPP environments, BApplication.IsPlaying must be read through reflection from the hotfix assembly,
        /// not through direct access to the AOT-compiled copy's static fields.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 3, des: "验证运行时标记已设置")]
        static public void BApplicationIsPlaying()
        {
            if (Application.isEditor)
            {
                // Editor 模式下直接访问即可，因为所有代码在同一编译域中。
                // In Editor mode, direct access works because all code is in the same compilation domain.
                if (!BApplication.IsPlaying)
                {
                    throw new Exception("BApplication.IsPlaying 未设置为 true");
                }
            }
            else
            {
                // Player 构建模式下，必须通过反射读取热更程序集中的 BApplication.IsPlaying，
                // 因为 AOT 编译副本与 HybridCLR 解释器维护的静态字段是独立的。
                // In player builds, BApplication.IsPlaying must be read through reflection from the hotfix assembly,
                // because the AOT-compiled copy and the HybridCLR interpreter maintain independent static fields.
                var isPlaying = ReadHotfixStaticProperty<bool>(
                    "BDFramework.Core.Tools.BApplication", "IsPlaying", false);
                if (!isPlaying)
                {
                    throw new Exception("BApplication.IsPlaying 未设置为 true（反射读取）");
                }
            }
            Debug.Log("[E2E] BApplication.IsPlaying = true");
        }

        /// <summary>
        /// 验证框架版本号可被读取。
        /// Verify that the framework version is readable.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 4, des: "验证框架版本号可读")]
        static public void FrameworkVersionReadable()
        {
            var version = BDLauncher.FrameworkVersion;
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception("框架版本号为空");
            }
            Debug.Log($"[E2E] 框架版本: {version}");
        }

        /// <summary>
        /// 通过 AppDomain 枚举从热更程序集中读取静态属性值。
        /// Read a static property value from the hotfix assembly through AppDomain enumeration.
        /// 在 IL2CPP + HybridCLR 环境中，AOT 编译副本与 HybridCLR 解释器维护独立的静态字段，
        /// 因此必须通过反射从热更程序集的运行时类型中读取实际值。
        /// In IL2CPP + HybridCLR environments, the AOT-compiled copy and the HybridCLR interpreter
        /// maintain independent static fields, so the actual value must be read through reflection
        /// from the hotfix assembly's runtime type.
        /// </summary>
        /// <typeparam name="T">属性值的期望类型。Expected type of the property value.</typeparam>
        /// <param name="typeName">类型的全限定名。Fully qualified type name.</param>
        /// <param name="propertyName">静态属性名。Static property name.</param>
        /// <param name="defaultValue">反射查找失败时的回退默认值。Fallback default value when reflection lookup fails.</param>
        /// <returns>热更程序集中静态属性的当前值。Current value of the static property from the hotfix assembly.</returns>
        private static T ReadHotfixStaticProperty<T>(string typeName, string propertyName, T defaultValue)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    var value = property.GetValue(null);
                    Debug.Log($"[E2E] 反射读取 {typeName}.{propertyName} = {value} (来自程序集 {assembly.GetName().Name})");
                    return value is T typedValue ? typedValue : defaultValue;
                }
            }

            Debug.LogWarning($"[E2E] 未在任何已加载程序集中找到 {typeName}.{propertyName}，使用默认值 {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// 通过 AppDomain 枚举从热更程序集中调用静态方法。
        /// Invoke a static method from the hotfix assembly through AppDomain enumeration.
        /// 在 IL2CPP + HybridCLR 环境中，AOT 编译副本与 HybridCLR 解释器维护独立的静态字段，
        /// 因此必须通过反射从热更程序集的运行时类型中调用方法以确保操作正确的状态。
        /// In IL2CPP + HybridCLR environments, the AOT-compiled copy and the HybridCLR interpreter
        /// maintain independent static fields, so methods must be invoked through reflection
        /// on the hotfix assembly's runtime type to ensure correct state is accessed.
        /// </summary>
        /// <typeparam name="T">方法返回值的期望类型。Expected type of the method return value.</typeparam>
        /// <param name="typeName">类型的全限定名。Fully qualified type name.</param>
        /// <param name="methodName">静态方法名。Static method name.</param>
        /// <param name="args">方法参数。Method arguments.</param>
        /// <returns>热更程序集中静态方法的返回值。Return value of the static method from the hotfix assembly.</returns>
        private static T InvokeHotfixStaticMethod<T>(string typeName, string methodName, object[] args)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, args);
                    Debug.Log($"[E2E] 反射调用 {typeName}.{methodName}() 成功 (来自程序集 {assembly.GetName().Name})");
                    return result is T typedResult ? typedResult : default;
                }
            }

            Debug.LogWarning($"[E2E] 未在任何已加载程序集中找到 {typeName}.{methodName}()");
            return default;
        }
    }
}
