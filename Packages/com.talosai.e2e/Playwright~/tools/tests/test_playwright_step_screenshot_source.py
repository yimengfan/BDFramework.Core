"""Talos Playwright 步骤截图源码回归测试。
Talos Playwright step screenshot source regression tests.

覆盖范围：
1. 公共 fixture 必须暴露 talosStep 包装器，用于统一记录步骤并在结束后截图。
2. UnityConnector 必须提供 captureScreenshot 能力，确保 Playwright 不需要感知底层 action 细节。
3. Windows BaseFlow 规范测试必须通过 talosStep 执行关键步骤，确保截图进入标准 Playwright 报告。
4. PC 启动脚本必须保留桌面窗口模式覆盖参数，避免远端 Player 回退到不稳定的默认分辨率。

Coverage:
1. Shared fixtures must expose the talosStep wrapper so steps are recorded uniformly and screenshots are attached afterwards.
2. UnityConnector must provide captureScreenshot so Playwright does not need to know lower-level action details.
3. The Windows BaseFlow spec must execute key steps through talosStep so screenshots land in the standard Playwright report.
4. The PC launcher script must keep desktop window-mode override arguments so remote players do not fall back to unstable default resolutions.
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
FIXTURES_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "fixtures.ts"
CONNECTOR_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "src" / "unity-connector.ts"
BASEFLOW_SPEC_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "testBaseFlow-e2e.spec.ts"
PC_TOOL_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tools" / "test-pc.sh"


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


def test_pc_tool_keeps_force_e2e_and_windows_player_log_capture() -> None:
    """验证 PC 工具脚本会保留强制 E2E 与桌面窗口参数，并让 Windows 分支把 Unity 日志写入文件后在失败路径回吐。
    Verify that the PC tool script preserves forced E2E and desktop window arguments and lets the Windows branch dump Unity logs on failure.
    """
    tool_content = PC_TOOL_PATH.read_text(encoding="utf-8")

    assert '"-talosForceE2E"' in tool_content
    assert '"-screen-fullscreen"' in tool_content
    assert '"-screen-width"' in tool_content
    assert '"-screen-height"' in tool_content
    assert '"-logFile"' in tool_content
    assert '"-"' in tool_content
    assert 'IS_WINDOWS_GIT_BASH=false' in tool_content
    assert 'Start-Process -FilePath' in tool_content
    assert '-WorkingDirectory' in tool_content
    assert '-RedirectStandardOutput' not in tool_content
    assert '-RedirectStandardError' not in tool_content
    assert 'unity-player.log' in tool_content
    assert 'print_windows_player_logs' in tool_content
    assert 'taskkill.exe //PID ${APP_PID}' in tool_content