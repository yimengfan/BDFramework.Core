using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Talos.E2E.Transport;

namespace Talos.E2E.Editor
{
    /// <summary>
    /// E2E 编辑器工具入口。
    /// Editor-side tool entrypoints for Talos E2E.
    /// 该类型只负责 Talos E2E 自身在 Editor 下的调试标记、生命周期切换与 TCP 服务管理；
    /// 宿主框架如果需要在 batchmode、PlayMode 或 editor-only 场景中准备自己的运行上下文，必须在宿主自己的 executeMethod 入口中先完成，再调用 Talos 的纯 E2E 原语。
    /// This type owns only Talos E2E debug markers, lifecycle transitions, and TCP service management inside the editor;
    /// when a host framework needs to prepare its own runtime context for batchmode, PlayMode, or editor-only runs, it must do that first in its own executeMethod entrypoint before calling the pure Talos E2E primitives.
    /// </summary>
    static public class E2EEditorTools
    {
        /// <summary>
        /// EditorPrefs 键，记录 E2E 服务是否处于活跃状态。
        /// EditorPrefs key that records whether the E2E service is active.
        /// </summary>
        private const string PREFS_E2E_ACTIVE = "TalosE2E_Active";

        /// <summary>
        /// EditorPrefs 键，记录 E2E 服务的 TCP 端口号。
        /// EditorPrefs key that records the TCP port of the E2E service.
        /// </summary>
        private const string PREFS_E2E_PORT = "TalosE2E_Port";

        /// <summary>
        /// 注册编辑器生命周期钩子。
        /// Register the editor lifecycle hooks.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeEditorLifecycleHooks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 在默认 StreamingAssets 热更目录中创建 DEBUG 标记。
        /// Create a DEBUG marker in the default StreamingAssets hotfix directory.
        /// </summary>
        [MenuItem("Talos/E2E Test/创建 DEBUG 标记")]
        static public void CreateDebugMarker()
        {
            var dir = DebugBuildMarker.GetStreamingMarkerDirectory();
            DebugBuildMarker.CreateMarker(dir);
            Debug.Log("[TalosE2E] DEBUG 标记创建完成");
        }

        /// <summary>
        /// 移除默认 StreamingAssets 热更目录中的 DEBUG 标记。
        /// Remove the DEBUG marker from the default StreamingAssets hotfix directory.
        /// </summary>
        [MenuItem("Talos/E2E Test/移除 DEBUG 标记")]
        static public void RemoveDebugMarker()
        {
            var markerPath = Path.Combine(DebugBuildMarker.GetStreamingMarkerDirectory(), DebugBuildMarker.MARKER_FILENAME);
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                Debug.Log("[TalosE2E] DEBUG 标记已移除");
            }
            DebugBuildMarker.ResetCache();
        }

        /// <summary>
        /// 检查当前是否有 DEBUG 标记。
        /// </summary>
        [MenuItem("Talos/E2E Test/检查 DEBUG 状态")]
        static public void CheckDebugStatus()
        {
            var isDebug = DebugBuildMarker.IsDebugBuild();
            Debug.Log($"[TalosE2E] 当前构建状态: {(isDebug ? "DEBUG" : "RELEASE")}");
        }

