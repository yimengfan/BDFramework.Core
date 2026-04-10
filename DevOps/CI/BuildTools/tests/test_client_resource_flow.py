"""Tests for the shared ClientRes batchmode flow wrappers and orchestration helpers."""

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


def test_build_client_res_code_wrappers_delegate_to_common_flow(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Verify code wrapper entry scripts forward their platform metadata to the shared flow."""
    module = importlib.import_module("BuildClientResCode.build_android")
    captured: dict[str, object] = {}

    def fake_runner(**kwargs):
        captured.update(kwargs)
        return 7

    monkeypatch.setattr(module, "run_platform_resource_build", fake_runner)

    assert module.main() == 7
    assert captured == {
        "platform_key": "android",
        "log_prefix": "[BuildCode][Android]",
        "description": "Build Android hotfix code via Unity batchmode.",
        "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid",
        "build_kind": "clientres_code",
        "artifact_kind": "code",
    }


def test_build_client_res_assetbundle_wrappers_delegate_to_common_flow(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Verify assetbundle wrapper entry scripts forward their platform metadata to the shared flow."""
    module = importlib.import_module("BuildClientResAssetbundle.build_ios")
    captured: dict[str, object] = {}

    def fake_runner(**kwargs):
        captured.update(kwargs)
        return 9

    monkeypatch.setattr(module, "run_platform_resource_build", fake_runner)

    assert module.main() == 9
    assert captured == {
        "platform_key": "ios",
        "log_prefix": "[BuildAssetbundle][iOS]",
        "description": "Build iOS hotfix assetbundle via Unity batchmode.",
        "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildAssetbundleIOS",
        "build_kind": "clientres_assetbundle",
        "artifact_kind": "assetbundle",
    }


def test_build_table_wrapper_delegates_to_common_flow(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Verify the table wrapper forwards its batchmode metadata to the shared table flow."""
    module = importlib.import_module("BuildClientResTable.build_table")
    captured: dict[str, object] = {}

    def fake_runner(**kwargs):
        captured.update(kwargs)
        return 11

    monkeypatch.setattr(module, "run_table_resource_build", fake_runner)

    assert module.main() == 11
    assert captured == {
        "log_prefix": "[BuildTable]",
        "description": "Build shared client.db / server.db via Unity batchmode.",
        "execute_method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildTable",
        "build_kind": "clientres_table",
    }


@pytest.mark.parametrize(
    ("artifact_kind", "upload_attr"),
    (
        pytest.param("code", "upload_client_res_code", id="code"),
        pytest.param("assetbundle", "upload_client_res_assetbundle", id="assetbundle"),
    ),
)
def test_run_platform_resource_build_executes_expected_flow(
    artifact_kind: str,
    upload_attr: str,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify platform resource builds execute the expected Unity and upload phases."""
    events: list[str] = []
    args = SimpleNamespace(
        client_version=" 0.1 ",
        build_name=" Nightly Build ",
        build_number=" 238 ",
        unity_version="2022.3.74f1",
        project_dir="/tmp/BDFramework.Core",
        dry_run=False,
    )
    project_dir = Path("/tmp/BDFramework.Core")
    unity_path = Path("/Applications/Unity/Hub/Editor/2022.3.74f1/Unity.app/Contents/MacOS/Unity")
    log_path = Path(f"/tmp/TCLog/Nightly_Build/238/{artifact_kind}_0.1.238.log")
    ci_output_root = Path(f"/tmp/BDFramework.Core/Library/CIOutputs/{artifact_kind}/Nightly_Build/238/android")

    monkeypatch.setattr(resource_flow, "configure_live_console_output", lambda: events.append("configure_live_console_output"))
    monkeypatch.setattr(resource_flow, "parse_platform_args", lambda _description: args)
    monkeypatch.setattr(resource_flow, "resolve_build_metadata", lambda build_name, build_number: ("Nightly Build", "238"))
    monkeypatch.setattr(resource_flow, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(resource_flow, "ensure_platform_allowed", lambda platform_key: events.append(f"ensure_platform_allowed:{platform_key}"))
    monkeypatch.setattr(
        resource_flow,
        "resolve_unity_executable",
        lambda unity_version, *, allow_missing: (events.append("resolve_unity_executable") or (unity_path, "2022.3.74f1")),
    )
    monkeypatch.setattr(resource_flow, "resolve_project_dir", lambda project_dir_arg: project_dir)
    monkeypatch.setattr(resource_flow, "get_log_path", lambda *args, **kwargs: log_path)
    monkeypatch.setattr(
        resource_flow,
        "prepare_clean_ci_output_root",
        lambda *args, **kwargs: ci_output_root,
    )

    def fake_build_batchmode_command(**kwargs):
        events.append("build_batchmode_command")
        assert kwargs["execute_method"].startswith("BDFramework.Editor.DevOps.PublishPipeLineCI")
        return [str(unity_path), "-batchmode", "-quit"]

    monkeypatch.setattr(resource_flow, "build_batchmode_command", fake_build_batchmode_command)

    def fake_run_batchmode(command, *, dry_run: bool):
        events.append("run_batchmode")
        assert command[-5:] == [
            "-buildTarget",
            "Android",
            "-ciOutputRoot",
            str(ci_output_root),
            "-quit",
        ]
        assert dry_run is False
        return 0

    monkeypatch.setattr(resource_flow, "run_batchmode", fake_run_batchmode)
    monkeypatch.setattr(resource_flow, "read_log_tail", lambda _log_path: "tail")

    def fake_upload(platform_key, *, output_root, build_number, fallback_build_label, log_prefix):
        events.append(upload_attr)
        assert platform_key == "android"
        assert output_root == ci_output_root
        assert build_number == "238"
        assert fallback_build_label == "0.1.238"
        assert log_prefix.startswith("[Build")
        return ["uploaded"]

    monkeypatch.setattr(resource_flow, upload_attr, fake_upload)

    assert (
        resource_flow.run_platform_resource_build(
            platform_key="android",
            log_prefix="[BuildCode][Android]",
            description="build",
            execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid",
            build_kind="clientres_code",
            artifact_kind=artifact_kind,
        )
        == 0
    )

    output = capsys.readouterr().out
    assert "ciOutputRoot=/tmp/BDFramework.Core/Library/CIOutputs" in output
    assert "unityBuildTarget=Android" in output
    assert "build finished successfully" in output
    assert events == [
        "configure_live_console_output",
        "ensure_platform_allowed:android",
        "resolve_unity_executable",
        "build_batchmode_command",
        "run_batchmode",
        upload_attr,
    ]


def test_run_platform_resource_build_dry_run_skips_upload(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify dry-run platform resource builds skip artifact upload after Unity succeeds."""
    args = SimpleNamespace(
        client_version="0.1",
        build_name=None,
        build_number=None,
        unity_version=None,
        project_dir="/tmp/BDFramework.Core",
        dry_run=True,
    )
    monkeypatch.setattr(resource_flow, "configure_live_console_output", lambda: None)
    monkeypatch.setattr(resource_flow, "parse_platform_args", lambda _description: args)
    monkeypatch.setattr(resource_flow, "resolve_build_metadata", lambda build_name, build_number: (None, None))
    monkeypatch.setattr(resource_flow, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(resource_flow, "ensure_platform_allowed", lambda platform_key: None)
    monkeypatch.setattr(
        resource_flow,
        "resolve_unity_executable",
        lambda unity_version, *, allow_missing: (Path("/Applications/Unity"), "2021.3.58f1"),
    )
    monkeypatch.setattr(resource_flow, "resolve_project_dir", lambda project_dir_arg: Path("/tmp/BDFramework.Core"))
    monkeypatch.setattr(resource_flow, "get_log_path", lambda *args, **kwargs: Path("/tmp/log.log"))
    monkeypatch.setattr(resource_flow, "prepare_clean_ci_output_root", lambda *args, **kwargs: Path("/tmp/output"))
    monkeypatch.setattr(resource_flow, "build_batchmode_command", lambda **kwargs: ["Unity", "-quit"])

    def fake_run_batchmode(command, *, dry_run):
        assert command[-5:] == ["-buildTarget", "Android", "-ciOutputRoot", "/tmp/output", "-quit"]
        return 0

    monkeypatch.setattr(resource_flow, "run_batchmode", fake_run_batchmode)

    uploaded = False

    def fake_upload(*args, **kwargs):
        nonlocal uploaded
        uploaded = True

    monkeypatch.setattr(resource_flow, "upload_client_res_code", fake_upload)

    assert (
        resource_flow.run_platform_resource_build(
            platform_key="android",
            log_prefix="[BuildCode][Android]",
            description="build",
            execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid",
            build_kind="clientres_code",
            artifact_kind="code",
        )
        == 0
    )

    output = capsys.readouterr().out
    assert "dry-run enabled, skip artifact upload" in output
    assert uploaded is False


def test_run_table_resource_build_uploads_shared_dbs(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify table resource builds upload the shared client and server DB outputs."""
    events: list[str] = []
    args = SimpleNamespace(
        client_version=None,
        build_name=" BuildTable ",
        build_number=" 77 ",
        unity_version="2022.3.74f1",
        project_dir="/tmp/BDFramework.Core",
        dry_run=False,
    )
    project_dir = Path("/tmp/BDFramework.Core")
    unity_path = Path("/Applications/Unity")
    log_path = Path("/tmp/TCLog/BuildTable/77/table_table.log")
    ci_output_root = Path("/tmp/BDFramework.Core/Library/CIOutputs/clientres_table/BuildTable/77")

    monkeypatch.setattr(resource_flow, "configure_live_console_output", lambda: events.append("configure_live_console_output"))
    monkeypatch.setattr(resource_flow, "parse_table_args", lambda _description: args)
    monkeypatch.setattr(resource_flow, "resolve_build_metadata", lambda build_name, build_number: ("BuildTable", "77"))
    monkeypatch.setattr(resource_flow, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(
        resource_flow,
        "resolve_unity_executable",
        lambda unity_version, *, allow_missing: (events.append("resolve_unity_executable") or (unity_path, "2022.3.74f1")),
    )
    monkeypatch.setattr(resource_flow, "resolve_project_dir", lambda project_dir_arg: project_dir)
    monkeypatch.setattr(resource_flow, "get_log_path", lambda *args, **kwargs: log_path)
    monkeypatch.setattr(resource_flow, "prepare_clean_ci_output_root", lambda *args, **kwargs: ci_output_root)
    monkeypatch.setattr(resource_flow, "build_batchmode_command", lambda **kwargs: ["Unity", "-quit"])

    def fake_run_batchmode(command, *, dry_run: bool):
        events.append("run_batchmode")
        assert command[-3:] == ["-ciOutputRoot", str(ci_output_root), "-quit"]
        return 0

    monkeypatch.setattr(resource_flow, "run_batchmode", fake_run_batchmode)
    monkeypatch.setattr(resource_flow, "read_log_tail", lambda _log_path: "tail")

    def fake_upload(local_platform_dir, *, output_root, build_number, fallback_build_label, log_prefix):
        events.append("upload_client_res_table")
        assert local_platform_dir == "osx"
        assert output_root == ci_output_root
        assert build_number == "77"
        assert fallback_build_label == "table"
        assert log_prefix == "[BuildTable]"
        return ["uploaded"]

    monkeypatch.setattr(resource_flow, "upload_client_res_table", fake_upload)

    assert (
        resource_flow.run_table_resource_build(
            log_prefix="[BuildTable]",
            description="build table",
            execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.BuildTable",
            build_kind="clientres_table",
        )
        == 0
    )

    output = capsys.readouterr().out
    assert "localDbPlatformDir=osx" in output
    assert "build finished successfully" in output
    assert events == [
        "configure_live_console_output",
        "resolve_unity_executable",
        "run_batchmode",
        "upload_client_res_table",
    ]