/**
 * 框架核心 E2E 测试——通过 Playwright 编排，在 Unity Player 中执行全部框架测试。
 * Framework-core E2E tests orchestrated by Playwright and executed against the Unity side.
 * 
 * 测试流程：
 * Flow:
 * 1. 通过 fixture 连接到 Unity Player 的 TCP 测试服务。
 * 1. Connect to the Unity TCP test service through the fixture.
 * 2. 获取测试列表，确认测试发现正常。
 * 2. Fetch the test list and confirm discovery is working.
 * 3. 执行 module-integration 套件（框架模块集成）。
 * 3. Execute the module-integration suite (framework module integration).
 * 4. 验证所有测试通过。
 * 4. Verify that all tests pass.
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：连接验证。
 * Suite: connection verification.
 * 确保与 Unity Player 的 TCP 通信通道正常。
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
 * Suite: discovery.
 * 验证 Unity 端能正确发现所有 E2E 测试用例。
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
});

/**
 * 测试套件：框架模块集成测试。
 * Suite: framework module integration test execution.
 * 执行 module-integration 套件（自动聚合所有框架模块的子套件），验证全部通过。
 */
test.describe('框架模块集成测试', () => {
  test('执行框架 module-integration 套件并全部通过', async ({ connector }) => {
    const { results, summary } = await connector.runSuite('module-integration');

    // 输出详细结果
    console.log('\n========================================');
    console.log('  框架模块集成测试结果汇总');
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

    // 验证所有框架测试通过
    expect(summary.failed).toBe(0);
    expect(summary.total).toBeGreaterThan(0);
  });
});
