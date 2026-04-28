"""Talos Playwright fixture 端口回归测试。
Talos Playwright fixture port regression tests.

覆盖范围：
1. 公共 runtime fixture 在未显式提供 LOCAL_PORT 时，必须默认跟随 UNITY_PORT。
2. UnityPlayer fixture 也必须遵循同样的端口回退规则，避免 Android/EditorPlayer 测试再次连回 10002。

Coverage:
1. The shared runtime fixture must default LOCAL_PORT to UNITY_PORT when LOCAL_PORT is not provided explicitly.
2. The UnityPlayer fixture must follow the same fallback rule so Android and EditorPlayer flows do not regress to 10002.
"""

from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
FIXTURES_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "fixtures.ts"
UNITYPLAYER_FIXTURES_PATH = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tests" / "fixtures-unityplayer.ts"


def test_runtime_fixture_defaults_local_port_to_unity_port() -> None:
    """验证 runtime fixture 会让 LOCAL_PORT 默认跟随 UNITY_PORT。
    Verify that the runtime fixture makes LOCAL_PORT default to UNITY_PORT.
    """
    content = FIXTURES_PATH.read_text(encoding="utf-8")

    assert "const unityPortEnv = process.env.UNITY_PORT || '10002';" in content
    assert "const localPort = parseInt(process.env.LOCAL_PORT || unityPortEnv, 10);" in content


def test_unityplayer_fixture_defaults_local_port_to_unity_port() -> None:
    """验证 UnityPlayer fixture 会让 LOCAL_PORT 默认跟随 UNITY_PORT。
    Verify that the UnityPlayer fixture makes LOCAL_PORT default to UNITY_PORT.
    """
    content = UNITYPLAYER_FIXTURES_PATH.read_text(encoding="utf-8")

    assert "const unityPortEnv = process.env.UNITY_PORT || '10002';" in content
    assert "const localPort = parseInt(process.env.LOCAL_PORT || unityPortEnv, 10);" in content