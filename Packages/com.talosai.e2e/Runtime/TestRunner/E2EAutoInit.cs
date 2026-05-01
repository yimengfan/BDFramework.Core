using System;
using Talos.E2E.Transport;
using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 测试自动入口——统一启动检测与端口策略的核心实现。
    /// E2E test auto-entry — core implementation of unified startup detection and port policy.
    /// 
    /// 设计角色：
    /// - E2E 启动的唯一执行入口，封装 Debug 构建检测、平台隔离端口池选择和候选端口重试逻辑。
    /// - 被 E2ESceneAutoStarter（场景挂载触发）和外部手动调用共同委托。
    /// - Editor 环境自动使用静态模式，真机使用 MonoBehaviour 模式。
    /// - 非 Debug 构建静默跳过。
    /// 
    /// Design role:
    /// - The sole execution entry for E2E startup, encapsulating Debug-build detection, platform-isolated port-pool selection, and candidate-port retry logic.
    /// - Delegated to by E2ESceneAutoStarter (scene-attachment trigger) and external manual calls alike.
    /// - Editor environment auto-selects static mode; player builds use MonoBehaviour mode.
    /// - Silently skips in non-Debug builds.
    /// 
    /// 调用路径 / Call paths:
    /// 1. E2ESceneAutoStarter.Awake() → E2EAutoInit.CheckAndLaunch()（场景挂载触发）
    /// 2. E2ESceneAutoStarter.ManualStart() → E2EAutoInit.CheckAndLaunch()（代码手动触发）
    /// 3. 任何外部代码 → E2EAutoInit.CheckAndLaunch()（直接调用）
    /// 
    /// 端口策略 / Port policy:
    /// CheckAndLaunch() 通过 TalosPortPolicy.ResolveCandidatePortsForCurrentPlatform()
    /// 获取当前平台的隔离候选端口池（Windows/Android/macOS/Editor 各自独立），
    /// 然后逐一尝试启动，某端口失败则自动重试下一个候选端口。
    /// </summary>
    [Preserve]
    static public class E2EAutoInit
    {
        /// <summary>
        /// 是否已成功启动 Talos E2E。
        /// Whether Talos E2E has already been launched successfully.
        /// 这里不再缓存“检查过但未启动”的失败结果，
        /// 这样 Android 等平台仍可在更晚的宿主可见入口补试一次，而不会被早期探测永久锁死。
        /// This no longer caches a failed “checked but not launched” result,
        /// so Android and similar platforms can retry from a later host-visible entrypoint instead of being locked out forever by the first probe.
        /// </summary>
        static private bool hasLaunched = false;

        /// <summary>
        /// IL2CPP 保活入口——在程序集加载后立即执行，确保本类型被编译到原生二进制。
        /// IL2CPP keep-alive entrypoint — executes immediately after assemblies are loaded,
        /// ensuring this type is compiled into the native binary.
        /// 在 IL2CPP 构建中，没有托管 DLL 文件，Assembly.Load() 无法工作。
        /// 只有被代码直接引用的类型才会被 IL2CPP 编译进原生二进制。
        /// [RuntimeInitializeOnLoadMethod] 让 Unity 的初始化系统直接调用本方法，
        /// 这创建了从 Unity 引擎到本类型的直接代码引用，强制 IL2CPP 保留整个类。
        /// In IL2CPP builds, there are no managed DLL files and Assembly.Load() does not work.
        /// Only types with direct code references get compiled into the native binary.
        /// [RuntimeInitializeOnLoadMethod] causes Unity's initialization system to call this method directly,
        /// creating a direct code reference from the Unity engine to this type, forcing IL2CPP to preserve the entire class.
        /// </summary>
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static private void EnsureTypePreservedInIL2CPP()
        {
            // 此方法体故意为空——其唯一目的是通过 [RuntimeInitializeOnLoadMethod]
            // 让 Unity 引擎持有对本类型的直接代码引用，从而防止 IL2CPP 裁剪本类。
            // This method body is intentionally empty — its sole purpose is to create a direct code reference
            // from the Unity engine to this type via [RuntimeInitializeOnLoadMethod], preventing IL2CPP from stripping this class.
        }

        /// <summary>
        /// 自动检测 Talos 启动条件，并在条件满足时启动 E2E 测试系统。
        /// Auto-detect the Talos launch conditions and start the E2E test system when they are satisfied.
        /// 该方法允许“未成功启动”的调用在后续再次补试，
        /// 但一旦真正启动成功，就会稳定拒绝重复启动请求。
        /// This method allows later retries when an earlier call did not actually launch the service,
        /// but once startup succeeds it consistently rejects duplicate launch requests.
        /// </summary>
        [Preserve]
        static public void CheckAndLaunch()
        {
            if (hasLaunched)
            {
                Debug.Log("[TalosE2E] E2E 测试系统已启动，跳过重复启动请求");
                return;
            }

            Debug.Log("[TalosE2E] 开始自动检测...");

            if (DebugBuildMarker.IsDebugBuild())
            {
                foreach (var candidatePort in TalosPortPolicy.ResolveCandidatePortsForCurrentPlatform())
                {
                    if (LaunchInternal(candidatePort))
                    {
                        return;
                    }
                }

                Debug.LogError("[TalosE2E] 所有候选端口均启动失败，E2E 未能拉起");
            }
            else
            {
                Debug.Log("[TalosE2E] 非 Debug 构建，跳过 E2E 自动启动");
            }
        }

        /// <summary>
        /// 内部启动逻辑，执行实际的 E2E 系统初始化。
        /// Internal startup logic that performs the actual E2E system initialization.
        /// 启动模式选择：
        /// Editor 环境使用静态模式，真机场景使用 MonoBehaviour 模式。
        /// Startup mode selection:
        /// the editor uses the static mode, while players use the MonoBehaviour mode.
        /// </summary>
        static private bool LaunchInternal(int port)
        {
            if (hasLaunched)
            {
                return true;
            }

            try
            {
                if (UnityEngine.Application.isEditor)
                {
                    // Editor 下使用静态模式——不依赖 MonoBehaviour，
                    // 由 E2EEditorTools.OnDidReloadScripts 管理进出 PlayMode 的 TCP 恢复。
                    Debug.Log($"[TalosE2E] Editor 模式，启动静态 E2E 服务，端口: {port}");
                    TalosE2EBootstrap.LaunchE2EStatic(port);
                }
                else
                {
                    // 真机使用 MonoBehaviour 模式——拥有完整 Unity 生命周期（Update、OnApplicationQuit 等）
                    Debug.Log($"[TalosE2E] 真机模式，启动 MonoBehaviour E2E 服务，端口: {port}");
                    TalosE2EBootstrap.LaunchE2E(port);
                }

                hasLaunched = true;
                return true;
            }
            catch (Exception ex)
            {
                TalosE2EBootstrap.Shutdown();
                Debug.LogWarning($"[TalosE2E] 端口 {port} 启动失败，将尝试下一个候选端口: {ex.Message}");
                return false;
            }
        }
    }
}
