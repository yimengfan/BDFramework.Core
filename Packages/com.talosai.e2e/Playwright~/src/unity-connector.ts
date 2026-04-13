/**
 * Unity TCP 连接器——Playwright 与 Unity Player 之间的 TCP 通信客户端。
 * 
 * 设计角色：
 * - 实现 Talos E2E 协议的 Node.js 端客户端。
 * - 提供 Promise 化的 API，便于在 Playwright 测试中 async/await 调用。
 * - 支持连接超时、消息超时、自动重连等容错机制。
 * 
 * 协议格式：
 * - 4 字节大端长度前缀 + UTF-8 JSON 字符串
 * 
 * 使用示例：
 * ```typescript
 * const client = new UnityConnector('192.168.1.100', 10002);
 * await client.connect();
 * const tests = await client.listTests();
 * const results = await client.runAllTests();
 * client.disconnect();
 * ```
 */

import * as net from 'net';

/** 协议消息类型常量，与 Unity 端 Protocol.cs 保持一致 */
export const MsgType = {
  HELLO: 'hello',
  HELLO_ACK: 'hello_ack',
  PING: 'ping',
  PONG: 'pong',
  RUN_TEST: 'run_test',
  RUN_ALL_TESTS: 'run_all_tests',
  RUN_SUITE: 'run_suite',
  TEST_RESULT: 'test_result',
  ALL_TESTS_COMPLETE: 'all_tests_complete',
  ACTION: 'action',
  ACTION_RESULT: 'action_result',
  LIST_TESTS: 'list_tests',
  TEST_LIST: 'test_list',
  LOG: 'log',
  ERROR: 'error',
  EDITOR_COMMAND: 'editor_command',
  EDITOR_COMMAND_RESULT: 'editor_command_result',
} as const;

/** Action 类型常量，与 Unity 端 Protocol.cs 的 Action 常量保持一致 */
export const ActionType = {
  GET_CONFIG: 'get_config',
  GET_VERSION: 'get_version',
  GET_ASSET_PATHS: 'get_asset_paths',
  GET_DATABASE_TABLES: 'get_database_tables',
  EXECUTE_SQL: 'execute_sql',
  GET_PLATFORM_INFO: 'get_platform_info',
  GET_PERF_METRICS: 'get_perf_metrics',
  GET_HOTFIX_STATUS: 'get_hotfix_status',
  GET_SCENE_NAME: 'get_scene_name',
  LIST_ASSETBUNDLE: 'list_assetbundle',
  GET_TEST_DETAILS: 'get_test_details',
  SCREENSHOT: 'screenshot',
  TRIGGER_DOWNLOAD: 'trigger_download',
} as const;

/** 单条测试用例描述符 */
export interface TestDescriptor {
  suite: string;
  className: string;
  methodName: string;
  description: string;
  order: number;
  timeout: number;
}

/** 单条测试执行结果 */
export interface TestResult {
  suite: string;
  className: string;
  methodName: string;
  description: string;
  passed: boolean;
  errorMessage: string;
  durationMs: number;
  timestamp: number;
}

/** 全部测试完成通知 */
export interface AllTestsComplete {
  total: number;
  passed: number;
  failed: number;
}

/** Action 执行结果 */
export interface ActionResult {
  action: string;
  success: boolean;
  data?: any;
  error?: string;
}

/** 编辑器命令执行结果 */
export interface EditorCommandResult {
  /** 是否执行成功 */
  success: boolean;
  /** 命令名 */
  command: string;
  /** 返回数据 */
  data?: any;
  /** 错误信息 */
  error?: string;
}

/** 等待特定消息类型的 Promise 解析器 */
interface PendingMessage {
  type: string;
  resolve: (data: any) => void;
  reject: (reason: Error) => void;
  timer: NodeJS.Timeout;
}

/**
 * Unity TCP 连接器——在 Playwright 端使用，连接到 Unity Player 内的 TalosTcpServer。
 */
export class UnityConnector {
  private socket: net.Socket | null = null;
  private buffer: Buffer = Buffer.alloc(0);
  private pendingMessages: PendingMessage[] = [];
  private globalMessageHandler: ((type: string, data: any) => void) | null = null;
  private isIntentionalDisconnect: boolean = false;

  /** 是否已连接 */
  get connected(): boolean {
    return this.socket !== null && !this.socket.destroyed;
  }