        /// <summary>
        /// BatchMode 下的 PlayMode 启动入口。
        /// Batchmode PlayMode launch entry.
        /// 该入口只做 Talos E2E 自身的 PlayMode handoff：注册 Editor 命令桥、记录活跃状态并进入 PlayMode；
        /// 宿主框架如果需要额外准备场景或运行上下文，必须在自己的 executeMethod 入口中先完成。
        /// This entry performs only the Talos E2E PlayMode handoff itself: it registers the editor command bridge, persists the active state, and enters PlayMode;
        /// if a host framework needs additional scene or runtime-context preparation, that work must happen first in the host-owned executeMethod entrypoint.
        /// </summary>
        public static void LaunchE2EBatchMode()
        {
            Debug.Log("[TalosE2E] LaunchE2EBatchMode 入口被调用");

            var port = ReadConfiguredPort();
            Debug.Log($"[TalosE2E] 配置端口: {port}");

            // Phase 1: 注入 Runtime 到 Editor 的命令桥，并记录活跃状态。
            // Phase 1: Inject the runtime-to-editor command bridge and persist the active state.
            TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
            Debug.Log("[TalosE2E] EditorCommandHandler 委托已注册（BatchMode）");
            MarkE2EActive(port);

            // Phase 2: 进入 PlayMode，后续启动交由 Runtime 侧统一驱动。
            // Phase 2: Enter PlayMode so runtime code owns the remaining startup flow.
            Debug.Log("[TalosE2E] 正在进入 PlayMode（E2E 启动将由 Runtime 侧驱动）...");
            EditorApplication.EnterPlaymode();
        }

        // ====================================================================
        // Editor-only 模式（不进入 PlayMode）
        // ====================================================================

        /// <summary>
        /// Editor-only 启动入口，不进入 PlayMode。
        /// Editor-only launch entry that does not enter PlayMode.
        /// 该入口只负责启动 Talos E2E 的纯静态 TCP 服务；
        /// 宿主框架若依赖额外初始化，必须在自己的 executeMethod 入口中先处理，再调用此方法。
        /// This entry only starts the Talos E2E pure-static TCP service;
        /// if the host framework requires extra initialization, it must handle that first in its own executeMethod entrypoint before calling this method.
        /// </summary>
        public static void LaunchE2EEditorOnly()
        {
            Debug.Log("[TalosE2E] LaunchE2EEditorOnly 入口被调用（Editor-only 模式，不进入 PlayMode）");

            var port = ReadConfiguredPort();
            Debug.Log($"[TalosE2E] 配置端口: {port}");

            try
            {
            // Phase 1: 注册 Editor 命令桥。
            // Phase 1: Register the editor command bridge.
                TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
                Debug.Log("[TalosE2E] EditorCommandHandler 委托已注册");

            // Phase 2: 启动纯静态 TCP 服务。
            // Phase 2: Start the pure-static TCP service.
                TalosE2EBootstrap.LaunchE2EStatic(port);
                Debug.Log($"[TalosE2E] E2E TCP 服务已启动，端口: {port}");

            // Phase 3: 保持 editor-only 主循环活跃，并记录当前活跃状态。
            // Phase 3: Keep the editor-only main loop alive and persist the active state.
                EditorApplication.update -= EditorOnlyTick;
                EditorApplication.update += EditorOnlyTick;
                MarkE2EActive(port);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalosE2E] Editor-only 启动失败: {ex}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[TalosE2E] Editor-only 模式初始化完成，等待 Playwright 连接...");
        }

        // ====================================================================
        // 同步执行模式（直接输出结果文件，不依赖 TCP 和主循环）
        // ====================================================================

