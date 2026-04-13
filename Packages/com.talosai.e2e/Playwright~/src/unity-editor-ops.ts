/**
 * Unity Editor 操作层——Playwright 端的 Editor 操作封装。
 *
 * 设计角色：
 * - 将 Unity Editor 操作按模块封装为 async/await 接口。
 * - 通过 UnityConnector 的 editor_command 通道访问 Unity 侧的通用反射网关。
 * - 基础 API 尽量在 Playwright 侧组合，避免在 Unity 侧持续堆积薄封装。
 * - 所有操作都在 Unity 主线程上执行，线程安全。
 * - Playwright 端发出的每个操作都会打印完整日志。
 *
 * 模块划分：
 * - EditorReflectionOps: 通用反射接口（静态方法调用、静态成员读取）
 * - EditorSceneOps: 基于反射组合的场景操作
 * - EditorPlayModeOps: 基于反射组合的 PlayMode 控制
 * - EditorBridgeOps: 基于反射组合的编辑器状态与菜单操作
 * - UnityEditorOps: 聚合门面，包含上述所有模块
 *
 * 使用示例：
 * ```typescript
 * const ops = new UnityEditorOps(connector);
 * await ops.scene.openScene('Assets/Scenes/BDFrame.unity');
 * await ops.playMode.enterPlayMode();
 * await ops.playMode.waitForPlayMode(true);
 * const state = await ops.bridge.getEditorState();
 * ```
 */

import { UnityConnector } from './unity-connector';

// ====================================================================
// 编辑器命令结果类型
// ====================================================================

/** 编辑器命令通用返回结构 */
export interface EditorCommandResult {
  /** 是否执行成功 */
  success: boolean;
  /** 命令名 */
  command: string;
  /** 返回数据（命令相关） */
  data?: any;
  /** 错误信息（失败时） */
  error?: string;
}

/** 场景信息 */
export interface SceneInfo {
  name: string;
  path: string;
  isLoaded: boolean;
  rootCount: number;
}

/** 编辑器状态快照 */
export interface EditorState {
  isPlaying: boolean;
  isCompiling: boolean;
  isUpdating: boolean;
  currentScene: string;
  currentScenePath: string;
  timeSinceStartup: number;
}

/** 反射调用返回结构——静态方法调用 */
export interface ReflectionMethodResult {
  kind: 'method';
  memberPath: string;
  declaringType?: string;
  returnType?: string;
  value?: any;
}

/** 反射调用返回结构——静态成员读取 */
export interface ReflectionMemberResult {
  kind: 'member';
  memberPath: string;
  declaringType?: string;
  valueType?: string;
  value?: any;
}

/**
 * 将 Unity 端归一化返回的场景对象转换为 Playwright 侧的 SceneInfo。
 * Unity 的 Scene 结构体会被反射层展开为普通对象，这里做最后一层字段整理。
 */
function normalizeSceneInfo(raw: any): SceneInfo {
  return {
    name: String(raw?.name ?? ''),
    path: String(raw?.path ?? ''),
    isLoaded: Boolean(raw?.isLoaded ?? false),
    rootCount: Number(raw?.rootCount ?? 0),
  };
}

// ====================================================================
// EditorSceneOps —— 场景操作模块
// ====================================================================

/**
 * 场景操作模块——提供场景打开、保存、查询等操作。
 */
export class EditorSceneOps {
  constructor(private reflection: EditorReflectionOps) {}

  /**
   * 打开指定场景。
   * @param scenePath 场景路径，如 'Assets/Scenes/BDFrame.unity'
   */
  async openScene(scenePath: string): Promise<SceneInfo> {
    console.log(`[EditorOps.scene] 打开场景: ${scenePath}`);
    const result = await this.reflection.invokeStatic(
      'UnityEditor.SceneManagement.EditorSceneManager.OpenScene',
      [scenePath],
    );
    const scene = normalizeSceneInfo(result.value);
    console.log(`[EditorOps.scene] 场景已打开: ${JSON.stringify(scene)}`);
    return scene;
  }

  /**
   * 获取当前活动场景信息。
   */
  async getCurrentScene(): Promise<SceneInfo> {
    console.log(`[EditorOps.scene] 查询当前场景`);
    const result = await this.reflection.invokeStatic('UnityEngine.SceneManagement.SceneManager.GetActiveScene');
    return normalizeSceneInfo(result.value);
  }

  /**
   * 保存当前场景。
   */
  async saveScene(): Promise<{ saved: boolean; scenePath?: string; reason?: string }> {
    console.log(`[EditorOps.scene] 保存场景`);
    const currentScene = await this.getCurrentScene();
    if (!currentScene.path) {
      return { saved: false, reason: '场景未保存到磁盘（可能是新建未保存场景）' };
    }

    const result = await this.reflection.invokeStatic('UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes');
    const saved = Boolean(result.value);
    const saveResult = { saved, scenePath: currentScene.path };
    console.log(`[EditorOps.scene] 保存结果: ${JSON.stringify(saveResult)}`);
    return saveResult;
  }
}

