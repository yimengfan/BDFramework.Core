/**
 * 框架业务流程 E2E 测试。
 * Framework business-flow E2E verification.
 *
 * 测试目的：
 * Goals:
 * - 验证应用启动后能正常进入预配置界面（WindowPreconfig）。
 * - Verify that the app can reach the preconfiguration screen (WindowPreconfig) after launch.
 * - 验证热更 DLL 已完成加载，核心框架类型可被枚举。
 * - Verify that the hotfix DLL has completed loading and core framework types can be enumerated.
 * - 验证服务器配置已正确加载并可访问。
 * - Verify that the server configuration has been loaded and is accessible.
 * - 验证框架基础模块：SQLite 多连接/加密/释放、AssetBundle 管理器、版本控制器、下载准备。
 * - Verify framework foundational modules: SQLite multi-connection/encryption/disposal, AssetBundle manager, version controller, download preparation.
 *
 * 测试范围：
 * Test scope:
 * - WindowPreconfig 界面已加载并激活。
 * - WindowPreconfig screen loaded and active.
 * - 服务器配置（FileServerUrl）可被访问。
 * - Server configuration (FileServerUrl) is accessible.
 * - 热更程序集已完成加载。
 * - Hotfix assembly has completed loading.
 * - 预配置界面按钮可交互。
 * - Preconfiguration screen buttons are interactive.
 * - SQLite 多连接管理、密码加密读写、连接释放、PRAGMA 配置。
 * - SQLite multi-connection management, password-protected read/write, connection disposal, PRAGMA configuration.
 * - AssetBundle 资源组操作、平台版本路径解析、Shader 查找。
 * - AssetBundle group operations, platform version path resolution, shader lookup.
 * - 版本控制器：客户端版本号、版本路径解析、版本信息结构。
 * - Version controller: client version number, versioned path resolution, version info structure.
 * - 下载准备：文件服务器 URL、下载路径构造、母包基础资源。
 * - Download preparation: file server URL, download path construction, base package resources.
 */

import type { UnityConnector } from '../src';
import { test, expect } from './fixtures';

const windowPreconfigSuites = [
  { suite: 'window-preconfig', title: '执行预配置界面验证套件', coverage: 'WindowPreconfig 界面可达性' },
] as const;

const windowPreconfigReadyRetryCount = 12;
const windowPreconfigReadyRetryDelayMs = 1000;
const windowPreconfigTransientReadinessErrors = [
  '未发现 WindowPreconfig 实例',
  '未发现 GameConfigManager.Inst 属性',
  'GameConfigManager.Inst 为空',
  '未发现 ServerConfigProcessor 类型',
] as const;

function collectWindowPreconfigFailureMessages(
  execution: Awaited<ReturnType<typeof runWindowPreconfigSuiteUntilReady>>,
): string[] {
  return execution.results
    .filter((result) => !result.passed && result.errorMessage)
    .map((result) => result.errorMessage as string);
}

function isWindowPreconfigStartupRace(messages: string[]): boolean {
  return (
    messages.length > 0 &&
    messages.every((message) =>
      windowPreconfigTransientReadinessErrors.some((transientError) => message.includes(transientError)),
    )
  );
}

async function runWindowPreconfigSuiteUntilReady(
  connector: UnityConnector,
): Promise<Awaited<ReturnType<typeof connector.runSuite>>> {
  let lastExecution: Awaited<ReturnType<typeof connector.runSuite>> | null = null;

  for (let attempt = 1; attempt <= windowPreconfigReadyRetryCount; attempt++) {
    const execution = await connector.runSuite('window-preconfig');
    lastExecution = execution;

    if (execution.summary.failed === 0) {
      if (attempt > 1) {
        console.log(`[FrameworkBusiness] WindowPreconfig 套件重试成功: attempt=${attempt}`);
      }
      return execution;
    }

    const failureMessages = collectWindowPreconfigFailureMessages(execution);
    const canRetry = attempt < windowPreconfigReadyRetryCount && isWindowPreconfigStartupRace(failureMessages);
    console.log(
      `[FrameworkBusiness] WindowPreconfig 套件待就绪: attempt=${attempt} failed=${execution.summary.failed} canRetry=${canRetry}`,
    );
    for (const failureMessage of failureMessages) {
      console.log(`[FrameworkBusiness] WindowPreconfig 待就绪原因=${failureMessage}`);
    }

    if (!canRetry) {
      return execution;
    }

    await new Promise((resolve) => setTimeout(resolve, windowPreconfigReadyRetryDelayMs));
  }

  return lastExecution ?? connector.runSuite('window-preconfig');
}