        /// <summary>
        /// 同步执行并导出结果的入口。
        /// Synchronous execution and export entry.
        /// 该入口只负责同步执行 Talos E2E 测试并写出结果文件；
        /// 宿主若依赖额外初始化，必须在自己的 executeMethod 入口里先完成。
        /// This entry only synchronously executes Talos E2E tests and writes a result file;
        /// if the host depends on additional initialization, it must complete that work first in its own executeMethod entrypoint.
        /// </summary>
        public static void RunE2EAndExport()
        {
            Debug.Log("[TalosE2E] RunE2EAndExport 入口被调用（同步执行模式）");

            var port = ReadConfiguredPort();
            var outputPath = ReadConfiguredOutputPath();
            Debug.Log($"[TalosE2E] 配置端口: {port}, 输出文件: {outputPath}");

            // Phase 1: 初始化测试运行器并执行所有测试。
            // Phase 1: Initialize the test runner and execute all tests.
            E2ETestRunner.Initialize();
            var testCount = E2ETestRunner.GetTestList().Count;
            Debug.Log($"[TalosE2E] 发现 {testCount} 个测试用例，开始同步执行...");

            // 同步执行所有测试并收集结果
            var allResults = new System.Collections.Generic.List<object>();
            int passed = 0;
            int failed = 0;

            E2ETestRunner.RunAll(result =>
            {
                allResults.Add(new
                {
                    suite = result.suite,
                    className = result.className,
                    methodName = result.methodName,
                    description = result.description,
                    passed = result.passed,
                    errorMessage = result.errorMessage,
                    durationMs = result.durationMs,
                    timestamp = result.timestamp
                });

                if (result.passed) passed++; else failed++;
            });

            // Phase 2: 写入结果文件。
            // Phase 2: Write the result file.
            var report = new
            {
                status = failed == 0 ? "PASSED" : "FAILED",
                total = passed + failed,
                passed,
                failed,
                results = allResults,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var json = LitJson.JsonMapper.ToJson(report);
            var fullPath = Path.IsPathRooted(outputPath)
                ? outputPath
                : Path.Combine(System.IO.Directory.GetCurrentDirectory(), outputPath);

            // 确保目录存在。
            // Ensure the output directory exists.
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, json);
            Debug.Log($"[TalosE2E] 测试结果已写入: {fullPath}");
            Debug.Log($"[TalosE2E] ===== 测试总结 =====");
            Debug.Log($"[TalosE2E]   总计: {passed + failed}");
            Debug.Log($"[TalosE2E]   通过: {passed}");
            Debug.Log($"[TalosE2E]   失败: {failed}");
            Debug.Log($"[TalosE2E]   状态: {(failed == 0 ? "✅ PASSED" : "❌ FAILED")}");
            Debug.Log($"[TalosE2E] ======================");

            // Phase 3: 设置退出码。
            // Phase 3: Set the process exit code.
            if (failed > 0)
            {
                Debug.LogError("[TalosE2E] 有测试失败，请查看结果文件获取详细信息");
                System.Environment.ExitCode = 1;
            }
        }

        /// <summary>
        /// Editor-only 模式的 tick 回调。
        /// Tick callback for editor-only mode.
        /// 在 batchmode 下它会持续处理主线程队列，确保静态 TCP 服务可以安全调用 Unity API。
        /// In batchmode it continuously drains the main-thread queue so the static TCP service can safely call Unity APIs.
        /// </summary>
        private static void EditorOnlyTick()
        {
            TalosE2EBootstrap.ProcessMainThreadActions();
        }

        /// <summary>
        /// 处理 Editor 的 PlayMode 生命周期切换。
        /// Handle PlayMode lifecycle transitions inside the editor.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var isActive = EditorPrefs.GetBool(PREFS_E2E_ACTIVE, false);
            if (!ShouldSuspendEditorOnlyServiceForPlayModeChange(state, isActive))
            {
                return;
            }

