using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Talos.E2E.Editor
{
    /// <summary>
    /// E2E 场景自动装配器。
    /// E2E scene auto-setup utility.
    /// 
    /// 设计角色：
    /// - 在 Editor Debug 模式下，自动扫描当前场景是否挂载了 E2ESceneAutoStarter 组件。
    /// - 如果未找到，自动创建 "[TalosE2E]" GameObject 并挂载 E2ESceneAutoStarter。
    /// - 完全在 E2E 包内闭环，不依赖 BDFramework 的任何脚本。
    /// 
    /// Design role:
    /// - In Editor Debug mode, auto-scans the current scene for E2ESceneAutoStarter component.
    /// - If not found, auto-creates a "[TalosE2E]" GameObject and attaches E2ESceneAutoStarter.
    /// - Fully self-contained within the E2E package, with zero dependency on BDFramework scripts.
    /// 
    /// 触发时机：
    /// - Editor 初始化时（[InitializeOnLoad]）
    /// - 进入 PlayMode 前
    /// - 场景打开时
    /// 
    /// Trigger timing:
    /// - Editor initialization ([InitializeOnLoad])
    /// - Before entering PlayMode
    /// - When a scene is opened
    /// </summary>
    [InitializeOnLoad]
    public static class E2ESceneAutoSetup
    {
        /// <summary>
        /// E2E 自动装配节点名。
        /// The name of the E2E auto-setup node.
        /// </summary>
        public const string E2ENodeName = "[TalosE2E]";

        /// <summary>
        /// EditorPrefs 键：是否禁用自动装配。
        /// EditorPrefs key: whether auto-setup is disabled.
        /// </summary>
        private const string PREFS_DISABLE_AUTO_SETUP = "TalosE2E_DisableAutoSetup";

        static E2ESceneAutoSetup()
        {
            // Phase 1: 注册生命周期钩子。
            // Phase 1: Register lifecycle hooks.
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        /// <summary>
        /// 是否启用自动装配。
        /// Whether auto-setup is enabled.
        /// </summary>
        public static bool IsAutoSetupEnabled => !EditorPrefs.GetBool(PREFS_DISABLE_AUTO_SETUP, false);

        /// <summary>
        /// 设置是否启用自动装配。
        /// Set whether auto-setup is enabled.
        /// </summary>
        public static void SetAutoSetupEnabled(bool enabled)
        {
            EditorPrefs.SetBool(PREFS_DISABLE_AUTO_SETUP, !enabled);
        }

        /// <summary>
        /// PlayMode 状态变化回调。
        /// PlayMode state change callback.
        /// 在即将进入 PlayMode 时扫描并装配 E2E 节点。
        /// Scans and sets up the E2E node right before entering PlayMode.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && IsAutoSetupEnabled)
            {
                EnsureE2ENodeInActiveScene();
            }
        }

        /// <summary>
        /// 场景打开回调。
        /// Scene opened callback.
        /// 在场景打开时扫描并装配 E2E 节点。
        /// Scans and sets up the E2E node when a scene is opened.
        /// </summary>
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (IsAutoSetupEnabled)
            {
                EnsureE2ENodeInScene(scene);
            }
        }

        /// <summary>
        /// 确保当前活动场景中存在 E2E 自动启动节点。
        /// Ensure the E2E auto-start node exists in the currently active scene.
        /// </summary>
        public static void EnsureE2ENodeInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[TalosE2E] 当前无有效活动场景，跳过 E2E 节点装配");
                return;
            }

            EnsureE2ENodeInScene(scene);
        }

        /// <summary>
        /// 确保指定场景中存在 E2E 自动启动节点。
        /// Ensure the E2E auto-start node exists in the specified scene.
        /// 仅在 Debug 模式（检测 DEBUG 标记或 Editor 环境）下执行装配。
        /// Only performs setup in Debug mode (detected via DEBUG marker or Editor environment).
        /// </summary>
        private static void EnsureE2ENodeInScene(Scene scene)
        {
            // Phase 1: 仅在 Debug 模式下装配。
            // Phase 1: Only setup in Debug mode.
            if (!IsDebugMode())
            {
                return;
            }

            // Phase 2: 检查场景中是否已存在 E2ESceneAutoStarter 组件。
            // Phase 2: Check if E2ESceneAutoStarter component already exists in the scene.
            var existing = Object.FindObjectOfType<E2ESceneAutoStarter>();
            if (existing != null)
            {
                Debug.Log($"[TalosE2E] 场景中已存在 E2ESceneAutoStarter（GameObject: {existing.gameObject.name}），跳过自动装配");
                return;
            }

            // Phase 3: 创建 E2E 节点并挂载组件。
            // Phase 3: Create E2E node and attach component.
            var e2eGo = new GameObject(E2ENodeName);
            SceneManager.MoveGameObjectToScene(e2eGo, scene);
            e2eGo.AddComponent<E2ESceneAutoStarter>();

            // 标记场景为脏，提示用户保存。
            // Mark scene dirty to prompt the user to save.
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[TalosE2E] 已在场景 '{scene.name}' 中自动创建 E2E 节点 '{E2ENodeName}' 并挂载 E2ESceneAutoStarter");
        }

        /// <summary>
        /// 判断当前是否为 Debug 模式。
        /// Determine whether the current mode is Debug mode.
        /// Editor 环境下始终视为 Debug 模式（开发者可通过禁用自动装配来跳过）。
        /// In the Editor, always treated as Debug mode (developers can skip by disabling auto-setup).
        /// </summary>
        private static bool IsDebugMode()
        {
            // Editor 环境默认视为 Debug 模式。
            // Editor environment is treated as Debug mode by default.
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                // 非 PlayMode 时检查 StreamingAssets 中的 DEBUG 标记文件。
                // Check the DEBUG marker file in StreamingAssets when not in PlayMode.
                var markerPath = Path.Combine(DebugBuildMarker.GetStreamingMarkerDirectory(), DebugBuildMarker.MARKER_FILENAME);
                if (File.Exists(markerPath))
                {
                    return true;
                }
#if ENABLE_E2ETEST
                return true;
#endif
                // Editor 环境下，如果既没有标记也没有编译宏，仍返回 true（开发者通常期望 E2E 在 Editor 可用）。
                // In Editor, if neither marker nor compile symbols are present, still return true
                // (developers typically expect E2E to be available in Editor).
                return true;
            }

            return DebugBuildMarker.IsDebugBuild();
        }

        /// <summary>
        /// 菜单项：为当前场景装配 E2E 节点。
        /// Menu item: set up the E2E node for the current scene.
        /// </summary>
        [MenuItem("Talos/E2E Test/为当前场景装配 E2E 节点")]
        public static void SetupE2ENodeInCurrentScene()
        {
            EnsureE2ENodeInActiveScene();
            Debug.Log("[TalosE2E] 手动装配完成");
        }

        /// <summary>
        /// 菜单项：从当前场景移除所有 E2E 节点。
        /// Menu item: remove all E2E nodes from the current scene.
        /// </summary>
        [MenuItem("Talos/E2E Test/从当前场景移除 E2E 节点")]
        public static void RemoveE2ENodesFromCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[TalosE2E] 当前无有效活动场景");
                return;
            }

            var starters = Object.FindObjectsOfType<E2ESceneAutoStarter>();
            if (starters.Length == 0)
            {
                Debug.Log("[TalosE2E] 当前场景中无 E2E 节点");
                return;
            }

            foreach (var starter in starters)
            {
                Debug.Log($"[TalosE2E] 移除 E2E 节点: {starter.gameObject.name}");
                Object.DestroyImmediate(starter.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[TalosE2E] 已从场景 '{scene.name}' 移除 {starters.Length} 个 E2E 节点");
        }

        /// <summary>
        /// 菜单项：切换自动装配开关。
        /// Menu item: toggle auto-setup.
        /// </summary>
        [MenuItem("Talos/E2E Test/切换 E2E 自动装配")]
        public static void ToggleAutoSetup()
        {
            bool current = IsAutoSetupEnabled;
            SetAutoSetupEnabled(!current);
            Debug.Log($"[TalosE2E] E2E 自动装配已{(IsAutoSetupEnabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 菜单项校验：显示当前自动装配状态。
        /// Menu item validation: show current auto-setup status.
        /// </summary>
        [MenuItem("Talos/E2E Test/切换 E2E 自动装配", true)]
        public static bool ToggleAutoSetupValidate()
        {
            Menu.SetChecked("Talos/E2E Test/切换 E2E 自动装配", IsAutoSetupEnabled);
            return true;
        }
    }
}