  /**
   * 连接到 Unity Player 的 TCP 测试服务端。
   * @param host Unity Player 的 IP 地址
   * @param port TCP 监听端口，默认 10002
   * @param timeoutMs 连接超时（毫秒），默认 30 秒
   */
  async connect(host: string, port: number = 10002, timeoutMs: number = 30000): Promise<void> {
    return new Promise((resolve, reject) => {
      const socket = new net.Socket();
      const timer = setTimeout(() => {
        socket.destroy();
        reject(new Error(`[TalosE2E] 连接超时: ${host}:${port} (${timeoutMs}ms)`));
      }, timeoutMs);

      socket.on('connect', () => {
        clearTimeout(timer);
        this.socket = socket;
        console.log(`[TalosE2E] 已连接到 Unity: ${host}:${port}`);
        resolve();
      });

      socket.on('data', (data: Buffer) => this.handleData(data));
      socket.on('error', (err) => {
        clearTimeout(timer);
        reject(new Error(`[TalosE2E] 连接错误: ${err.message}`));
      });
      socket.on('close', () => {
        this.socket = null;
        if (this.isIntentionalDisconnect) {
          this.isIntentionalDisconnect = false;
          return;
        }
        this.rejectAllPending(new Error('连接已关闭'));
      });

      socket.connect(port, host);
    });
  }

  /**
   * 断开与 Unity Player 的连接。
   */
  disconnect(): void {
    if (this.socket) {
      this.isIntentionalDisconnect = true;
      this.socket.destroy();
      this.socket = null;
    }
    this.rejectAllPending(new Error('主动断开连接'));
  }

  /**
   * 重连到 Unity Player——断开旧连接后反复尝试连接，直到成功或超时。
   * 适用于 Domain Reload 后 TCP 服务自动恢复的场景。
   *
   * @param host 目标主机
   * @param port 目标端口
   * @param timeoutMs 总超时（毫秒），默认 60 秒
   * @param intervalMs 每次重连间隔，默认 2 秒
   */
  async reconnect(host: string, port: number = 10002, timeoutMs: number = 60000, intervalMs: number = 2000): Promise<void> {
    console.log(`[TalosE2E] 开始重连: ${host}:${port}（超时 ${timeoutMs}ms）`);

    // 先断开旧连接
    this.disconnect();

    const start = Date.now();
    let attempt = 0;

    while (Date.now() - start < timeoutMs) {
      attempt++;
      try {
        await this.connect(host, port, intervalMs);
        const hello = await this.hello();
        console.log(`[TalosE2E] ✅ 重连成功（第 ${attempt} 次）: version=${hello.version}, tests=${hello.testCount}`);
        return;
      } catch (err: any) {
        if (attempt % 5 === 0 || attempt === 1) {
          console.log(`[TalosE2E] 重连中 (${attempt})... ${err.message}`);
        }
        await new Promise(resolve => setTimeout(resolve, intervalMs));
      }
    }

    throw new Error(`[TalosE2E] 重连超时 (${timeoutMs}ms, ${attempt} 次尝试)`);
  }

  /**
   * 发送握手请求，验证 Unity 端服务可用。
   * @returns Unity 端返回的握手确认信息
   */
  async hello(timeoutMs: number = 10000): Promise<{ version: string; testCount: number }> {
    const response = await this.sendAndWait(MsgType.HELLO, {}, MsgType.HELLO_ACK, timeoutMs);
    return { version: response.version, testCount: response.testCount };
  }

  /**
   * 获取 Unity 端已发现的所有测试用例列表。
   * @returns 测试用例描述符数组
   */
  async listTests(timeoutMs: number = 10000): Promise<TestDescriptor[]> {
    const response = await this.sendAndWait(MsgType.LIST_TESTS, {}, MsgType.TEST_LIST, timeoutMs);
    return response.tests as TestDescriptor[];
  }

  /**
   * 执行单个测试用例。
   * @param methodName 测试方法名
   * @returns 测试执行结果
   */
  async runTest(methodName: string, timeoutMs: number = 120000): Promise<TestResult> {
    const response = await this.sendAndWait(
      MsgType.RUN_TEST,
      { methodName },
      MsgType.TEST_RESULT,
      timeoutMs,
    );
    return response.result as TestResult;
  }

  /**
   * 执行指定套件的所有测试用例。
   * @param suite 套件名称
   * @returns 所有测试执行结果和完成通知
   */
  async runSuite(suite: string, timeoutMs: number = 300000): Promise<{ results: TestResult[]; summary: AllTestsComplete }> {
    this.send({ type: MsgType.RUN_SUITE, suite });
    return this.collectTestResults(timeoutMs);
  }

  /**
   * 执行所有已发现的测试用例。
   * @returns 所有测试执行结果和完成通知
   */
  async runAllTests(timeoutMs: number = 600000): Promise<{ results: TestResult[]; summary: AllTestsComplete }> {
    this.send({ type: MsgType.RUN_ALL_TESTS });
    return this.collectTestResults(timeoutMs);
  }

  /**
   * 注册全局消息处理器，用于接收实时推送的日志和结果。
   */
  onMessage(handler: (type: string, data: any) => void): void {
    this.globalMessageHandler = handler;
  }

