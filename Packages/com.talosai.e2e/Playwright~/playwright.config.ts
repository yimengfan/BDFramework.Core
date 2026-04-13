import { defineConfig } from '@playwright/test';

/**
 * 无平台后缀的测试文件按设计可在所有平台项目复用。
 * 显式平台后缀只会分发到对应项目。
 */
const crossPlatformTestMatch = /^(?!.*-(EditorPlayer|Android|Windows|MacOS)-e2e\.spec\.ts$).*-e2e\.spec\.ts$/;

/**
 * Playwright 配置文件。
 * 
 * Talos E2E 测试不使用 Playwright 的浏览器能力，
 * 而是将其作为测试编排框架，通过 TCP 连接 Unity Player。
 * 
 * 关键配置说明：
 * - testDir: 测试文件目录
 * - timeout: 单个测试超时（5分钟，Unity 端测试可能较慢）
 * - retries: 失败重试次数
 * - reporter: 生成 HTML 报告和 JUnit XML
 * - 平台注入: 由启动脚本通过 PLATFORM 环境变量传入，project 只负责测试文件分发
 * 
 * 项目说明：
 * - batchmode: Unity batchmode（无界面）模式，运行无平台后缀的跨平台用例
 * - unityplayer: Unity headed GUI 模式，运行跨平台用例与 `*-EditorPlayer-e2e.spec.ts` 用例
 * - android / windows / macos: 运行跨平台用例与各自平台后缀用例
 */
export default defineConfig({
  testDir: './tests',
  workers: 1,               // 单目标串行，避免多个 worker 同时争用同一个 Unity 实例
  fullyParallel: false,        // 串行执行，避免多个测试同时操作 Unity
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 10 * 60 * 1000,     // 10分钟超时（Graph 模式需要进入 PlayMode，耗时较长）
  expect: {
    timeout: 60 * 1000,         // 60秒断言超时
  },
  reporter: [
    ['list'],
    ['html', { open: 'never', outputFolder: 'test-results/html' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],
  outputDir: 'test-results/artifacts',
  use: {
    // 自定义配置通过环境变量传入
    // UNITY_HOST: Unity Player 的 IP 地址
    // UNITY_PORT: Unity Player 的 TCP 端口
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'batchmode',
      testMatch: crossPlatformTestMatch,
    },
    {
      name: 'unityplayer',
      testMatch: [crossPlatformTestMatch, /-EditorPlayer-e2e\.spec\.ts$/],
    },
    {
      name: 'android',
      testMatch: [crossPlatformTestMatch, /-Android-e2e\.spec\.ts$/],
    },
    {
      name: 'windows',
      testMatch: [crossPlatformTestMatch, /-Windows-e2e\.spec\.ts$/],
    },
    {
      name: 'macos',
      testMatch: [crossPlatformTestMatch, /-MacOS-e2e\.spec\.ts$/],
    },
  ],
});
