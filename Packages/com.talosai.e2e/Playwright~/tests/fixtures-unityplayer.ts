/**
 * UnityPlayer (headed GUI) 模式 fixture——Playwright 测试的基础设施。
 *
 * 设计角色：
 * - 以正常带界面的 Unity 启动，支持 E2E 测试和正常 Graph 模式交互。
 * - 提供 UnityConnector、UnityEditorOps 和 DeviceManager 实例。
 * - UnityEditorOps 具备场景操作、PlayMode 控制、菜单执行、万能反射等能力。
 *
 * 与 fixtures.ts（batchmode）的区别：
 * - 此 fixture 额外注入 editorOps，提供完整的 Editor 操作能力。
 * - 适用于 headed GUI 模式测试，如打开场景 → 进入 PlayMode → 等待测试。
 * - batchmode fixture 不包含 editorOps，适用于无 GUI 的 CI 环境。
 */

import { test as base, expect } from '@playwright/test';
import { UnityConnector, DeviceManager, Platform, UnityEditorOps } from '../src';

/**
 * 扩展 Playwright fixture，添加 Unity 连接器、编辑器操作和设备管理器。
 */
export const test = base.extend<{
  /** Unity TCP 连接器，已连接到 Unity Editor */
  connector: UnityConnector;
  /** Unity Editor 操作聚合门面，包含 scene / playMode / bridge / reflection 子模块 */
  editorOps: UnityEditorOps;
  /** 设备管理器，负责安装/启动应用 */
  device: DeviceManager;
}>({
  // Fixture: 初始化设备管理器
  device: async ({}, use) => {
    const platform = (process.env.PLATFORM || 'unityplayer') as Platform;
    const unityPortEnv = process.env.UNITY_PORT || '10002';
    const unityPort = parseInt(unityPortEnv, 10);
    const localPort = parseInt(process.env.LOCAL_PORT || unityPortEnv, 10);

    const device = new DeviceManager({
      platform,
      unityPort,
      localPort,
      adbSerial: process.env.ADB_SERIAL,
    });

    await use(device);
  },

  // Fixture: 连接到 Unity Editor（TCP 服务需已由外部启动脚本启动）
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

  // Fixture: 创建 UnityEditorOps 实例
  editorOps: async ({ connector }, use) => {
    const ops = new UnityEditorOps(connector);
    await use(ops);
  },
});

export { expect };
