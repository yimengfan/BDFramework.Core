/**
 * 业务流程 E2E 测试。
 * Business-flow E2E verification.
 *
 * 测试目的：
 * Goals:
 * - 通过业务侧 business-integration 套件一键验证所有业务模块的集成测试链路。
 * - One-click verify all business modules' integration test chains via the business-integration suite.
 * - 不写测试逻辑，只编排调用并校验汇总结果。
 * - No test logic, only orchestration and summary verification.
 *
 * 涵盖的业务模块 / Covered business modules:
 * - UI 业务流程（预配置界面、服务器配置、热更程序集）
 * - Asset 业务（多资源组操作、平台版本路径解析、Shader 查找）
 * - 版本控制业务（客户端版本号、版本路径解析）
 * - 下载准备与更新（文件服务器 URL、下载路径构造、母包资源）
 */

import { test, expect } from './fixtures';

/**
 * 测试套件：业务模块集成验证。
 * Suite: business module integration verification.
 * 执行 business-integration 套件（自动聚合所有业务模块的子套件），验证全部通过。
 */
test.describe('业务模块集成验证', () => {
  test('业务模块集成测试全部通过', async ({ connector, talosStep }) => {
    await talosStep('执行业务 business-integration 套件', async () => {
      const { summary } = await connector.runSuite('business-integration');
      console.log(`[BusinessIntegration] total=${summary.total} passed=${summary.passed} failed=${summary.failed}`);
      expect(summary.total).toBeGreaterThan(0);
      expect(summary.failed).toBe(0);
    });
  });
});
