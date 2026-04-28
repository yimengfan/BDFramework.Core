"""Talos FrameworkBusiness Playwright 源码回归测试。
Talos FrameworkBusiness Playwright source regression tests.

覆盖范围：
Coverage:
1. FrameworkBusiness 的 WindowPreconfig 套件会在启动竞态下按短周期重试，而不是把首次探测结果直接判成永久失败。
1. The FrameworkBusiness WindowPreconfig suite retries through startup races instead of treating the first probe as a permanent failure.
2. 重试只针对已知的页面就绪信号缺失，不会吞掉真正的业务回归。
2. Retries stay limited to known screen-readiness gaps and do not swallow real business regressions.
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
FRAMEWORK_BUSINESS_SPEC_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "testFrameworkBusiness-e2e.spec.ts"
FRAMEWORK_BUSINESS_BUILDTYPE_PATH = (
    REPO_ROOT / ".test-DevOps" / ".teamcity" / "buildTypes" / "TalosAIStep02FrameworkBusinessTest.kt"
)
BUILD_AND_RUN_BUILDTYPE_PATH = (
    REPO_ROOT / ".test-DevOps" / ".teamcity" / "buildTypes" / "TalosAIBuildAndRunE2ETest.kt"
)


def test_framework_business_window_preconfig_suite_retries_startup_race() -> None:
    """验证 FrameworkBusiness 会对 WindowPreconfig 启动竞态做短时重试。
    Verify that FrameworkBusiness retries through short-lived WindowPreconfig startup races.
    """
    content = FRAMEWORK_BUSINESS_SPEC_PATH.read_text(encoding="utf-8")

    assert "runWindowPreconfigSuiteUntilReady" in content
    assert "windowPreconfigReadyRetryCount = 12" in content
    assert "windowPreconfigReadyRetryDelayMs = 1000" in content
    assert "未发现 WindowPreconfig 实例" in content
    assert "未发现 GameConfigManager.Inst 属性" in content
    assert "GameConfigManager.Inst 为空" in content
    assert "未发现 ServerConfigProcessor 类型" in content
    assert "WindowPreconfig 套件待就绪" in content
    assert "runWindowPreconfigSuiteUntilReady(connector)" in content


def test_framework_business_buildtype_relies_on_runner_platform_defaults() -> None:
    """验证 step_02 TeamCity buildType 不再把平台专属默认值硬编码给所有平台。
    Verify that the step_02 TeamCity buildType no longer hardcodes platform-specific defaults for every platform.

    已移除的参数（默认值由 runner PlatformProfile 和 buildtools.toml 兜底）：
    Removed parameters (defaults fall back to runner PlatformProfile and buildtools.toml):
    - talos.e2e.package.build.type.id / --package-build-type-id
    - talos.e2e.unity.host / --unity-host
    - talos.e2e.unity.port / --unity-port
    - talos.e2e.package.extra.args / --package-build-extra-args
    - talos.e2e.adb.serial / --adb-serial
    - talos.e2e.adb.connect.targets / --adb-connect-targets
    - talos.e2e.emulator.type / --emulator-type
    - talos.e2e.mumu.auto.start / --start-mumu
    - talos.e2e.mumu.exe.path / --mumu-exe-path
    """
    content = FRAMEWORK_BUSINESS_BUILDTYPE_PATH.read_text(encoding="utf-8")

    # 第一轮移除：package.build.type.id, unity.host, unity.port
    # Round 1 removals: package.build.type.id, unity.host, unity.port
    assert 'param("talos.e2e.package.build.type.id"' not in content
    assert 'param("talos.e2e.unity.host"' not in content
    assert 'param("talos.e2e.unity.port"' not in content
    assert '--package-build-type-id' not in content
    assert '--unity-host' not in content
    assert '--unity-port' not in content

    # 第二轮移除：package.extra.args, adb.serial, adb.connect.targets, emulator.type, mumu.auto.start, mumu.exe.path
    # Round 2 removals: package.extra.args, adb.serial, adb.connect.targets, emulator.type, mumu.auto.start, mumu.exe.path
    assert 'param("talos.e2e.package.extra.args"' not in content
    assert 'param("talos.e2e.adb.serial"' not in content
    assert 'param("talos.e2e.adb.connect.targets"' not in content
    assert 'param("talos.e2e.emulator.type"' not in content
    assert 'param("talos.e2e.mumu.auto.start"' not in content
    assert 'param("talos.e2e.mumu.exe.path"' not in content
    assert '--package-build-extra-args' not in content
    assert '--adb-serial' not in content
    assert '--adb-connect-targets' not in content
    assert '--emulator-type' not in content
    assert '--start-mumu' not in content
    assert '--mumu-exe-path' not in content

    # 仍然保留的 DSL 传递参数
    # DSL parameters that remain (not delegated to runner defaults)
    assert '--package-build-number "%talos.e2e.package.build.number%"' in content
    assert '--test-file "%talos.e2e.test.file%"' in content



def test_build_and_run_buildtype_relies_on_runner_platform_defaults() -> None:
    """验证 BuildAndRun TeamCity buildType 不再把平台专属默认值硬编码给所有平台。
    Verify that the BuildAndRun TeamCity buildType no longer hardcodes platform-specific defaults for every platform.
    """
    content = BUILD_AND_RUN_BUILDTYPE_PATH.read_text(encoding="utf-8")

    # 所有已移除的 param 声明
    # All removed param declarations
    assert 'param("talos.e2e.package.build.type.id"' not in content
    assert 'param("talos.e2e.unity.host"' not in content
    assert 'param("talos.e2e.unity.port"' not in content
    assert 'param("talos.e2e.package.extra.args"' not in content
    assert 'param("talos.e2e.adb.serial"' not in content
    assert 'param("talos.e2e.adb.connect.targets"' not in content
    assert 'param("talos.e2e.emulator.type"' not in content
    assert 'param("talos.e2e.mumu.auto.start"' not in content
    assert 'param("talos.e2e.mumu.exe.path"' not in content

    # 所有已移除的 CLI 参数
    # All removed CLI args
    assert '--package-build-type-id' not in content
    assert '--unity-host' not in content
    assert '--unity-port' not in content
    assert '--package-build-extra-args' not in content
    assert '--adb-serial' not in content
    assert '--adb-connect-targets' not in content
    assert '--emulator-type' not in content
    assert '--start-mumu' not in content
    assert '--mumu-exe-path' not in content

    # 仍然保留的 DSL 传递参数
    # DSL parameters that remain
    assert '--package-build-number "%talos.e2e.package.build.number%"' in content
    assert '--test-file "%talos.e2e.test.file%"' in content
