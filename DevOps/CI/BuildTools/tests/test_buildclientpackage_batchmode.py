from __future__ import annotations

from pathlib import Path
import sys

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
BUILD_CLIENT_PACKAGE_ROOT = BUILD_TOOLS_ROOT / "BuildClientPackage"
if str(BUILD_CLIENT_PACKAGE_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_CLIENT_PACKAGE_ROOT))

import _unity_batchmode_logs as batchmode_logs
import _unity_batchmode_paths as batchmode_paths
import _unity_batchmode_shared as batchmode_shared


def test_normalize_named_paths_formats_versioned_entries_in_order() -> None:
    candidates = batchmode_paths.normalize_named_paths(
        host_os="mac",
        group_name="versioned",
        raw_group={
            "hub": "/Applications/Unity{version}/Unity.app/Contents/MacOS/Unity",
            "archive": "/Volumes/Editors/{version}/Unity",
        },
        unity_version="2022.3.74f1",
    )

    assert candidates == [
        "/Applications/Unity2022.3.74f1/Unity.app/Contents/MacOS/Unity",
        "/Volumes/Editors/2022.3.74f1/Unity",
    ]


def test_resolve_project_dir_rejects_missing_markers(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    project_dir = tmp_path / "BDFramework.Core"
    project_dir.mkdir()
    monkeypatch.setattr(
        batchmode_paths,
        "get_project_settings",
        lambda: {"markers": ["Assets", "Packages", "ProjectSettings"]},
    )

    with pytest.raises(batchmode_paths.UnityBatchModeError, match="Missing markers"):
        batchmode_paths.resolve_project_dir(str(project_dir))


def test_resolve_unity_executable_prefers_existing_env_path(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    unity_path = tmp_path / "Unity"
    unity_path.write_text("unity binary", encoding="utf-8")
    monkeypatch.setenv("UNITY_PATH", str(unity_path))

    resolved_path, resolved_version = batchmode_paths.resolve_unity_executable("2022.3.74f1")

    assert resolved_path == unity_path
    assert resolved_version == "2022.3.74f1"


def test_resolve_unity_executable_falls_back_after_invalid_env_path(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    fallback_path = tmp_path / "Unity"
    fallback_path.write_text("unity binary", encoding="utf-8")
    messages: list[str] = []
    captured_args: dict[str, str | None] = {}

    monkeypatch.setenv("UNITY_PATH", str(tmp_path / "missing-unity"))
    monkeypatch.setattr(batchmode_paths, "safe_console_print", messages.append)
    monkeypatch.setattr(batchmode_paths, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(
        batchmode_paths,
        "get_unity_settings",
        lambda: {"supported_versions": ["2022.3.74f1"]},
    )

    def fake_build_unity_candidates(
        host_os: str,
        unity_version: str,
        invalid_env_unity_path: str | None = None,
    ) -> list[str]:
        captured_args["host_os"] = host_os
        captured_args["unity_version"] = unity_version
        captured_args["invalid_env_unity_path"] = invalid_env_unity_path
        return [str(tmp_path / "missing-candidate"), str(fallback_path)]

    monkeypatch.setattr(batchmode_paths, "build_unity_candidates", fake_build_unity_candidates)

    resolved_path, resolved_version = batchmode_paths.resolve_unity_executable("2022.3.74f1")

    assert resolved_path == fallback_path
    assert resolved_version == "2022.3.74f1"
    assert captured_args == {
        "host_os": "mac",
        "unity_version": "2022.3.74f1",
        "invalid_env_unity_path": str(tmp_path / "missing-unity"),
    }
    assert any("continue searching fallback candidates" in message for message in messages)


def test_resolve_unity_executable_allow_missing_returns_first_candidate(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    missing_candidate = tmp_path / "Unity"
    messages: list[str] = []

    monkeypatch.delenv("UNITY_PATH", raising=False)
    monkeypatch.setattr(batchmode_paths, "safe_console_print", messages.append)
    monkeypatch.setattr(batchmode_paths, "detect_host_os", lambda: "mac")
    monkeypatch.setattr(
        batchmode_paths,
        "get_unity_settings",
        lambda: {"supported_versions": ["2022.3.74f1"]},
    )
    monkeypatch.setattr(
        batchmode_paths,
        "build_unity_candidates",
        lambda host_os, unity_version, invalid_env_unity_path=None: [str(missing_candidate)],
    )

    resolved_path, resolved_version = batchmode_paths.resolve_unity_executable(
        "2022.3.74f1",
        allow_missing=True,
    )

    assert resolved_path == missing_candidate
    assert resolved_version == "2022.3.74f1"
    assert any("dry-run allows continuing" in message for message in messages)


def test_get_log_path_uses_ci_root_when_build_metadata_present(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    project_dir = tmp_path / "workspace"
    project_dir.mkdir()
    monkeypatch.setenv("CI_LOG_ROOT_NAME", "CI logs")
    monkeypatch.setattr(batchmode_shared, "get_disk_root", lambda _: tmp_path)

    log_path = batchmode_shared.get_log_path(
        "android",
        "0.1.238",
        project_dir=project_dir,
        build_name="Nightly Build",
        build_number="238",
    )

    assert log_path == (
        tmp_path / "CI_logs" / "Nightly_Build" / "238" / "android_0.1.238.log"
    )


def test_emit_unity_log_updates_buffers_partial_lines(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    log_path = tmp_path / "unity.log"
    messages: list[str] = []
    state = batchmode_logs.UnityLogStreamingState()
    monkeypatch.setattr(batchmode_logs, "safe_console_print", messages.append)

    log_path.write_text("line1", encoding="utf-8")
    batchmode_logs.emit_unity_log_updates(log_path, state)
    assert messages == []
    assert state.partial_line == "line1"

    log_path.write_text("line1\nline2\nline3", encoding="utf-8")
    batchmode_logs.emit_unity_log_updates(log_path, state)
    assert messages == ["line1", "line2"]
    assert state.partial_line == "line3"

    batchmode_logs.emit_unity_log_updates(log_path, state, flush_partial=True)
    assert messages == ["line1", "line2", "line3"]
    assert state.partial_line == ""


def test_run_batchmode_dry_run_prints_command_and_skips_execution(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    messages: list[str] = []
    command = [
        "/Applications/Unity Hub/Editor/2022.3.74f1/Unity",
        "-batchmode",
        "-projectPath",
        "/tmp/BDFramework.Core",
    ]

    monkeypatch.setattr(batchmode_logs, "safe_console_print", messages.append)
    monkeypatch.setattr(
        batchmode_logs.subprocess,
        "run",
        lambda *args, **kwargs: pytest.fail("subprocess.run should not be called in dry-run"),
    )
    monkeypatch.setattr(
        batchmode_logs.subprocess,
        "Popen",
        lambda *args, **kwargs: pytest.fail("subprocess.Popen should not be called in dry-run"),
    )

    assert batchmode_logs.run_batchmode(command, dry_run=True) == 0
    assert messages[0] == "[UnityBatchMode] command="
    assert messages[1].startswith('"/Applications/Unity Hub/Editor/2022.3.74f1/Unity"')
    assert messages[-1] == "[UnityBatchMode] dry-run enabled, skip Unity execution."


def test_run_batchmode_terminates_stuck_process_after_success_marker(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    messages: list[str] = []
    log_path = tmp_path / "unity.log"
    command = ["/Applications/Unity", "-logFile", str(log_path)]

    class FakeProcess:
        def __init__(self) -> None:
            self.returncode: int | None = None
            self.terminated = False
            self.killed = False

        def poll(self) -> int | None:
            return self.returncode

        def terminate(self) -> None:
            self.terminated = True
            self.returncode = 0

        def wait(self, timeout: float | None = None) -> int:
            return self.returncode or 0

        def kill(self) -> None:
            self.killed = True
            self.returncode = -9

    process = FakeProcess()

    def fake_emit(log_path_value, state, *, flush_partial: bool = False) -> None:
        assert log_path_value == log_path
        if not flush_partial:
            state.saw_completion_marker = True
            state.completed_successfully = True
            state.last_activity_at = 0.0

    monkeypatch.setattr(batchmode_logs, "safe_console_print", messages.append)
    monkeypatch.setattr(batchmode_logs, "cleanup_stale_hybridclr_outputs", lambda _: ())
    monkeypatch.setattr(batchmode_logs, "extract_project_dir_from_command", lambda _: None)
    monkeypatch.setattr(batchmode_logs, "extract_log_path_from_command", lambda _: log_path)
    monkeypatch.setattr(batchmode_logs, "emit_unity_log_updates", fake_emit)
    monkeypatch.setattr(batchmode_logs.subprocess, "Popen", lambda _: process)
    monkeypatch.setattr(batchmode_logs.time, "monotonic", lambda: 31.0)
    monkeypatch.setattr(batchmode_logs.time, "sleep", lambda _: None)

    assert batchmode_logs.run_batchmode(command) == 0
    assert process.terminated is True
    assert process.killed is False
    assert any("completion marker detected but Unity process is still running" in message for message in messages)


def test_run_batchmode_terminates_stuck_process_after_failure_marker(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    log_path = tmp_path / "unity.log"
    command = ["/Applications/Unity", "-logFile", str(log_path)]

    class FakeProcess:
        def __init__(self) -> None:
            self.returncode: int | None = None

        def poll(self) -> int | None:
            return self.returncode

        def terminate(self) -> None:
            self.returncode = 0

        def wait(self, timeout: float | None = None) -> int:
            return self.returncode or 0

        def kill(self) -> None:
            self.returncode = -9

    process = FakeProcess()

    def fake_emit(log_path_value, state, *, flush_partial: bool = False) -> None:
        assert log_path_value == log_path
        if not flush_partial:
            state.saw_completion_marker = True
            state.completed_successfully = False
            state.last_activity_at = 0.0

    monkeypatch.setattr(batchmode_logs, "cleanup_stale_hybridclr_outputs", lambda _: ())
    monkeypatch.setattr(batchmode_logs, "extract_project_dir_from_command", lambda _: None)
    monkeypatch.setattr(batchmode_logs, "extract_log_path_from_command", lambda _: log_path)
    monkeypatch.setattr(batchmode_logs, "emit_unity_log_updates", fake_emit)
    monkeypatch.setattr(batchmode_logs.subprocess, "Popen", lambda _: process)
    monkeypatch.setattr(batchmode_logs.time, "monotonic", lambda: 31.0)
    monkeypatch.setattr(batchmode_logs.time, "sleep", lambda _: None)

    assert batchmode_logs.run_batchmode(command) == 1