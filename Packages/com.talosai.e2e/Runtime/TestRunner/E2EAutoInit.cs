using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 测试自动入口——热更代码加载完成后的自动检测和启动点。
    /// 
    /// 设计角色：
    /// - 作为热更 DLL 中的一个 Manager，在 HotfixScriptLoder.Start() 流程中被自动发现和执行。
    /// - 检测当前是否为 Debug 构建（通过 DEBUG 标记文件）。
    /// - 如果是 Debug 构建，自动启动 E2E 测试 TCP 服务。
    /// - 如果是 Release 构建，跳过不执行任何操作。
    /// 
    /// 集成方式：
    /// 在游戏的热更入口（如 IHotfixGameStart 实现）中调用 CheckAndLaunch()。
    /// 也可以在框架启动完成后由业务代码手动调用。
    /// 
    /// 使用示例：
    /// <code>
    /// // 在游戏热更入口中
    /// public class GameHotfixStart : IHotfixGameStart
    /// {
    ///     public void Start()
    ///     {
    ///         // ... 游戏初始化逻辑 ...
    ///         
    ///         // 自动检测并启动 E2E 测试
    ///         E2EAutoInit.CheckAndLaunch();
    ///     }
    /// }
    /// </code>
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
        /// 自动检测 Talos 启动条件，并在条件满足时启动 E2E 测试系统。
        /// Auto-detect the Talos launch conditions and start the E2E test system when they are satisfied.
        /// 该方法允许“未成功启动”的调用在后续再次补试，
        /// 但一旦真正启动成功，就会稳定拒绝重复启动请求。
        /// This method allows later retries when an earlier call did not actually launch the service,
        /// but once startup succeeds it consistently rejects duplicate launch requests.
        /// </summary>
        /// <param name="port">TCP 服务监听端口，默认 10002。可通过命令行参数 -talosPort 覆盖。</param>
        [Preserve]
        static public void CheckAndLaunch(int port = Transport.Protocol.DefaultPort)
        {
            if (hasLaunched)
            {
                Debug.Log("[TalosE2E] E2E 测试系统已启动，跳过重复启动请求");
                return;
            }

            Debug.Log("[TalosE2E] 开始自动检测...");

            // 阶段 1：统一收集当前进程可见的 Talos 参数，覆盖普通命令行与 Android Intent `unity` extra。
            // Phase 1: Collect the Talos arguments visible to the current process, covering both normal command-line args and the Android Intent `unity` extra.
            var args = RuntimeLaunchArguments.ResolveCurrentProcessArguments();
            if (RuntimeLaunchArguments.TryGetArgumentValue(args, "-talosPort", out var customPortText))
            {
                if (int.TryParse(customPortText, out var customPort))
                {
                    port = customPort;
                    Debug.Log($"[TalosE2E] 从启动参数读取端口: {port}");
                }
                else
                {
                    Debug.LogWarning($"[TalosE2E] 启动参数中的 -talosPort 非法，已忽略: {customPortText}");
                }
            }

            // 阶段 2：优先处理强制模式，允许 Android 从 Intent extra 补齐 `-talosForceE2E`。
            // Phase 2: Handle forced mode first so Android can recover `-talosForceE2E` from the Intent extra.
            if (ForcedModeStartupFallback.TryLaunchFromForcedMode(args, port, LaunchInternal))
            {
                Debug.Log("[TalosE2E] 检测到强制模式参数，已进入 E2E 启动流程");
                return;
            }

            // 阶段 3：回退到 Debug 构建判定。
            // Phase 3: Fall back to the Debug-build decision.
            if (DebugBuildMarker.IsDebugBuild())
            {
                LaunchInternal(port);
            }
            else
            {
                Debug.Log("[TalosE2E] 非 Debug 构建，当前调用未启动 E2E；后续若出现强制模式参数仍允许再次补试");
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
        static private void LaunchInternal(int port)
        {
            if (hasLaunched)
            {
                return;
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] E2E 测试系统启动失败: {ex}");
            }
        }
    }
}
