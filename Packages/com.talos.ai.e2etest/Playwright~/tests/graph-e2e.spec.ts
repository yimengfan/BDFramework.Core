/**
 * Graph 模式 E2E 测试——通过 Playwright 编排，在 Unity Editor (headed GUI) 中执行。
 *
 * 测试流程：
 * 1. 通过 fixture 连接到 Unity Editor 的 TCP 测试服务。
 * 2. 打开 BDFramework 场景。
 * 3. 进入 PlayMode。
 * 4. 等待测试开始。
 * 5. 执行 E2E 测试用例。
 * 6. 验证所有测试通过。
 *
 * 前置条件：
 * - Unity Editor 已以 GUI 模式启动，并运行了 TCP 服务。
 * - 可通过 test-unityplayer.sh 脚本启动。
 */

import { test, expect } from './fixtures-unityplayer';

/**
 * 测试套件：Editor 基础操作验证。
 * 确保 Playwright 与 Unity Editor 的 TCP 通信通道正常。
 */
test.describe('Editor 基础操作验证', () => {
  test('应能成功连接到 Unity Editor', async ({ connector }) => {
    expect(connector.connected).toBe(true);
  });

  test('应能查询编辑器状态', async ({ editorOps }) => {
    const state = await editorOps.bridge.getEditorState();
    expect(state).toBeDefined();
    expect(typeof state.isPlaying).toBe('boolean');
    expect(typeof state.isCompiling).toBe('boolean');
    console.log(`[GraphE2E] 编辑器状态: isPlaying=${state.isPlaying}, scene=${state.currentScene}`);
  });
});

/**
 * 测试套件：Graph 模式主流程。
 * 打开 BDFramework 场景 → 进入 PlayMode → 执行测试 → 验证通过。
 */
