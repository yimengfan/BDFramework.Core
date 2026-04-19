"""Talos 宿主 BaseFlow 套件源码回归测试。
Talos host BaseFlow-suite source regression tests.

覆盖范围：
Coverage:
1. step_01 的 asset-load 与 framework-integration 套件必须留在宿主包内，而不能继续依赖未被母包装载的 Runtime.Test 程序集。
1. The step_01 asset-load and framework-integration suites must remain in the host package instead of depending on the Runtime.Test assembly that is not loaded by the base package.
2. 宿主 BaseFlow 套件必须显式带 Preserve 和对应 suite 标记，避免 Windows/Android Player 再次只发现 launch。
2. The host BaseFlow suites must explicitly carry Preserve and the corresponding suite markers so Windows and Android players do not regress to discovering only launch.
3. 宿主 BaseFlow 套件必须通过反射访问热更资源与 SQLite 公开入口，而不是静态根引用 Runtime.Test。
3. The host BaseFlow suites must access the hotfix resource and SQLite public entrypoints through reflection instead of statically rooting Runtime.Test.
"""

from __future__ import annotations

import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
HOST_BASEFLOW_TESTS_PATH = (
    REPO_ROOT
    / "Packages"
    / "com.popo.bdframework"
    / "Runtime.HostE2E"
    / "BaseFlowHostRuntimeTests.cs"
)


def test_host_baseflow_suites_stay_in_host_package() -> None:
    """验证 step_01 的新增基础系统套件保留在宿主侧独立程序集。
    Verify that the added foundational step_01 suites remain in a host-owned dedicated assembly.
    """
    content = HOST_BASEFLOW_TESTS_PATH.read_text(encoding="utf-8")

    assert "namespace BDFramework.HostE2E" in content
    assert "public static class BaseFlowHostRuntimeTests" in content
    assert "typeof(BDFramework.Test" not in content
    assert '"BDFramework.ResourceMgr.BResources"' in content
    assert '"SQLite4Unity3d.SQLiteConnection"' in content


def test_host_baseflow_suites_keep_preserved_entrypoints() -> None:
    """验证宿主 BaseFlow 套件显式保留 Player IL2CPP 入口。
    Verify that the host BaseFlow suites explicitly preserve the player IL2CPP entrypoints.
    """
    content = HOST_BASEFLOW_TESTS_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[Preserve\]\s*public static class BaseFlowHostRuntimeTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"framework-integration\", order: 1", content)
    assert "RequireLoadedAssembly" in content
    assert "RequireLoadedType" in content
    assert "RequireInstanceGenericMethod" in content
    assert '"BDFramework.Core.Tools.BApplication"' in content
    assert '"SQLite4Unity3d.SQLiteConnectionString"' in content
    assert '"SQLite4Unity3d.SQLiteOpenFlags"' in content
    assert "typeof(Action<>).MakeGenericType(sqliteConnectionType)" in content
    assert "Type.Missing" in content
    assert 'GetConstructor(new[] { typeof(string), sqliteOpenFlagsType, typeof(bool) })' not in content
    assert "ResolveSqliteProbeRoot" in content
    assert "windows-systemprofile-temp-fallback" in content
    assert "Path.GetTempPath()" in content
    assert "sqliteOpenPath" in content
    assert "fileApiPath" in content
    assert "ReadRequiredStaticStringProperty" in content
    assert "CombinePath" in content
    assert "Application.persistentDataPath" in content
