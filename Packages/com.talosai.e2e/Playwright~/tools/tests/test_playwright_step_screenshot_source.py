"""Talos Playwright 步骤截图源码回归测试。

覆盖范围：
1. 公共 fixture 必须暴露 talosStep 包装器，用于统一记录步骤并在结束后截图。
2. UnityConnector 必须提供 captureScreenshot 能力，确保 Playwright 不需要感知底层 action 细节。
3. Windows BaseFlow 规范测试必须通过 talosStep 执行关键步骤，确保截图进入标准 Playwright 报告。
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
FIXTURES_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "fixtures.ts"
CONNECTOR_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "src" / "unity-connector.ts"
BASEFLOW_SPEC_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "基础启动流程-e2e.spec.ts"
PC_TOOL_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tools" / "test-pc.sh"


def test_playwright_step_screenshot_contract_is_wired() -> None:
    """验证 Playwright 公共层已经接入步骤截图契约，且 BaseFlow 会实际使用它。"""
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


def test_pc_tool_keeps_force_e2e_and_player_log_streaming() -> None:
    """验证 PC 工具脚本会保留强制 E2E 参数，并把 Unity Player 日志直接回流到 TeamCity 控制台。"""
    tool_content = PC_TOOL_PATH.read_text(encoding="utf-8")

    assert '"-talosForceE2E"' in tool_content
    assert '"-logFile"' in tool_content
    assert '"-"' in tool_content
    assert 'IS_WINDOWS_GIT_BASH=false' in tool_content
    assert 'Start-Process -FilePath' in tool_content
    assert '-WorkingDirectory' in tool_content
    assert 'taskkill.exe //PID ${APP_PID}' in tool_content