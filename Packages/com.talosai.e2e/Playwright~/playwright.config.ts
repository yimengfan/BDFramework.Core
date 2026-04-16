import { defineConfig } from '@playwright/test';

/**
 * 无平台后缀的测试文件按设计可在所有平台项目复用。
 * Tests without an explicit platform suffix are reusable across runtime-oriented platform projects by design.
 * 显式平台后缀只会分发到对应项目。
 * Explicit platform suffixes are dispatched only to their matching projects.
 */
const crossPlatformTestMatch = /^(?!.*-(EditorPlayer|Android|Windows|MacOS)-e2e\.spec\.ts$).*-e2e\.spec\.ts$/;
const editorPlayerTestMatch = /-EditorPlayer-e2e\.spec\.ts$/;

/**
 * Playwright 配置文件。
 * Playwright configuration file.
 * 
 * Talos E2E 测试不使用 Playwright 的浏览器能力，
 * Talos E2E does not use Playwright's browser automation; 
 * 而是将其作为测试编排框架，通过 TCP 连接 Unity Player。
 * instead it uses Playwright as the orchestration framework that connects to Unity through TCP.
 * 
 * 关键配置说明：
 * Key settings:
 * - testDir: 测试文件目录
 * - testDir: test file directory
 * - timeout: 单个测试超时（5分钟，Unity 端测试可能较慢）
 * - timeout: per-test timeout because Unity-side execution can be slow
 * - retries: 失败重试次数
 * - retries: retry count on failure
 * - reporter: 生成 HTML 报告和 JUnit XML
 * - reporter: emit HTML and JUnit reports
 * - 平台注入: 由启动脚本通过 PLATFORM 环境变量传入，project 只负责测试文件分发
 * - platform injection: startup scripts pass PLATFORM, and each project only controls test-file routing
 * 
 * 项目说明：
 * Project routing:
 * - batchmode: Unity batchmode（无界面）模式，运行无平台后缀的跨平台用例
 * - batchmode: Unity batchmode (headless) runs the reusable runtime-oriented cases without a platform suffix
 * - unityplayer: Unity headed GUI mode runs only `*-EditorPlayer-e2e.spec.ts` editor-control cases
 * - android / windows / macos: 运行跨平台用例与各自平台后缀用例
 * - android / windows / macos: run reusable runtime-oriented cases plus each platform's explicit suffix cases
 */
export default defineConfig({
  testDir: './tests',
  workers: 1,               // 单目标串行，避免多个 worker 同时争用同一个 Unity 实例。 / Run serially per target so multiple workers do not contend for the same Unity instance.
  fullyParallel: false,        // 串行执行，避免多个测试同时操作 Unity。 / Keep execution serial so multiple tests do not operate the same Unity instance concurrently.
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 10 * 60 * 1000,     // 10分钟超时（Graph 模式需要进入 PlayMode，耗时较长）。 / Use a 10-minute timeout because Graph flows can require PlayMode transitions.
  expect: {
    timeout: 60 * 1000,         // 60秒断言超时。 / Use a 60-second assertion timeout.
  },
  reporter: [
    ['list'],
    ['html', { open: 'never', outputFolder: 'test-results/html' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],
  outputDir: 'test-results/artifacts',
  use: {
    // 自定义配置通过环境变量传入。 / Custom options are injected through environment variables.
    // UNITY_HOST: Unity Player 的 IP 地址。 / Unity Player IP address.
    // UNITY_PORT: Unity Player 的 TCP 端口。 / Unity Player TCP port.
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'batchmode',
      testMatch: crossPlatformTestMatch,
    },
    {
      name: 'unityplayer',
      testMatch: editorPlayerTestMatch,
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
