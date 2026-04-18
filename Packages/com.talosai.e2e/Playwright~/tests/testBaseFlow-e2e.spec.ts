/**
 * 基础启动流程 E2E 测试。
 * Base-flow E2E verification.
 *
 * 测试目的：
 * Goals:
 * - 为 TeamCity 的 Talos BaseFlow 任务提供一个稳定的基础系统回归入口。
 * - Provide a stable foundational-system regression entry for the TeamCity Talos BaseFlow job.
 * - 明确覆盖热更 DLL、AB 资源系统与 SQLite 三条基础运行链路。
 * - Explicitly cover the hotfix-DLL, AssetBundle resource, and SQLite runtime paths.
 */

import { test, expect } from './fixtures';

const baseFlowSuites = [
  { suite: 'launch', title: '执行热更启动套件', coverage: '热更 DLL 可用性' },
  { suite: 'asset-load', title: '执行资源加载套件', coverage: 'AB 资产系统可用性' },
  { suite: 'framework-integration', title: '执行框架集成套件', coverage: 'SQLite 可用性' },
] as const;

/**
 * 测试套件：基础系统流程。
 * Suite: foundational system flow.
 */
test.describe('基础系统流程', () => {
  /**
   * 用例：执行 Unity 端基础系统套件并要求全部通过。
   * Case: run the Unity-side foundational-system suites and require all cases to pass.
   */
  test('执行基础系统套件并全部通过', async ({ connector, device, talosStep }) => {
    const projectName = await talosStep('记录当前平台项目', async () => {
      const currentProjectName = device.getPlaywrightProject();
      console.log(`[BaseFlow] 当前平台项目: ${currentProjectName}`);
      return currentProjectName;
    });

    const executions: Array<{
      suite: string;
      title: string;
      coverage: string;
      results: Awaited<ReturnType<typeof connector.runSuite>>['results'];
      summary: Awaited<ReturnType<typeof connector.runSuite>>['summary'];
    }> = [];

    for (const suiteConfig of baseFlowSuites) {
      const execution = await talosStep(suiteConfig.title, async () => connector.runSuite(suiteConfig.suite));
      executions.push({
        suite: suiteConfig.suite,
        title: suiteConfig.title,
        coverage: suiteConfig.coverage,
        results: execution.results,
        summary: execution.summary,
      });
    }

    await talosStep('输出并校验基础系统结果', async () => {
      let total = 0;
      let failed = 0;

      for (const execution of executions) {
        total += execution.summary.total;
        failed += execution.summary.failed;

        console.log(
          `[BaseFlow] 套件结果: suite=${execution.suite} coverage=${execution.coverage} total=${execution.summary.total} passed=${execution.summary.passed} failed=${execution.summary.failed}`,
        );

        for (const result of execution.results) {
          console.log(
            `[BaseFlow] ${result.passed ? 'PASS' : 'FAIL'} suite=${result.suite} method=${result.methodName} durationMs=${result.durationMs}`,
          );
          if (!result.passed && result.errorMessage) {
            console.log(`[BaseFlow] error=${result.errorMessage}`);
          }
        }

        expect(execution.summary.total).toBeGreaterThan(0);
        expect(execution.summary.failed).toBe(0);
      }

      console.log(`[BaseFlow] 基础系统汇总: total=${total}, failed=${failed}`);

      console.log(`[BaseFlow] 当前平台项目(校验阶段): ${projectName}`);
      expect(total).toBeGreaterThan(0);
      expect(failed).toBe(0);
    });
  });
});