from __future__ import annotations

from pathlib import Path
import sys
from types import SimpleNamespace


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
BUILD_CLIENT_PACKAGE_ROOT = BUILD_TOOLS_ROOT / "BuildClientPackage"
if str(BUILD_CLIENT_PACKAGE_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_CLIENT_PACKAGE_ROOT))

from Common.artifact_uploader import UploadedArtifact
from package_artifacts import (
    build_publish_package_summary,
    clear_publish_package_dir,
    list_publish_package_files,
    upload_publish_package,
)
from unity3d_batchmode import get_ci_log_root_name


def create_publish_output(project_dir: Path, *, platform_key: str = "windows") -> Path:
    output_dir = project_dir / "DevOps" / "PublishPackages" / platform_key
    (output_dir / "Game_Data").mkdir(parents=True)
    (output_dir / "BuildReport").mkdir(parents=True)
    (output_dir / "Launcher.exe").write_bytes(b"launcher payload")
    (output_dir / "Game_Data" / "globalgamemanagers").write_bytes(b"game data payload")
    (output_dir / "BuildReport" / "summary.json").write_text(
        '{"result": "success", "platform": "windows"}',
        encoding="utf-8",
    )
    return output_dir


def test_get_ci_log_root_name_uses_teamcity_default(monkeypatch) -> None:
    monkeypatch.delenv("CI_LOG_ROOT_NAME", raising=False)
    monkeypatch.setenv("TEAMCITY_VERSION", "2025.11")

    assert get_ci_log_root_name() == "TCLog"


def test_get_ci_log_root_name_prefers_explicit_value(monkeypatch) -> None:
    monkeypatch.setenv("CI_LOG_ROOT_NAME", "CI logs")
    monkeypatch.setenv("TEAMCITY_VERSION", "2025.11")

    assert get_ci_log_root_name() == "CI_logs"


def test_clear_publish_package_dir_recreates_empty_directory(tmp_path: Path) -> None:
    output_dir = create_publish_output(tmp_path)

    cleared_dir = clear_publish_package_dir("windows", project_dir=tmp_path)

    assert cleared_dir == output_dir
    assert cleared_dir.exists()
    assert cleared_dir.is_dir()
    assert list(cleared_dir.iterdir()) == []


def test_build_publish_package_summary_prefers_build_number(tmp_path: Path) -> None:
    output_dir = create_publish_output(tmp_path)

    summary = build_publish_package_summary(
        "windows",
        project_dir=tmp_path,
        build_number="238",
        client_version="0.1.238",
    )

    assert summary.source_dir == output_dir
    assert summary.build_label == "238"
    assert summary.remote_root == "ClientPackage_windows/238"
    assert summary.file_count == 3
    assert summary.total_bytes == (
        len(b"launcher payload")
        + len(b"game data payload")
        + len('{"result": "success", "platform": "windows"}'.encode("utf-8"))
    )


def test_upload_publish_package_falls_back_to_client_version_and_logs_progress(
    tmp_path: Path,
    monkeypatch,
    capsys,
) -> None:
    output_dir = create_publish_output(tmp_path)
    fake_settings = SimpleNamespace(
        base_url="http://127.0.0.1:20001",
        config_path=Path("/tmp/buildtools.toml"),
    )

    def fake_upload_client_package(
        source_path,
        *,
        platform,
        build_number,
        settings,
        on_uploading=None,
        on_uploaded=None,
        **_,
    ):
        assert Path(source_path) == output_dir
        assert platform == "windows"
        assert build_number == "0.1.238"
        assert settings is fake_settings

        results = []
        files = list_publish_package_files(output_dir)
        total_files = len(files)
        for index, file_path in enumerate(files, start=1):
            remote_path = (
                f"ClientPackage_windows/0.1.238/"
                f"{file_path.relative_to(output_dir).as_posix()}"
            )
            if on_uploading is not None:
                on_uploading(index, total_files, file_path, remote_path)

            uploaded = UploadedArtifact(
                local_path=file_path,
                remote_path=remote_path,
                size=file_path.stat().st_size,
                sha256=f"sha-{index}",
                status_code=201,
                integrity_status="verified",
            )
            results.append(uploaded)
            if on_uploaded is not None:
                on_uploaded(index, total_files, uploaded)

        return results

    import package_artifacts

    monkeypatch.setattr(package_artifacts, "resolve_file_server_settings", lambda: fake_settings)
    monkeypatch.setattr(package_artifacts, "upload_client_package", fake_upload_client_package)

    results = upload_publish_package(
        "windows",
        project_dir=tmp_path,
        build_number=None,
        client_version="0.1.238",
        log_prefix="[TestUpload]",
    )
    output = capsys.readouterr().out

    assert len(results) == 3
    assert "[TestUpload] uploadBuildLabel=0.1.238" in output
    assert "[TestUpload] uploadRemoteRoot=ClientPackage_windows/0.1.238" in output
    assert "[TestUpload] uploadProgress=1/3 state=uploading" in output
    assert "[TestUpload] uploadProgress=3/3 state=uploaded" in output
    assert "[TestUpload] uploadedFiles=3" in output