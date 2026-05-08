using System;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Game.E2E
{
    /// <summary>
    /// 版本控制器业务能力 E2E 测试套件。
    /// Version controller business-capability E2E test suite.
    /// 验证客户端版本号读取、版本路径解析和版本信息结构。
    /// Verify client version reading, versioned path resolution, and version info structure.
    /// </summary>
    [Preserve]
    public static class VersionBusinessTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string GameConfigManagerTypeName = "BDFramework.Configure.GameConfigManager";
        private const string GameBaseConfigProcessorTypeName = "BDFramework.Configure.GameBaseConfigProcessor";
        private const string ClientAssetsUtilsTypeName = "BDFramework.Asset.ClientAssetsUtils";
        private const string AssetsVersionInfoTypeName = "BDFramework.ResourceMgr.AssetsVersionInfo";

        /// <summary>
        /// 验证客户端版本号可从配置中正确读取。
        /// Verify that the client version number can be correctly read from configuration.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 1, des: "验证客户端版本号可读")]
        public static void ClientVersionReadable()
        {
            Debug.Log("[E2E] 测试目的=验证客户端版本号可读 实现手段=反射读取 GameBaseConfigProcessor.Config.ClientVersionNum");

            var configNestedType = GetGameBaseConfigNestedType();
            var config = GetConfigInstance(configNestedType);
            var clientVersionNum = ReadField<string>(config, configNestedType, "ClientVersionNum");
            if (string.IsNullOrWhiteSpace(clientVersionNum))
                throw new Exception("ClientVersionNum 为空");

            Debug.Log($"[E2E] 客户端版本号: {clientVersionNum}");
        }

        /// <summary>
        /// 验证带版本号的资源路径解析正确。
        /// Verify that asset path resolution with version number is correct.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 2, des: "验证版本号资源路径解析")]
        public static void AssetPathResolutionWithVersion()
        {
            Debug.Log("[E2E] 测试目的=验证版本号资源路径解析 实现手段=反射调用 ClientAssetsUtils.GetMultiAssetsLoadPath 并验证主备路径");

            var configNestedType = GetGameBaseConfigNestedType();
            var config = GetConfigInstance(configNestedType);
            var clientVersion = ReadField<string>(config, configNestedType, "ClientVersionNum");
            if (string.IsNullOrWhiteSpace(clientVersion))
                throw new Exception("ClientVersionNum 为空");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);
            var runtimePlatform = (RuntimePlatform)bApplicationType.GetProperty("RuntimePlatform", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);

            var clientAssetsUtilsType = RequireLoadedType(ClientAssetsUtilsTypeName);
            var getPathMethod = clientAssetsUtilsType.GetMethod("GetMultiAssetsLoadPath",
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(RuntimePlatform), typeof(string) }, null);
            if (getPathMethod == null) throw new Exception("未发现 ClientAssetsUtils.GetMultiAssetsLoadPath");

            var result = getPathMethod.Invoke(null, new object[] { runtimePlatform, clientVersion });
            if (result == null) throw new Exception("GetMultiAssetsLoadPath 返回 null");

            // ValueTuple Item1/Item2 是字段(field)而非属性(property)，
            // IL2CPP 下 GetProperty 返回 null，必须使用 GetField。
            // ValueTuple Item1/Item2 are fields, not properties;
            // IL2CPP returns null from GetProperty, so GetField is required.
            var resultType = result.GetType();
            var firstPath = (resultType.GetField("Item1")?.GetValue(result) as string) ?? "";
            var secondPath = (resultType.GetField("Item2")?.GetValue(result) as string) ?? "";

            if (string.IsNullOrEmpty(firstPath) && string.IsNullOrEmpty(secondPath))
                throw new Exception("资源路径解析失败：主路径和备用路径均为空");

            Debug.Log($"[E2E] 版本号资源路径: clientVersion={clientVersion} platform={runtimePlatform}");
            Debug.Log($"[E2E] 主路径: {firstPath}");
            Debug.Log($"[E2E] 备用路径: {secondPath}");
        }

        /// <summary>
        /// 验证版本信息结构字段完整。
        /// Verify that the version info structure fields are complete.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "version-business", order: 3, des: "验证版本信息结构字段")]
        public static void VersionInfoStructureValidation()
        {
            Debug.Log("[E2E] 测试目的=验证版本信息结构字段 实现手段=反射读取 AssetsVersionInfo 类型并验证 Platfrom/Version/SubPckMap 属性");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var assetsVersionInfoType = hotfixAssembly.GetType(AssetsVersionInfoTypeName);
            if (assetsVersionInfoType == null)
                throw new Exception($"未发现 AssetsVersionInfo 类型: {AssetsVersionInfoTypeName}");

            var platfromProp = assetsVersionInfoType.GetProperty("Platfrom")
                ?? throw new Exception("未发现 AssetsVersionInfo.Platfrom");
            var versionProp = assetsVersionInfoType.GetProperty("Version")
                ?? throw new Exception("未发现 AssetsVersionInfo.Version");
            var subPckMapProp = assetsVersionInfoType.GetProperty("SubPckMap")
                ?? throw new Exception("未发现 AssetsVersionInfo.SubPckMap");

            var versionInfo = Activator.CreateInstance(assetsVersionInfoType);
            platfromProp.SetValue(versionInfo, "TalosTest");
            versionProp.SetValue(versionInfo, "1.0.0");

            if (!string.Equals(platfromProp.GetValue(versionInfo) as string, "TalosTest", StringComparison.Ordinal))
                throw new Exception("Platfrom 读写异常");
            if (!string.Equals(versionProp.GetValue(versionInfo) as string, "1.0.0", StringComparison.Ordinal))
                throw new Exception("Version 读写异常");

            Debug.Log("[E2E] 版本信息结构验证: Platfrom=TalosTest Version=1.0.0");
        }

        #region Shared Config Helpers

        private static Type GetGameBaseConfigNestedType()
        {
            var gameConfigManagerType = FindLoadedType(GameConfigManagerTypeName)
                ?? throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            var inst = gameConfigManagerType.GetProperty("Inst", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null)
                ?? throw new Exception("GameConfigManager.Inst 为空");

            var getConfigMethod = gameConfigManagerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new Exception("未发现 GameConfigManager.GetConfig");

            var gameBaseConfigProcessorType = FindLoadedType(GameBaseConfigProcessorTypeName)
                ?? throw new Exception($"未发现 GameBaseConfigProcessor 类型: {GameBaseConfigProcessorTypeName}");
            var configNestedType = gameBaseConfigProcessorType.GetNestedType("Config")
                ?? throw new Exception("未发现 GameBaseConfigProcessor.Config");

            // Store in static fields for reuse
            _cachedConfigManagerInstance = inst;
            _cachedGetConfigMethod = getConfigMethod;
            _cachedConfigNestedType = configNestedType;
            return configNestedType;
        }

        private static object _cachedConfigManagerInstance;
        private static MethodInfo _cachedGetConfigMethod;
        private static Type _cachedConfigNestedType;

        private static object GetConfigInstance(Type configNestedType)
        {
            if (_cachedConfigNestedType == configNestedType && _cachedConfigManagerInstance != null && _cachedGetConfigMethod != null)
            {
                var getConfigGeneric = _cachedGetConfigMethod.MakeGenericMethod(configNestedType);
                var config = getConfigGeneric.Invoke(_cachedConfigManagerInstance, null);
                if (config == null) throw new Exception("GameBaseConfigProcessor.Config 配置实例为空");
                return config;
            }
            throw new Exception("配置未初始化，请先调用 GetGameBaseConfigNestedType");
        }

        private static T ReadField<T>(object instance, Type type, string fieldName)
        {
            var field = type.GetField(fieldName) ?? throw new Exception($"未发现字段: {fieldName}");
            return (T)field.GetValue(instance);
        }

        #endregion

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

        private static Type RequireLoadedType(string typeName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }
            throw new Exception($"未发现已装载类型: {typeName}");
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