test.describe('Graph 模式主流程', () => {
  test('打开 BDFRamework 场景并执行 Editor-only 测试', async ({ connector, editorOps }) => {
    // Phase 1: 查询初始状态
    const initialState = await editorOps.bridge.getEditorState();
    console.log(`[GraphE2E] 初始状态: isPlaying=${initialState.isPlaying}, scene=${initialState.currentScene}`);

    // Phase 2: 如果在 PlayMode 中，先退出
    if (initialState.isPlaying) {
      console.log('[GraphE2E] 检测到 PlayMode 已激活，先退出');
      await editorOps.playMode.exitPlayMode();
      await editorOps.playMode.waitForPlayMode(false, 15000);
    }

    // Phase 3: 打开 BDFramework 场景
    const scenePaths = [
      'Assets/Scenes/BDFrame.unity',
      'Assets/Scenes/BDFrame_Debug.unity',
    ];

    let sceneOpened = false;
    for (const scenePath of scenePaths) {
      try {
        console.log(`[GraphE2E] 尝试打开场景: ${scenePath}`);
        const sceneInfo = await editorOps.scene.openScene(scenePath);
        console.log(`[GraphE2E] 场景已打开: ${sceneInfo.name} (${sceneInfo.path})`);
        sceneOpened = true;
        break;
      } catch (err: any) {
        console.log(`[GraphE2E] 场景打开失败（可能不存在）: ${scenePath} - ${err.message}`);
      }
    }

    if (!sceneOpened) {
      console.log('[GraphE2E] 未找到 BDFrame 场景，使用当前场景继续');
    }

    // Phase 4: 验证 Editor 操作
    const currentScene = await editorOps.scene.getCurrentScene();
    console.log(`[GraphE2E] 当前场景: ${currentScene.name} (${currentScene.path})`);
    expect(currentScene.isLoaded).toBe(true);

    // Phase 5: 通过万能反射接口验证
    console.log('[GraphE2E] 测试万能反射接口...');
    const reflectionResult = await editorOps.reflection.executeMethod(
      'Talos.E2E.Editor.E2EEditorTools.CheckDebugStatus',
    );
    console.log(`[GraphE2E] 反射调用成功: returnType=${reflectionResult.returnType}`);

    // Phase 6: 列出并执行 Editor-only 测试
    const tests = await connector.listTests();
    console.log(`[GraphE2E] 发现 ${tests.length} 个测试用例`);
    expect(tests.length).toBeGreaterThan(0);

    const { results, summary } = await connector.runAllTests();

    // 输出详细结果
    console.log('\n========================================');
    console.log('  Talos E2E Graph 模式测试结果汇总');
    console.log('========================================');
    console.log(`  总计: ${summary.total}`);
    console.log(`  通过: ${summary.passed}`);
    console.log(`  失败: ${summary.failed}`);
    console.log('========================================\n');

    for (const r of results) {
      const status = r.passed ? '✅' : '❌';
      console.log(`  ${status} [${r.suite}] ${r.methodName} - ${r.description} (${r.durationMs}ms)`);
      if (!r.passed) {
        console.log(`     错误: ${r.errorMessage}`);
      }
    }

    // Phase 7: 验证所有测试通过
    expect(summary.failed).toBe(0);
    expect(summary.total).toBeGreaterThan(0);
    console.log('[GraphE2E] ✅ 全部测试通过');
  });

  test('完整 PlayMode 生命周期: Editor → PlayMode → Editor → PlayMode', async ({ connector, editorOps, device }) => {
    // 完整连接生命周期测试：
    // Editor connect → enter PlayMode → reconnect → exit PlayMode → reconnect → re-enter PlayMode → reconnect
    // 模拟真机场景：每次 PlayMode 切换 = 应用重启，Playwright 必须重连。
    //
    // 注意：enter_playmode / exit_playmode 会触发 Domain Reload，
    // TCP 连接可能在命令响应返回之前就断开，这是正常行为。
    // 使用 try-catch 忽略连接断开异常，直接走 reconnect。

    const host = device.getConnectionHost();
    const port = device.getConnectionPort();

    // ======== Phase 0: 确保从非 PlayMode 开始 ========
    const state0 = await editorOps.bridge.getEditorState();
    console.log(`[Lifecycle] Phase 0: isPlaying=${state0.isPlaying}`);
    if (state0.isPlaying) {
      console.log('[Lifecycle] 退出 PlayMode...');
      try { await editorOps.playMode.exitPlayMode(); } catch (_) { /* Domain Reload 会断连 */ }
      await connector.reconnect(host, port, 30000, 2000);
    }

    // ======== Phase 1: Editor 模式 → 进入 PlayMode → 重连 ========
    console.log('[Lifecycle] Phase 1: Editor → PlayMode');
    try { await editorOps.playMode.enterPlayMode(); } catch (_) { /* Domain Reload 会断连 */ }
    await connector.reconnect(host, port, 60000, 2000);
    const state1 = await connector.listTests();
    console.log(`[Lifecycle] Phase 1 ✅ PlayMode 内已重连, ${state1.length} 个测试`);
    expect(state1.length).toBeGreaterThan(0);

    // 在 PlayMode 内跑一次全部测试
    const result1 = await connector.runAllTests();
    console.log(`[Lifecycle] Phase 1 测试: ${result1.summary.passed}/${result1.summary.total} 通过`);
    expect(result1.summary.failed).toBe(0);

    // ======== Phase 2: PlayMode → 退出 → Editor 模式重连 ========
    console.log('[Lifecycle] Phase 2: PlayMode → Editor');
    try { await editorOps.playMode.exitPlayMode(); } catch (_) { /* Domain Reload 会断连 */ }
    await connector.reconnect(host, port, 60000, 2000);
    const state2 = await connector.listTests();
    console.log(`[Lifecycle] Phase 2 ✅ Editor 模式已重连, ${state2.length} 个测试`);
    expect(state2.length).toBeGreaterThan(0);

    // 在 Editor 模式下跑一次全部测试
    const result2 = await connector.runAllTests();
    console.log(`[Lifecycle] Phase 2 测试: ${result2.summary.passed}/${result2.summary.total} 通过`);
    expect(result2.summary.failed).toBe(0);

    // ======== Phase 3: 再次进入 PlayMode → 重连（验证可重复性）========
    console.log('[Lifecycle] Phase 3: Editor → PlayMode (re-enter)');
    try { await editorOps.playMode.enterPlayMode(); } catch (_) { /* Domain Reload 会断连 */ }
    await connector.reconnect(host, port, 60000, 2000);
    const state3 = await connector.listTests();
    console.log(`[Lifecycle] Phase 3 ✅ 再次 PlayMode 内已重连, ${state3.length} 个测试`);
    expect(state3.length).toBeGreaterThan(0);

    const result3 = await connector.runAllTests();
    console.log(`[Lifecycle] Phase 3 测试: ${result3.summary.passed}/${result3.summary.total} 通过`);
    expect(result3.summary.failed).toBe(0);

    console.log('[GraphE2E] ✅ 完整 PlayMode 生命周期测试通过');
    console.log('  Phase 1: Editor→PlayMode ✅');
    console.log('  Phase 2: PlayMode→Editor ✅');
    console.log('  Phase 3: Editor→PlayMode ✅');
  });
});