// ====================================================================
// EditorPlayModeOps —— PlayMode 控制模块
// ====================================================================

/**
 * PlayMode 控制模块——提供进入/退出 PlayMode、等待状态变化等操作。
 */
export class EditorPlayModeOps {
  constructor(private reflection: EditorReflectionOps) {}

  /**
   * 进入 PlayMode。
   * 注意：此操作是异步的，Unity 需要若干帧才能完成进入。
   * 调用后应使用 waitForPlayMode(true) 等待。
   */
  async enterPlayMode(): Promise<void> {
    console.log(`[EditorOps.playMode] 进入 PlayMode`);
    await this.reflection.invokeStatic('UnityEditor.EditorApplication.EnterPlaymode');
    console.log(`[EditorOps.playMode] EnterPlaymode 已触发，等待生效...`);
  }

  /**
   * 退出 PlayMode。
   * 注意：此操作是异步的，调用后应使用 waitForPlayMode(false) 等待。
   */
  async exitPlayMode(): Promise<void> {
    console.log(`[EditorOps.playMode] 退出 PlayMode`);
    await this.reflection.invokeStatic('UnityEditor.EditorApplication.ExitPlaymode');
    console.log(`[EditorOps.playMode] ExitPlaymode 已触发，等待生效...`);
  }

  /**
   * 查询当前是否处于 PlayMode。
   */
  async isPlaying(): Promise<boolean> {
    return this.reflection.getStaticValue<boolean>('UnityEditor.EditorApplication.isPlaying');
  }

  /**
   * 等待 PlayMode 状态达到期望值。
   * @param expected 期望的 isPlaying 状态
   * @param timeoutMs 超时毫秒，默认 30 秒
   * @param intervalMs 轮询间隔，默认 500ms
   */
  async waitForPlayMode(expected: boolean, timeoutMs: number = 30000, intervalMs: number = 500): Promise<void> {
    console.log(`[EditorOps.playMode] 等待 PlayMode=${expected}（超时 ${timeoutMs}ms）`);
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
      const playing = await this.isPlaying();
      if (playing === expected) {
        console.log(`[EditorOps.playMode] PlayMode 已达到 ${expected}（耗时 ${Date.now() - start}ms）`);
        return;
      }
      await new Promise(resolve => setTimeout(resolve, intervalMs));
    }
    throw new Error(`[EditorOps.playMode] 等待 PlayMode=${expected} 超时 (${timeoutMs}ms)`);
  }
}

// ====================================================================
// EditorBridgeOps —— 编辑器通用操作模块
// ====================================================================

/**
 * 编辑器通用操作模块——提供菜单执行、状态查询等操作。
 */
export class EditorBridgeOps {
  constructor(private reflection: EditorReflectionOps) {}

  /**
   * 执行 Unity 菜单项——等效于点击菜单。
   * @param menuPath 菜单路径，如 'Talos/E2E Test/检查 DEBUG 状态'
   */
  async executeMenuItem(menuPath: string): Promise<void> {
    console.log(`[EditorOps.bridge] 执行菜单项: ${menuPath}`);
    await this.reflection.invokeStatic('UnityEditor.EditorApplication.ExecuteMenuItem', [menuPath]);
    console.log(`[EditorOps.bridge] 菜单项已执行: ${menuPath}`);
  }

  /**
   * 获取编辑器当前状态快照。
   */
  async getEditorState(): Promise<EditorState> {
    console.log(`[EditorOps.bridge] 查询编辑器状态`);
    // 当前 UnityConnector 只按消息类型匹配响应，不按 requestId 多路复用，
    // 所以同一时刻不能并发等待多个 editor_command_result，这里保持串行调用。
    const isPlaying = await this.reflection.getStaticValue<boolean>('UnityEditor.EditorApplication.isPlaying');
    const isCompiling = await this.reflection.getStaticValue<boolean>('UnityEditor.EditorApplication.isCompiling');
    const isUpdating = await this.reflection.getStaticValue<boolean>('UnityEditor.EditorApplication.isUpdating');
    const timeSinceStartup = await this.reflection.getStaticValue<number>('UnityEditor.EditorApplication.timeSinceStartup');
    const sceneResult = await this.reflection.invokeStatic('UnityEngine.SceneManagement.SceneManager.GetActiveScene');

    const scene = normalizeSceneInfo(sceneResult.value);
    const state: EditorState = {
      isPlaying,
      isCompiling,
      isUpdating,
      currentScene: scene.name,
      currentScenePath: scene.path,
      timeSinceStartup,
    };

    console.log(`[EditorOps.bridge] 编辑器状态: isPlaying=${state.isPlaying}, scene=${state.currentScene}`);
    return state;
  }

