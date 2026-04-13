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

import { test as base, expect } from '@playwright/test';
import { UnityConnector, DeviceManager, Platform } from '../src';

/**
 * 扩展 Playwright fixture，添加 Unity 连接器和设备管理器。
 */
export const test = base.extend<{
  /** Unity TCP 连接器，已连接到 Unity Player */
  connector: UnityConnector;
  /** 设备管理器，负责安装/启动应用 */
  device: DeviceManager;
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
});

export { expect };