  /**
   * 发送 Ping 保活消息，验证连接仍活跃。
   * @returns 服务端返回的时间戳
   */
  async ping(timeoutMs: number = 5000): Promise<{ timestamp: number }> {
    const response = await this.sendAndWait(MsgType.PING, {}, MsgType.PONG, timeoutMs);
    return { timestamp: response.timestamp };
  }

  /**
   * 执行自定义 Action——发送 action 指令并等待 Unity 端返回结果。
   * 
   * 使用示例：
   * ```typescript
   * const version = await connector.executeAction(ActionType.GET_VERSION);
   * const config = await connector.executeAction(ActionType.GET_CONFIG);
   * const perf = await connector.executeAction(ActionType.GET_PERF_METRICS);
   * ```
   * 
   * @param action Action 类型名称，使用 ActionType 常量
   * @param params 额外参数（如 SQL、folder 等）
   * @returns Action 执行结果
   */
  async executeAction(action: string, params: Record<string, any> = {}, timeoutMs: number = 30000): Promise<ActionResult> {
    const response = await this.sendAndWait(
      MsgType.ACTION,
      { action, ...params },
      MsgType.ACTION_RESULT,
      timeoutMs,
    );
    return {
      action: response.action,
      success: response.success,
      data: response.data,
      error: response.error,
    };
  }

