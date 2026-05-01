using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 场景自启动组件。
    /// E2E scene auto-start MonoBehaviour.
    /// 
    /// 设计角色：
    /// - 挂载到场景中的 GameObject 上，在 Awake 时触发 E2E 自动检测与启动。
    /// - 完全替代 BDFramework 中的 E2E 启动调用，实现框架层解耦。
    /// - 在 Debug 构建中自动激活；在 Release 构建中静默跳过。
    /// - 不自行管理端口；端口策略由 E2EAutoInit → TalosPortPolicy 统一处理：
    ///   按平台选择隔离端口池，候选端口逐一尝试，失败自动重试下一个。
    ///
    /// Design role:
    /// - Attach to a scene GameObject; triggers E2E auto-detection and startup on Awake.
    /// - Fully replaces the E2E startup call in BDFramework, achieving framework-layer decoupling.
    /// - Auto-activates in Debug builds; silently skips in Release builds.
    /// - Does not manage ports directly; port policy is handled uniformly by E2EAutoInit → TalosPortPolicy:
    ///   platform-isolated port pools, sequential candidate-port probing, automatic retry on failure.
    /// 
    /// 使用方式：
    /// 1. 在场景中创建空 GameObject（推荐命名 "[TalosE2E]"）。
    /// 2. 挂载此组件（无需配置端口，端口由 TalosPortPolicy 按平台自动决定）。
    /// 3. 进入 PlayMode 后自动触发 E2E 启动检测。
    /// 
    /// Usage:
    /// 1. Create an empty GameObject in the scene (recommended name "[TalosE2E]").
    /// 2. Attach this component (no port configuration needed; TalosPortPolicy resolves ports per platform).
    /// 3. E2E auto-detection triggers when entering PlayMode.
    /// 
    /// 端口策略说明 / Port policy notes:
    /// 本组件不持有端口字段，启动逻辑全部委托给 E2EAutoInit.CheckAndLaunch()：
    /// - TalosPortPolicy 按当前平台返回隔离候选端口池（Windows/Android/macOS/Editor 各自独立）。
    /// - CheckAndLaunch() 遍历候选端口逐一尝试启动，某端口失败则自动重试下一个候选端口。
    /// - Editor 环境自动使用 LaunchE2EStatic()，真机使用 LaunchE2E() MonoBehaviour 模式。
    ///
    /// This component does not hold a port field; startup logic is fully delegated to E2EAutoInit.CheckAndLaunch():
    /// - TalosPortPolicy returns a platform-isolated candidate-port pool for the current platform.
    /// - CheckAndLaunch() iterates candidates, trying each port; on failure it automatically retries the next candidate.
    /// - Editor environment auto-selects LaunchE2EStatic(); player builds use LaunchE2E() MonoBehaviour mode.
    /// </summary>
    [Preserve]
    public class E2ESceneAutoStarter : MonoBehaviour
    {
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
        /// Unity 生命周期：场景加载后自动触发 E2E 启动检测。
        /// Unity lifecycle: auto-trigger E2E startup detection after scene load.
        /// 启动逻辑委托给 E2EAutoInit.CheckAndLaunch()，由其按 TalosPortPolicy
        /// 平台隔离端口池逐一尝试，失败自动重试下一个候选端口。
        /// Startup logic is delegated to E2EAutoInit.CheckAndLaunch(), which follows
        /// TalosPortPolicy platform-isolated port pools with sequential retry on failure.
        /// </summary>
        private void Awake()
        {
            // Phase 1: 注册单例，防止重复启动。
            // Phase 1: Register singleton to prevent duplicate startup.
            Instance = this;

            // Phase 2: 委托给 E2EAutoInit，由其处理 Debug 检测、平台端口池、候选端口遍历和重试。
            // Phase 2: Delegate to E2EAutoInit, which handles Debug detection, platform port pools, candidate iteration and retry.
            Debug.Log("[TalosE2E] 场景自启动组件触发 E2E 自动检测...");
            E2EAutoInit.CheckAndLaunch();
        }

        /// <summary>
        /// 手动触发启动（可供外部调用，用于非场景挂载的启动路径）。
        /// Manual startup trigger (callable externally for startup paths that don't use scene attachment).
        /// 同样委托给 E2EAutoInit.CheckAndLaunch()，复用完整的平台端口策略和重试逻辑。
        /// Also delegates to E2EAutoInit.CheckAndLaunch(), reusing the full platform port policy and retry logic.
        /// </summary>
        public static void ManualStart()
        {
            Debug.Log("[TalosE2E] 手动触发 E2E 自动检测...");
            E2EAutoInit.CheckAndLaunch();
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
