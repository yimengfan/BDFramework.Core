"""Talos 启动画面自动跳过回归测试。

覆盖范围：
1. 预配置界面必须识别 -talosForceE2E 命令行参数。
2. 识别后必须自动执行启动入口，避免桌面 E2E 在等待 TCP 前陷入按钮点击死锁。
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
WINDOW_PRECONFIG_PATH = REPO_ROOT / "Assets" / "Scenes" / "Simple" / "AOTStart" / "WindowPreconfig.cs"


def test_window_preconfig_supports_talos_force_e2e_auto_launch() -> None:
    """验证桌面预配置界面已经支持 Talos E2E 的命令行自动启动入口。"""
    content = WINDOW_PRECONFIG_PATH.read_text(encoding="utf-8")

    assert "ShouldAutoLaunchForTalosE2E" in content
    assert '"-talosForceE2E"' in content
    assert 'Onclick_PassAndLaunch();' in content