/**
 * 测试套件：框架业务流程。
 * Suite: framework business flow.
 */
test.describe('框架业务流程', () => {
  /**
   * 用例：执行 Unity 端预配置界面套件并要求全部通过。
   * Case: run the Unity-side preconfiguration-screen suites and require all cases to pass.
   */
  test('执行预配置界面套件并全部通过', async ({ connector, device, talosStep }) => {
    const projectName = await talosStep('记录当前平台项目', async () => {
      const currentProjectName = device.getPlaywrightProject();
      console.log(`[FrameworkBusiness] 当前平台项目: ${currentProjectName}`);
      return currentProjectName;
    });

    const executions: Array<{
      suite: string;
      title: string;
      coverage: string;
      results: Awaited<ReturnType<typeof connector.runSuite>>['results'];
      summary: Awaited<ReturnType<typeof connector.runSuite>>['summary'];
    }> = [];

    for (const suiteConfig of windowPreconfigSuites) {
      const execution = await talosStep(suiteConfig.title, async () => runWindowPreconfigSuiteUntilReady(connector));
      executions.push({
        suite: suiteConfig.suite,
        title: suiteConfig.title,
        coverage: suiteConfig.coverage,
        results: execution.results,
        summary: execution.summary,
      });
    }

    await talosStep('输出并校验预配置界面结果', async () => {
      let total = 0;
      let failed = 0;

      for (const execution of executions) {
        total += execution.summary.total;
        failed += execution.summary.failed;

        console.log(
          `[FrameworkBusiness] 套件结果: suite=${execution.suite} coverage=${execution.coverage} total=${execution.summary.total} passed=${execution.summary.passed} failed=${execution.summary.failed}`,
        );

        for (const result of execution.results) {
          console.log(
            `[FrameworkBusiness] ${result.passed ? 'PASS' : 'FAIL'} suite=${result.suite} method=${result.methodName} durationMs=${result.durationMs}`,
          );
          if (!result.passed && result.errorMessage) {
            console.log(`[FrameworkBusiness] error=${result.errorMessage}`);
          }
        }

        expect(execution.summary.total).toBeGreaterThan(0);
        expect(execution.summary.failed).toBe(0);
      }

      console.log(`[FrameworkBusiness] 预配置界面汇总: total=${total}, failed=${failed}`);

      console.log(`[FrameworkBusiness] 当前平台项目(校验阶段): ${projectName}`);
      expect(total).toBeGreaterThan(0);
      expect(failed).toBe(0);
    });
  });

  /**
   * 用例：验证热更程序集已加载。
   * Case: verify that the hotfix assembly has loaded.
   */
  test('验证热更程序集已加载', async ({ connector, talosStep }) => {
    await talosStep('检查热更程序集加载状态', async () => {
      const { results, summary } = await connector.runSuite('launch');

      console.log(`[FrameworkBusiness] 热更程序集检查: total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);

      for (const result of results) {
        console.log(
          `[FrameworkBusiness] ${result.passed ? 'PASS' : 'FAIL'} suite=${result.suite} method=${result.methodName} des=${result.description}`,
        );
        if (!result.passed && result.errorMessage) {
          console.log(`[FrameworkBusiness] error=${result.errorMessage}`);
        }
      }

      expect(summary.total).toBeGreaterThan(0);
      expect(summary.failed).toBe(0);
    });
  });

  /**
   * 用例：验证预配置界面按钮可交互。
   * Case: verify that the preconfiguration screen buttons are interactive.
   * TODO: 后续迭代将添加按钮点击交互测试（下载、修复模式等）。
   * TODO: future iterations will add button click interaction tests (download, repair mode, etc.).
   */
  test('验证预配置界面按钮可交互', async ({ connector, talosStep }) => {
    await talosStep('检查预配置界面按钮状态', async () => {
      const { results, summary } = await runWindowPreconfigSuiteUntilReady(connector);

      // 找到按钮交互检查测试
      const buttonTest = results.find((r) => r.methodName.includes('ButtonsInteractive'));
      if (buttonTest) {
        console.log(
          `[FrameworkBusiness] 按钮交互检查: method=${buttonTest.methodName} passed=${buttonTest.passed}`,
        );
        if (!buttonTest.passed && buttonTest.errorMessage) {
          console.log(`[FrameworkBusiness] error=${buttonTest.errorMessage}`);
        }
      }

      expect(summary.failed).toBe(0);
    });
  });
});

/**
 * 测试套件：日志监听验证。
 * Suite: log monitoring verification.
 * 通过日志确认热更加载、页面进入等关键事件已发生。
 * Confirm through logs that hotfix loading, page entry, and other key events have occurred.
 */
test.describe('日志监听验证', () => {
  /**
   * 用例：验证关键启动日志已输出。
   * Case: verify that key startup logs have been output.
   */
  test('应输出关键启动日志', async ({ connector, talosStep }) => {
    await talosStep('检查关键启动日志', async () => {
      // 运行 launch 套件以触发日志检查
      const { results, summary } = await connector.runSuite('launch');

      console.log(`[LogMonitor] 启动日志检查: total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);

      // 验证热更程序集加载日志
      const assemblyLoadTest = results.find((r) => r.methodName.includes('AssemblyLoaded'));
      if (assemblyLoadTest) {
        console.log(
          `[LogMonitor] 热更程序集加载: method=${assemblyLoadTest.methodName} passed=${assemblyLoadTest.passed}`,
        );
        expect(assemblyLoadTest.passed).toBe(true);
      }

      expect(summary.failed).toBe(0);
    });
  });

  /**
   * 用例：验证 WindowPreconfig 进入日志已输出。
   * Case: verify that WindowPreconfig entry log has been output.
   */
  test('应输出 WindowPreconfig 进入日志', async ({ connector, talosStep }) => {
    await talosStep('检查 WindowPreconfig 进入日志', async () => {
      const { results, summary } = await runWindowPreconfigSuiteUntilReady(connector);

      console.log(`[LogMonitor] WindowPreconfig 日志检查: total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);

      // 验证界面加载日志
      const screenLoadTest = results.find((r) => r.methodName.includes('ScreenLoaded'));
      if (screenLoadTest) {
        console.log(
          `[LogMonitor] 界面加载: method=${screenLoadTest.methodName} passed=${screenLoadTest.passed}`,
        );
        expect(screenLoadTest.passed).toBe(true);
      }

      expect(summary.failed).toBe(0);
    });
  });
});

/**
 * 通用套件执行辅助函数。
 * General suite execution helper.
 * 运行指定套件并记录结果，要求全部通过。
 * Run the specified suite, log results, and require all cases to pass.
 */
async function runSuiteAndRequireAllPass(
  connector: UnityConnector,
  suite: string,
  tag: string,
): Promise<void> {
  const { results, summary } = await connector.runSuite(suite);

  console.log(
    `[${tag}] 套件结果: suite=${suite} total=${summary.total} passed=${summary.passed} failed=${summary.failed}`,
  );

  for (const result of results) {
    console.log(
      `[${tag}] ${result.passed ? 'PASS' : 'FAIL'} suite=${result.suite} method=${result.methodName} des=${result.description}`,
    );
    if (!result.passed && result.errorMessage) {
      console.log(`[${tag}] error=${result.errorMessage}`);
    }
  }

  expect(summary.total).toBeGreaterThan(0);
  expect(summary.failed).toBe(0);
}

/**
 * 测试套件：框架基础模块验证。
 * Suite: framework foundational module verification.
 * 验证 SQLite、AssetBundle 管理器、版本控制器和下载准备等基础能力。
 * Verify foundational capabilities: SQLite, AssetBundle manager, version controller, and download preparation.
 * 这些套件依赖热更已加载、配置已就绪（即 window-preconfig 套件已通过）。
 * These suites depend on hotfix loaded and config ready (i.e., window-preconfig suite has passed).
 */
test.describe('框架基础模块验证', () => {
  /**
   * 用例：验证 SQLite 深度能力。
   * Case: verify SQLite deep capabilities.
   */
  test('SQLite 深度测试', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 深度测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite-business', 'SqliteBusiness');
    });
  });

  /**
   * 用例：验证 AssetBundle 管理器能力。
   * Case: verify AssetBundle manager capabilities.
   */
  test('AssetBundle 管理器测试', async ({ connector, talosStep }) => {
    await talosStep('执行 AssetBundle 管理器测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-business', 'AssetBusiness');
    });
  });

  /**
   * 用例：验证版本控制器能力。
   * Case: verify version controller capabilities.
   */
  test('版本控制器测试', async ({ connector, talosStep }) => {
    await talosStep('执行版本控制器测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'version-business', 'VersionBusiness');
    });
  });

  /**
   * 用例：验证下载准备能力。
   * Case: verify download preparation capabilities.
   */
  test('下载准备测试', async ({ connector, talosStep }) => {
    await talosStep('执行下载准备测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'download-prep', 'DownloadPrep');
    });
  });
});
