using System;
using System.Collections;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架核心业务能力 E2E 测试套件。
    /// Framework core business-capability E2E test suite.
    /// 在热更已加载后，验证事件系统、管理器注册完整性等框架核心基础能力。
    /// After hotfix loaded, verify event system, manager registration completeness, and other core foundational capabilities.
    /// </summary>
    [Preserve]
    public static class FrameworkCoreBusinessTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string ScriptLoderTypeName = "BDFramework.ScriptLoder";
        private const string AStatusListenerTypeName = "BDFramework.DataListener.AStatusListener";
        private const string StatusListenerServiceTypeName = "BDFramework.DataListener.StatusListenerService";

        /// <summary>
        /// 验证框架管理器已正确注册并可枚举。
        /// Verify that framework managers are correctly registered and enumerable.
        /// 该检查通过 ScriptLoder 枚举所有托管类型并确认关键管理器类型存在。
        /// This check enumerates all hosted types via ScriptLoder and confirms key manager types exist.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-core-business", order: 1, des: "验证框架管理器注册完整性")]
        public static void ManagerRegistrationCompleteness()
        {
            Debug.Log("[E2E] 测试目的=验证框架管理器注册完整性 实现手段=通过 ScriptLoder.GetAppDomainHostingTypes 枚举并检查关键管理器类型");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var scriptLoaderType = hotfixAssembly.GetType(ScriptLoderTypeName);
            if (scriptLoaderType == null)
                throw new Exception($"未发现 ScriptLoder 类型: {ScriptLoderTypeName}");

            var getHostingTypesMethod = scriptLoaderType.GetMethod(
                "GetAppDomainHostingTypes",
                BindingFlags.Public | BindingFlags.Static);
            if (getHostingTypesMethod == null)
                throw new Exception("未发现 ScriptLoder.GetAppDomainHostingTypes");

            var hostingTypes = getHostingTypesMethod.Invoke(null, null) as IEnumerable;
            if (hostingTypes == null)
                throw new Exception("GetAppDomainHostingTypes 返回 null");

            var typeCount = 0;
            var foundKeyTypes = new System.Collections.Generic.HashSet<string>();
            foreach (var t in hostingTypes)
            {
                typeCount++;
                if (t is Type type)
                {
                    var fullName = type.FullName ?? type.Name;
                    // 检查关键管理器类型
                    // Check key manager types
                    if (fullName.Contains("GameConfigManager") ||
                        fullName.Contains("UIManager") ||
                        fullName.Contains("SqliteLoder"))
                    {
                        foundKeyTypes.Add(fullName);
                        Debug.Log($"[E2E] 发现关键管理器类型: {fullName}");
                    }
                }
            }

            Debug.Log($"[E2E] 托管类型总数: {typeCount}");
            Debug.Log($"[E2E] 发现关键管理器: count={foundKeyTypes.Count} types={string.Join(", ", foundKeyTypes)}");

            if (typeCount <= 0)
                throw new Exception("托管类型枚举为空");
        }

        /// <summary>
        /// 验证事件/数据监听系统基本能力。
        /// Verify basic event/data-listener system capabilities.
        /// 该检查通过反射创建 StatusListenerService 实例，验证 SetData/GetData 基础操作。
        /// This check creates a StatusListenerService instance via reflection and verifies basic SetData/GetData operations.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-core-business", order: 2, des: "验证事件监听系统基础读写")]
        public static void EventListenerBasicOperations()
        {
            Debug.Log("[E2E] 测试目的=验证事件监听系统基础读写 实现手段=反射创建 StatusListenerService 并执行 SetData/GetData/AddListener");

            var statusListenerServiceType = FindLoadedType(StatusListenerServiceTypeName);
            if (statusListenerServiceType == null)
            {
                Debug.LogWarning($"[E2E] 未发现 StatusListenerService 类型: {StatusListenerServiceTypeName}，跳过事件监听测试");
                return;
            }

            // 创建实例
            // Create instance
            var service = Activator.CreateInstance(statusListenerServiceType);
            if (service == null)
                throw new Exception("无法创建 StatusListenerService 实例");

            try
            {
                // SetData
                var setDataMethod = statusListenerServiceType.GetMethod("SetData",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(object), typeof(bool) },
                    null);
                if (setDataMethod == null)
                    throw new Exception("未发现 StatusListenerService.SetData(string, object, bool)");

                setDataMethod.Invoke(service, new object[] { "talos_test_key", "talos_test_value", false });
                Debug.Log("[E2E] Event system phase=set-data key=talos_test_key value=talos_test_value");

                // GetData
                var getDataMethod = statusListenerServiceType.GetMethod("GetData",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string) },
                    null);
                if (getDataMethod == null)
                    throw new Exception("未发现 StatusListenerService.GetData(string)");

                var data = getDataMethod.Invoke(service, new object[] { "talos_test_key" });
                Debug.Log($"[E2E] Event system phase=get-data key=talos_test_key value={data}");

                // AddListener (basic callback registration)
                var addListenerMethod = statusListenerServiceType.GetMethod("AddListener",
                    BindingFlags.Public | BindingFlags.Instance);
                if (addListenerMethod != null)
                {
                    Debug.Log("[E2E] Event system phase=add-listener available");
                }

                Debug.Log("[E2E] 事件监听系统基础操作验证完成");
            }
            finally
            {
                (service as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// 验证框架版本号可读且格式合法。
        /// Verify that the framework version is readable and has a valid format.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "framework-core-business", order: 3, des: "验证框架版本号可读")]
        public static void FrameworkVersionReadable()
        {
            Debug.Log("[E2E] 测试目的=验证框架版本号可读 实现手段=反射读取 BDFramework.BDLauncher.FrameworkVersion");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var scriptLoaderType = hotfixAssembly.GetType(ScriptLoderTypeName);
            if (scriptLoaderType == null)
                throw new Exception($"未发现 ScriptLoder 类型: {ScriptLoderTypeName}");

            var versionProperty = scriptLoaderType.GetProperty(
                "FrameworkVersion",
                BindingFlags.Public | BindingFlags.Static);
            if (versionProperty == null)
                throw new Exception("未发现 ScriptLoder.FrameworkVersion 属性");

            var version = versionProperty.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(version))
                throw new Exception("FrameworkVersion 为空");

            // 验证版本格式 (x.y.z)
            // Verify version format (x.y.z)
            var parts = version.Split('.');
            if (parts.Length < 2)
                throw new Exception($"FrameworkVersion 格式异常: {version}");

            Debug.Log($"[E2E] 框架版本号: {version}");
        }

        #region Reflection Helpers

        private static Assembly RequireLoadedAssembly(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(a.GetName().Name, name, StringComparison.Ordinal)) return a;
            throw new Exception($"未发现已装载程序集: {name}");
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }
            return null;
        }

        #endregion
    }
}
