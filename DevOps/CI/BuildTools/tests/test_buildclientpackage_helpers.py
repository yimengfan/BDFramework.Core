"""BuildClientPackage 产物准备与上传辅助函数测试。

BuildClientPackage artifact preparation and upload helper tests.

测试覆盖范围：
1. CI 日志根名称：验证 TeamCity 构建默认和显式覆盖两种模式。
2. 发布目录清理：验证清理后重新创建空的上传根目录。
3. HybridCLR 残留清理：验证遗留生成文件和 meta 文件被一起删除。
4. 发布摘要构建：验证使用 CI 构建号作为远端标签。
5. iOS 产物打包：验证 iOS 发布产物被重新打包为 Xcode 项目 ZIP。
6. Windows 产物拆分：验证运行时负载和禁止发布负载被拆分为独立 ZIP。
7. 发布上传：验证回退到 clientVersion、透传文件服务器覆盖地址并发出进度日志。
Coverage includes:
1. CI log-root naming for TeamCity defaults and explicit overrides.
2. Publish-directory cleanup recreating an empty upload root.
3. HybridCLR legacy-output cleanup removing generated files and meta files together.
4. Publish-summary construction preferring CI build numbers as remote labels.
5. iOS packaging repacking the Xcode project into a ZIP archive.
6. Windows packaging splitting runtime payloads and do-not-publish payloads.
7. Publish uploads falling back to clientVersion, forwarding file-server overrides, and emitting progress logs.
"""

from __future__ import annotations

from pathlib import Path
import sys
from types import SimpleNamespace
import zipfile


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
BUILD_CLIENT_PACKAGE_ROOT = BUILD_TOOLS_ROOT / "BuildClientPackage"
if str(BUILD_CLIENT_PACKAGE_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_CLIENT_PACKAGE_ROOT))

from Common.artifact_uploader import UploadedArtifact
from package_artifacts import (
    build_publish_package_summary,
    clear_publish_package_dir,
    list_publish_package_files,
    prepare_publish_package_upload_source,
    upload_publish_package,
)
from unity3d_batchmode import cleanup_stale_hybridclr_outputs, get_ci_log_root_name


APP_DIR_NAME = "com.popo.bdframework.demo"


def create_publish_output(
    project_dir: Path,
    *,
    platform_key: str = "windows",
    include_do_not_publish: bool = False,
    additional_do_not_publish_dir_names: tuple[str, ...] = (),
) -> Path:
    """创建模拟的 Windows 发布输出目录树，用于辅助函数测试。

    参数：
        project_dir: 项目根目录。
        platform_key: 平台标识（默认 windows）。
        include_do_not_publish: 是否包含 "不要发布" 目录。
        additional_do_not_publish_dir_names: 额外的禁止发布目录名列表。
    """
    output_dir = project_dir / "DevOps" / "PublishPackages" / platform_key
    app_dir = output_dir / APP_DIR_NAME
    (app_dir / "Game_Data").mkdir(parents=True)
    (app_dir / "BuildReport").mkdir(parents=True)
    (app_dir / "Launcher.exe").write_bytes(b"launcher payload")
    (app_dir / "Game_Data" / "globalgamemanagers").write_bytes(b"game data payload")
    (app_dir / "BuildReport" / "summary.json").write_text(
        '{"result": "success", "platform": "windows"}',
        encoding="utf-8",
    )

    if include_do_not_publish:
        (app_dir / "不要发布").mkdir(parents=True)
        (app_dir / "不要发布" / "notes.txt").write_text(
            "debug-only payload",
            encoding="utf-8",
        )

    for dir_name in additional_do_not_publish_dir_names:
        (app_dir / dir_name).mkdir(parents=True)
        (app_dir / dir_name / "notes.txt").write_text(
            f"debug-only payload for {dir_name}",
            encoding="utf-8",
        )

    return output_dir


def create_ios_publish_output(project_dir: Path) -> Path:
    """创建模拟的 iOS 发布输出目录树（Xcode 工程和 IPA），用于辅助函数测试。"""
    output_dir = project_dir / "DevOps" / "PublishPackages" / "ios"
    xcode_dir = output_dir / APP_DIR_NAME
    (xcode_dir / "Classes").mkdir(parents=True)
    (xcode_dir / "Info.plist").write_text("<plist />", encoding="utf-8")
    (xcode_dir / "Classes" / "AppDelegate.mm").write_text(
        "// xcode payload",
        encoding="utf-8",
    )
    (output_dir / f"{APP_DIR_NAME}.ipa").write_bytes(b"ipa payload")
    return output_dir


