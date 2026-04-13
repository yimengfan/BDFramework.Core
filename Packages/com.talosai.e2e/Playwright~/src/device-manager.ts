/**
 * 设备管理器——负责应用程序的安装、启动和生命周期管理。
 * 
 * 设计角色：
 * - 提供 Android（通过 ADB）和 Windows/macOS（通过进程管理）的设备操作。
 * - 在 CI 环境中自动安装包体并启动应用程序。
 * - 支持 ADB 端口转发，将 Unity TCP 端口映射到本地。
 * 
 * 使用示例：
 * ```typescript
 * const device = new DeviceManager('android');
 * await device.install('/path/to/app.apk');
 * await device.forwardPort(10002);
 * await device.launch();
 * ```
 */

import { execFile } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { promisify } from 'util';

const execFileAsync = promisify(execFile);

/** 支持的目标平台 */
export type Platform = 'unityplayer' | 'android' | 'windows' | 'macos';

/** 设备管理器配置 */
export interface DeviceConfig {
  /** 目标平台 */
  platform: Platform;
  /** ADB 设备序列号（Android 多设备时使用） */
  adbSerial?: string;
  /** Unity TCP 端口 */
  unityPort: number;
  /** 本地映射端口（ADB forward 目标端口） */
  localPort?: number;
  /** 启动超时（毫秒） */
  launchTimeout?: number;
}

/**
 * 设备管理器——负责安装、启动、停止应用程序以及端口转发。
 */
export class DeviceManager {
  public readonly platform: Platform;
  public readonly config: DeviceConfig;

  constructor(config: DeviceConfig | Platform) {
    if (typeof config === 'string') {
      this.config = { platform: config, unityPort: 10002 };
      this.platform = config;
    } else {
      this.config = config;
      this.platform = config.platform;
    }
  }

  /**
   * 安装应用程序到目标设备。
   * @param packagePath 安装包路径（.apk / .exe 目录 / .app 目录）
   */
  async install(packagePath: string): Promise<void> {
    console.log(`[TalosE2E] 安装应用: ${packagePath}`);

    switch (this.platform) {
      case 'android':
        await this.adb('install', '-r', '-t', packagePath);
        break;
      case 'windows':
      case 'macos':
        // 桌面平台不需要安装，验证文件存在即可
        if (!fs.existsSync(packagePath)) {
          throw new Error(`[TalosE2E] 应用路径不存在: ${packagePath}`);
        }
        console.log(`[TalosE2E] 桌面应用验证通过: ${packagePath}`);
        break;
    }
  }

  /**
   * 启动应用程序。
   * @param options 启动选项
   */
  async launch(options?: {
    /** Android Activity 名称 */
    activity?: string;
    /** 额外命令行参数传递给 Unity */
    extraArgs?: string[];
  }): Promise<void> {
    console.log(`[TalosE2E] 启动应用 (platform=${this.platform})`);

    switch (this.platform) {
      case 'android':
        const activity = options?.activity || 'com.popo.bdframework/com.unity3d.player.UnityPlayerActivity';
        const intentArgs = ['-n', activity];
        if (options?.extraArgs) {
          for (const arg of options.extraArgs) {
            intentArgs.push('--ez', arg, 'true');
          }
        }
        await this.adb('shell', 'am', 'start', ...intentArgs);
        break;
      case 'windows':
        // Windows: 直接启动 exe
        break;
      case 'macos':
        // macOS: open .app
        break;
    }

    // 等待应用启动
    const timeout = this.config.launchTimeout || 15000;
    await this.waitForAppReady(timeout);
  }

  /**
   * 强制停止应用程序。
   */
  async forceStop(packageName?: string): Promise<void> {
    const pkg = packageName || 'com.popo.bdframework';

    switch (this.platform) {
      case 'android':
        await this.adb('shell', 'am', 'force-stop', pkg);
        break;
      case 'windows':
      case 'macos':
        // 桌面平台通过进程名关闭
        break;
    }
  }

