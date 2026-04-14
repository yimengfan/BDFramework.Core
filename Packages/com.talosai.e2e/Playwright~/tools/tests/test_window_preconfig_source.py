"""Talos WindowPreconfig 阶段回归测试。

覆盖范围：
1. 预配置界面仍然会识别 -talosForceE2E 模式并打印阶段日志。
2. 该界面不能再因为 Talos 参数自动跳过，否则无法覆盖预配置界面的 UI 测试内容。
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
WINDOW_PRECONFIG_PATH = REPO_ROOT / "Assets" / "Scenes" / "Simple" / "AOTStart" / "WindowPreconfig.cs"


def test_window_preconfig_keeps_screen_visible_in_talos_force_e2e_mode() -> None:
    """验证桌面预配置界面在 Talos E2E 模式下仍然保持可见，不会自动跳过。"""
    content = WINDOW_PRECONFIG_PATH.read_text(encoding="utf-8")

    assert '"-talosForceE2E"' in content
    assert "WindowPreconfig 保持可见" in content
    assert "ShouldAutoLaunchForTalosE2E" not in content
    assert "跳过预配置界面并直接进入框架启动" not in content