def read_zip_entries(zip_path: Path) -> list[str]:
    """读取 ZIP 压缩包并返回排序后的条目列表，用于断言打包内容。"""
    with zipfile.ZipFile(zip_path) as archive:
        return sorted(archive.namelist())


def test_get_ci_log_root_name_uses_teamcity_default(monkeypatch) -> None:
    """验证 TeamCity 构建默认将日志根目录设为 TCLog。"""
    monkeypatch.delenv("CI_LOG_ROOT_NAME", raising=False)
    monkeypatch.setenv("TEAMCITY_VERSION", "2025.11")

    assert get_ci_log_root_name() == "TCLog"


def test_get_ci_log_root_name_prefers_explicit_value(monkeypatch) -> None:
    """验证显式设置的 CI 日志根名称会覆盖 TeamCity 默认值。"""
    monkeypatch.setenv("CI_LOG_ROOT_NAME", "CI logs")
    monkeypatch.setenv("TEAMCITY_VERSION", "2025.11")

    assert get_ci_log_root_name() == "CI_logs"


def test_clear_publish_package_dir_recreates_empty_directory(tmp_path: Path) -> None:
    """验证清理发布输出目录后会重新创建一个空的上传根目录。"""
    output_dir = create_publish_output(tmp_path)

    cleared_dir = clear_publish_package_dir("windows", project_dir=tmp_path)

    assert cleared_dir == output_dir
    assert cleared_dir.exists()
    assert cleared_dir.is_dir()
    assert list(cleared_dir.iterdir()) == []


def test_cleanup_stale_hybridclr_outputs_removes_legacy_generated_files(tmp_path: Path) -> None:
    """验证过期的 HybridCLR 生成文件（AOTGenericReferences.cs、link.xml）及其 meta 文件被一起删除。"""
    legacy_dir = tmp_path / "Assets" / "HybridCLRGenerate"
    legacy_dir.mkdir(parents=True)
    legacy_aot_file = legacy_dir / "AOTGenericReferences.cs"
    legacy_link_file = legacy_dir / "link.xml"
    legacy_aot_meta = Path(f"{legacy_aot_file}.meta")
    legacy_link_meta = Path(f"{legacy_link_file}.meta")
    legacy_aot_file.write_text("public class AOTGenericReferences {}", encoding="utf-8")
    legacy_link_file.write_text("<linker />", encoding="utf-8")
    legacy_aot_meta.write_text("meta", encoding="utf-8")
    legacy_link_meta.write_text("meta", encoding="utf-8")

    removed_paths = cleanup_stale_hybridclr_outputs(tmp_path)

    assert removed_paths == (
        legacy_aot_file,
        legacy_aot_meta,
        legacy_link_file,
        legacy_link_meta,
    )
    assert not legacy_aot_file.exists()
    assert not legacy_aot_meta.exists()
    assert not legacy_link_file.exists()
    assert not legacy_link_meta.exists()
    assert not legacy_dir.exists()


def test_build_publish_package_summary_prefers_build_number(tmp_path: Path) -> None:
    """验证发布摘要优先使用 CI 构建号作为远端标签。"""
    output_dir = create_publish_output(tmp_path)

    summary = build_publish_package_summary(
        "windows",
        project_dir=tmp_path,
        build_number="238",
        client_version="0.1.238",
    )

    assert summary.source_dir == output_dir
    assert summary.upload_source_path == output_dir
    assert summary.build_label == "238"
    assert summary.remote_root == "ClientPackage_windows/238"
    assert summary.file_count == 3
    assert summary.total_bytes == (
        len(b"launcher payload")
        + len(b"game data payload")
        + len('{"result": "success", "platform": "windows"}'.encode("utf-8"))
    )


def test_prepare_publish_package_upload_source_for_ios_uses_xcode_project_zip(
    tmp_path: Path,
) -> None:
    """验证 iOS 发布产物被重新打包为单个 Xcode 工程 ZIP 文件。"""
    output_dir = create_ios_publish_output(tmp_path)

    prepared_dir = prepare_publish_package_upload_source(
        "ios",
        source_dir=output_dir,
        staging_dir=tmp_path / "staging",
    )

    prepared_files = list_publish_package_files(prepared_dir)

    assert [file_path.name for file_path in prepared_files] == [f"{APP_DIR_NAME}.zip"]
    assert read_zip_entries(prepared_files[0]) == [
        f"{APP_DIR_NAME}/Classes/AppDelegate.mm",
        f"{APP_DIR_NAME}/Info.plist",
    ]


