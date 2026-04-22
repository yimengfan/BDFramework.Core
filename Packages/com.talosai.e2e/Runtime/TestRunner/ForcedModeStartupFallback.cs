using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// Talos 强制模式补偿启动辅助器。
    /// Talos forced-mode fallback startup helper.
    /// 当宿主已经进入 `-talosForceE2E` 路径，但更早阶段的自动启动入口没有成功拉起 TCP 服务时，
    /// 该辅助器允许宿主在更晚、但仍足够早的可见入口处显式补触发一次 Talos E2E 自动检测。
    /// When the host has already entered the `-talosForceE2E` path but the earlier auto-start entrypoint failed to bring up the TCP service,
    /// this helper lets the host explicitly replay Talos E2E auto-detection from a later yet still early visible entrypoint.
    /// </summary>
    [Preserve]
    public static class ForcedModeStartupFallback
    {
        /// <summary>
        /// 检查命令行参数里是否包含 Talos 强制模式标记。
        /// Check whether the command-line arguments contain the Talos forced-mode flag.
        /// </summary>
        /// <param name="args">命令行参数序列。Command-line argument sequence.</param>
        /// <returns>存在 `-talosForceE2E` 时返回 true，否则返回 false。Returns true when `-talosForceE2E` is present; otherwise false.</returns>
        public static bool ContainsTalosForceE2EArgument(IEnumerable<string> args)
        {
            if (args == null)
            {
                return false;
            }

            foreach (var arg in args)
            {
                if (string.Equals(arg, "-talosForceE2E", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在强制模式下补触发一次 Talos E2E 自动检测。
        /// Replay Talos E2E auto-detection once when forced mode is active.
        /// 该方法本身不实现去重，而是依赖调用侧传入的自动检测入口（当前为 `E2EAutoInit.CheckAndLaunch`）
        /// 继续沿用其已有的幂等保护，以便宿主只在真正的强制模式里追加一次补偿调用。
        /// This method does not implement its own deduplication; instead it relies on the supplied auto-detection entrypoint
        /// (currently `E2EAutoInit.CheckAndLaunch`) to reuse its existing idempotency guard, so the host adds only one compensating replay in true forced mode.
        /// </summary>
        /// <param name="args">命令行参数序列。Command-line argument sequence.</param>
        /// <param name="port">补偿启动时使用的 TCP 端口。TCP port to use for the fallback launch.</param>
        /// <param name="launchAction">真正执行补偿启动的动作。Action that performs the fallback launch.</param>
        /// <returns>若当前处于强制模式并已执行补偿调用则返回 true，否则返回 false。Returns true when forced mode is active and the fallback was invoked; otherwise false.</returns>
        public static bool TryLaunchFromForcedMode(IEnumerable<string> args, int port, Action<int> launchAction)
        {
            if (!ContainsTalosForceE2EArgument(args))
            {
                return false;
            }

            if (launchAction == null)
            {
                throw new ArgumentNullException(nameof(launchAction));
            }

            launchAction(port);
            return true;
        }
    }
}