  /**
   * 执行编辑器命令——发送 editor_command 指令并等待 Unity 端返回结果。
   *
    * 此方法是 UnityEditorOps 底层通道，封装了 editor_command 协议的请求-响应模式。
    * 在当前设计下，command 通常是 reflect_invoke_static 或 reflect_get_static，
    * 由 Playwright 侧组合 Unity 官方 API、框架 API 和项目 API 调用链。
   * Playwright 端发出的每个命令都会打印完整的请求和响应日志。
   *
    * @param command 命令名称，如 'reflect_invoke_static'、'reflect_get_static'
   * @param params 命令参数
   * @param timeoutMs 超时毫秒，默认 30 秒
   * @returns 命令执行结果
   */
  async executeEditorCommand(
    command: string,
    params: Record<string, any> = {},
    timeoutMs: number = 30000,
  ): Promise<EditorCommandResult> {
    const requestId = `req_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;

    console.log(`[TalosE2E] 发送编辑器命令: command=${command}, requestId=${requestId}, params=${JSON.stringify(params)}`);

    const response = await this.sendAndWait(
      MsgType.EDITOR_COMMAND,
      { command, requestId, params },
      MsgType.EDITOR_COMMAND_RESULT,
      timeoutMs,
    );

    console.log(`[TalosE2E] 编辑器命令响应: command=${command}, success=${response.success}, requestId=${response.requestId}`);

    if (!response.success) {
      const errMsg = response.error || '未知错误';
      console.error(`[TalosE2E] 编辑器命令失败: command=${command}, error=${errMsg}`);
      throw new Error(`[TalosE2E] 编辑器命令 '${command}' 执行失败: ${errMsg}`);
    }

    return {
      success: response.success,
      command: response.command,
      data: response.data,
      error: response.error,
    };
  }

  /**
   * 执行所有测试并逐条回调进度。
   * 与 runAllTests 不同，此方法通过 onTestResult 回调实时推送每条测试结果，
   * 适合在 UI 中展示进度条或实时日志。
   * 
   * @param onTestResult 每条测试完成时的回调
   * @returns 全部测试完成后的汇总
   */
  async runAllTestsWithProgress(
    onTestResult: (result: TestResult, index: number, total: number) => void,
    timeoutMs: number = 600000,
  ): Promise<{ results: TestResult[]; summary: AllTestsComplete }> {
    const results: TestResult[] = [];
    let summary: AllTestsComplete | null = null;
    let estimatedTotal = 0;

    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error(`[TalosE2E] 等待测试完成超时 (${timeoutMs}ms)`));
      }, timeoutMs);

      // 先发送 list_tests 获取总数（已在 hello 中获得）
      const collector = (type: string, data: any) => {
        if (type === MsgType.TEST_RESULT) {
          const result = data.result as TestResult;
          results.push(result);
          onTestResult(result, results.length, estimatedTotal);
        } else if (type === MsgType.ALL_TESTS_COMPLETE) {
          clearTimeout(timer);
          summary = data as AllTestsComplete;
          this.globalMessageHandler = null;
          resolve({ results, summary: summary! });
        }
      };

      const originalHandler = this.globalMessageHandler;
      this.globalMessageHandler = (type: string, data: any) => {
        collector(type, data);
        originalHandler?.(type, data);
      };

      // 发送 run_all_tests
      this.send({ type: MsgType.RUN_ALL_TESTS });
    });
  }

  /**
   * 等待 Unity TCP 服务就绪——反复尝试连接直到成功或超时。
   * 用于启动脚本中等待 Unity batchmode 初始化完成。
   * 
   * @param host 目标主机
   * @param port 目标端口
   * @param maxRetries 最大重试次数，默认 60 次
   * @param retryIntervalMs 重试间隔，默认 1000ms
   */
  static async waitForReady(
    host: string = '127.0.0.1',
    port: number = 10002,
    maxRetries: number = 60,
    retryIntervalMs: number = 1000,
  ): Promise<UnityConnector> {
    const connector = new UnityConnector();

    for (let i = 0; i < maxRetries; i++) {
      try {
        await connector.connect(host, port, retryIntervalMs);
        const hello = await connector.hello();
        console.log(`[TalosE2E] Unity 就绪: version=${hello.version}, tests=${hello.testCount} (${i + 1}/${maxRetries})`);
        return connector;
      } catch (err) {
        if (i < maxRetries - 1) {
          process.stdout.write('.');
        } else {
          throw new Error(`[TalosE2E] 等待 Unity 就绪超时 (${maxRetries} 次)`);
        }
      }
    }

    throw new Error('[TalosE2E] 不应到达此处');
  }

  // ======== 内部实现 ========

  /**
   * 收集测试执行结果，直到收到 all_tests_complete 通知。
   */
  private async collectTestResults(timeoutMs: number): Promise<{ results: TestResult[]; summary: AllTestsComplete }> {
    const results: TestResult[] = [];
    let summary: AllTestsComplete | null = null;

    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error(`[TalosE2E] 等待测试完成超时 (${timeoutMs}ms)`));
      }, timeoutMs);

      // 注册临时的消息收集器
      const collector = (type: string, data: any) => {
        if (type === MsgType.TEST_RESULT) {
          results.push(data.result as TestResult);
        } else if (type === MsgType.ALL_TESTS_COMPLETE) {
          clearTimeout(timer);
          summary = data as AllTestsComplete;
          this.globalMessageHandler = null; // 移除收集器
          resolve({ results, summary: summary! });
        }
      };

      // 链式处理：先通过全局处理器收集
      const originalHandler = this.globalMessageHandler;
      this.globalMessageHandler = (type: string, data: any) => {
        collector(type, data);
        originalHandler?.(type, data);
      };
    });
  }

  /**
   * 发送消息并等待特定类型的响应。
   */
  private async sendAndWait(
    sendType: string,
    payload: Record<string, any>,
    expectType: string,
    timeoutMs: number,
  ): Promise<any> {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        // 移除超时的等待项
        this.pendingMessages = this.pendingMessages.filter(
          (p) => p.type !== expectType || p.reject !== reject,
        );
        reject(new Error(`[TalosE2E] 等待 ${expectType} 超时 (${timeoutMs}ms)`));
      }, timeoutMs);

      this.pendingMessages.push({ type: expectType, resolve, reject, timer });
      this.send({ type: sendType, ...payload });
    });
  }

  /**
   * 发送 JSON 消息（长度前缀协议编码）。
   */
  private send(data: Record<string, any>): void {
    if (!this.socket) throw new Error('[TalosE2E] 未连接，无法发送消息');

    const body = Buffer.from(JSON.stringify(data), 'utf-8');
    const header = Buffer.alloc(4);
    header.writeUInt32BE(body.length, 0);

    this.socket.write(Buffer.concat([header, body]));
  }

  /**
   * 处理接收到的 TCP 数据，解析长度前缀协议。
   */
  private handleData(data: Buffer): void {
    this.buffer = Buffer.concat([this.buffer, data]);

    // 循环解析完整的消息
    while (this.buffer.length >= 4) {
      const length = this.buffer.readUInt32BE(0);
      if (this.buffer.length < 4 + length) break; // 数据不完整，等待更多

      const body = this.buffer.subarray(4, 4 + length);
      this.buffer = this.buffer.subarray(4 + length);

      try {
        const json = JSON.parse(body.toString('utf-8'));
        this.dispatchMessage(json);
      } catch (err) {
        console.error('[TalosE2E] 消息解析失败:', err);
      }
    }
  }

  /**
   * 分发已解析的消息到对应的处理器。
   */
  private dispatchMessage(json: any): void {
    const type = json.type;

    // 全局消息处理器
    this.globalMessageHandler?.(type, json);

    // 查找匹配的等待项
    const pendingIndex = this.pendingMessages.findIndex((p) => p.type === type);
    if (pendingIndex >= 0) {
      const pending = this.pendingMessages[pendingIndex];
      this.pendingMessages.splice(pendingIndex, 1);
      clearTimeout(pending.timer);
      pending.resolve(json);
    }
  }

  /**
   * 拒绝所有等待中的 Promise。
   */
  private rejectAllPending(reason: Error): void {
    for (const pending of this.pendingMessages) {
      clearTimeout(pending.timer);
      pending.reject(reason);
    }
    this.pendingMessages = [];
  }
}