def test_prepare_publish_package_upload_source_for_windows_splits_runtime_and_do_not_publish(
    tmp_path: Path,
) -> None:
    """验证 Windows 发布产物被拆分为运行时负载和禁止发布负载两个独立 ZIP 文件。"""
    output_dir = create_publish_output(
        tmp_path,
        include_do_not_publish=True,
        additional_do_not_publish_dir_names=(
            f"{APP_DIR_NAME}_BurstDebugInformation_DoNotShip",
        ),
    )

    prepared_dir = prepare_publish_package_upload_source(
        "windows",
        source_dir=output_dir,
        staging_dir=tmp_path / "staging",
    )

    prepared_files = {file_path.name: file_path for file_path in list_publish_package_files(prepared_dir)}

    assert set(prepared_files) == {
        f"{APP_DIR_NAME}.zip",
        f"{APP_DIR_NAME}_不要发布.zip",
        f"{APP_DIR_NAME}_{APP_DIR_NAME}_BurstDebugInformation_DoNotShip.zip",
    }
    assert read_zip_entries(prepared_files[f"{APP_DIR_NAME}.zip"]) == [
        f"{APP_DIR_NAME}/BuildReport/summary.json",
        f"{APP_DIR_NAME}/Game_Data/globalgamemanagers",
        f"{APP_DIR_NAME}/Launcher.exe",
    ]
    assert read_zip_entries(prepared_files[f"{APP_DIR_NAME}_不要发布.zip"]) == [
        f"{APP_DIR_NAME}/不要发布/notes.txt",
    ]
    assert read_zip_entries(
        prepared_files[f"{APP_DIR_NAME}_{APP_DIR_NAME}_BurstDebugInformation_DoNotShip.zip"]
    ) == [
        f"{APP_DIR_NAME}/{APP_DIR_NAME}_BurstDebugInformation_DoNotShip/notes.txt",
    ]


def test_upload_publish_package_falls_back_to_client_version_and_logs_progress(
    tmp_path: Path,
    monkeypatch,
    capsys,
) -> None:
    """验证发布上传在缺少构建号时回退到 clientVersion，并为每个 ZIP 文件发出进度日志。

    Verify that uploads fall back to clientVersion, forward the file-server override URL, and emit per-file progress logs.
    """
    output_dir = create_publish_output(tmp_path, include_do_not_publish=True)
    fake_settings = SimpleNamespace(
        base_url="https://files.example.com/fileserver",
        config_path=Path("/tmp/buildtools.toml"),
    )
    captured_resolve_kwargs: dict[str, str | None] = {}

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
        prepared_dir = Path(source_path)
        assert platform == "windows"
        assert build_number == "0.1.238"
        assert settings is fake_settings
        assert prepared_dir != output_dir
        assert prepared_dir.is_dir()

        files = list_publish_package_files(prepared_dir)
        assert {file_path.name for file_path in files} == {
            f"{APP_DIR_NAME}.zip",
            f"{APP_DIR_NAME}_不要发布.zip",
        }

        results = []
        total_files = len(files)
        for index, file_path in enumerate(files, start=1):
            remote_path = (
                f"ClientPackage_windows/0.1.238/"
                f"{file_path.relative_to(prepared_dir).as_posix()}"
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

    def fake_resolve_file_server_settings(*, server_url=None, **_):
        captured_resolve_kwargs["server_url"] = server_url
        return fake_settings

    monkeypatch.setattr(package_artifacts, "resolve_file_server_settings", fake_resolve_file_server_settings)
    monkeypatch.setattr(package_artifacts, "upload_client_package", fake_upload_client_package)

    results = upload_publish_package(
        "windows",
        project_dir=tmp_path,
        build_number=None,
        client_version="0.1.238",
        log_prefix="[TestUpload]",
        file_server_url="https://files.example.com/fileserver",
    )
    output = capsys.readouterr().out

    assert len(results) == 2
    assert captured_resolve_kwargs == {"server_url": "https://files.example.com/fileserver"}
    assert f"[TestUpload] uploadSourceDir={output_dir}" in output
    assert "[TestUpload] uploadPreparedSource=" in output
    assert "[TestUpload] uploadBuildLabel=0.1.238" in output
    assert "[TestUpload] uploadRemoteRoot=ClientPackage_windows/0.1.238" in output
    assert "[TestUpload] uploadServerUrlOverride=https://files.example.com/fileserver" in output
    assert "[TestUpload] uploadServerUrl=https://files.example.com/fileserver" in output
    assert "[TestUpload] uploadFileCount=2" in output
    assert "[TestUpload] uploadProgress=1/2 state=uploading" in output
    assert "[TestUpload] uploadProgress=2/2 state=uploaded" in output
    assert "[TestUpload] uploadedFiles=2" in output