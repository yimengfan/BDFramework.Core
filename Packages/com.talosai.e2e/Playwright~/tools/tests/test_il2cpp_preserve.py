"""Talos IL2CPP 反射保活测试。

覆盖范围：
1. 反射入口 E2EAutoInit 在 IL2CPP 下必须显式带 Preserve。
2. 反射方法 CheckAndLaunch 也必须显式带 Preserve，避免只靠字符串查找时被裁剪。
3. 业务层 step_01 launch 套件也必须显式带 Preserve，避免 Android IL2CPP 下握手成功但 tests=0。
4. BaseFlow 追加的 asset-load 套件也必须显式带 Preserve，避免 Player 中只剩 launch 被发现。
5. BaseFlow 追加的 framework-integration 套件也必须显式带 Preserve，避免 SQLite 与资源集成检查被 IL2CPP 裁剪。

Talos IL2CPP reflection preservation tests.

Coverage:
1. The reflection entry E2EAutoInit must be explicitly preserved under IL2CPP.
2. The reflection method CheckAndLaunch must also be explicitly preserved so string-based lookup is not stripped.
3. The host-side step_01 launch suite must also be explicitly preserved so Android IL2CPP does not handshake successfully with tests=0.
4. The added asset-load suite must also be explicitly preserved so the player does not discover only launch tests.
5. The added framework-integration suite must also be explicitly preserved so the SQLite and resource integration checks survive IL2CPP stripping.
"""

from __future__ import annotations

from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[5]
E2E_AUTO_INIT_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "E2EAutoInit.cs"
LAUNCH_TESTS_PATH = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime.Test" / "Runtime" / "E2E" / "LaunchTests.cs"
ASSET_LOAD_TESTS_PATH = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime.Test" / "Runtime" / "E2E" / "AssetLoadTests.cs"
FRAMEWORK_INTEGRATION_TESTS_PATH = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime.Test" / "Runtime" / "E2E" / "FrameworkIntegrationTests.cs"


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

    assert (
        "using UnityEngine.Scripting;" in content
        or "using Preserve = UnityEngine.Scripting.PreserveAttribute;" in content
    )
    assert re.search(r"\[Preserve\]\s*static public class LaunchTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 2", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 3", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 4", content)


def test_asset_load_suite_preserves_player_entrypoints() -> None:
    """验证 BaseFlow 里的 asset-load 套件在 Player IL2CPP 下不会被裁剪掉。
    Verify that the BaseFlow asset-load suite is explicitly preserved so player IL2CPP builds still discover it.
    """
    content = ASSET_LOAD_TESTS_PATH.read_text(encoding="utf-8")

    assert "using UnityEngine.Scripting;" in content
    assert re.search(r"\[Preserve\]\s*static public class AssetLoadTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 2", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 3", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 999", content)


def test_framework_integration_suite_preserves_player_entrypoints() -> None:
    """验证 BaseFlow 里的 framework-integration 套件在 Player IL2CPP 下不会被裁剪掉。
    Verify that the BaseFlow framework-integration suite is explicitly preserved so player IL2CPP builds still discover it.
    """
    content = FRAMEWORK_INTEGRATION_TESTS_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*public static class FrameworkIntegrationTests", content)
    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*\[E2ETest\(suite: \"framework-integration\", order: 1", content)
    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*\[E2ETest\(suite: \"framework-integration\", order: 2", content)
    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*\[E2ETest\(suite: \"framework-integration\", order: 3", content)
    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*\[E2ETest\(suite: \"framework-integration\", order: 4", content)
    assert re.search(r"\[(?:Preserve|UnityEngine\.Scripting\.Preserve)\]\s*\[E2ETest\(suite: \"framework-integration\", order: 5", content)
