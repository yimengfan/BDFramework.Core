using System.IO;
using UnityEditor;
using UnityEngine;
using BDFramework;
using BDFramework.Core.Tools;
using BDFramework.Configure;
using BDFramework.Asset;
using BDFramework.Sql;
using BDFramework.ResourceMgr;
using Talos.E2E.Transport;

namespace Talos.E2E.Editor
{
    /// <summary>
    /// E2E 测试编辑器工具——提供 Editor 下的测试辅助功能。
    /// 
    /// 设计角色：
    /// - 在 Editor 中创建/移除 DEBUG 标记文件。
    /// - 启动/停止 E2E 测试 TCP 服务（Editor 模式下）。
    /// - 通过 [DidReloadScripts] 在 Domain Reload 后自动恢复 TCP 服务。
    /// - 查看 E2E 测试系统状态。
    /// 
    /// Domain Reload 恢复机制：
    /// Unity Editor 进入/退出 PlayMode 时会触发 Domain Reload，所有静态变量被清零。
    /// 此类使用 EditorPrefs 记录 E2E 服务状态，在 Domain Reload 后自动恢复 TCP 服务。
    /// 恢复流程：DidReloadScripts → 检测 PlayMode + E2E 标记 → 恢复委托 → 重启 TCP。
    /// </summary>
    static public class E2EEditorTools
    {
        /// <summary>
        /// EditorPrefs 键——E2E 服务是否活跃。
        /// </summary>
        private const string PREFS_E2E_ACTIVE = "TalosE2E_Active";

        /// <summary>
        /// EditorPrefs 键——E2E 服务的 TCP 端口号。
        /// </summary>
        private const string PREFS_E2E_PORT = "TalosE2E_Port";
        /// <summary>
        /// 在 StreamingAssets 热更目录中创建 DEBUG 标记。
        /// </summary>
        [MenuItem("Talos/E2E Test/创建 DEBUG 标记")]
        static public void CreateDebugMarker()
        {
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            var dir = Path.Combine(Application.streamingAssetsPath, platform, "script", "hotfix");
            DebugBuildMarker.CreateMarker(dir);
            Debug.Log("[TalosE2E] DEBUG 标记创建完成");
        }

        /// <summary>
        /// 移除 StreamingAssets 热更目录中的 DEBUG 标记。
        /// </summary>
        [MenuItem("Talos/E2E Test/移除 DEBUG 标记")]
        static public void RemoveDebugMarker()
        {
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            var markerPath = Path.Combine(Application.streamingAssetsPath, platform, "script", "hotfix", DebugBuildMarker.MARKER_FILENAME);
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
        /// BatchMode 入口——供 CI 的 Unity Player 测试脚本调用。
        /// 通过 Unity -batchmode -executeMethod 启动 Editor，进入 PlayMode 并等待框架自动启动 E2E。
        /// 
        /// 职责（精简版）：
        /// - 打开 BDFrame 场景
        /// - 注册 EditorCommandHandler 委托
        /// - 标记 E2E 活跃（供 DidReloadScripts 恢复用）
        /// - 进入 PlayMode
        /// 
        /// 不负责：框架初始化、TCP 启动——这些由 BDLauncherBridge.Launch() → TryStartE2EAutomation() 统一处理。
        /// 
        /// 使用方式（命令行）：
        /// <code>
        /// Unity -batchmode -projectPath &lt;project&gt; -executeMethod Talos.E2E.Editor.E2EEditorTools.LaunchE2EBatchMode -talosPort 10002 -talosForceE2E
        /// </code>
        /// </summary>
        public static void LaunchE2EBatchMode()
        {
            Debug.Log("[TalosE2E] LaunchE2EBatchMode 入口被调用");

            // Phase 1: 读取命令行参数
            int port = 10002;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-talosPort", System.StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(args[i + 1], out port);
                }
            }
            Debug.Log($"[TalosE2E] 配置端口: {port}");

