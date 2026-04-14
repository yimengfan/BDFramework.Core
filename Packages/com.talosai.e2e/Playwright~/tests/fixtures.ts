/**
 * E2E 测试 fixture——Playwright 测试的基础设施。
 * 
 * 设计角色：
 * - 在每个测试前自动连接到 Unity Player。
 * - 提供统一的 UnityConnector 和 DeviceManager 实例。
 * - 处理连接生命周期（建立、断开）。
 * 
 * 使用方式：
 * Playwright 通过 test.use() 注入 fixture，测试中直接使用 connector。
 */

import { readFile } from 'node:fs/promises';
import { test as base, expect } from '@playwright/test';
import type { TestInfo } from '@playwright/test';
import { UnityConnector, DeviceManager, Platform } from '../src';

/** 单个 Talos 测试步骤包装器。 */
type TalosStep = <T>(title: string, action: () => Promise<T>) => Promise<T>;

/**
 * 构造统一的步骤截图附件名。
 * 这里保持 ASCII，避免不同 TeamCity agent 或 zip 解包器在附件文件名编码上出现差异。
 */
function buildStepAttachmentName(stepIndex: number): string {
  return `talos-step-${String(stepIndex).padStart(2, '0')}.png`;
}

/**
 * 在步骤结束后向 Unity 请求截图，并把结果附加到标准 Playwright 报告。
 */
async function attachUnityStepScreenshot(connector: UnityConnector, testInfo: TestInfo, stepIndex: number, title: string): Promise<void> {
  if (!connector.connected) {
    throw new Error(`connector is disconnected before screenshot, step=${stepIndex}, title=${title}`);
  }

  const screenshot = await connector.captureScreenshot(`step-${String(stepIndex).padStart(2, '0')}-${title}`);
  if (screenshot.contentBase64) {
    await testInfo.attach(buildStepAttachmentName(stepIndex), {
      body: Buffer.from(screenshot.contentBase64, 'base64'),
      contentType: 'image/png',
    });
    return;
  }

  if (screenshot.path) {
    await testInfo.attach(buildStepAttachmentName(stepIndex), {
      body: await readFile(screenshot.path),
      contentType: 'image/png',
    });
    return;
  }

  throw new Error(`Unity screenshot result is empty, step=${stepIndex}, title=${title}`);
}

/**
 * 扩展 Playwright fixture，添加 Unity 连接器和设备管理器。
 */
export const test = base.extend<{
  /** Unity TCP 连接器，已连接到 Unity Player */
  connector: UnityConnector;
  /** 设备管理器，负责安装/启动应用 */
  device: DeviceManager;
  /** Talos 统一步骤包装器，负责 test.step 与步骤结束后的截图归档。 */
  talosStep: TalosStep;
}>({
  // Fixture: 初始化设备管理器
  device: async ({}, use) => {
    const platform = (process.env.PLATFORM || 'unityplayer') as Platform;
    const unityPort = parseInt(process.env.UNITY_PORT || '10002', 10);
    const localPort = parseInt(process.env.LOCAL_PORT || '10002', 10);

    const device = new DeviceManager({
      platform,
      unityPort,
      localPort,
      adbSerial: process.env.ADB_SERIAL,
    });

    await use(device);
  },

  // Fixture: 连接到 Unity Player
  connector: async ({ device }, use) => {
    const connector = new UnityConnector();
    const host = device.getConnectionHost();
    const port = device.getConnectionPort();

    // 连接并握手
    await connector.connect(host, port);
    const hello = await connector.hello();
    console.log(`[TalosE2E] Unity 握手成功: version=${hello.version}, tests=${hello.testCount}`);

    await use(connector);

    // 测试结束后断开连接
    connector.disconnect();
  },

  // Fixture: 统一封装 Talos 步骤与截图归档
  talosStep: async ({ connector }, use, testInfo) => {
    let stepIndex = 0;

    await use(async <T>(title: string, action: () => Promise<T>): Promise<T> => {
      const currentStepIndex = stepIndex + 1;

      return base.step(title, async () => {
        let actionResult: T | undefined;
        let actionError: unknown;

        try {
          actionResult = await action();
        } catch (error) {
          actionError = error;
        }

        try {
          await attachUnityStepScreenshot(connector, testInfo, currentStepIndex, title);
        } catch (screenshotError) {
          const warning = `[TalosE2E] 步骤截图失败 step=${currentStepIndex} title=${title} error=${String(screenshotError)}`;
          console.warn(warning);
          await testInfo.attach(`talos-step-${String(currentStepIndex).padStart(2, '0')}-warning.txt`, {
            body: Buffer.from(`${warning}\n`, 'utf-8'),
            contentType: 'text/plain',
          });
        }

        stepIndex = currentStepIndex;
        if (actionError) {
          throw actionError;
        }

        return actionResult as T;
      });
    });
  },
});

export { expect };
