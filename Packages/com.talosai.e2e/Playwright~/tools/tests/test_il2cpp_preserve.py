"""Talos IL2CPP 反射保活测试。

覆盖范围：
1. 反射入口 E2EAutoInit 在 IL2CPP 下必须显式带 Preserve。
2. 反射方法 CheckAndLaunch 也必须显式带 Preserve，避免只靠字符串查找时被裁剪。
"""

from __future__ import annotations

from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[5]
E2E_AUTO_INIT_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "E2EAutoInit.cs"


def test_e2e_auto_init_preserves_reflection_entrypoints() -> None:
    """验证 IL2CPP 下通过字符串反射调用的 Talos E2E 入口已经显式保活。"""
    content = E2E_AUTO_INIT_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[Preserve\]\s*static public class E2EAutoInit", content)
    assert re.search(r"\[Preserve\]\s*static public void CheckAndLaunch", content)