            // Phase 2: 打开 BDFrame 启动场景（框架自带）
            // 该场景包含 BDLauncher，进入 PlayMode 后会自动走热更 DLL 加载流程
            var scenePath = System.IO.Path.Combine("Assets", "Scenes", "BDFrame.unity");
            if (System.IO.File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[TalosE2E] 已打开场景: {scenePath}");
            }
            else
            {
                // 尝试带 Debug 后缀的场景
                var altScenePath = System.IO.Path.Combine("Assets", "Scenes", "BDFrame_Debug.unity");
                if (System.IO.File.Exists(altScenePath))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(altScenePath);
                    Debug.Log($"[TalosE2E] 已打开场景: {altScenePath}");
                }
                else
                {
                    Debug.LogWarning("[TalosE2E] 未找到 BDFrame 场景，将使用当前场景进入 PlayMode");
                }
            }

            // Phase 3: 注册编辑器命令处理器委托
            // Runtime 程序集无法引用 Editor 程序集，通过委托注入 EditorCommandDispatcher
            TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
            Debug.Log("[TalosE2E] EditorCommandHandler 委托已注册（BatchMode）");

            // Phase 4: 标记 E2E 活跃状态，以便 Domain Reload 后自动恢复
            MarkE2EActive(port);

            // Phase 5: 进入 PlayMode
            // 进入 PlayMode 后，BDLauncher → BDLauncherBridge.Launch() → TryStartE2EAutomation()
            // E2E 的 TCP 启动完全由 Runtime 侧驱动，Editor 侧不再干预。
            Debug.Log("[TalosE2E] 正在进入 PlayMode（E2E 启动将由 BDLauncherBridge 驱动）...");
            EditorApplication.EnterPlaymode();
        }

        // ====================================================================
        // Editor-only 模式（不进入 PlayMode）
        // ====================================================================

        /// <summary>
        /// Editor-only 入口——不进入 PlayMode，直接在 Editor 环境中初始化框架并启动 E2E TCP 服务。
        /// 
        /// 使用场景：
        /// - 当 Unity Pro 许可证过期或无效时，进入 PlayMode 会触发 CheckLicenseActivated() segfault。
        /// - 此方法绕过 PlayMode，在 Editor 环境中手动执行框架初始化的各个步骤。
        /// 
        /// 执行流程：
        /// 1. 读取命令行参数
        /// 2. 手动执行框架初始化（配置加载 → 资源系统 → 数据库）
        /// 3. 启动 E2E TCP 服务（纯静态模式，不依赖 MonoBehaviour）
        /// 
        /// 使用方式（命令行）：
        /// <code>
        /// Unity -batchmode -nographics -projectPath &lt;project&gt; \
        ///   -executeMethod Talos.E2E.Editor.E2EEditorTools.LaunchE2EEditorOnly \
        ///   -talosPort 10002 -talosForceE2E
        /// </code>
        /// </summary>
        public static void LaunchE2EEditorOnly()
        {
            Debug.Log("[TalosE2E] LaunchE2EEditorOnly 入口被调用（Editor-only 模式，不进入 PlayMode）");

            // Phase 1: 读取命令行参数
            int port = 10002;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-talosPort", System.StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(args[i + 1], out port);
                }
            }
            Debug.Log($"[TalosE2E] 配置端口: {port}");

            // Phase 2: 手动初始化框架子系统
            // 复制 BDLauncherBridge.Launch() 的关键步骤，跳过 IEnumeratorTool（E2E 测试不需要协程）
            InitFrameworkSubsystems();

            // Phase 2.5: 注册编辑器命令处理器委托
            // Runtime 程序集无法引用 Editor 程序集，通过委托注入 EditorCommandDispatcher
            TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
            Debug.Log("[TalosE2E] EditorCommandHandler 委托已注册");

            // Phase 3: 启动 E2E TCP 服务（纯静态模式）
            try
            {
                TalosE2EBootstrap.LaunchE2EStatic(port);
                Debug.Log($"[TalosE2E] E2E TCP 服务已启动，端口: {port}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalosE2E] TCP 服务启动失败: {ex}");
                EditorApplication.Exit(1);
                return;
            }

            // Phase 4: 注册 EditorApplication.update 以保持 TCP 服务运行
            // batchmode 下 Unity 需要持续 tick 才能处理网络消息
            EditorApplication.update += EditorOnlyTick;

            // Phase 5: 标记 E2E 活跃状态，以便 Domain Reload 后自动恢复
            MarkE2EActive(port);

            Debug.Log("[TalosE2E] Editor-only 模式初始化完成，等待 Playwright 连接...");
        }

        // ====================================================================
        // 同步执行模式（直接输出结果文件，不依赖 TCP 和主循环）
        // ====================================================================

        /// <summary>
        /// 同步执行入口——在 Editor batchmode + -quit 模式下使用。
        /// 
        /// 设计角色：
        /// - 当 Unity batchmode 因许可证问题无法保持主循环运行时使用。
        /// - 初始化框架后直接同步执行所有 E2E 测试。
        /// - 测试结果写入 JSON 文件（默认路径: 项目根目录/talos_e2e_results.json）。
        /// - 退出码反映测试结果（0=全部通过，1=有失败）。
        /// 
        /// 使用场景：
        /// - 本地开发环境 Unity 许可证无效导致 batchmode 主循环崩溃。
        /// - CI 环境中作为快速验证模式（不依赖 Playwright TCP 连接）。
        /// 
        /// 使用方式（命令行）：
        /// <code>
        /// Unity -batchmode -nographics -quit -projectPath &lt;project&gt; \
        ///   -executeMethod Talos.E2E.Editor.E2EEditorTools.RunE2EAndExport \
        ///   -talosPort 10002 -talosForceE2E \
        ///   -talosOutput results.json
        /// </code>
        /// </summary>
        public static void RunE2EAndExport()
        {
            Debug.Log("[TalosE2E] RunE2EAndExport 入口被调用（同步执行模式）");

            // Phase 1: 读取命令行参数
            int port = 10002;
            string outputPath = "talos_e2e_results.json";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-talosPort", System.StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(args[i + 1], out port);
                }
                if (string.Equals(args[i], "-talosOutput", System.StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = args[i + 1];
                }
            }
            Debug.Log($"[TalosE2E] 配置端口: {port}, 输出文件: {outputPath}");

            // Phase 2: 手动初始化框架子系统（与 LaunchE2EEditorOnly 相同）
            InitFrameworkSubsystems();

            // Phase 3: 初始化测试运行器并执行所有测试
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

            // Phase 4: 写入结果文件
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

            // 确保目录存在
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

            // Phase 5: 设置退出码（Unity -quit 模式下无法直接设置退出码，
            // 但可以通过让方法抛异常来让 Unity 以非零码退出）
            // 这里我们选择正常退出，由外部脚本检查结果文件内容
            if (failed > 0)
            {
                // 在 Unity -batchmode -quit 中，可以通过日志中的 "FAILED" 标记来判断
                Debug.LogError("[TalosE2E] 有测试失败，请查看结果文件获取详细信息");
                // 强制设置 EditorApplication.Exit 在 -quit 模式下无效，
                // 使用 Environment.Exit 来设置退出码
                System.Environment.ExitCode = 1;
            }
        }

        /// <summary>
        /// 初始化框架子系统——提取公共逻辑供 LaunchE2EEditorOnly 和 RunE2EAndExport 共用。
        /// </summary>
        private static void InitFrameworkSubsystems()
        {
            try
            {
                // 标记框架进入运行时阶段
                BApplication.IsPlaying = true;
                Debug.Log("[TalosE2E] BApplication.IsPlaying = true");

                // 加载框架基础配置
                BDFramework.Configure.GameConfigLoder.LoadFrameBaseConfig();
                Debug.Log("[TalosE2E] 框架基础配置加载完成");

                var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
                if (config != null)
                {
                    Debug.Log($"[TalosE2E] 框架版本: {BDLauncher.FrameworkVersion}, 母包版本: {config.ClientVersionNum}");

                    // 资源双路径解析
                    var clientVersion = config.ClientVersionNum;
                    var (firstLoadDir, secondLoadDir) =
                        ClientAssetsUtils.GetMultiAssetsLoadPath(BApplication.RuntimePlatform, clientVersion);

                    // Editor 模式下使用 secondLoadDir 作为主要路径
                    if (Application.isEditor)
                    {
                        firstLoadDir = secondLoadDir;
                    }
                    Debug.Log($"[TalosE2E] 资源路径 - 主路径: {firstLoadDir}");
                    Debug.Log($"[TalosE2E] 资源路径 - 备用路径: {secondLoadDir}");

                    // 检查基础资源完整性
                    ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
                    Debug.Log("[TalosE2E] 基础资源完整性检查完成");

                    // 初始化资源系统
                    BResources.Init(config.ArtRoot, firstLoadDir, secondLoadDir);
                    Debug.Log("[TalosE2E] 资源系统初始化完成");

                    // 初始化 SQLite
                    SqliteLoder.Init(config.SQLRoot, firstLoadDir, secondLoadDir);
                    Debug.Log("[TalosE2E] SQLite 初始化完成");
                }
                else
                {
                    Debug.LogWarning("[TalosE2E] 无法获取框架配置，跳过资源和数据库初始化");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalosE2E] 框架初始化失败: {ex}");
                // 即使初始化部分失败，仍然继续执行测试
            }
        }

        /// <summary>
        /// Editor-only 模式的 tick 回调。
        /// 在 batchmode 下持续调用以：
        /// 1. 保持 Editor 主循环活跃
        /// 2. 处理 TCP 消息队列（从工作线程转移到主线程执行）
        /// </summary>
        private static void EditorOnlyTick()
        {
            // TCP 服务端的消息在独立线程中入队，
            // 此处从主线程出队处理，确保 Unity API 可用
            TalosE2EBootstrap.ProcessMainThreadActions();
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
        /// Domain Reload 后自动恢复 E2E TCP 服务。
        /// 
        /// 触发时机：Unity 脚本编译完成后（包括进入/退出 PlayMode 的 Domain Reload）。
        /// 
        /// 恢复流程：
        /// 职责（精简版）：
        /// 1. 恢复 EditorCommandHandler 委托（Runtime→Editor 桥，Domain Reload 后静态变量被清零）
        /// 2. 如果在 PlayMode 中：仅恢复委托，不启动 TCP——TCP 由 BDLauncherBridge.Launch() 驱动
        /// 3. 如果不在 PlayMode（退出 PlayMode 后）：恢复静态 TCP 服务
        /// 
        /// 注意：此方法只在 Editor 环境中运行，不会影响真机构建。
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnDidReloadScripts()
        {
            // Phase 1: 检查是否需要恢复
            bool wasActive = EditorPrefs.GetBool(PREFS_E2E_ACTIVE, false);
            if (!wasActive)
            {
                return;
            }

            int port = EditorPrefs.GetInt(PREFS_E2E_PORT, Protocol.DefaultPort);
            Debug.Log($"[TalosE2E] DidReloadScripts: 检测到 E2E 活跃标记, port={port}, isPlaying={EditorApplication.isPlaying}");

            // Phase 2: 恢复 EditorCommandHandler 委托（Domain Reload 后静态变量被清零，必须重新注入）
            TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;
            Debug.Log("[TalosE2E] EditorCommandHandler 委托已恢复（DidReloadScripts）");

            if (EditorApplication.isPlaying)
            {
                // Phase 3a: PlayMode 内——仅恢复委托，不启动 TCP。
                // BDLauncherBridge.Launch() → TryStartE2EAutomation() 会统一驱动 E2E 启动。
                Debug.Log("[TalosE2E] PlayMode 内恢复委托完成，TCP 启动将由 BDLauncherBridge 驱动");
            }
            else
            {
                // Phase 3b: 非 PlayMode（退出 PlayMode 后）——恢复静态 TCP 服务
                // 这处理了"退出 PlayMode 后 Editor 仍然需要保持 TCP 服务"的场景
                Debug.Log("[TalosE2E] 非 PlayMode，恢复静态 TCP 服务...");
                RestoreStaticService(port);
            }
        }

        /// <summary>
        /// 恢复静态模式的 TCP 服务（退出 PlayMode 后调用）。
        /// 
        /// 使用场景：从 PlayMode 退出回到 Editor 后，需要保持 TCP 连接以接收外部测试命令。
        /// 重新初始化框架子系统、注册委托、启动静态 TCP、恢复 tick。
        /// </summary>
        private static void RestoreStaticService(int port)
        {
            try
            {
                // 重新初始化框架
                InitFrameworkSubsystems();

                // 重新注册委托
                TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch;

                // 启动静态 TCP 服务
                TalosE2EBootstrap.LaunchE2EStatic(port);
                Debug.Log($"[TalosE2E] 静态 TCP 服务已恢复，端口: {port}");

                // 恢复 tick
                EditorApplication.update -= EditorOnlyTick;
                EditorApplication.update += EditorOnlyTick;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalosE2E] 恢复静态服务失败: {ex}");
                ClearE2EMark();
            }
        }
    }
}
