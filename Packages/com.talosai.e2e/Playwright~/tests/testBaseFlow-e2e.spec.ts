/**
 * 基础启动流程 E2E 测试。
 * Base-flow E2E verification.
 *
 * 测试目的：
 * Goals:
 * - 为 TeamCity 的 Talos BaseFlow 任务提供一个稳定的基础系统回归入口。
 * - Provide a stable foundational-system regression entry for the TeamCity Talos BaseFlow job.
 * - 通过 module-integration 套件一键验证框架所有基础模块。
 * - One-click verify all framework foundational modules via the module-integration suite.
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：基础系统流程。
 * Suite: foundational system flow.
 * 执行 module-integration 套件（自动聚合所有框架模块的子套件），验证全部通过。
 */
test.describe('基础系统流程', () => {
  test('执行框架 module-integration 套件并全部通过', async ({ connector, device, talosStep }) => {
    const projectName = await talosStep('记录当前平台项目', async () => {
      const currentProjectName = device.getPlaywrightProject();
      console.log(`[BaseFlow] 当前平台项目: ${currentProjectName}`);
      return currentProjectName;
    });

    await talosStep('执行 framework module-integration 套件', async () => {
      const { results, summary } = await connector.runSuite('module-integration');

      console.log(`[BaseFlow] 模块集成结果: total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);
      for (const r of results) {
        console.log(`[BaseFlow] ${r.passed ? 'PASS' : 'FAIL'} suite=${r.suite} method=${r.methodName} durationMs=${r.durationMs}`);
        if (!r.passed && r.errorMessage) {
          console.log(`[BaseFlow] error=${r.errorMessage}`);
        }
      }

      console.log(`[BaseFlow] 当前平台项目(校验阶段): ${projectName}`);
      expect(summary.total).toBeGreaterThan(0);
      expect(summary.failed).toBe(0);
    });
  });
});
