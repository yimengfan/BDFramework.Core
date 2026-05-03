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
 * - 框架契约：启动顺序、AOT 加载、配置回退链、资源路径契约。
 * - Framework contract: startup order, AOT loading, config fallback chain, resource path contracts.
 * - SQLite 契约：密码回退、FastJson 数组序列化。
 * - SQLite contract: password fallback, FastJson array serialization.
 * - CSV 契约：加载/保存/引用/忽略列。
 * - CSV contract: load/save/quote/ignored columns.
 * - 日志契约：加密往返、导出、设置规范化、保留策略。
 * - Logs contract: crypto roundtrip, export, settings normalization, retention policy.
 * - ServiceStore API：模块隔离、单例/瞬态容器。
 * - ServiceStore API: module isolation, singleton/transient container.
 * - 对象池 API：预热/复用/扩容/销毁回调。
 * - ObjectPool API: warm/reuse/grow/destroy callback.
 * - 工具 API：路径拼接、文件写入、哈希一致性。
 * - Utility API: path combine, file write, hash consistency.
 * - 版本控制器 API：版本号操作、ClientAssets 路径/构建信息。
 * - VersionController API: version number ops, ClientAssets path/build info.
 * - 资源加载全链路：初始化/加载/卸载。
 * - Asset load full pipeline: init/load/unload.
 * - 资源遍历：加载/批量加载/资源组路径。
 * - Asset traversal: load/batch load/asset group path.
 * - 下载更新：版本号/资产路径/母包资源。
 * - Download update: version/asset path/base package resources.
 * - 框架集成：启动上下文/配置管线/资源管线/SQLite 管线/日志管线。
 * - Framework integration: startup context/config/resource/SQLite/logging pipeline readiness.
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
          const icon = result.passed ? '✅' : '❌';
          console.log(
            `  [${icon}] ${result.suite}.${result.methodName}  ${result.description} (${result.durationMs}ms)`,
          );
          if (!result.passed && result.errorMessage) {
            console.log(`      错误: ${result.errorMessage}`);
          }
        }

        expect(execution.summary.total).toBeGreaterThan(0);
        expect(execution.summary.failed).toBe(0);
      }

      console.log(`[FrameworkBusiness] 预配置界面汇总: total=${total}, failed=${failed}`);
      if (failed > 0) {
        console.log(`  ── 失败明细 ──`);
        for (const execution of executions) {
          for (const result of execution.results) {
            if (!result.passed) {
              console.log(`  ❌ ${result.suite}.${result.methodName}  ${result.description}`);
              console.log(`      错误: ${result.errorMessage}`);
            }
          }
        }
        console.log(`  ── 失败明细结束 ──`);
      }

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
        const icon = result.passed ? '✅' : '❌';
        console.log(
          `  [${icon}] ${result.suite}.${result.methodName}  ${result.description} (${result.durationMs}ms)`,
        );
        if (!result.passed && result.errorMessage) {
          console.log(`      错误: ${result.errorMessage}`);
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
        const icon = buttonTest.passed ? '✅' : '❌';
        console.log(
          `  [${icon}] ${buttonTest.suite}.${buttonTest.methodName}  ${buttonTest.description}`,
        );
        if (!buttonTest.passed && buttonTest.errorMessage) {
          console.log(`      错误: ${buttonTest.errorMessage}`);
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
        const icon = assemblyLoadTest.passed ? '✅' : '❌';
        console.log(
          `  [${icon}] ${assemblyLoadTest.suite}.${assemblyLoadTest.methodName}  ${assemblyLoadTest.description}`,
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
        const icon = screenLoadTest.passed ? '✅' : '❌';
        console.log(
          `  [${icon}] ${screenLoadTest.suite}.${screenLoadTest.methodName}  ${screenLoadTest.description}`,
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
 * 运行指定套件，逐条打印 ✅/❌ 结果，最后输出汇总和失败明细。
 * Run the specified suite, print per-case ✅/❌ results, then output summary and failure details.
 */
async function runSuiteAndRequireAllPass(
  connector: UnityConnector,
  suite: string,
  tag: string,
): Promise<void> {
  const { results, summary } = await connector.runSuite(suite);

  // 逐条输出结果 / Print per-case results
  for (const result of results) {
    const icon = result.passed ? '✅' : '❌';
    console.log(`  [${icon}] ${result.suite}.${result.methodName}  ${result.description} (${result.durationMs}ms)`);
    if (!result.passed && result.errorMessage) {
      console.log(`      错误: ${result.errorMessage}`);
    }
  }

  // 汇总输出 / Summary output
  const allPassed = summary.failed === 0;
  const summaryIcon = allPassed ? '✅' : '❌';
  console.log(`  [${summaryIcon}] [${tag}] 套件结果: suite=${suite} total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);

  // 失败明细 / Failure details
  if (summary.failed > 0) {
    const failures = results.filter((r) => !r.passed);
    console.log(`  ── 失败明细 ──`);
    for (const f of failures) {
      console.log(`  ❌ ${f.suite}.${f.methodName}  ${f.description}`);
      console.log(`      错误: ${f.errorMessage}`);
    }
    console.log(`  ── 失败明细结束 ──`);
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

  /**
   * 用例：验证框架核心业务能力。
   * Case: verify framework core business capabilities.
   */
  test('框架核心业务测试', async ({ connector, talosStep }) => {
    await talosStep('执行框架核心业务测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-core-business', 'FrameworkCoreBusiness');
    });
  });

  /**
   * 用例：验证资源加载全链路。
   * Case: verify asset load full pipeline.
   */
  test('资源加载全链路测试', async ({ connector, talosStep }) => {
    await talosStep('执行资源加载全链路测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-load', 'AssetLoad');
    });
  });

  /**
   * 用例：验证资源遍历能力。
   * Case: verify asset traversal capabilities.
   */
  test('资源遍历测试', async ({ connector, talosStep }) => {
    await talosStep('执行资源遍历测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-traversal', 'AssetTraversal');
    });
  });

  /**
   * 用例：验证下载更新能力。
   * Case: verify download update capabilities.
   */
  test('下载更新测试', async ({ connector, talosStep }) => {
    await talosStep('执行下载更新测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'download-update', 'DownloadUpdate');
    });
  });

  /**
   * 用例：验证框架集成管线就绪。
   * Case: verify framework integration pipeline readiness.
   */
  test('框架集成管线测试', async ({ connector, talosStep }) => {
    await talosStep('执行框架集成管线测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-integration', 'FrameworkIntegration');
    });
  });
});

/**
 * 测试套件：框架契约验证。
 * Suite: framework contract verification.
 * 验证框架启动契约、SQLite 契约、CSV 契约和日志契约。
 * Verify framework startup contract, SQLite contract, CSV contract, and logs contract.
 */
test.describe('框架契约验证', () => {
  /**
   * 用例：验证框架启动与配置契约。
   * Case: verify framework startup and configuration contracts.
   */
  test('框架启动契约测试', async ({ connector, talosStep }) => {
    await talosStep('执行框架启动契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-contract', 'FrameworkContract');
    });
  });

  /**
   * 用例：验证 SQLite 契约。
   * Case: verify SQLite contracts.
   */
  test('SQLite 契约测试', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite-contract', 'SqliteContract');
    });
  });

  /**
   * 用例：验证 CSV 契约。
   * Case: verify CSV contracts.
   */
  test('CSV 契约测试', async ({ connector, talosStep }) => {
    await talosStep('执行 CSV 契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'csv-contract', 'CsvContract');
    });
  });

  /**
   * 用例：验证日志契约。
   * Case: verify logs contracts.
   */
  test('日志契约测试', async ({ connector, talosStep }) => {
    await talosStep('执行日志契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'logs-contract', 'LogsContract');
    });
  });
});

/**
 * 测试套件：框架 API 契约验证。
 * Suite: framework API contract verification.
 * 验证 ServiceStore、对象池、工具函数和版本控制器的 API 契约。
 * Verify ServiceStore, object pool, utility function, and version controller API contracts.
 */
test.describe('框架 API 契约验证', () => {
  /**
   * 用例：验证 ServiceStore API 契约。
   * Case: verify ServiceStore API contract.
   */
  test('ServiceStore API 测试', async ({ connector, talosStep }) => {
    await talosStep('执行 ServiceStore API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'service-store-api', 'ServiceStoreApi');
    });
  });

  /**
   * 用例：验证对象池 API 契约。
   * Case: verify object pool API contract.
   */
  test('对象池 API 测试', async ({ connector, talosStep }) => {
    await talosStep('执行对象池 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'object-pool-api', 'ObjectPoolApi');
    });
  });

  /**
   * 用例：验证工具函数 API 契约。
   * Case: verify utility API contract.
   */
  test('工具函数 API 测试', async ({ connector, talosStep }) => {
    await talosStep('执行工具函数 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'utility-api', 'UtilityApi');
    });
  });

  /**
   * 用例：验证版本控制器 API 契约。
   * Case: verify version controller API contract.
   */
  test('版本控制器 API 测试', async ({ connector, talosStep }) => {
    await talosStep('执行版本控制器 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'version-controller-api', 'VersionControllerApi');
    });
  });
});
