using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using UnityEngine;
using LitJson;
using Talos.E2E.Transport;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 测试引导组件——整个 Talos E2E 测试系统的运行时入口。
    /// 
    /// 设计角色：
    /// - 在热更 DLL 加载后自动挂载到场景中的 GameObject 上。
    /// - 启动 TCP 服务端，接受 Playwright 端的连接和指令。
    /// - 将 Playwright 端的协议消息路由到 E2ETestRunner 执行。
    /// - 将测试结果实时推送回 Playwright 端。
    /// 
    /// 生命周期：
    /// 1. 热更 DLL 加载后，检查是否存在 E2E 测试标记（DEBUG 模式）。
    /// 2. 如果标记存在，自动创建 GameObject 并挂载此组件。
    /// 3. 在 Start() 中初始化测试运行器并启动 TCP 服务。
    /// 4. OnApplicationQuit() 中清理资源。
    /// 
    /// 使用示例（在热更代码中手动启动）：
    /// <code>
    /// TalosE2EBootstrap.LaunchE2E(10002);
    /// </code>
    /// </summary>
    public class TalosE2EBootstrap : MonoBehaviour
    {
        /// <summary>
        /// TCP 服务端实例。
        /// </summary>
        private TalosTcpServer _server;

        /// <summary>
        /// 纯静态模式下的 TCP 服务端实例（不依赖 MonoBehaviour）。
        /// </summary>
        private static TalosTcpServer _staticServer;

        /// <summary>
        /// 主线程消息队列——TCP 工作线程将消息入队，主线程出队处理。
        /// 解决 Unity API（如 persistentDataPath、Shader.Find）只能在主线程调用的问题。
        /// </summary>
        private static readonly ConcurrentQueue<string> _mainThreadQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// 编辑器命令处理器委托——由 Editor 程序集设置。
        /// 由于 Runtime 程序集无法引用 Editor 程序集（Unity 限制），
        /// editor_command 的实际处理逻辑通过此委托在 Editor 端注入。
        /// 参数: (string command, JsonData parameters) → 返回 object 结果。
        /// </summary>
        public static Func<string, JsonData, object> EditorCommandHandler { get; set; }

        /// <summary>
        /// 单例引用，用于防止重复创建。
        /// </summary>
        private static TalosE2EBootstrap _instance;

        /// <summary>
        /// 当前是否已启动 E2E 测试模式。
        /// </summary>
        public static bool IsE2EActive => _instance != null;

        /// <summary>
        /// 手动启动 E2E 测试引导。
        /// 通常由热更代码在检测到 E2E 测试 DLL 后调用。
        /// </summary>
        /// <param name="port">TCP 监听端口，默认 10002。</param>
        public static void LaunchE2E(int port = Protocol.DefaultPort)
        {
            if (_instance != null)
            {
                Debug.LogWarning("[TalosE2E] E2E 测试引导已启动，先释放旧实例再重新启动");
                Shutdown();
            }

            // 同样释放可能残留的静态服务
            if (_staticServer != null)
            {
                Debug.LogWarning("[TalosE2E] 发现残留的静态 TCP 服务，先释放");
                Shutdown();
            }

            Debug.Log("[TalosE2E] 正在启动 E2E 测试引导...");

            // 创建持久化 GameObject
            var go = new GameObject("[TalosE2E]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TalosE2EBootstrap>();
            _instance.InitServer(port);
        }

        /// <summary>
        /// 纯静态模式启动 E2E 测试服务——不依赖 MonoBehaviour，不需要 PlayMode。
        /// 
        /// 设计角色：
        /// - 在 Editor batchmode 下使用，当进入 PlayMode 会触发许可证崩溃时。
        /// - 直接在 Editor 环境中启动 TCP 服务和测试运行器。
        /// - 所有消息处理同步执行（不使用协程）。
        /// 
        /// 生命周期：
        /// 1. 初始化测试运行器，发现所有 [E2ETest] 用例。
        /// 2. 启动 TCP 服务端，接受 Playwright 端连接。
        /// 3. 收到消息后同步处理并返回结果。
        /// </summary>
        /// <param name="port">TCP 监听端口，默认 10002。</param>
        public static void LaunchE2EStatic(int port = Protocol.DefaultPort)
        {
            // 如果已有 MonoBehaviour 实例或静态服务在运行，先释放再重新启动
            if (_instance != null || _staticServer != null)
            {
                Debug.LogWarning("[TalosE2E] 发现已有的 E2E 服务（MonoBehaviour 或静态），先释放再重新启动");
                Shutdown();
            }

            Debug.Log("[TalosE2E] 正在启动 E2E 测试引导（纯静态模式，无 MonoBehaviour）...");

            // Phase 1: 初始化测试运行器
            E2ETestRunner.Initialize();

            // Phase 2: 创建并启动 TCP 服务端
            _staticServer = new TalosTcpServer(port);
            _staticServer.OnMessage += HandleStaticMessage;
            _staticServer.OnError += (err) => Debug.LogError($"[TalosE2E] TCP 错误: {err}");
            _staticServer.Start();

            Debug.Log($"[TalosE2E] E2E 静态服务就绪，端口: {port}，测试用例数: {E2ETestRunner.GetTestList().Count}");
        }

        /// <summary>
        /// 初始化 TCP 服务端和测试运行器。
        /// </summary>
        private void InitServer(int port)
        {
            // Phase 1: 初始化测试运行器，发现所有 [E2ETest] 用例。
            E2ETestRunner.Initialize();

            // Phase 2: 创建并启动 TCP 服务端。
            _server = new TalosTcpServer(port);
            // 使用队列模式：TCP 工作线程入队，主线程 Update() 出队处理。
            // 确保所有 Unity API（persistentDataPath、Shader.Find 等）在主线程执行。
            _server.OnMessage += EnqueueMessage;
            _server.OnError += (err) => Debug.LogError($"[TalosE2E] TCP 错误: {err}");
            _server.Start();

            Debug.Log($"[TalosE2E] E2E 测试系统就绪，端口: {port}，测试用例数: {E2ETestRunner.GetTestList().Count}");
        }

        /// <summary>
        /// 将 TCP 工作线程收到的消息入队到主线程队列。
        /// 不在 TCP 线程直接处理——Unity API 只能在主线程调用。
        /// </summary>
        private void EnqueueMessage(string json)
        {
            _mainThreadQueue.Enqueue(json);
        }

        /// <summary>
        /// Unity 生命周期：每帧处理 TCP 消息队列。
        /// 确保 Unity API 在主线程安全执行。
        /// </summary>
        private void Update()
        {
            ProcessMainThreadActions();
        }

        /// <summary>
        /// 处理从 Playwright 端收到的协议消息。
        /// 消息格式为 JSON，包含 type 字段标识消息类型。
        /// </summary>
        /// <param name="json">收到的 JSON 消息字符串。</param>
        private void HandleMessage(string json)
        {
            try
            {
                var msg = JsonMapper.ToObject(json);
                var type = (string)msg["type"];

                Debug.Log($"[TalosE2E] 收到指令: {type}");

                switch (type)
                {
                    case Protocol.MsgHello:
                        SendResponse(new { type = Protocol.MsgHelloAck, version = "0.1.0", testCount = E2ETestRunner.GetTestList().Count });
                        break;

                    case Protocol.MsgPing:
                        SendResponse(new { type = Protocol.MsgPong, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
                        break;

                    case Protocol.MsgListTests:
                        HandleListTests();
                        break;

                    case Protocol.MsgRunTest:
                        HandleRunTest(msg);
                        break;

                    case Protocol.MsgRunSuite:
                        HandleRunSuite(msg);
                        break;

                    case Protocol.MsgRunAllTests:
                        HandleRunAllTests();
                        break;

                    case Protocol.MsgAction:
                        HandleAction(msg);
                        break;

                    case Protocol.MsgEditorCommand:
                        HandleEditorCommand(msg);
                        break;

                    default:
                        SendResponse(new { type = Protocol.MsgError, message = $"未知消息类型: {type}" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 消息处理异常: {ex}");
                SendResponse(new { type = Protocol.MsgError, message = ex.Message });
            }
        }

        /// <summary>
        /// 处理 list_tests 指令，返回所有已发现的测试用例列表。
        /// </summary>
        private void HandleListTests()
        {
            var tests = E2ETestRunner.GetTestList();
            SendResponse(new { type = Protocol.MsgTestList, tests });
        }

        /// <summary>
        /// 处理 run_test 指令，执行单个测试并返回结果。
        /// </summary>
        private void HandleRunTest(JsonData msg)
        {
            var methodName = (string)msg["methodName"];
            var result = E2ETestRunner.RunSingle(methodName);
            SendResponse(new { type = Protocol.MsgTestResult, result });
            SendResponse(new { type = Protocol.MsgAllTestsComplete, total = 1, passed = result.passed ? 1 : 0, failed = result.passed ? 0 : 1 });
        }

        /// <summary>
        /// 处理 run_suite 指令，执行指定套件的所有测试并逐条推送结果。
        /// </summary>
        private void HandleRunSuite(JsonData msg)
        {
            var suite = (string)msg["suite"];
            var results = E2ETestRunner.RunSuite(suite);

            foreach (var r in results)
            {
                SendResponse(new { type = Protocol.MsgTestResult, result = r });
            }

            SendResponse(new
            {
                type = Protocol.MsgAllTestsComplete,
                total = results.Count,
                passed = results.Count(r => r.passed),
                failed = results.Count(r => !r.passed)
            });
        }

        /// <summary>
        /// 处理 run_all_tests 指令，执行所有测试用例并逐条推送结果。
        /// 这是 CI 流程中最常用的指令。
        /// </summary>
        private void HandleRunAllTests()
        {
            // 在协程中执行以避免阻塞主线程
            StartCoroutine(RunAllTestsCoroutine());
        }

        /// <summary>
        /// 协程：逐条执行所有测试用例，每条执行后推送结果。
        /// 每个测试之间等待一帧，确保 Unity 引擎正常更新。
        /// </summary>
        private IEnumerator RunAllTestsCoroutine()
        {
            int passed = 0;
            int failed = 0;
            int total = 0;

            var results = E2ETestRunner.RunAll(result =>
            {
                total++;
                if (result.passed) passed++; else failed++;
                SendResponse(new { type = Protocol.MsgTestResult, result });
            });

            // 等待一帧确保所有消息已发送
            yield return null;

            SendResponse(new
            {
                type = Protocol.MsgAllTestsComplete,
                total,
                passed,
                failed
            });

            Debug.Log($"[TalosE2E] 全部测试完成: 总计={total}, 通过={passed}, 失败={failed}");
        }

        /// <summary>
        /// 处理 action 指令——执行自定义动作（框架级操作）。
        /// 目前支持：start_download, trigger_update, restart_app。
        /// </summary>
        private void HandleAction(JsonData msg)
        {
            var action = (string)msg["action"];
            Debug.Log($"[TalosE2E] 执行动作: {action}");

            // TODO: 根据业务需要扩展更多动作
            switch (action)
            {
                case "screenshot":
                {
                    var screenshotName = TryGetString(msg, "name", "talos-step");
                    StartCoroutine(CaptureScreenshotAndRespond(action, screenshotName));
                    break;
                }
                default:
                    SendResponse(new { type = Protocol.MsgActionResult, action, success = false, error = $"未实现的动作: {action}" });
                    break;
            }
        }

        /// <summary>
        /// 在 MonoBehaviour 运行态截取当前 Game 画面，并把 PNG 内容回传给 Playwright。
        /// 这里统一等待一帧结束，避免在渲染未完成时拿到空白或未定义截图。
        /// </summary>
        private IEnumerator CaptureScreenshotAndRespond(string action, string screenshotName)
        {
            yield return new WaitForEndOfFrame();

            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null)
                {
                    SendResponse(new { type = Protocol.MsgActionResult, action, success = false, error = "截图失败：未获取到有效的屏幕纹理" });
                    yield break;
                }

                var bytes = texture.EncodeToPNG();
                if (bytes == null || bytes.Length == 0)
                {
                    SendResponse(new { type = Protocol.MsgActionResult, action, success = false, error = "截图失败：PNG 编码结果为空" });
                    yield break;
                }

                var screenshotDir = Path.Combine(Application.persistentDataPath, "talos-e2e", "screenshots");
                Directory.CreateDirectory(screenshotDir);

                var fileName = BuildScreenshotFileName(screenshotName);
                var screenshotPath = Path.Combine(screenshotDir, fileName);
                File.WriteAllBytes(screenshotPath, bytes);

                Debug.Log($"[TalosE2E] 步骤截图已保存: {screenshotPath}");
                SendResponse(new
                {
                    type = Protocol.MsgActionResult,
                    action,
                    success = true,
                    data = new
                    {
                        fileName,
                        path = screenshotPath,
                        contentBase64 = Convert.ToBase64String(bytes),
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 步骤截图失败: {ex}");
                SendResponse(new { type = Protocol.MsgActionResult, action, success = false, error = $"截图失败: {ex.Message}" });
            }
            finally
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        /// <summary>
        /// 生成稳定且可落盘的截图文件名，避免步骤标题中的路径分隔符污染文件系统。
        /// </summary>
        private static string BuildScreenshotFileName(string rawName)
        {
            var normalized = string.IsNullOrWhiteSpace(rawName) ? "talos-step" : rawName.Trim();
            var safeChars = normalized.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var safeName = new string(safeChars).Trim('_');
            if (string.IsNullOrEmpty(safeName))
            {
                safeName = "talos-step";
            }

            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{safeName}.png";
        }

        /// <summary>
        /// 处理 editor_command 指令——通过委托路由到 Editor 程序集。
        /// 在 MonoBehaviour 模式下，此方法在主线程执行。
        /// </summary>
        private void HandleEditorCommand(JsonData msg)
        {
            var command = (string)msg["command"];
            var requestId = TryGetString(msg, "requestId", "");

            Debug.Log($"[TalosE2E] 收到编辑器命令（MonoBehaviour 模式）: {command}, requestId={requestId}");

            try
            {
                var parameters = TryGetField(msg, "params");

                // 通过委托调用 Editor 端的 EditorCommandDispatcher
                if (EditorCommandHandler == null)
                {
                    throw new InvalidOperationException(
                        "EditorCommandHandler 未设置。" +
                        "请在 Editor 端调用 TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch");
                }

                var result = EditorCommandHandler(command, parameters);

                Debug.Log($"[TalosE2E] 编辑器命令完成（MonoBehaviour 模式）: {command}, 成功=true");
                SendResponse(new
                {
                    type = Protocol.MsgEditorCommandResult,
                    requestId,
                    command,
                    success = true,
                    data = result,
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 编辑器命令异常（MonoBehaviour 模式）: {command} -> {ex}");
                SendResponse(new
                {
                    type = Protocol.MsgEditorCommandResult,
                    requestId,
                    command,
                    success = false,
                    error = $"{ex.GetType().Name}: {ex.Message}",
                });
            }
        }

        /// <summary>
        /// 向所有已连接客户端发送 JSON 响应。
        /// </summary>
        private void SendResponse(object data)
        {
            var json = JsonMapper.ToJson(data);
            _server?.Broadcast(json);
        }

        /// <summary>
        /// 统一发送响应——自动路由到 MonoBehaviour 或静态 TCP 服务端。
        /// 在主线程处理消息时使用，确保无论哪种模式都能正确发送。
        /// </summary>
        private static void BroadcastResponse(object data)
        {
            var json = JsonMapper.ToJson(data);
            // 优先使用 MonoBehaviour 实例的服务端
            if (_instance != null && _instance._server != null)
            {
                _instance._server.Broadcast(json);
            }
            // 回退到静态服务端
            else if (_staticServer != null)
            {
                _staticServer.Broadcast(json);
            }
        }

        /// <summary>
        /// 统一释放所有 E2E 资源——销毁 MonoBehaviour 实例、停止静态 TCP 服务、清空消息队列。
        /// 在 LaunchE2E / LaunchE2EStatic 发现已有连接时调用，确保端口和资源被正确释放。
        /// </summary>
        public static void Shutdown()
        {
            Debug.Log("[TalosE2E] 正在释放所有 E2E 资源...");

            // Phase 1: 停止 MonoBehaviour 实例的 TCP 服务
            if (_instance != null)
            {
                _instance._server?.Stop();
                _instance._server = null;

                // 销毁 GameObject
                if (_instance.gameObject != null)
                {
                    UnityEngine.Object.Destroy(_instance.gameObject);
                }
                _instance = null;
            }

            // Phase 2: 停止静态 TCP 服务
            if (_staticServer != null)
            {
                _staticServer.Stop();
                _staticServer = null;
            }

            // Phase 3: 清空消息队列，避免残留消息被新实例处理
            while (_mainThreadQueue.TryDequeue(out _)) { }

            Debug.Log("[TalosE2E] E2E 资源释放完成");
        }

        /// <summary>
        /// Unity 生命周期：应用退出时清理 TCP 服务端资源。
        /// </summary>
        private void OnApplicationQuit()
        {
            Debug.Log("[TalosE2E] 应用退出，清理 E2E 测试资源");
            _server?.Stop();
            _server = null;
        }

        /// <summary>
        /// Unity 生命周期：应用暂停时暂停测试。
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"[TalosE2E] 应用暂停状态变化: {pauseStatus}");
        }

        // ====================================================================
        // 纯静态模式的消息处理（不依赖 MonoBehaviour）
        // ====================================================================

        /// <summary>
        /// 静态模式下的消息处理器——将消息入队到主线程队列，不直接在 TCP 工作线程执行。
        /// TCP 工作线程不能调用 Unity API（persistentDataPath、Shader.Find 等），
        /// 所以将消息转发到主线程处理。
        /// </summary>
        /// <param name="json">收到的 JSON 消息字符串。</param>
        private static void HandleStaticMessage(string json)
        {
            // 入队到主线程，由 ProcessMainThreadActions() 在主线程中处理
            _mainThreadQueue.Enqueue(json);
        }

        /// <summary>
        /// 在主线程中处理所有排队的消息。
        /// 由 EditorApplication.update 或 MonoBehaviour.Update 调用，确保 Unity API 可用。
        /// 每帧最多处理 32 条消息，避免单帧耗时过长。
        /// </summary>
        public static void ProcessMainThreadActions()
        {
            int processed = 0;
            const int maxPerFrame = 32;

            while (processed < maxPerFrame && _mainThreadQueue.TryDequeue(out var json))
            {
                processed++;
                ProcessSingleMessageOnMainThread(json);
            }
        }

        /// <summary>
        /// 在主线程上处理单条协议消息——路由到对应的处理逻辑。
        /// 所有 Unity API 调用在此方法及其子方法中执行，确保线程安全。
        /// 使用 BroadcastResponse 自动路由到 MonoBehaviour 或静态 TCP 服务端。
        /// </summary>
        private static void ProcessSingleMessageOnMainThread(string json)
        {
            try
            {
                var msg = JsonMapper.ToObject(json);
                var type = (string)msg["type"];

                Debug.Log($"[TalosE2E] 主线程处理指令: {type}");

                switch (type)
                {
                    case Protocol.MsgHello:
                        BroadcastResponse(new { type = Protocol.MsgHelloAck, version = "0.1.0", testCount = E2ETestRunner.GetTestList().Count });
                        break;

                    case Protocol.MsgPing:
                        BroadcastResponse(new { type = Protocol.MsgPong, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
                        break;

                    case Protocol.MsgListTests:
                        var tests = E2ETestRunner.GetTestList();
                        BroadcastResponse(new { type = Protocol.MsgTestList, tests });
                        break;

                    case Protocol.MsgRunTest:
                        var methodName = (string)msg["methodName"];
                        var singleResult = E2ETestRunner.RunSingle(methodName);
                        BroadcastResponse(new { type = Protocol.MsgTestResult, result = singleResult });
                        BroadcastResponse(new { type = Protocol.MsgAllTestsComplete, total = 1, passed = singleResult.passed ? 1 : 0, failed = singleResult.passed ? 0 : 1 });
                        break;

                    case Protocol.MsgRunSuite:
                        var suite = (string)msg["suite"];
                        var suiteResults = E2ETestRunner.RunSuite(suite);
                        foreach (var r in suiteResults)
                        {
                            BroadcastResponse(new { type = Protocol.MsgTestResult, result = r });
                        }
                        BroadcastResponse(new
                        {
                            type = Protocol.MsgAllTestsComplete,
                            total = suiteResults.Count,
                            passed = suiteResults.Count(r => r.passed),
                            failed = suiteResults.Count(r => !r.passed)
                        });
                        break;

                    case Protocol.MsgRunAllTests:
                        HandleStaticRunAllTests();
                        break;

                    case Protocol.MsgAction:
                        var action = (string)msg["action"];
                        Debug.Log($"[TalosE2E] 执行动作（主线程）: {action}");
                        if (string.Equals(action, "screenshot", StringComparison.OrdinalIgnoreCase))
                        {
                            BroadcastResponse(new { type = Protocol.MsgActionResult, action, success = false, error = "静态模式暂不支持步骤截图，请在 Player 或 PlayMode 运行态执行该动作" });
                        }
                        else
                        {
                            BroadcastResponse(new { type = Protocol.MsgActionResult, action, success = false, error = $"未实现的动作: {action}" });
                        }
                        break;

                    case Protocol.MsgEditorCommand:
                        HandleStaticEditorCommand(msg);
                        break;

                    default:
                        BroadcastResponse(new { type = Protocol.MsgError, message = $"未知消息类型: {type}" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 主线程消息处理异常: {ex}");
                BroadcastResponse(new { type = Protocol.MsgError, message = ex.Message });
            }
        }

        /// <summary>
        /// 同步执行所有测试用例并逐条推送结果（主线程安全）。
        /// 不使用协程，直接同步执行——适用于 MonoBehaviour 和静态两种模式。
        /// </summary>
        private static void HandleStaticRunAllTests()
        {
            int passed = 0;
            int failed = 0;
            int total = 0;

            // 同步执行所有测试（不使用协程）
            E2ETestRunner.RunAll(result =>
            {
                total++;
                if (result.passed) passed++; else failed++;
                BroadcastResponse(new { type = Protocol.MsgTestResult, result });
            });

            BroadcastResponse(new
            {
                type = Protocol.MsgAllTestsComplete,
                total,
                passed,
                failed
            });

            Debug.Log($"[TalosE2E] 全部测试完成: 总计={total}, 通过={passed}, 失败={failed}");
        }

        /// <summary>
        /// 旧版静态模式发送响应——保留向后兼容。
        /// 新代码应使用 BroadcastResponse，自动路由到活跃的 TCP 服务端。
        /// </summary>
        private static void StaticSendResponse(object data)
        {
            BroadcastResponse(data);
        }

        /// <summary>
        /// 处理 editor_command 消息——通过委托路由到 Editor 程序集的 EditorCommandDispatcher。
        /// editor_command 是万能编辑器操作通道，Playwright 端可执行任意 UnityEditor 接口。
        /// 由于此方法已在主线程上执行（由 ProcessMainThreadActions 调用），所有 Unity API 可安全使用。
        /// </summary>
        private static void HandleStaticEditorCommand(LitJson.JsonData msg)
        {
            var command = (string)msg["command"];
            var requestId = TryGetString(msg, "requestId", "");

            Debug.Log($"[TalosE2E] 收到编辑器命令: {command}, requestId={requestId}");

            try
            {
                // 提取参数
                var parameters = TryGetField(msg, "params");

                // 通过委托调用 Editor 端的 EditorCommandDispatcher
                if (EditorCommandHandler == null)
                {
                    throw new InvalidOperationException(
                        "EditorCommandHandler 未设置。" +
                        "请在 Editor 端调用 TalosE2EBootstrap.EditorCommandHandler = EditorCommandDispatcher.Dispatch");
                }

                var result = EditorCommandHandler(command, parameters);

                Debug.Log($"[TalosE2E] 编辑器命令完成: {command}, 成功=true");
                BroadcastResponse(new
                {
                    type = Protocol.MsgEditorCommandResult,
                    requestId,
                    command,
                    success = true,
                    data = result,
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TalosE2E] 编辑器命令异常: {command} -> {ex}");
                BroadcastResponse(new
                {
                    type = Protocol.MsgEditorCommandResult,
                    requestId,
                    command,
                    success = false,
                    error = $"{ex.GetType().Name}: {ex.Message}",
                });
            }
        }

        /// <summary>
        /// 安全获取 JsonData 中的字符串字段——字段不存在时返回默认值。
        /// LitJson 的 JsonData 不提供 ContainsKey 方法，使用 try-catch 安全访问。
        /// </summary>
        private static string TryGetString(JsonData msg, string key, string defaultValue)
        {
            try
            {
                var val = msg[key];
                return val != null ? (string)val : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全获取 JsonData 中的字段——字段不存在时返回 null。
        /// </summary>
        private static JsonData TryGetField(JsonData msg, string key)
        {
            try
            {
                return msg[key];
            }
            catch
            {
                return null;
            }
        }
    }
}
