/**
 * 模块集成测试 E2E 规范。
 * Module integration E2E specification.
 *
 * 测试目的：
 * Goals:
 * - 按模块维度执行 BDFramework 所有核心模块的集成测试链路。
 * - Execute BDFramework all core module integration test chains by module dimension.
 * - 验证每个模块从 contract → business → integration 的完整测试覆盖。
 * - Verify each module's complete test coverage from contract → business → integration.
 * - 确认 E2ESuiteCatalog 目录与实际运行时套件保持同步。
 * - Confirm E2ESuiteCatalog stays in sync with actual runtime suites.
 *
 * 设计原则：
 * Design principles:
 * - 每个模块独立测试，单个模块失败不影响其他模块。
 * - Each module is tested independently; a single module's failure does not affect others.
 * - 模块内按测试金字塔顺序执行（contract → business → integration）。
 * - Within each module, tests follow the test pyramid order (contract → business → integration).
 * - 该规范与 testFrameworkBusiness-e2e.spec.ts 互补：
 *   - testFrameworkBusiness 按功能维度验证各子套件
 *   - 本规范按模块维度验证完整测试链路
 * - This spec complements testFrameworkBusiness-e2e.spec.ts:
 *   - testFrameworkBusiness verifies sub-suites by functional dimension
 *   - This spec verifies complete test chains by module dimension
 */

import type { UnityConnector } from '../src';
import { test, expect } from './fixtures';

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
 * 测试套件：SQLite 模块集成验证。
 * Suite: SQLite module integration verification.
 * 按 contract → business → integration 顺序执行 SQLite 模块所有子套件。
 * Execute all SQLite module sub-suites in contract → business → integration order.
 */
test.describe('SQLite 模块集成验证', () => {
  test('SQLite 契约验证', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite-contract', 'SqliteContract');
    });
  });

  test('SQLite 基础操作验证', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 基础操作测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite', 'SqliteBasic');
    });
  });

  test('SQLite 业务逻辑验证', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 业务逻辑测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite-business', 'SqliteBusiness');
    });
  });

  test('SQLite 集成验证', async ({ connector, talosStep }) => {
    await talosStep('执行 SQLite 集成测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'sqlite-integration', 'SqliteIntegration');
    });
  });
});

/**
 * 测试套件：Asset 模块集成验证。
 * Suite: Asset module integration verification.
 * 按 api → business → integration 顺序执行资源加载和版本控制子套件。
 * Execute asset loading and version control sub-suites in api → business → integration order.
 */
test.describe('Asset 模块集成验证', () => {
  test('版本控制器 API 验证', async ({ connector, talosStep }) => {
    await talosStep('执行版本控制器 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'version-controller-api', 'VersionControllerApi');
    });
  });

  test('版本控制器业务验证', async ({ connector, talosStep }) => {
    await talosStep('执行版本控制器业务测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'version-business', 'VersionBusiness');
    });
  });

  test('资源加载全链路验证', async ({ connector, talosStep }) => {
    await talosStep('执行资源加载测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-load', 'AssetLoad');
    });
  });

  test('AssetBundle 业务验证', async ({ connector, talosStep }) => {
    await talosStep('执行 AssetBundle 业务测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-business', 'AssetBusiness');
    });
  });

  test('资源遍历验证', async ({ connector, talosStep }) => {
    await talosStep('执行资源遍历测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'asset-traversal', 'AssetTraversal');
    });
  });

  test('下载准备验证', async ({ connector, talosStep }) => {
    await talosStep('执行下载准备测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'download-prep', 'DownloadPrep');
    });
  });

  test('下载更新验证', async ({ connector, talosStep }) => {
    await talosStep('执行下载更新测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'download-update', 'DownloadUpdate');
    });
  });
});

/**
 * 测试套件：Framework 模块集成验证。
 * Suite: Framework module integration verification.
 * 按 contract → business → integration 顺序执行框架核心子套件。
 * Execute framework core sub-suites in contract → business → integration order.
 */
test.describe('Framework 模块集成验证', () => {
  test('框架契约验证', async ({ connector, talosStep }) => {
    await talosStep('执行框架契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-contract', 'FrameworkContract');
    });
  });

  test('框架核心业务验证', async ({ connector, talosStep }) => {
    await talosStep('执行框架核心业务测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-core-business', 'FrameworkCoreBusiness');
    });
  });

  test('框架集成管线验证', async ({ connector, talosStep }) => {
    await talosStep('执行框架集成管线测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'framework-integration', 'FrameworkIntegration');
    });
  });
});

/**
 * 测试套件：ServiceStore 模块集成验证。
 * Suite: ServiceStore module integration verification.
 */
test.describe('ServiceStore 模块集成验证', () => {
  test('ServiceStore API 验证', async ({ connector, talosStep }) => {
    await talosStep('执行 ServiceStore API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'service-store-api', 'ServiceStoreApi');
    });
  });
});

/**
 * 测试套件：Utility 模块集成验证。
 * Suite: Utility module integration verification.
 */
test.describe('Utility 模块集成验证', () => {
  test('工具函数 API 验证', async ({ connector, talosStep }) => {
    await talosStep('执行工具函数 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'utility-api', 'UtilityApi');
    });
  });

  test('对象池 API 验证', async ({ connector, talosStep }) => {
    await talosStep('执行对象池 API 测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'object-pool-api', 'ObjectPoolApi');
    });
  });

  test('日志契约验证', async ({ connector, talosStep }) => {
    await talosStep('执行日志契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'logs-contract', 'LogsContract');
    });
  });

  test('CSV 契约验证', async ({ connector, talosStep }) => {
    await talosStep('执行 CSV 契约测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'csv-contract', 'CsvContract');
    });
  });
});

/**
 * 测试套件：Launch 模块集成验证。
 * Suite: Launch module integration verification.
 */
test.describe('Launch 模块集成验证', () => {
  test('启动流程验证', async ({ connector, talosStep }) => {
    await talosStep('执行启动流程测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'launch', 'Launch');
    });
  });

  test('宿主启动验证', async ({ connector, talosStep }) => {
    await talosStep('执行宿主启动测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'host-launch', 'HostLaunch');
    });
  });

  test('宿主资源加载验证', async ({ connector, talosStep }) => {
    await talosStep('执行宿主资源加载测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'host-asset-load', 'HostAssetLoad');
    });
  });

  test('宿主框架集成验证', async ({ connector, talosStep }) => {
    await talosStep('执行宿主框架集成测试套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'host-framework-integration', 'HostFrameworkIntegration');
    });
  });
});

/**
 * 测试套件：模块集成入口与目录完整性验证。
 * Suite: module integration entry and catalog integrity verification.
 * 验证 module-integration 套件和 E2ESuiteCatalog 的完整性。
 * Verify module-integration suite and E2ESuiteCatalog integrity.
 */
test.describe('模块集成入口验证', () => {
  test('模块集成测试入口验证', async ({ connector, talosStep }) => {
    await talosStep('执行模块集成测试入口套件', async () => {
      await runSuiteAndRequireAllPass(connector, 'module-integration', 'ModuleIntegration');
    });
  });
});
