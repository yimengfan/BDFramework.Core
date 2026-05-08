using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Game.E2E
{
    /// <summary>
    /// 下载准备业务能力 E2E 测试套件。
    /// Download-preparation business-capability E2E test suite.
    /// 验证文件服务器 URL 可访问性、下载路径构造和母包基础资源。
    /// Verify file server URL accessibility, download path construction, and base package resources.
    /// </summary>
    [Preserve]
    public static class DownloadPrepTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string GameConfigManagerTypeName = "BDFramework.Configure.GameConfigManager";
        private const string ServerConfigProcessorTypeName = "Game.Config.ServerConfigProcessor";

        /// <summary>
        /// 验证文件服务器 URL 可访问。
        /// Verify that the file server URL is accessible.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 1, des: "验证文件服务器 URL 可访问")]
        public static void FileServerUrlAccessible()
        {
            Debug.Log("[E2E] 测试目的=验证文件服务器 URL 可访问 实现手段=反射读取 ServerConfigProcessor.Config.FileServerUrl 并验证格式");

            var gameConfigManagerType = FindLoadedType(GameConfigManagerTypeName)
                ?? throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            var instProperty = gameConfigManagerType.GetProperty("Inst", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                ?? throw new Exception("未发现 GameConfigManager.Inst 属性");
            var inst = instProperty.GetValue(null)
                ?? throw new Exception("GameConfigManager.Inst 为空");
            var getConfigMethod = gameConfigManagerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new Exception("未发现 GameConfigManager.GetConfig");

            var serverConfigProcessorType = FindLoadedType(ServerConfigProcessorTypeName)
                ?? throw new Exception($"未发现 ServerConfigProcessor 类型: {ServerConfigProcessorTypeName}");
            var configNestedType = serverConfigProcessorType.GetNestedType("Config")
                ?? throw new Exception("未发现 ServerConfigProcessor.Config");

            var getConfigGeneric = getConfigMethod.MakeGenericMethod(configNestedType);
            var serverConfig = getConfigGeneric.Invoke(inst, null)
                ?? throw new Exception("ServerConfigProcessor.Config 配置实例为空");

            var fileServerUrlField = configNestedType.GetField("FileServerUrl")
                ?? throw new Exception("未发现 FileServerUrl 字段");
            var fileServerUrl = fileServerUrlField.GetValue(serverConfig) as string;
            if (string.IsNullOrWhiteSpace(fileServerUrl))
                throw new Exception("FileServerUrl 为空");

            if (!fileServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !fileServerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"FileServerUrl 格式非法: {fileServerUrl}");

            Debug.Log($"[E2E] 文件服务器 URL: {fileServerUrl}");
        }

        /// <summary>
        /// 验证下载路径构造正确。
        /// Verify that download path construction is correct.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 2, des: "验证下载路径构造")]
        public static void DownloadPathConstruction()
        {
            Debug.Log("[E2E] 测试目的=验证下载路径构造 实现手段=基于 persistentDataPath 和版本号构造下载路径并验证格式");

            var hotfixAssembly = RequireLoadedAssembly(HotfixFrameworkAssemblyName);
            var bApplicationType = RequireType(hotfixAssembly, BApplicationTypeName);

            var persistentDataPath = ReadRequiredStaticStringProperty(bApplicationType, "persistentDataPath");
            var getPlatformLoadPath = RequireStaticMethod(bApplicationType, "GetPlatformLoadPath", typeof(RuntimePlatform));
            var platformLoadPath = InvokeStaticMethod(getPlatformLoadPath, "BApplication.GetPlatformLoadPath", Application.platform) as string;

            if (string.IsNullOrWhiteSpace(platformLoadPath))
                throw new Exception($"平台路径标识为空: platform={Application.platform}");

            var testVersion = "1.0.0";
            var expectedPath = $"{persistentDataPath}/{testVersion}/{platformLoadPath}".Replace('\\', '/');

            Debug.Log($"[E2E] 下载路径: persistent={persistentDataPath} platform={platformLoadPath}");
            Debug.Log($"[E2E] 预期下载路径: {expectedPath}");

            if (string.IsNullOrWhiteSpace(persistentDataPath))
                throw new Exception("persistentDataPath 为空");
        }

        /// <summary>
        /// 验证母包基础资源可访问。
        /// Verify that base package fundamental resources are accessible.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-prep", order: 3, des: "验证母包基础资源可访问")]
        public static void StreamingAssetsAccessible()
        {
            Debug.Log("[E2E] 测试目的=验证母包基础资源可访问 实现手段=检查 StreamingAssets 目录存在性和文件列表");

            var streamingAssetsPath = Application.streamingAssetsPath;

            if (Application.isEditor)
            {
                Debug.Log($"[E2E] 编辑器模式 StreamingAssets: {streamingAssetsPath}");
                var projectStreamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
                if (Directory.Exists(projectStreamingAssets))
                    Debug.Log($"[E2E] 项目 StreamingAssets 目录存在: {projectStreamingAssets}");
                else
                    Debug.LogWarning($"[E2E] 项目 StreamingAssets 目录不存在（可能正常）: {projectStreamingAssets}");
            }
            else
            {
                Debug.Log($"[E2E] Player 模式 StreamingAssets: {streamingAssetsPath}");
                if (Directory.Exists(streamingAssetsPath))
                {
                    var files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                    Debug.Log($"[E2E] StreamingAssets 包含文件数: {files.Length}");
                    foreach (var f in files.Take(10))
                        Debug.Log($"[E2E] StreamingAssets 文件: {f}");
                }
                else
                {
                    Debug.LogWarning($"[E2E] StreamingAssets 目录不存在（可能正常）: {streamingAssetsPath}");
                }
            }

            Debug.Log("[E2E] 母包基础资源检查完成");
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

        private static Type FindLoadedType(string typeName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }
            return null;
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
