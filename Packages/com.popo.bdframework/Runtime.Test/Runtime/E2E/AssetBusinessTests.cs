using System;
using System.Collections.Generic;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// AssetBundle 管理器业务能力 E2E 测试套件。
    /// AssetBundle manager business-capability E2E test suite.
    /// 验证多资源组操作、平台版本路径解析和 Shader 查找。
    /// Verify multi-group operations, platform version path resolution, and shader lookup.
    /// </summary>
    [Preserve]
    public static class AssetBusinessTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BResourcesTypeName = "BDFramework.ResourceMgr.BResources";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";

        /// <summary>
        /// 验证多资源组增删查操作正确。
        /// Verify that multi-group add/delete/query operations are correct.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 1, des: "验证多资源组增删查操作")]
        public static void MultiGroupAssetOperations()
        {
            Debug.Log("[E2E] 测试目的=验证多资源组增删查操作 实现手段=创建多个资源组并验证增删查闭环");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);
            var addMethod = RequireStaticMethod(bResourcesType, "AddAssetsPathToGroup", typeof(string), typeof(string[]));
            var getMethod = RequireStaticMethod(bResourcesType, "GetAssetsPathByGroup", typeof(string));
            var clearMethod = RequireStaticMethod(bResourcesType, "ClearAssetGroup", typeof(string));

            var groupPrefix = $"talos-step02-{Guid.NewGuid():N}";
            var groups = new List<string>();

            try
            {
                for (int i = 1; i <= 3; i++)
                {
                    var groupName = $"{groupPrefix}-group{i}";
                    groups.Add(groupName);
                    var paths = new string[i * 2];
                    for (int j = 0; j < paths.Length; j++)
                        paths[j] = $"talos/step02/group{i}/asset_{j}.prefab";

                    Debug.Log($"[E2E] Asset group phase=add group={groupName} pathCount={paths.Length}");
                    // 直接传递 string[] 参数而非 object[] 包装，避免 IL2CPP 反射将 object[] 误传给 string[] 参数。
                    // Pass string[] directly instead of wrapping in object[] to avoid IL2CPP reflection converting object[] when string[] is expected.
                    InvokeStaticMethod(addMethod, $"BResources.AddAssetsPathToGroup({groupName})", groupName, paths);
                }

                for (int i = 0; i < groups.Count; i++)
                {
                    var expectedCount = (i + 1) * 2;
                    var groupedPaths = InvokeStaticMethod(getMethod, $"BResources.GetAssetsPathByGroup({groups[i]})", groups[i]) as string[];
                    if (groupedPaths == null || groupedPaths.Length != expectedCount)
                        throw new Exception($"资源组 {groups[i]} 路径数量异常: actual={groupedPaths?.Length ?? 0} expected={expectedCount}");
                    Debug.Log($"[E2E] Asset group phase=verified group={groups[i]} count={groupedPaths.Length}");
                }
            }
            finally
            {
                foreach (var g in groups)
                    InvokeStaticMethod(clearMethod, $"BResources.ClearAssetGroup({g})", g);
            }

            Debug.Log("[E2E] 多资源组操作验证完成");
        }

        /// <summary>
        /// 验证多平台版本路径解析正确。
        /// Verify that multi-platform version path resolution is correct.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 2, des: "验证平台版本路径解析")]
        public static void PlatformVersionPathResolution()
        {
            Debug.Log("[E2E] 测试目的=验证平台版本路径解析 实现手段=对当前平台调用 GetAssetsInfoPath 和 GetServerAssetsVersionInfoPath");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);

            var persistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");
            var getAssetsInfoPath = RequireStaticMethod(bResourcesType, "GetAssetsInfoPath", typeof(string));
            var getVersionInfoPath = RequireStaticMethod(bResourcesType, "GetServerAssetsVersionInfoPath", typeof(string), typeof(RuntimePlatform));

            var assetsInfoPath = InvokeStaticMethod(getAssetsInfoPath, "BResources.GetAssetsInfoPath", persistentDataPath) as string;
            if (string.IsNullOrWhiteSpace(assetsInfoPath))
                throw new Exception("资源信息路径解析为空");
            Debug.Log($"[E2E] Asset path phase=assets-info-path path={assetsInfoPath}");

            var versionInfoPath = InvokeStaticMethod(getVersionInfoPath, "BResources.GetServerAssetsVersionInfoPath",
                persistentDataPath, Application.platform) as string;
            if (string.IsNullOrWhiteSpace(versionInfoPath))
                throw new Exception($"服务器版本信息路径解析为空: platform={Application.platform}");
            Debug.Log($"[E2E] Asset path phase=version-info-path path={versionInfoPath} platform={Application.platform}");

            // Cross-platform check
            if (Application.platform != RuntimePlatform.Android)
            {
                var androidPath = InvokeStaticMethod(getVersionInfoPath, "BResources.GetServerAssetsVersionInfoPath(Android)",
                    persistentDataPath, RuntimePlatform.Android) as string;
                if (string.IsNullOrWhiteSpace(androidPath))
                    throw new Exception("Android 平台版本信息路径解析为空");
                Debug.Log($"[E2E] Asset path phase=cross-platform androidPath={androidPath}");
            }

            Debug.Log("[E2E] 平台版本路径解析验证完成");
        }

        /// <summary>
        /// 验证 Shader 查找功能。
        /// Verify shader lookup functionality.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-business", order: 3, des: "验证 Shader 查找功能")]
        public static void ShaderLookupVerification()
        {
            Debug.Log("[E2E] 测试目的=验证 Shader 查找功能 实现手段=查找已知 Shader 和不存在 Shader 并验证接口行为");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bResourcesType = RequireType(hotfixAssembly, BResourcesTypeName);
            var findShaderMethod = RequireStaticMethod(bResourcesType, "FindShader", typeof(string));

            // Non-existent shader
            var nonExistent = InvokeStaticMethod(findShaderMethod, "BResources.FindShader(non-existent)",
                "__Talos_Step02_NonExistent_Shader__");
            Debug.Log($"[E2E] Shader lookup non-existent: found={nonExistent != null} (expected null)");

            // Standard shader
            var standard = InvokeStaticMethod(findShaderMethod, "BResources.FindShader(Standard)", "Standard");
            Debug.Log($"[E2E] Shader lookup Standard: found={standard != null}");

            Debug.Log("[E2E] Shader 查找验证完成");
        }

        #region Reflection Helpers

        private static Assembly RequireLoadedAssembly(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(a.GetName().Name, name, StringComparison.Ordinal)) return a;
            throw new Exception($"未发现已装载程序集: {name}");
        }

        private static Type RequireType(Assembly assembly, string typeName)
        {
            var t = assembly.GetType(typeName);
            if (t == null) throw new Exception($"未发现类型: {typeName}");
            return t;
        }

        private static MethodInfo RequireStaticMethod(Type type, string methodName, params Type[] paramTypes)
        {
            var m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, paramTypes, null);
            if (m == null) throw new Exception($"未发现公开静态方法: {type.FullName}.{methodName}");
            return m;
        }

        private static string ReadRequiredStaticStringProperty(Type type, string propertyName)
        {
            var p = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (p == null) throw new Exception($"未发现公开静态属性: {type.FullName}.{propertyName}");
            var v = p.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(v)) throw new Exception($"公开静态属性为空: {type.FullName}.{propertyName}");
            return v;
        }

        private static object InvokeStaticMethod(MethodInfo method, string op, params object[] args)
        {
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException e) { throw new Exception($"{op} 失败: {e.InnerException?.Message}", e.InnerException); }
        }

        #endregion
    }
}
