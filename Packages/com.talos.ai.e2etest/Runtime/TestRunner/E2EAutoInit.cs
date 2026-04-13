using System;
using UnityEngine;
using BDFramework;
using BDFramework.Core.Tools;

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
    static public class E2EAutoInit
    {
        /// <summary>
        /// 是否已完成自动初始化检查。
        /// </summary>
        static private bool hasChecked = false;

        /// <summary>
        /// 自动检测 Debug 构建标记，如果条件满足则启动 E2E 测试系统。
        /// 此方法可安全地被多次调用，只会执行一次。
        /// </summary>
        /// <param name="port">TCP 服务监听端口，默认 10002。可通过命令行参数 -talosPort 覆盖。</param>
        static public void CheckAndLaunch(int port = Transport.Protocol.DefaultPort)
        {
            if (hasChecked) return;
            hasChecked = true;

            Debug.Log("[TalosE2E] 开始自动检测...");

            // Phase 1: 读取命令行参数覆盖端口
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-talosPort", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(args[i + 1], out var customPort))
                    {
                        port = customPort;
                        Debug.Log($"[TalosE2E] 从命令行参数读取端口: {port}");
                    }
                }

                // 强制启用 E2E 测试模式（不依赖 DEBUG 标记文件）
                if (string.Equals(args[i], "-talosForceE2E", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("[TalosE2E] 检测到 -talosForceE2E 参数，强制启用 E2E 测试");
                    LaunchInternal(port);
                    return;
                }
            }

            // Phase 2: 检查 DEBUG 构建标记
            if (DebugBuildMarker.IsDebugBuild())
            {
                LaunchInternal(port);
            }
            else
            {
                Debug.Log("[TalosE2E] 非 Debug 构建，跳过 E2E 测试系统启动");
            }
        }

        /// <summary>
        /// 内部启动逻辑，执行实际的 E2E 系统初始化。
        /// </summary>
        static private void LaunchInternal(int port)
        {
            try
            {
                Debug.Log($"[TalosE2E] 正在启动 E2E 测试系统，端口: {port}");
                TalosE2EBootstrap.LaunchE2E(port);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] E2E 测试系统启动失败: {ex}");
            }
        }
    }
}