  /**
   * 等待编辑器编译完成。
   * @param timeoutMs 超时毫秒，默认 60 秒
   * @param intervalMs 轮询间隔，默认 1000ms
   */
  async waitForCompile(timeoutMs: number = 60000, intervalMs: number = 1000): Promise<void> {
    console.log(`[EditorOps.bridge] 等待编译完成（超时 ${timeoutMs}ms）`);
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
      const state = await this.getEditorState();
      if (!state.isCompiling) {
        console.log(`[EditorOps.bridge] 编译已完成（耗时 ${Date.now() - start}ms）`);
        return;
      }
      await new Promise(resolve => setTimeout(resolve, intervalMs));
    }
    throw new Error(`[EditorOps.bridge] 等待编译完成超时 (${timeoutMs}ms)`);
  }
}

// ====================================================================
// EditorReflectionOps —— 万能反射接口模块
// ====================================================================

/**
 * 通用反射接口模块——允许 Playwright 直接调用 Unity Editor 中的 public static API。
 *
 * 使用场景：
 * - 直接调用 Unity 官方 API、框架 API、项目 API。
 * - 读取 Editor 静态状态，而不是在 Unity 侧额外封装 CmdIsPlaying 之类的薄命令。
 * - 仅限 Editor 模式，不会进入 Runtime 构建。
 *
 * 使用示例：
 * ```typescript
 * const methodResult = await ops.reflection.invokeStatic(
 *   'Talos.E2E.Editor.E2EEditorTools.CheckDebugStatus'
 * );
 * const isPlaying = await ops.reflection.getStaticValue<boolean>(
 *   'UnityEditor.EditorApplication.isPlaying'
 * );
 * ```
 */
export class EditorReflectionOps {
  constructor(private connector: UnityConnector) {}

  /**
   * 通过反射执行 Unity Editor 中的 public static 方法。
   *
   * @param memberPath 完整方法路径，格式: 'Namespace.ClassName.MethodName'
   * @param args 可选参数数组
   * @returns 反射调用结果
   */
  async invokeStatic(memberPath: string, args?: any[]): Promise<ReflectionMethodResult> {
    const params: Record<string, any> = { memberPath };
    if (args && args.length > 0) {
      params.args = args;
    }
    console.log(`[EditorOps.reflection] 反射静态方法调用: ${memberPath}(${args ? args.length : 0} 个参数)`);
    const result = await this.connector.executeEditorCommand('reflect_invoke_static', params);
    console.log(`[EditorOps.reflection] 调用完成: ${memberPath}, 返回类型=${result.data?.returnType}`);
    return result.data as ReflectionMethodResult;
  }

  /**
   * 读取 Unity Editor 中的 public static 属性或字段。
   *
   * @param memberPath 完整成员路径，格式: 'Namespace.ClassName.MemberName'
   * @returns 成员读取结果
   */
  async getStaticMember(memberPath: string): Promise<ReflectionMemberResult> {
    console.log(`[EditorOps.reflection] 读取静态成员: ${memberPath}`);
    const result = await this.connector.executeEditorCommand('reflect_get_static', { memberPath });
    console.log(`[EditorOps.reflection] 读取完成: ${memberPath}, 成员类型=${result.data?.valueType}`);
    return result.data as ReflectionMemberResult;
  }

  /**
   * 直接读取静态成员的值。
   */
  async getStaticValue<T>(memberPath: string): Promise<T> {
    const result = await this.getStaticMember(memberPath);
    return result.value as T;
  }

  /**
   * 兼容旧命名的反射调用入口。
   */
  async executeMethod(methodPath: string, args?: any[]): Promise<ReflectionMethodResult> {
    return this.invokeStatic(methodPath, args);
  }
}

// ====================================================================
// UnityEditorOps —— 聚合门面
// ====================================================================

/**
 * Unity Editor 操作聚合门面——包含所有子模块，作为统一入口。
 *
 * 设计角色：
 * - 将 Editor 操作按职责拆分为独立子模块（scene / playMode / bridge / reflection）。
 * - 每个子模块可独立使用，也可通过此门面统一访问。
 * - 所有操作底层都走 TCP 的 editor_command 协议。
 *
 * 使用示例：
 * ```typescript
 * const ops = new UnityEditorOps(connector);
 *
 * // 场景操作
 * await ops.scene.openScene('Assets/Scenes/BDFrame.unity');
 *
 * // PlayMode 控制
 * await ops.playMode.enterPlayMode();
 * await ops.playMode.waitForPlayMode(true);
 *
 * // 查询编辑器状态
 * const state = await ops.bridge.getEditorState();
 *
 * // 万能反射调用
 * await ops.reflection.executeMethod('SomeClass.SomeMethod');
 * ```
 */
export class UnityEditorOps {
  /** 场景操作模块 */
  public readonly scene: EditorSceneOps;
  /** PlayMode 控制模块 */
  public readonly playMode: EditorPlayModeOps;
  /** 编辑器通用操作模块 */
  public readonly bridge: EditorBridgeOps;
  /** 万能反射接口模块 */
  public readonly reflection: EditorReflectionOps;

  constructor(private connector: UnityConnector) {
    this.reflection = new EditorReflectionOps(connector);
    this.scene = new EditorSceneOps(this.reflection);
    this.playMode = new EditorPlayModeOps(this.reflection);
    this.bridge = new EditorBridgeOps(this.reflection);
  }
}
