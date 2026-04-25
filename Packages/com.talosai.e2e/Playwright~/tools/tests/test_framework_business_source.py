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
