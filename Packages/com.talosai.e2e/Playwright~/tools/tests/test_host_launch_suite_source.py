"""Talos 宿主 launch 套件源码回归测试。
Talos host launch-suite source regression tests.

覆盖范围：
Coverage:
1. step_01 使用的 launch 宿主测试必须留在宿主包内，而不能继续根引用依赖热更层的 Runtime.Test 程序集。
1. The launch host tests used by step_01 must remain in the host package instead of rooting the Runtime.Test assembly that depends on the hotfix layer.
2. 宿主 launch 套件必须显式带 Preserve 和 launch suite 标记，避免 Android IL2CPP 下再次出现 tests=0。
2. The host launch suite must explicitly carry Preserve and the launch-suite markers so Android IL2CPP does not regress back to tests=0.
3. 静态 Assets/link.xml 不能再保留 HybridCLR 热更程序集 BDFramework.Core，避免 stripped-AOT 临时工程解析不存在的程序集。
3. The static Assets/link.xml must no longer preserve the HybridCLR hot-update assembly BDFramework.Core so the stripped-AOT temp project does not resolve a missing assembly.
"""

from __future__ import annotations

import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
HOST_LAUNCH_TESTS_PATH = (
    REPO_ROOT
    / "Packages"
    / "com.popo.bdframework"
    / "Runtime.HostE2E"
    / "LaunchFlowHostTests.cs"
)
STATIC_LINK_XML_PATH = REPO_ROOT / "Assets" / "link.xml"


def test_host_launch_suite_stays_in_host_package() -> None:
    """验证 step_01 launch 套件迁移到宿主侧独立程序集。
    Verify that the step_01 launch suite has moved into a host-owned dedicated assembly.
    """
    content = HOST_LAUNCH_TESTS_PATH.read_text(encoding="utf-8")

    assert "namespace BDFramework.HostE2E" in content
    assert "static class LaunchFlowHostTests" in content
    assert "BDFramework.Test.E2E" not in content
    assert "typeof(BDFramework.Test.E2E.LaunchTests).Assembly" not in content


def test_host_launch_suite_keeps_preserved_launch_entrypoints() -> None:
    """验证宿主 launch 套件显式保留 Android IL2CPP 入口。
    Verify that the host launch suite explicitly preserves the Android IL2CPP entrypoints.
    """
    content = HOST_LAUNCH_TESTS_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[Preserve\]\s*public static class LaunchFlowHostTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 2", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"launch\", order: 3", content)
    assert "FindLoadedAssembly" in content
    assert "GetAppDomainHostingTypes" in content


def test_static_link_xml_excludes_hot_update_bdframework_core() -> None:
    """验证静态 link.xml 不再保留 HybridCLR 热更程序集。
    Verify that the static link.xml no longer preserves the HybridCLR hot-update assembly.
    """
    content = STATIC_LINK_XML_PATH.read_text(encoding="utf-8")

    assert "BDFramework.Core" not in content
    assert '<assembly fullname="ServiceStack.Text" preserve="all"/>' in content
    assert "HybridCLR 热更程序集会在运行时单独装载" in content