/**
 * 模块集成测试 E2E 规范。
 * Module integration E2E specification.
 *
 * 测试目的：
 * Goals:
 * - 通过框架侧 module-integration 套件一键验证所有框架模块的集成测试链路。
 * - One-click verify all framework modules' integration test chains via the module-integration suite.
 * - 不写测试逻辑，只编排调用并校验汇总结果。
 * - No test logic, only orchestration and summary verification.
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：框架模块集成验证。
 * Suite: framework module integration verification.
 * 执行 module-integration 套件（自动聚合所有框架模块的子套件），验证全部通过。
 */
test.describe('框架模块集成验证', () => {
  test('框架模块集成测试全部通过', async ({ connector, talosStep }) => {
    await talosStep('执行 framework module-integration 套件', async () => {
      const { summary } = await connector.runSuite('module-integration');
      console.log(`[ModuleIntegration] total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);
      expect(summary.total).toBeGreaterThan(0);
      expect(summary.failed).toBe(0);
    });
  });
});
