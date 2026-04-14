/**
 * 基础启动流程 E2E 测试。
 *
 * 测试目的：
 * - 为 TeamCity 的 Talos BaseFlow 任务提供一个稳定、最小的跨平台回归入口。
 * - 仅验证 Unity Player 已经完成启动流程套件，不把更重的资源或业务断言耦合进第一步。
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：基础启动流程。
 */
test.describe('基础启动流程', () => {
  /**
   * 用例：执行 Unity 端“启动流程”测试套件并要求全部通过。
   */
  test('执行启动流程套件并全部通过', async ({ connector, device, talosStep }) => {
    const projectName = await talosStep('记录当前平台项目', async () => {
      const currentProjectName = device.getPlaywrightProject();
      console.log(`[BaseFlow] 当前平台项目: ${currentProjectName}`);
      return currentProjectName;
    });

    const execution = await talosStep('执行 Unity 启动流程套件', async () => connector.runSuite('启动流程'));

    await talosStep('输出并校验启动流程结果', async () => {
      const { results, summary } = execution;

      console.log(
        `[BaseFlow] 启动流程结果: total=${summary.total}, passed=${summary.passed}, failed=${summary.failed}`,
      );

      for (const result of results) {
        console.log(
          `[BaseFlow] ${result.passed ? 'PASS' : 'FAIL'} suite=${result.suite} method=${result.methodName} durationMs=${result.durationMs}`,
        );
        if (!result.passed && result.errorMessage) {
          console.log(`[BaseFlow] error=${result.errorMessage}`);
        }
      }

      console.log(`[BaseFlow] 当前平台项目(校验阶段): ${projectName}`);
      expect(summary.total).toBeGreaterThan(0);
      expect(summary.failed).toBe(0);
    });
  });
});