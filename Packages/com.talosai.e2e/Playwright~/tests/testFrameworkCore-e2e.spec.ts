/**
 * 框架核心 E2E 测试——通过 Playwright 编排，在 Unity Player 中执行全部测试。
 * Framework-core E2E tests orchestrated by Playwright and executed against the Unity side.
 * 
 * 测试流程：
 * Flow:
 * 1. 通过 fixture 连接到 Unity Player 的 TCP 测试服务。
 * 1. Connect to the Unity TCP test service through the fixture.
 * 2. 获取测试列表，确认发现测试用例。
 * 2. Fetch the test list and confirm discovery works.
 * 3. 执行所有测试用例。
 * 3. Execute all test cases.
 * 4. 验证所有测试通过。
 * 4. Verify that all tests pass.
 * 5. 输出详细测试报告。
 * 5. Print the detailed execution report.
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：连接验证。
 * 确保与 Unity Player 的 TCP 通信通道正常。
 * Suite: connection verification.
 * Ensure the TCP communication channel to Unity is healthy.
 */
test.describe('Unity 连接验证', () => {
  test('应能成功连接到 Unity Player', async ({ connector }) => {
    expect(connector.connected).toBe(true);
  });

  test('应能成功完成握手', async ({ connector }) => {
    // 连接时已通过 fixture 完成握手，验证 connected 状态即可
    expect(connector.connected).toBe(true);
  });
});

/**
 * 测试套件：测试发现。
 * 验证 Unity 端能正确发现所有 E2E 测试用例。
 * Suite: discovery.
 * Verify that the Unity side can discover all E2E test cases.
 */
test.describe('测试发现', () => {
  test('应能列出所有测试用例', async ({ connector }) => {
    const tests = await connector.listTests();

    // 验证至少有一些测试用例被发现
    expect(tests.length).toBeGreaterThan(0);

    // 验证测试描述符格式正确
    for (const t of tests) {
      expect(t.suite).toBeTruthy();
      expect(t.methodName).toBeTruthy();
      expect(t.className).toBeTruthy();
      expect(typeof t.order).toBe('number');
    }

    console.log(`[TalosE2E] 发现 ${tests.length} 个测试用例:`);
    for (const t of tests) {
      console.log(`  [${t.suite}] ${t.methodName} - ${t.description}`);
    }
  });

  test('应包含 launch 测试套件', async ({ connector }) => {
    const tests = await connector.listTests();
    const launchTests = tests.filter((t) => t.suite === 'launch');
    expect(launchTests.length).toBeGreaterThan(0);
  });

  test('应包含 asset-load 测试套件', async ({ connector }) => {
    const tests = await connector.listTests();
    const assetTests = tests.filter((t) => t.suite === 'asset-load');
    expect(assetTests.length).toBeGreaterThan(0);
  });
});

/**
 * 测试套件：执行全部框架测试。
 * 按顺序执行 Unity 端所有 E2E 测试用例，验证全部通过。
 * Suite: execute the full framework test set.
 * Run all Unity-side E2E cases in order and require all of them to pass.
 */
test.describe('框架核心 E2E 测试执行', () => {
  test('执行所有 E2E 测试用例并全部通过', async ({ connector }) => {
    const { results, summary } = await connector.runAllTests();

    // 输出详细结果
    console.log('\n========================================');
    console.log('  Talos E2E 测试结果汇总');
    console.log('========================================');
    console.log(`  总计: ${summary.total}`);
    console.log(`  通过: ${summary.passed}`);
    console.log(`  失败: ${summary.failed}`);
    console.log('========================================\n');

    // 逐条输出结果
    for (const r of results) {
      const status = r.passed ? '✅' : '❌';
      console.log(`  ${status} [${r.suite}] ${r.methodName} - ${r.description} (${r.durationMs}ms)`);
      if (!r.passed) {
        console.log(`     错误: ${r.errorMessage}`);
      }
    }

    // 验证所有测试通过
    expect(summary.failed).toBe(0);
    expect(summary.total).toBeGreaterThan(0);
  });
});

/**
 * 测试套件：按套件执行。
 * 验证可以按套件名称单独执行测试。
 * Suite: run by suite.
 * Verify that a suite can be executed in isolation by its suite name.
 */
test.describe('按套件执行测试', () => {
  test('单独执行 launch 套件', async ({ connector }) => {
    const { results, summary } = await connector.runSuite('launch');

    expect(results.length).toBeGreaterThan(0);
    expect(summary.failed).toBe(0);

    console.log(`\n[launch 套件] 总计=${summary.total}, 通过=${summary.passed}, 失败=${summary.failed}`);
  });
});