            Debug.Log("[TalosE2E] 即将进入 PlayMode，先暂停 editor-only 静态服务");
            TalosE2EBootstrap.Shutdown();
            EditorApplication.update -= EditorOnlyTick;
        }

        /// <summary>
        /// 判断当前 PlayMode 切换是否需要暂停 editor-only 静态服务。
        /// Decide whether the current PlayMode transition should suspend the editor-only static service.
        /// </summary>
        private static bool ShouldSuspendEditorOnlyServiceForPlayModeChange(PlayModeStateChange state, bool isActive)
        {
            return isActive && state == PlayModeStateChange.ExitingEditMode;
        }

        // ====================================================================
        // Domain Reload 恢复机制
        // ====================================================================

        /// <summary>
        /// 标记 E2E 服务活跃——在启动 TCP 服务时调用。
        /// 信息写入 EditorPrefs，Domain Reload 后仍可读取。
        /// </summary>
        private static void MarkE2EActive(int port)
        {
            EditorPrefs.SetBool(PREFS_E2E_ACTIVE, true);
            EditorPrefs.SetInt(PREFS_E2E_PORT, port);
            Debug.Log($"[TalosE2E] E2E 状态已标记: port={port}");
        }

        /// <summary>
        /// 清除 E2E 活跃标记。
        /// </summary>
        private static void ClearE2EMark()
        {
            EditorPrefs.SetBool(PREFS_E2E_ACTIVE, false);
            EditorPrefs.DeleteKey(PREFS_E2E_PORT);
        }

        /// <summary>
        /// Domain Reload 后恢复 E2E 所需的 Editor 状态。
        /// Restore the Editor state required by E2E after a domain reload.
        /// 进入 PlayMode 后只恢复 Runtime→Editor 命令桥；
        /// 退出 PlayMode 回到编辑态后，才恢复 editor-only 的静态 TCP 服务。
        /// After entering PlayMode this method restores only the runtime-to-editor command bridge;
        /// it restores the editor-only static TCP service only after control returns to edit mode.
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            bool wasActive = EditorPrefs.GetBool(PREFS_E2E_ACTIVE, false);
            if (!wasActive)
            {
                return;
            }

            int port = EditorPrefs.GetInt(PREFS_E2E_PORT, Protocol.DefaultPort);
            Debug.Log($"[TalosE2E] DidReloadScripts: 检测到 E2E 活跃标记, port={port}, isPlaying={EditorApplication.isPlaying}");

            // Phase 1: 恢复 Runtime→Editor 的命令委托。
            // Phase 1: Restore the runtime-to-editor command delegate.
            TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
            Debug.Log("[TalosE2E] EditorCommandHandler 委托已恢复（DidReloadScripts）");

            if (!ShouldRestoreStaticServiceAfterReload(wasActive, EditorApplication.isPlaying))
            {
                Debug.Log("[TalosE2E] PlayMode 内恢复委托完成，TCP 启动将由 BDLauncherBridge 驱动");
                return;
            }

            Debug.Log("[TalosE2E] 非 PlayMode，恢复静态 TCP 服务...");
            RestoreStaticService(port);
        }

        /// <summary>
        /// 判断脚本域重载后是否需要恢复 editor-only 静态服务。
        /// Decide whether the editor-only static service should be restored after a domain reload.
        /// </summary>
        private static bool ShouldRestoreStaticServiceAfterReload(bool wasActive, bool isPlaying)
        {
            return wasActive && !isPlaying;
        }

        /// <summary>
        /// 恢复 editor-only 静态 TCP 服务。
        /// Restore the editor-only static TCP service.
        /// 当 Editor 从 PlayMode 回到编辑态后，宿主需要重新准备 editor-only 上下文，
        /// 然后由 E2E 重新启动静态 TCP 服务并恢复主线程 tick。
        /// After the editor returns from PlayMode to edit mode, the host must re-prepare its editor-only context,
        /// and then E2E restarts the static TCP service and restores the main-thread tick.
        /// </summary>
        private static void RestoreStaticService(int port)
        {
            try
            {
                TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
                TalosE2EBootstrap.LaunchE2EStatic(port);
                Debug.Log($"[TalosE2E] 静态 TCP 服务已恢复，端口: {port}");

                EditorApplication.update -= EditorOnlyTick;
                EditorApplication.update += EditorOnlyTick;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalosE2E] 恢复静态服务失败: {ex}");
                ClearE2EMark();
            }
        }

        /// <summary>
        /// 从命令行参数读取 Talos 端口。
        /// Read the Talos port from command line arguments.
        /// </summary>
        private static int ReadConfiguredPort(int defaultPort = Protocol.DefaultPort)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (!string.Equals(args[index], "-talosPort", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(args[index + 1], out var port))
                {
                    return port;
                }
            }

            return defaultPort;
        }

        /// <summary>
        /// 从命令行参数读取结果导出路径。
        /// Read the result export path from command line arguments.
        /// </summary>
        private static string ReadConfiguredOutputPath(string defaultOutputPath = "talos_e2e_results.json")
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], "-talosOutput", StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return defaultOutputPath;
        }

    }
}
