"""Talos WindowPreconfig 阶段回归测试。
Talos WindowPreconfig stage regression tests.

覆盖范围：
Coverage:
1. 预配置界面仍然会识别 -talosForceE2E 模式并打印阶段日志。
1. The preconfiguration screen must still recognise -talosForceE2E mode and emit a stage log.
2. 宿主代码必须显式根引用宿主侧 launch/BaseFlow 测试程序集，但不能在 WindowPreconfig 阶段再次触发 E2EAutoInit.CheckAndLaunch，避免与 ScriptLoder.Init 的真实启动链重复占用端口。
2. Host code must explicitly root the host-owned launch/BaseFlow test assemblies, but it must not invoke E2EAutoInit.CheckAndLaunch again from WindowPreconfig, otherwise it races with the real ScriptLoder.Init startup path and reuses the same port.
3. 该界面不能再因为 Talos 参数自动跳过，否则无法覆盖预配置界面的 UI 测试内容。
3. The screen must not auto-skip because of Talos flags, otherwise the preconfiguration UI tests lose coverage.
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
WINDOW_PRECONFIG_PATH = REPO_ROOT / "Assets" / "Scenes" / "Simple" / "AOTStart" / "WindowPreconfig.cs"


def test_window_preconfig_keeps_screen_visible_in_talos_force_e2e_mode() -> None:
    """验证 WindowPreconfig 在 Talos 强制模式下保留宿主显式启动并保持界面可见。
    Verify that WindowPreconfig keeps the host-owned suite rooting and stays visible in forced Talos mode.
    """
    content = WINDOW_PRECONFIG_PATH.read_text(encoding="utf-8")

    assert "RuntimeLaunchArguments.ResolveCurrentProcessArguments()" in content
    assert "ForcedModeStartupFallback.TryLaunchFromForcedMode" in content
    assert "当前处于 -talosForceE2E 模式" in content
    assert "typeof(BDFramework.HostE2E.LaunchFlowHostTests).Assembly" in content
    assert "typeof(BDFramework.HostE2E.BaseFlowHostRuntimeTests).Assembly" in content
    assert "宿主已绑定 launch/BaseFlow 宿主测试程序集" in content
    assert "ScriptLoder.Init 仍是首选启动入口" in content
    assert "Talos.E2E.E2EAutoInit.CheckAndLaunch();" not in content
    assert "宿主已显式调用 E2EAutoInit.CheckAndLaunch" not in content
    assert "ShouldAutoLaunchForTalosE2E" not in content
    assert "跳过预配置界面并直接进入框架启动" not in content
