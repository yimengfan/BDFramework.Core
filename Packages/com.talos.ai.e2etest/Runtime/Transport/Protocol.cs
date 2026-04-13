/// <summary>
/// Talos E2E 测试框架——TCP 传输层。
/// 提供轻量级 TCP 服务端，用于接收 Playwright 端的测试指令并回传结果。
/// 设计上与 BDFramework 的 Telepathy 库解耦，直接使用 System.Net 以减少依赖。
/// </summary>
namespace Talos.E2E.Transport
{
    /// <summary>
    /// 传输层协议常量定义。
    /// 所有通信均使用 4 字节大端长度前缀 + UTF-8 JSON 明文。
    /// </summary>
    static public class Protocol
    {
        /// <summary>
        /// 默认测试服务监听端口，可通过命令行参数 -talosPort 覆盖。
        /// </summary>
        public const int DefaultPort = 10002;

        /// <summary>
        /// 消息类型：握手请求，Playwright 连接后的第一条消息。
        /// </summary>
        public const string MsgHello = "hello";

        /// <summary>
        /// 消息类型：握手响应，Unity 端确认连接。
        /// </summary>
        public const string MsgHelloAck = "hello_ack";

        /// <summary>
        /// 消息类型：Ping 保活。
        /// </summary>
        public const string MsgPing = "ping";

        /// <summary>
        /// 消息类型：Pong 保活响应。
        /// </summary>
        public const string MsgPong = "pong";

        /// <summary>
        /// 消息类型：执行单个测试指令。
        /// Payload 包含 testSuite 和 testName。
        /// </summary>
        public const string MsgRunTest = "run_test";

        /// <summary>
        /// 消息类型：执行所有测试指令。
        /// </summary>
        public const string MsgRunAllTests = "run_all_tests";

        /// <summary>
        /// 消息类型：执行指定测试套件。
        /// Payload 包含 testSuite。
        /// </summary>
        public const string MsgRunSuite = "run_suite";

        /// <summary>
        /// 消息类型：单个测试结果。
        /// 由 Unity 端在测试完成后逐条推送。
        /// </summary>
        public const string MsgTestResult = "test_result";

        /// <summary>
        /// 消息类型：全部测试完成通知。
        /// </summary>
        public const string MsgAllTestsComplete = "all_tests_complete";

        /// <summary>
        /// 消息类型：执行自定义动作指令。
        /// 用于框架级操作（如触发下载、切换场景等）。
        /// </summary>
        public const string MsgAction = "action";

        /// <summary>
        /// 消息类型：动作执行结果。
        /// </summary>
        public const string MsgActionResult = "action_result";

        /// <summary>
        /// 消息类型：查询测试列表。
        /// </summary>
        public const string MsgListTests = "list_tests";

        /// <summary>
        /// 消息类型：测试列表响应。
        /// </summary>
        public const string MsgTestList = "test_list";

        /// <summary>
        /// 消息类型：日志消息推送。
        /// Unity 端将运行时日志实时推送给 Playwright 端。
        /// </summary>
        public const string MsgLog = "log";

        /// <summary>
        /// 消息类型：错误消息。
        /// </summary>
        public const string MsgError = "error";

        /// <summary>
        /// 消息类型：编辑器命令——Playwright 端请求执行 UnityEditor 操作。
        /// Payload 包含 command（命令名）和可选的 params（参数字典）。
        /// 用于 headed GUI 模式下控制 Unity Editor 行为。
        /// </summary>
        public const string MsgEditorCommand = "editor_command";

        /// <summary>
        /// 消息类型：编辑器命令执行结果。
        /// </summary>
        public const string MsgEditorCommandResult = "editor_command_result";
    }
}
