using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 场景自启动组件。
    /// E2E scene auto-start MonoBehaviour.
    /// 
    /// 设计角色：
    /// - 挂载到场景中的 GameObject 上，在 Awake 时自动检测并启动 E2E 测试系统。
    /// - 完全替代 BDFramework 中的 E2E 启动调用，实现框架层解耦。
    /// - 在 Debug 构建中自动激活；在 Release 构建中静默跳过。
    /// 
    /// Design role:
    /// - Attach to a scene GameObject; auto-detects and starts the E2E test system on Awake.
    /// - Fully replaces the E2E startup call in BDFramework, achieving framework-layer decoupling.
    /// - Auto-activates in Debug builds; silently skips in Release builds.
    /// 
    /// 使用方式：
    /// 1. 在场景中创建空 GameObject（推荐命名 "[TalosE2E]"）。
    /// 2. 挂载此组件，配置端口（默认使用平台默认端口）。
    /// 3. 进入 PlayMode 后自动启动 E2E 服务。
    /// 
    /// Usage:
    /// 1. Create an empty GameObject in the scene (recommended name "[TalosE2E]").
    /// 2. Attach this component, configure port (defaults to platform default port).
    /// 3. E2E service auto-starts when entering PlayMode.
    /// 
    /// Editor 辅助：
    /// 在 Editor Debug 模式下，E2ESceneAutoSetup 会自动扫描场景并补挂此组件。
    /// 
    /// Editor assistance:
    /// In Editor Debug mode, E2ESceneAutoSetup auto-scans the scene and attaches this component if missing.
    /// </summary>
    [Preserve]
    public class E2ESceneAutoStarter : MonoBehaviour
    {
        /// <summary>
        /// E2E 服务 TCP 监听端口。0 表示使用平台默认端口。
        /// TCP listen port for the E2E service. 0 means use the platform default port.
        /// </summary>
        [SerializeField]
        [Tooltip("TCP 监听端口，0 表示使用平台默认端口。TCP listen port; 0 means use the platform default port.")]
        private int port = 0;

        /// <summary>
        /// 是否已完成启动（防止 Awake 和外部重复调用）。
        /// Whether startup has already completed (prevents duplicate calls from Awake and external callers).
        /// </summary>
        private static bool hasStarted = false;

        /// <summary>
        /// 当前场景中的自启动组件实例。
        /// The auto-starter component instance in the current scene.
        /// </summary>
        public static E2ESceneAutoStarter Instance { get; private set; }

        /// <summary>
        /// IL2CPP 保活入口。
        /// IL2CPP keep-alive entrypoint.
        /// 通过 [RuntimeInitializeOnLoadMethod] 创建从 Unity 引擎到本类型的直接代码引用，
        /// 确保 IL2CPP 代码生成阶段将 Talos.E2E.Runtime 程序集编译进原生二进制。
        /// Creates a direct code reference from the Unity engine to this type via [RuntimeInitializeOnLoadMethod],
        /// ensuring IL2CPP code generation includes the Talos.E2E.Runtime assembly in the native binary.
        /// </summary>
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static private void EnsureTypePreservedInIL2CPP()
        {
            // 方法体故意为空——唯一目的是通过 [RuntimeInitializeOnLoadMethod] 让 Unity 引擎持有直接代码引用。
            // Method body intentionally empty — sole purpose is to create a direct code reference via [RuntimeInitializeOnLoadMethod].
        }

        /// <summary>
        /// Unity 生命周期：场景加载后自动检测并启动 E2E 测试系统。
        /// Unity lifecycle: auto-detect and start the E2E test system after scene load.
        /// </summary>
        private void Awake()
        {
            // Phase 1: 注册单例，防止重复启动。
            // Phase 1: Register singleton to prevent duplicate startup.
            Instance = this;

            // Phase 2: 仅在 Debug 构建中启动 E2E。
            // Phase 2: Start E2E only in Debug builds.
            TryAutoStart();
        }

        /// <summary>
        /// 尝试自动启动 E2E 服务。
        /// Try to auto-start the E2E service.
        /// 检测 DEBUG 标记，如果当前为 Debug 构建则启动 E2E TCP 服务。
        /// Checks the DEBUG marker; if the current build is a Debug build, starts the E2E TCP service.
        /// </summary>
        private void TryAutoStart()
        {
            if (hasStarted)
            {
                Debug.Log("[TalosE2E] E2E 测试系统已启动，跳过重复启动请求");
                return;
            }

            if (!DebugBuildMarker.IsDebugBuild())
            {
                Debug.Log("[TalosE2E] 非 Debug 构建，跳过 E2E 自动启动");
                return;
            }

            Debug.Log("[TalosE2E] 场景自启动组件检测到 Debug 构建，开始启动 E2E 测试系统...");

            try
            {
                int effectivePort = port > 0 ? port : Transport.Protocol.DefaultPort;
                Debug.Log($"[TalosE2E] 场景自启动组件启动 E2E 服务，端口: {effectivePort}");
                TalosE2EBootstrap.LaunchE2E(effectivePort);
                hasStarted = true;
                Debug.Log("[TalosE2E] 场景自启动组件已成功启动 E2E 测试系统");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 场景自启动组件启动 E2E 失败: {ex}");
            }
        }

        /// <summary>
        /// 手动触发启动（可供外部调用，用于非场景挂载的启动路径）。
        /// Manual startup trigger (callable externally for startup paths that don't use scene attachment).
        /// </summary>
        /// <param name="customPort">自定义端口，0 表示使用默认端口。Custom port; 0 means use default.</param>
        public static void ManualStart(int customPort = 0)
        {
            if (hasStarted)
            {
                Debug.Log("[TalosE2E] E2E 测试系统已启动，跳过重复启动请求");
                return;
            }

            if (!DebugBuildMarker.IsDebugBuild())
            {
                Debug.Log("[TalosE2E] 非 Debug 构建，跳过 E2E 手动启动");
                return;
            }

            int effectivePort = customPort > 0 ? customPort : Transport.Protocol.DefaultPort;
            Debug.Log($"[TalosE2E] 手动启动 E2E 服务，端口: {effectivePort}");
            TalosE2EBootstrap.LaunchE2E(effectivePort);
            hasStarted = true;
        }

        /// <summary>
        /// 重置启动状态（仅测试用）。
        /// Reset startup state (for testing only).
        /// </summary>
        internal static void ResetForTesting()
        {
            hasStarted = false;
            Instance = null;
        }

        /// <summary>
        /// Unity 生命周期：销毁时清理单例引用。
        /// Unity lifecycle: clean up singleton reference on destroy.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
