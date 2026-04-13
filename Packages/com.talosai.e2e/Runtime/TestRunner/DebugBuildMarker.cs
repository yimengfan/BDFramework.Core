using System.IO;
using UnityEngine;
using BDFramework.Configure;
using BDFramework.Core.Tools;

namespace Talos.E2E
{
    /// <summary>
    /// DEBUG 构建标记系统——检测和验证当前是否为 Debug 构建包体。
    /// 
    /// 设计角色：
    /// - 在 CI 构建 Debug 包时，在热更资源目录下创建 DEBUG 标记文件。
    /// - 运行时检测该标记，自动启用 E2E 测试能力。
    /// - 防止 Debug 构建被意外发布为 Release。
    /// 
    /// 标记文件位置：
    /// - {persistentDataPath}/{clientVersion}/{platform}/script/hotfix/DEBUG
    /// - 或 StreamingAssets/{platform}/script/hotfix/DEBUG
    /// 
    /// 使用示例：
    /// <code>
    /// if (DebugBuildMarker.IsDebugBuild())
    /// {
    ///     TalosE2EBootstrap.LaunchE2E();
    /// }
    /// </code>
    /// </summary>
    static public class DebugBuildMarker
    {
        /// <summary>
        /// 标记文件名。
        /// </summary>
        public const string MARKER_FILENAME = "DEBUG";

        /// <summary>
        /// 缓存是否为 Debug 构建的结果，避免重复检测文件系统。
        /// </summary>
        private static bool? _isDebugBuild;

        /// <summary>
        /// 检测当前是否为 Debug 构建。
        /// 通过在热更脚本目录中查找 DEBUG 标记文件判断。
        /// </summary>
        /// <returns>如果是 Debug 构建返回 true，否则返回 false。</returns>
        static public bool IsDebugBuild()
        {
            if (_isDebugBuild.HasValue)
            {
                return _isDebugBuild.Value;
            }

            // 检查条件编译宏
#if ENABLE_E2ETEST
            _isDebugBuild = true;
            Debug.Log("[TalosE2E] 检测到 ENABLE_E2ETEST 宏，确认为 Debug 构建");
            return true;
#endif

#if ENABLE_DEBUG && !UNITY_EDITOR
            _isDebugBuild = true;
            Debug.Log("[TalosE2E] 检测到 ENABLE_DEBUG 宏，确认为 Debug 构建");
            return true;
#endif

            // 检查 StreamingAssets 中的标记文件
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            var streamingPath = Path.Combine(Application.streamingAssetsPath, platform, "script", "hotfix", MARKER_FILENAME);
            if (File.Exists(streamingPath))
            {
                _isDebugBuild = true;
                Debug.Log($"[TalosE2E] 检测到 StreamingAssets 中的 DEBUG 标记: {streamingPath}");
                return true;
            }

            // 检查 persistentDataPath 中的标记文件
            var config = BDFramework.Configure.GameConfigManager.Inst?.GetConfig<GameBaseConfigProcessor.Config>();
            if (config != null)
            {
                var persistentPath = Path.Combine(
                    Application.persistentDataPath,
                    config.ClientVersionNum,
                    platform,
                    "script", "hotfix", MARKER_FILENAME);
                if (File.Exists(persistentPath))
                {
                    _isDebugBuild = true;
                    Debug.Log($"[TalosE2E] 检测到 persistentDataPath 中的 DEBUG 标记: {persistentPath}");
                    return true;
                }
            }

            _isDebugBuild = false;
            Debug.Log("[TalosE2E] 未检测到 DEBUG 标记，当前为 Release 构建");
            return false;
        }

        /// <summary>
        /// 重置缓存，强制下次重新检测。
        /// </summary>
        static public void ResetCache()
        {
            _isDebugBuild = null;
        }

        /// <summary>
        /// 在指定目录下创建 DEBUG 标记文件。
        /// 由 CI 构建脚本在 Debug 构建时调用。
        /// </summary>
        /// <param name="directory">标记文件所在目录（热更脚本目录）。</param>
        static public void CreateMarker(string directory)
        {
            var markerPath = Path.Combine(directory, MARKER_FILENAME);
            var content = $"Talos E2E Debug Build\nCreated: {System.DateTimeOffset.UtcNow:O}\nWARNING: This build is NOT for release distribution.";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(markerPath, content);
            Debug.Log($"[TalosE2E] DEBUG 标记已创建: {markerPath}");
        }
    }
}
