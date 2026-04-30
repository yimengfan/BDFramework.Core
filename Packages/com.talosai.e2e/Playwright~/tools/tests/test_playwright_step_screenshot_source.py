"""Talos Playwright 步骤截图源码回归测试。
Talos Playwright step screenshot source regression tests.

覆盖范围：
1. 公共 fixture 必须暴露 talosStep 包装器，用于统一记录步骤并在结束后截图。
2. UnityConnector 必须提供 captureScreenshot 能力，确保 Playwright 不需要感知底层 action 细节。
3. Windows BaseFlow 规范测试必须通过 talosStep 执行关键步骤，确保截图进入标准 Playwright 报告。
4. PC 启动脚本不得再注入桌面窗口覆盖参数，而应回退到包体默认窗口设置。
5. Windows BaseFlow 规范测试必须覆盖热更 DLL、AB 资源系统与 SQLite 三条基础链路。
6. BaseFlow TeamCity buildType 必须拆分为 prepare / run 两个步骤，并通过本地包体参数交接。

Coverage:
1. Shared fixtures must expose the talosStep wrapper so steps are recorded uniformly and screenshots are attached afterwards.
2. UnityConnector must provide captureScreenshot so Playwright does not need to know lower-level action details.
3. The Windows BaseFlow spec must execute key steps through talosStep so screenshots land in the standard Playwright report.
4. The PC launcher script must no longer inject desktop window override arguments and should defer to the packaged default window settings.
5. The Windows BaseFlow spec must cover the hotfix-DLL, AssetBundle resource, and SQLite foundational paths.
6. The BaseFlow TeamCity buildType must be split into prepare and run steps and hand off the local package path explicitly.
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
FIXTURES_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "fixtures.ts"
CONNECTOR_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "src" / "unity-connector.ts"
BASEFLOW_SPEC_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "testBaseFlow-e2e.spec.ts"
PC_TOOL_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tools" / "test-pc.sh"
BASEFLOW_BUILDTYPE_PATH = REPO_ROOT / ".test-DevOps" / ".teamcity" / "buildTypes" / "TalosAIStep01BaseFlowTest.kt"


def test_playwright_step_screenshot_contract_is_wired() -> None:
    """验证 Playwright 公共层已经接入步骤截图契约，且 BaseFlow 会实际使用它。
    Verify that the shared Playwright layer is wired to the step-screenshot contract and that BaseFlow actually uses it.
    """
    fixtures_content = FIXTURES_PATH.read_text(encoding="utf-8")
    connector_content = CONNECTOR_PATH.read_text(encoding="utf-8")
    baseflow_content = BASEFLOW_SPEC_PATH.read_text(encoding="utf-8")

    assert "talosStep" in fixtures_content
    assert "attachUnityStepScreenshot" in fixtures_content
    assert "connector.captureScreenshot(" in fixtures_content

    assert "async captureScreenshot(" in connector_content
    assert "ActionType.SCREENSHOT" in connector_content

    assert "talosStep" in baseflow_content
    assert "await talosStep(" in baseflow_content


def test_pc_tool_defers_window_shape_to_package_defaults_and_keeps_windows_player_log_capture() -> None:
    """验证 PC 工具脚本不再覆写桌面窗口参数，并让 Windows 分支把 Unity 日志写入文件后在失败路径回吐。
    Verify that the PC tool script no longer overrides desktop window arguments and lets the Windows branch dump Unity logs on failure.
    """
    tool_content = PC_TOOL_PATH.read_text(encoding="utf-8")

    assert '"-screen-fullscreen"' not in tool_content
    assert '"-screen-width"' not in tool_content
    assert '"-screen-height"' not in tool_content
    assert '"-popupwindow"' not in tool_content
    assert '"-logFile"' in tool_content
    assert '"-"' in tool_content
    assert 'IS_WINDOWS_GIT_BASH=false' in tool_content
    assert 'Start-Process -FilePath' in tool_content
    assert '-WorkingDirectory' in tool_content
    assert '-RedirectStandardOutput' not in tool_content
    assert '-RedirectStandardError' not in tool_content
    assert 'unity-player-${PLAYER_LOG_FILE_SUFFIX}.log' in tool_content
    assert 'print_windows_player_logs' in tool_content
    assert 'taskkill.exe //PID ${APP_PID}' in tool_content
    assert 'resolve_current_build_id() {' in tool_content
    assert 'cleanup_stale_test_result_player_logs' in tool_content
    # -batchmode -nographics 必须只出现在 IS_WINDOWS_TEAMCITY 条件分支内，
    # 非 Windows TeamCity 环境（macOS/Linux 本地、macOS CI）不应无条件传递。
    # -batchmode -nographics must only appear inside the IS_WINDOWS_TEAMCITY conditional,
    # non-Windows TeamCity environments (macOS/Linux local, macOS CI) must not pass them unconditionally.
    tool_content_lines = tool_content.splitlines()
    batchmode_lines = [i for i, line in enumerate(tool_content_lines)
                       if 'PLAYER_LAUNCH_ARGS+=("-batchmode" "-nographics")' in line]
    for idx in batchmode_lines:
        # 向上搜索最近的 if 语句，确认 -batchmode 在条件块内
        # Search upward for the nearest if-statement to confirm -batchmode is inside a conditional
        found_guard = False
        for j in range(idx - 1, max(idx - 10, -1), -1):
            stripped = tool_content_lines[j].strip()
            if stripped.startswith('if '):
                if 'IS_WINDOWS_TEAMCITY' in stripped:
                    found_guard = True
                break
        assert found_guard, (
            f'PLAYER_LAUNCH_ARGS+=("-batchmode" "-nographics") 出现在第 {idx + 1} 行，'
            '但未检测到 IS_WINDOWS_TEAMCITY 条件保护'
        )


def test_baseflow_spec_covers_foundational_runtime_suites() -> None:
    """验证 BaseFlow spec 会执行热更、AB 资源与 SQLite 相关的基础套件。
    Verify that the BaseFlow spec executes the foundational suites covering hotfix, AssetBundle resource, and SQLite checks.
    """
    baseflow_content = BASEFLOW_SPEC_PATH.read_text(encoding="utf-8")

    assert "suite: 'launch'" in baseflow_content
    assert "suite: 'asset-load'" in baseflow_content
    assert "suite: 'framework-integration'" in baseflow_content
    assert "热更 DLL 可用性" in baseflow_content
    assert "AB 资产系统可用性" in baseflow_content
    assert "SQLite 可用性" in baseflow_content


def test_baseflow_buildtype_splits_prepare_and_run_steps() -> None:
    """验证 BaseFlow TeamCity buildType 已拆为 prepare/run 两步，并使用本地包体参数交接。
    Verify that the BaseFlow TeamCity buildType is split into prepare and run steps and passes the prepared local package path between them.
    """
    buildtype_content = BASEFLOW_BUILDTYPE_PATH.read_text(encoding="utf-8")

    assert 'param("talos.e2e.local.package.path", "")' in buildtype_content
    assert 'name = "Prepare Windows package for Talos BaseFlow"' in buildtype_content
    assert 'name = "Run Talos BaseFlow on prepared Windows package"' in buildtype_content
    assert '--phase prepare' in buildtype_content
    assert '--phase run' in buildtype_content
    assert '--package-path "%talos.e2e.local.package.path%"' in buildtype_content
