"""Tests for the ClientRes file-server verification wrappers and shared verify flow."""

from __future__ import annotations

import importlib
from pathlib import Path
import sys
from types import SimpleNamespace

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common import client_resource_flow as resource_flow  # noqa: E402


@pytest.mark.parametrize(
    ("module_name", "expected_payload"),
    (
        pytest.param(
            "VerifyClientRes.verify_android",
            {
                "platform_key": "android",
                "log_prefix": "[VerifyClientRes][Android]",
                "description": "Verify Android ClientRes file-server download via Unity batchmode.",
                "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResAndroid",
            },
            id="android",
        ),
        pytest.param(
            "VerifyClientRes.verify_ios",
            {
                "platform_key": "ios",
                "log_prefix": "[VerifyClientRes][iOS]",
                "description": "Verify iOS ClientRes file-server download via Unity batchmode.",
                "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResIOS",
            },
            id="ios",
        ),
        pytest.param(
            "VerifyClientRes.verify_windows",
            {
                "platform_key": "windows",
                "log_prefix": "[VerifyClientRes][Windows]",
                "description": "Verify Windows ClientRes file-server download via Unity batchmode.",
                "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResWindows",
            },
            id="windows",
        ),
    ),
)
def test_verify_wrappers_delegate_to_common_flow(
    module_name: str,
    expected_payload: dict[str, object],
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Verify each platform wrapper forwards its metadata to the shared verification flow."""
    module = importlib.import_module(module_name)
    captured: dict[str, object] = {}

    def fake_runner(**kwargs):
        captured.update(kwargs)
        return 5

    monkeypatch.setattr(module, "run_platform_resource_verify", fake_runner)

    assert module.main() == 5
    assert captured == expected_payload


def test_run_platform_resource_verify_executes_expected_flow(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify platform verification builds the expected Unity command and resolves file-server settings."""
    events: list[str] = []
    args = SimpleNamespace(
        client_version=" 0.1 ",
        expected_code_version=" 101 ",
        expected_assetbundle_version=" 202 ",
        expected_table_version=" 303 ",
        server_url=None,
        config=None,
        build_name=" VerifyClientRes_android ",
        build_number=" 88 ",
        unity_version="2022.3.74f1",
        project_dir="/tmp/BDFramework.Core",
        dry_run=False,
    )
    base_project_dir = Path("/tmp/BDFramework.Core")
    project_dir = Path("/tmp/android/BDFramework.Core")
    unity_path = Path("/Applications/Unity/Hub/Editor/2022.3.74f1/Unity.app/Contents/MacOS/Unity")
    log_path = Path("/tmp/TCLog/VerifyClientRes_android/88/android_0.1.88.log")

    monkeypatch.setattr(resource_flow, "configure_live_console_output", lambda: events.append("configure_live_console_output"))
    monkeypatch.setattr(resource_flow, "parse_platform_verify_args", lambda _description: args)
    monkeypatch.setattr(resource_flow, "resolve_build_metadata", lambda build_name, build_number: ("VerifyClientRes_android", "88"))
    monkeypatch.setattr(resource_flow, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(resource_flow, "ensure_platform_allowed", lambda platform_key: events.append(f"ensure_platform_allowed:{platform_key}"))
    monkeypatch.setattr(
        resource_flow,
        "resolve_file_server_settings",
        lambda **kwargs: SimpleNamespace(
            base_url="http://192.168.0.240:20001/",
            config_path=Path("/tmp/buildtools.toml"),
        ),
    )
    monkeypatch.setattr(
        resource_flow,
        "resolve_unity_executable",
        lambda unity_version, *, allow_missing: (events.append("resolve_unity_executable") or (unity_path, "2022.3.74f1")),
    )
    monkeypatch.setattr(resource_flow, "resolve_project_dir", lambda project_dir_arg: base_project_dir)
    monkeypatch.setattr(
        resource_flow,
        "prepare_platform_ci_project_dir",
        lambda **kwargs: (events.append("prepare_platform_ci_project_dir") or project_dir),
    )
    monkeypatch.setattr(resource_flow, "get_log_path", lambda *args, **kwargs: log_path)

    def fake_build_batchmode_command(**kwargs):
        events.append("build_batchmode_command")
        assert kwargs["execute_method"] == "BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResAndroid"
        assert kwargs["client_version"] == "0.1.88"
        return [str(unity_path), "-batchmode", "-quit"]

    monkeypatch.setattr(resource_flow, "build_batchmode_command", fake_build_batchmode_command)

    def fake_run_batchmode(command, *, dry_run: bool):
        events.append("run_batchmode")
        assert command[-11:] == [
            "-buildTarget",
            "Android",
            "-fileServerUrl",
            "http://192.168.0.240:20001",
            "-expectedCodeVersion",
            "101",
            "-expectedAssetbundleVersion",
            "202",
            "-expectedTableVersion",
            "303",
            "-quit",
        ]
        assert dry_run is False
        return 0

    monkeypatch.setattr(resource_flow, "run_batchmode", fake_run_batchmode)
    monkeypatch.setattr(resource_flow, "read_log_tail", lambda _log_path: "tail")

    assert (
        resource_flow.run_platform_resource_verify(
            platform_key="android",
            log_prefix="[VerifyClientRes][Android]",
            description="verify",
            execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResAndroid",
        )
        == 0
    )

    output = capsys.readouterr().out
    assert "unityBuildTarget=Android" in output
    assert "fileServerUrl=http://192.168.0.240:20001" in output
    assert "expectedVersionInfo=101.202.303" in output
    assert "baseProjectDir=/tmp/BDFramework.Core" in output
    assert "projectDir=/tmp/android/BDFramework.Core" in output
    assert "verification finished successfully" in output
    assert events == [
        "configure_live_console_output",
        "ensure_platform_allowed:android",
        "resolve_unity_executable",
        "prepare_platform_ci_project_dir",
        "build_batchmode_command",
        "run_batchmode",
    ]


def test_run_platform_resource_verify_dry_run_uses_override_server_url(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Verify dry-run verification forwards the explicit server-url override into config resolution."""
    args = SimpleNamespace(
        client_version="0.1",
        expected_code_version="101",
        expected_assetbundle_version="202",
        expected_table_version="303",
        server_url="http://127.0.0.1:20001",
        config="/tmp/buildtools.toml",
        build_name=None,
        build_number=None,
        unity_version=None,
        project_dir="/tmp/BDFramework.Core",
        dry_run=True,
    )
    monkeypatch.setattr(resource_flow, "configure_live_console_output", lambda: None)
    monkeypatch.setattr(resource_flow, "parse_platform_verify_args", lambda _description: args)
    monkeypatch.setattr(resource_flow, "resolve_build_metadata", lambda build_name, build_number: (None, None))
    monkeypatch.setattr(resource_flow, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(resource_flow, "ensure_platform_allowed", lambda platform_key: None)

    captured_settings: dict[str, object] = {}

    def fake_resolve_file_server_settings(**kwargs):
        captured_settings.update(kwargs)
        return SimpleNamespace(base_url="http://127.0.0.1:20001", config_path=Path("/tmp/buildtools.toml"))

    monkeypatch.setattr(resource_flow, "resolve_file_server_settings", fake_resolve_file_server_settings)
    monkeypatch.setattr(
        resource_flow,
        "resolve_unity_executable",
        lambda unity_version, *, allow_missing: (Path("/Applications/Unity"), "2021.3.58f1"),
    )
    monkeypatch.setattr(resource_flow, "resolve_project_dir", lambda project_dir_arg: Path("/tmp/BDFramework.Core"))
    monkeypatch.setattr(
        resource_flow,
        "prepare_platform_ci_project_dir",
        lambda **kwargs: Path("/tmp/windows/BDFramework.Core"),
    )
    monkeypatch.setattr(resource_flow, "get_log_path", lambda *args, **kwargs: Path("/tmp/log.log"))
    monkeypatch.setattr(resource_flow, "build_batchmode_command", lambda **kwargs: ["Unity", "-quit"])

    def fake_run_batchmode(command, *, dry_run):
        assert dry_run is True
        assert command[-11:] == [
            "-buildTarget",
            "Win64",
            "-fileServerUrl",
            "http://127.0.0.1:20001",
            "-expectedCodeVersion",
            "101",
            "-expectedAssetbundleVersion",
            "202",
            "-expectedTableVersion",
            "303",
            "-quit",
        ]
        return 0

    monkeypatch.setattr(resource_flow, "run_batchmode", fake_run_batchmode)
    monkeypatch.setattr(resource_flow, "read_log_tail", lambda _log_path: "tail")

    assert (
        resource_flow.run_platform_resource_verify(
            platform_key="windows",
            log_prefix="[VerifyClientRes][Windows]",
            description="verify",
            execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResWindows",
        )
        == 0
    )

    assert captured_settings == {
        "server_url": "http://127.0.0.1:20001",
        "config_path": "/tmp/buildtools.toml",
    }