  /**
   * 设置 ADB 端口转发。
   * 将设备的 Unity TCP 端口映射到本地端口，使 Playwright 可以通过 localhost 连接。
   * @param remotePort 远程（设备）端口，默认使用 config.unityPort
   * @param localPort 本地端口，默认使用 config.localPort 或与 remotePort 相同
   */
  async forwardPort(remotePort?: number, localPort?: number): Promise<void> {
    const rPort = remotePort ?? this.config.unityPort;
    const lPort = localPort ?? this.config.localPort ?? rPort;

    if (this.platform === 'android') {
      // 先移除已有的转发
      try { await this.adb('forward', '--remove', `tcp:${lPort}`); } catch { /* 忽略 */ }
      // 设置新转发
      await this.adb('forward', `tcp:${lPort}`, `tcp:${rPort}`);
      console.log(`[TalosE2E] ADB 端口转发已设置: localhost:${lPort} -> device:${rPort}`);
    }
  }

  /**
   * 移除 ADB 端口转发。
   */
  async removeForward(localPort?: number): Promise<void> {
    const lPort = localPort ?? this.config.localPort ?? this.config.unityPort;

    if (this.platform === 'android') {
      try {
        await this.adb('forward', '--remove', `tcp:${lPort}`);
      } catch { /* 忽略 */ }
    }
  }

  /**
   * 获取连接 Unity 所需的主机地址。
   * Android 通过 ADB forward 使用 localhost，其他平台直接使用设备 IP。
   */
  getConnectionHost(): string {
    switch (this.platform) {
      case 'unityplayer':
        return '127.0.0.1';
      case 'android':
        return '127.0.0.1';
      case 'windows':
        return process.env.UNITY_HOST || '127.0.0.1';
      case 'macos':
        return process.env.UNITY_HOST || '127.0.0.1';
    }
  }

  /**
   * 获取连接 Unity 所需的端口号。
   */
  getConnectionPort(): number {
    if (this.platform === 'android') {
      return this.config.localPort ?? this.config.unityPort;
    }
    return this.config.unityPort;
  }

  /**
   * 获取平台对应的 Playwright project 名称。
   */
  getPlaywrightProject(): string {
    switch (this.platform) {
      case 'unityplayer':
        return 'macos'; // Unity batchmode 在当前平台运行
      case 'android':
        return 'android';
      case 'windows':
        return 'windows';
      case 'macos':
        return 'macos';
    }
  }

  // ======== 内部实现 ========

  /**
   * 执行 ADB 命令。
   */
  private async adb(...args: string[]): Promise<{ stdout: string; stderr: string }> {
    const adbArgs = this.config.adbSerial ? ['-s', this.config.adbSerial, ...args] : args;
    return execFileAsync('adb', adbArgs, { timeout: 60000 });
  }

  /**
   * 等待应用程序就绪（通过尝试 TCP 连接检测）。
   */
  private async waitForAppReady(timeoutMs: number): Promise<void> {
    const host = this.getConnectionHost();
    const port = this.getConnectionPort();
    const startTime = Date.now();

    while (Date.now() - startTime < timeoutMs) {
      try {
        const net = require('net');
        await new Promise<void>((resolve, reject) => {
          const socket = new net.Socket();
          socket.setTimeout(2000);
          socket.on('connect', () => { socket.destroy(); resolve(); });
          socket.on('error', () => { socket.destroy(); reject(); });
          socket.on('timeout', () => { socket.destroy(); reject(); });
          socket.connect(port, host);
        });
        console.log(`[TalosE2E] 应用已就绪 (${host}:${port})`);
        return;
      } catch {
        await new Promise((r) => setTimeout(r, 1000));
      }
    }

    throw new Error(`[TalosE2E] 等待应用就绪超时 (${timeoutMs}ms)`);
  }
}
