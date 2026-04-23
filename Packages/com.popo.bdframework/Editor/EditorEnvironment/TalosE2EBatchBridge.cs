using System;
using System.IO;
using System.Reflection;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using Talos.E2E.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BDFramework.Editor.Environment
{
    /// <summary>
    /// Talos E2E 的 BDFramework 宿主批入口。
    /// BDFramework-owned batch entrypoints for Talos E2E.
    /// 该类型只存在于 BDFramework.Editor 程序集中，负责把 BDFramework 自己的场景准备、资源初始化与 SQLite 初始化
    /// 组合到显式的 <c>-executeMethod</c> 入口里，然后再调用 Talos E2E 提供的纯 E2E 启动原语。
    /// This type lives only in the BDFramework.Editor assembly and combines BDFramework-owned scene preparation,
    /// resource initialization, and SQLite initialization into explicit <c>-executeMethod</c> entrypoints before calling the pure Talos E2E launch primitives.
    /// </summary>
    public static class TalosE2EBatchBridge
    {
        private const string TalosForceE2EArgument = "-talosForceE2E";
        private const string EditorOnlyLauncherObjectName = "TalosE2E.EditorOnlyBDLauncher";

        private static readonly string[] LaunchSceneCandidates =
        {
            Path.Combine("Assets", "Scenes", "BDFrame.unity"),
            Path.Combine("Assets", "Scenes", "BDFrame_Debug.unity")
        };

        /// <summary>
        /// 注册宿主侧的 Editor 生命周期钩子。
        /// Register the host-owned editor lifecycle hooks.
        /// 当 Talos E2E 在 editor-only 模式下触发 PlayMode 往返或脚本域重载时，
        /// 宿主需要自己恢复像 <c>BApplication.IsPlaying</c> 这样的宿主上下文标记，而不是把责任交回 Talos 包。
        /// When Talos E2E triggers a PlayMode round trip or script-domain reload in editor-only mode,
        /// the host must restore host-owned context markers such as <c>BApplication.IsPlaying</c> on its own instead of pushing that responsibility back into the Talos package.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeEditorLifecycleHooks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 启动带宿主上下文的 Talos E2E PlayMode 批入口。
        /// Launch the Talos E2E PlayMode batch entry with host context.
        /// 该入口先准备 BDFramework 的启动场景，再把控制权交给 Talos E2E 的纯 PlayMode handoff。
        /// This entry prepares the BDFramework launch scene first and then hands control to the pure Talos E2E PlayMode handoff.
        /// </summary>
        public static void LaunchTalosE2EBatchMode()
        {
            // Phase 1: 先切到 BDFramework 的启动场景，确保进入 PlayMode 后由宿主自己驱动启动链路。
            // Phase 1: Switch to the BDFramework launch scene first so the host drives its own startup chain after entering PlayMode.
            PreparePlayModeLaunchScene();

            // Phase 2: 再调用纯 E2E 入口，让 Talos 只负责自己的 Editor 生命周期管理。
            // Phase 2: Then call the pure E2E entry so Talos owns only its own editor lifecycle management.
            E2EEditorTools.LaunchE2EBatchMode();
        }

        /// <summary>
        /// 启动带宿主上下文的 Talos E2E editor-only 批入口。
        /// Launch the Talos E2E editor-only batch entry with host context.
        /// 该入口先初始化 BDFramework 自己的运行上下文，再启动 Talos 的静态 TCP 服务。
        /// This entry initializes the BDFramework-owned runtime context first and then starts the Talos static TCP service.
        /// </summary>
        public static void LaunchTalosE2EEditorOnly()
        {
            // Phase 1: 初始化 BDFramework 自己的 editor-only 运行上下文。
            // Phase 1: Initialize the BDFramework-owned editor-only runtime context.
            PrepareEditorOnlyRuntime();

            // Phase 2: 调用纯 E2E 入口启动静态 TCP 服务。
            // Phase 2: Call the pure E2E entry to start the static TCP service.
            E2EEditorTools.LaunchE2EEditorOnly();
        }

        /// <summary>
        /// 启动带宿主上下文的 Talos E2E 同步导出批入口。
        /// Launch the Talos E2E synchronous-export batch entry with host context.
        /// 该入口先初始化 BDFramework 自己的运行上下文，再执行 Talos 的同步测试导出。
        /// This entry initializes the BDFramework-owned runtime context first and then executes the Talos synchronous test export.
        /// </summary>
        public static void RunTalosE2EAndExport()
        {
            // Phase 1: 初始化 BDFramework 自己的 editor-only 运行上下文。
            // Phase 1: Initialize the BDFramework-owned editor-only runtime context.
            PrepareEditorOnlyRuntime();

            // Phase 2: 调用纯 E2E 入口执行同步测试并导出结果。
            // Phase 2: Call the pure E2E entry to execute synchronous tests and export results.
            E2EEditorTools.RunE2EAndExport();
        }

        /// <summary>
        /// 在脚本域重载后恢复宿主侧 editor-only 上下文标记。
        /// Restore the host-owned editor-only context marker after a script-domain reload.
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            RestoreEditorOnlyRuntimeFlagIfNeeded("DidReloadScripts");
        }

        /// <summary>
        /// 在从 PlayMode 回到编辑态后恢复宿主侧 editor-only 上下文标记。
        /// Restore the host-owned editor-only context marker after returning from PlayMode to edit mode.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            RestoreEditorOnlyRuntimeFlagIfNeeded("EnteredEditMode");
        }

        /// <summary>
        /// 准备 BDFramework 的 PlayMode 启动场景。
        /// Prepare the BDFramework launch scene for PlayMode.
        /// </summary>
        private static void PreparePlayModeLaunchScene()
        {
            foreach (var candidate in LaunchSceneCandidates)
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                EditorSceneManager.OpenScene(candidate);
                Debug.Log($"[TalosE2E] BDFramework 批入口已打开启动场景: {candidate}");
                return;
            }

            Debug.LogWarning("[TalosE2E] BDFramework 批入口未找到 BDFrame 启动场景，将沿用当前场景");
        }

        /// <summary>
        /// 准备 BDFramework 自己的 editor-only 运行上下文。
        /// Prepare the BDFramework-owned editor-only runtime context.
        /// 该初始化仍然完全属于 BDFramework 自己，Talos E2E 只消费已经准备好的上下文，不负责这些三方库或业务系统的初始化。
        /// This initialization remains fully owned by BDFramework itself; Talos E2E only consumes the prepared context and does not initialize those third-party or business systems.
        /// </summary>
        private static void PrepareEditorOnlyRuntime()
        {
            EnsureEditorOnlyRuntimeFlag("PrepareEditorOnlyRuntime");
            EnsureEditorOnlyLauncherSignal(null);

            GameConfigLoder.LoadFrameworkConfig();
            var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            if (config == null)
            {
                Debug.LogWarning("[TalosE2E] BDFramework 批入口无法获取框架配置，跳过 editor-only 上下文初始化");
                return;
            }

            EnsureEditorOnlyLauncherSignal(config.ClientVersionNum);

            Debug.Log($"[TalosE2E] BDFramework 批入口框架版本: {BDLauncher.FrameworkVersion}, 母包版本: {config.ClientVersionNum}");

            var (firstLoadDir, secondLoadDir) = ClientAssetsUtils.GetMultiAssetsLoadPath(
                BApplication.RuntimePlatform,
                config.ClientVersionNum);
            if (Application.isEditor)
            {
                firstLoadDir = secondLoadDir;
            }

            ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
            BResources.Init(config.ArtRoot, firstLoadDir, secondLoadDir);
            SqliteLoder.Init(config.SQLRoot, firstLoadDir, secondLoadDir);
            Debug.Log("[TalosE2E] BDFramework 批入口已完成 editor-only 运行上下文初始化");
        }

        /// <summary>
        /// 为 editor-only Talos 会话补齐宿主启动器信号。
        /// Ensure the host launcher signal exists for the editor-only Talos session.
        /// 本地 sync 回退 gate 不会像真实 PlayMode 那样自动跑过启动场景，因此这里要显式复用现有启动器或创建一个隐藏占位实例，
        /// 让宿主侧 launch suite 能读取与真机链路等价的最小版本信号。
        /// The local sync fallback gate does not automatically traverse the startup scene like a real PlayMode session,
        /// so this method explicitly reuses an existing launcher or creates a hidden placeholder instance so the host launch suite can read the minimal version signal that matches the device flow contract.
        /// </summary>
        /// <param name="clientVersion">本次 editor-only 会话应暴露的母包版本；为空时沿用现有值。</param>
        /// <param name="clientVersion">Base-package version that should be exposed for this editor-only session; when empty, the current value is preserved.</param>
        private static void EnsureEditorOnlyLauncherSignal(string clientVersion)
        {
            Debug.Log($"[TalosE2E] BDFramework 批入口开始补齐启动器信号: hasInst={(BDLauncher.Inst != null)} requestedClientVersion={(string.IsNullOrWhiteSpace(clientVersion) ? "<keep>" : clientVersion)}");

            var launcher = BDLauncher.Inst;
            if (!launcher)
            {
                launcher = GameObject.FindObjectOfType<BDLauncher>();
            }

            if (!launcher)
            {
                var launcherObject = new GameObject(EditorOnlyLauncherObjectName)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                launcher = launcherObject.AddComponent<BDLauncher>();
                launcher.hideFlags = HideFlags.HideAndDontSave;
                Debug.Log("[TalosE2E] BDFramework 批入口已创建 editor-only BDLauncher 占位实例");
            }

            if (!string.IsNullOrWhiteSpace(clientVersion))
            {
                launcher.ClientVersion = clientVersion.Trim();
            }

            AssignEditorOnlyLauncherInstance(launcher);
            Debug.Log($"[TalosE2E] BDFramework 批入口已补齐启动器信号: name={launcher.name} clientVersion={launcher.ClientVersion}");
        }

        /// <summary>
        /// 通过反射回写 editor-only 启动器单例。
        /// Assign the editor-only launcher singleton through reflection.
        /// <c>BDLauncher.Inst</c> 的 setter 仍然保持私有，避免运行时入口被普通业务代码随意重写；
        /// 这里作为框架基础设施只在 batchmode editor-only 补环境时做一次受控回写。
        /// The setter of <c>BDLauncher.Inst</c> remains private so ordinary business code cannot overwrite the runtime entry casually;
        /// this framework-infrastructure helper performs one controlled assignment only while restoring the batchmode editor-only environment.
        /// </summary>
        /// <param name="launcher">要注册为当前宿主启动器的实例。</param>
        /// <param name="launcher">Instance to register as the current host launcher.</param>
        private static void AssignEditorOnlyLauncherInstance(BDLauncher launcher)
        {
            var instProperty = typeof(BDLauncher).GetProperty(nameof(BDLauncher.Inst), BindingFlags.Public | BindingFlags.Static);
            var instSetter = instProperty?.GetSetMethod(true);
            if (instSetter == null)
            {
                throw new MissingMethodException("未找到 BDLauncher.Inst 的私有 setter，无法回写 editor-only 启动器实例");
            }

            instSetter.Invoke(null, new object[] { launcher });
        }

        /// <summary>
        /// 确保宿主侧 editor-only 会话的运行时标记已经恢复。
        /// Ensure that the host-owned runtime marker has been restored for the editor-only session.
        /// </summary>
        private static void EnsureEditorOnlyRuntimeFlag(string source)
        {
            BApplication.IsPlaying = true;
            Debug.Log($"[TalosE2E] BDFramework 批入口已标记 BApplication.IsPlaying = true source={source}");
        }

        /// <summary>
        /// 按需恢复宿主侧 editor-only 运行时标记。
        /// Restore the host-owned editor-only runtime marker when needed.
        /// 只有在 Talos E2E 命令行会话仍处于 editor-only 场景且当前不在 PlayMode 时才会执行恢复，
        /// 这样可以避免把普通 Unity Editor 会话误判成 E2E 会话。
        /// Restoration runs only when the Talos E2E command-line session is still in the editor-only scenario and the editor is not currently in PlayMode,
        /// which avoids misclassifying normal Unity editor sessions as E2E sessions.
        /// </summary>
        private static void RestoreEditorOnlyRuntimeFlagIfNeeded(string source)
        {
            if (!HasTalosForceE2ECommandLineFlag() || EditorApplication.isPlaying || BApplication.IsPlaying)
            {
                return;
            }

            EnsureEditorOnlyRuntimeFlag(source);
        }

        /// <summary>
        /// 判断当前 Unity 进程是否仍由 Talos E2E 命令行启动。
        /// Determine whether the current Unity process is still running under the Talos E2E command-line session.
        /// </summary>
        private static bool HasTalosForceE2ECommandLineFlag()
        {
            var args = System.Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (string.Equals(arg, TalosForceE2EArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}