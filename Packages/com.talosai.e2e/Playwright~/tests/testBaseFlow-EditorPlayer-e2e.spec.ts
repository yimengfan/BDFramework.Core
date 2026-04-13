/**
 * EditorPlayer 基础流程 E2E 测试。
 *
 * 测试目的：
 * - 验证 Playwright 能通过 Editor 控制面完成一次最小闭环。
 * - 验证 BDFrame 场景加载、PlayMode 进入、等待、退出链路稳定。
 *
 * 测试步骤：
 * 1. 打开 Assets/Scenes/BDFrame.unity。
 * 2. 进入 PlayMode，并在 Domain Reload 后重连。
 * 3. 等待 30 秒。
 * 4. 退出 PlayMode，并再次重连。
 *
 * TODO: 后续在此基础上补充更完整的业务断言。
 */

import { test, expect } from './fixtures-unityplayer';

/**
 * 测试套件：EditorPlayer 基础流程。
 * 该套件只保留最小可复用链路，避免把复杂业务断言提前耦合进基础稳定性验证。
 */
test.describe('基础流程_EditorPlayer', () => {
  /**
   * 用例：打开 BDFrame 场景，进入 PlayMode，等待 30 秒后退出。
   */
  test('打开BDFrame场景并完成一次PlayMode往返_EditorPlayer', async ({ connector, editorOps, device }) => {
    const host = device.getConnectionHost();
    const port = device.getConnectionPort();

    /**
     * 带重连等待 PlayMode 状态。
     * Domain Reload 期间 editor_command 可能短暂不可用，这里允许失败后重连再试。
     */
    const waitForPlayStateWithReconnect = async (expected: boolean, phaseName: string) => {
      const deadline = Date.now() + 60000;
      let attempt = 0;

      while (Date.now() < deadline) {
        attempt++;
        try {
          if (!connector.connected) {
            await connector.reconnect(host, port, 60000, 2000);
          }

          const isPlaying = await editorOps.reflection.getStaticValue<boolean>(
            'UnityEditor.EditorApplication.isPlaying',
          );
          console.log(`[BaseFlow] ${phaseName} 第 ${attempt} 次检查: isPlaying=${isPlaying}`);

          if (isPlaying === expected) {
            return;
          }
        } catch (error) {
          console.log(`[BaseFlow] ${phaseName} 第 ${attempt} 次检查失败，尝试重连: ${String(error)}`);
          await connector.reconnect(host, port, 60000, 2000);
        }

        await new Promise((resolve) => setTimeout(resolve, 1000));
      }

      throw new Error(`[BaseFlow] ${phaseName} 等待 PlayMode=${expected} 超时`);
    };

    // 阶段一：确保从非 PlayMode 开始，避免前置状态污染。
    const initialState = await editorOps.bridge.getEditorState();
    console.log(
      `[BaseFlow] 初始状态: isPlaying=${initialState.isPlaying}, scene=${initialState.currentScene}`,
    );
    if (initialState.isPlaying) {
      console.log('[BaseFlow] 检测到当前处于 PlayMode，先执行退出');
      try {
        await editorOps.playMode.exitPlayMode();
      } catch (error) {
        console.log(`[BaseFlow] 退出 PlayMode 过程中连接重置，按预期继续重连: ${String(error)}`);
      }
      await waitForPlayStateWithReconnect(false, '初始清理');
    }

    // 阶段二：打开基线场景，确保后续 PlayMode 流程在固定入口执行。
    const sceneInfo = await editorOps.scene.openScene('Assets/Scenes/BDFrame.unity');
    console.log(`[BaseFlow] 场景已打开: ${sceneInfo.name} (${sceneInfo.path})`);
    expect(sceneInfo.isLoaded).toBe(true);
    expect(sceneInfo.name).toBe('BDFrame');

    // 阶段三：进入 PlayMode，并在 Domain Reload 后重连。
    console.log('[BaseFlow] 进入 PlayMode');
    try {
      await editorOps.playMode.enterPlayMode();
    } catch (error) {
      console.log(`[BaseFlow] 进入 PlayMode 过程中连接重置，按预期继续重连: ${String(error)}`);
    }
    await waitForPlayStateWithReconnect(true, '进入 PlayMode');

    // 阶段四：保持 PlayMode 30 秒，为后续补充业务断言预留稳定窗口。
    console.log('[BaseFlow] PlayMode 保持 30 秒');
    await new Promise((resolve) => setTimeout(resolve, 30000));

    // 阶段五：退出 PlayMode，并确认回到 Editor 空闲态。
    console.log('[BaseFlow] 退出 PlayMode');
    try {
      await editorOps.playMode.exitPlayMode();
    } catch (error) {
      console.log(`[BaseFlow] 退出 PlayMode 过程中连接重置，按预期继续重连: ${String(error)}`);
    }
    await waitForPlayStateWithReconnect(false, '退出 PlayMode');

    const finalIsPlaying = await editorOps.reflection.getStaticValue<boolean>(
      'UnityEditor.EditorApplication.isPlaying',
    );
    console.log(`[BaseFlow] 结束状态: isPlaying=${finalIsPlaying}`);
    expect(finalIsPlaying).toBe(false);
  });
});