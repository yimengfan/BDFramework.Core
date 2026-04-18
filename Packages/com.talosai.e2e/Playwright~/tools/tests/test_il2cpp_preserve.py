"""Talos IL2CPP 反射保活测试。

覆盖范围：
1. 反射入口 E2EAutoInit 在 IL2CPP 下必须显式带 Preserve。
2. 反射方法 CheckAndLaunch 也必须显式带 Preserve，避免只靠字符串查找时被裁剪。
3. 业务层 step_01 launch 套件也必须显式带 Preserve，避免 Android IL2CPP 下握手成功但 tests=0。

Talos IL2CPP reflection preservation tests.

Coverage:
1. The reflection entry E2EAutoInit must be explicitly preserved under IL2CPP.
2. The reflection method CheckAndLaunch must also be explicitly preserved so string-based lookup is not stripped.
3. The host-side step_01 launch suite must also be explicitly preserved so Android IL2CPP does not handshake successfully with tests=0.
"""

from __future__ import annotations

from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[5]
E2E_AUTO_INIT_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "E2EAutoInit.cs"
LAUNCH_TESTS_PATH = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime.Test" / "Runtime" / "E2E" / "LaunchTests.cs"


def test_e2e_auto_init_preserves_reflection_entrypoints() -> None:
    """验证 IL2CPP 下通过字符串反射调用的 Talos E2E 入口已经显式保活。
    Verify that the Talos E2E entrypoints invoked through string-based reflection are explicitly preserved for IL2CPP.
    """
    content = E2E_AUTO_INIT_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[Preserve\]\s*static public class E2EAutoInit", content)
    assert re.search(r"\[Preserve\]\s*static public void CheckAndLaunch", content)


def test_launch_suite_preserves_android_il2cpp_entrypoints() -> None:
    """验证 step_01 launch 套件在 Android IL2CPP 下不会被裁剪成 tests=0。
    Verify that the step_01 launch suite is explicitly preserved so Android IL2CPP does not strip it down to tests=0.
    """
    content = LAUNCH_TESTS_PATH.read_text(encoding="utf-8")

    assert "using UnityEngine.Scripting;" in content
    assert re.search(r"\[Preserve\]\s*static public class LaunchTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 2", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 3", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 4", content)
