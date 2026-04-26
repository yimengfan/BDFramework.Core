using System;
using UnityEngine;

namespace Talos.E2E.Transport
{
    /// <summary>
    /// Talos 运行时端口策略。
    /// Talos runtime port policy.
    /// app 启动时不再读取外部 `-talosPort` 参数，而是在框架内按平台选择隔离后的候选端口池。
    /// App startup no longer reads an external `-talosPort` argument; instead it selects a platform-isolated candidate-port pool inside the framework.
    /// </summary>
    public static class TalosPortPolicy
    {
        public static readonly int[] WindowsPlayerPorts = { 10002, 10012, 10022 };
        public static readonly int[] AndroidPlayerPorts = { 11002, 11012, 11022 };
        public static readonly int[] MacOSPlayerPorts = { 12002, 12012, 12022 };
        public static readonly int[] EditorPorts = { 13002, 13012, 13022 };

        /// <summary>
        /// 解析当前运行环境对应的候选端口。
        /// Resolve the candidate ports for the current runtime environment.
        /// </summary>
        public static int[] ResolveCandidatePortsForCurrentPlatform()
        {
            if (Application.isEditor)
            {
                return EditorPorts;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return WindowsPlayerPorts;
                case RuntimePlatform.Android:
                    return AndroidPlayerPorts;
                case RuntimePlatform.OSXPlayer:
                    return MacOSPlayerPorts;
                default:
                    return WindowsPlayerPorts;
            }
        }
    }
}
