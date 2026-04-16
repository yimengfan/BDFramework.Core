using System.IO;
using UnityEngine;

namespace Talos.E2E
{
    /// <summary>
    /// DEBUG 构建标记检测器。
    /// DEBUG build marker detector.
    /// 该类型只负责解析 Talos E2E 自身的标记文件位置，并允许宿主注册额外目录；
    /// 它不直接依赖任何业务框架的路径 API 或配置系统。
    /// This type only resolves Talos E2E marker locations and allows hosts to register additional directories;
    /// it does not directly depend on any game-framework path APIs or configuration systems.
    /// </summary>
    static public class DebugBuildMarker
    {
        /// <summary>
        /// 标记文件名。
        /// Marker file name.
        /// </summary>
        public const string MARKER_FILENAME = "DEBUG";

        /// <summary>
        /// 缓存是否为 Debug 构建的结果，避免重复检测文件系统。
        /// Cached DEBUG-build result to avoid repeated filesystem probing.
        /// </summary>
        private static bool? _isDebugBuild;

        /// <summary>
        /// 检测当前是否为 DEBUG 构建。
        /// Detect whether the current build is a DEBUG build.
        /// 该方法只检查编译宏和 Talos E2E 自身约定的 StreamingAssets 标记位置，
        /// 不解释任何宿主框架的配置、持久化目录或第三方初始化状态。
        /// This method checks only compile-time symbols and the StreamingAssets marker location defined by Talos E2E itself,
        /// and does not interpret any host-framework configuration, persistent-data directories, or third-party initialization state.
        /// </summary>
        /// <returns>如果是 DEBUG 构建返回 true，否则返回 false。 Returns true when the current build is a DEBUG build; otherwise false.</returns>
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

            var markerPath = Path.Combine(GetStreamingMarkerDirectory(), MARKER_FILENAME);
            if (File.Exists(markerPath))
            {
                _isDebugBuild = true;
                Debug.Log($"[TalosE2E] 检测到 DEBUG 标记: {markerPath}");
                return true;
            }

            _isDebugBuild = false;
            Debug.Log("[TalosE2E] 未检测到 DEBUG 标记，当前为 Release 构建");
            return false;
        }

        /// <summary>
        /// 获取默认 StreamingAssets 标记目录。
        /// Get the default StreamingAssets marker directory.
        /// </summary>
        public static string GetStreamingMarkerDirectory()
        {
            return Path.Combine(Application.streamingAssetsPath, GetPlatformDirectory(Application.platform), "script", "hotfix");
        }

        /// <summary>
        /// 将 Unity 运行平台映射为 Talos E2E 约定的目录名。
        /// Map a Unity runtime platform to the directory name used by Talos E2E.
        /// </summary>
        public static string GetPlatformDirectory(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "osx";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                default:
                    return platform.ToString().Replace("Editor", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>
        /// 重置缓存，强制下次重新检测。
        /// Reset the cached result so the next probe recomputes it.
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
