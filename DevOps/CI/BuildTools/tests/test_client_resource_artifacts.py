from __future__ import annotations

from pathlib import Path
import sys

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.client_resource_artifacts import (  # noqa: E402
    ART_ASSETS_DIRNAME,
    ART_ASSET_METADATA_FILENAMES,
    ASSETS_INFO_FILENAME,
    ASSETS_SUBPACK_INFO_FILENAME,
    CLIENT_DB_FILENAME,
    ClientResourceArtifactsError,
    LOCAL_DB_FILENAME,
    PACKAGE_BUILD_INFO_FILENAME,
    SCRIPT_DIRNAME,
    SERVER_DATA_DIRNAME,
    SERVER_DB_FILENAME,
    build_expected_remote_files,
    build_upload_summary,
    has_real_assetbundle_payload,
    list_source_files,
    parse_assetbundle_manifest_paths,
    prepare_assetbundle_upload_source,
    prepare_clean_ci_output_root,
    prepare_code_upload_source,
    prepare_table_upload_source,
    upload_client_res_assetbundle,
    validate_uploaded_artifacts,
)
from Common.artifact_uploader import FileServerClientSettings, UploadedArtifact  # noqa: E402


def test_prepare_clean_ci_output_root_recreates_existing_directory(tmp_path: Path) -> None:
    project_dir = tmp_path / "BDFramework.Core"
    output_root = project_dir / "Library" / "CIOutputs" / "clientres_code" / "Nightly" / "18" / "android"
    output_root.mkdir(parents=True)
    stale_file = output_root / "stale.txt"
    stale_file.write_text("old", encoding="utf-8")

    prepared = prepare_clean_ci_output_root(
        project_dir,
        build_kind="clientres_code",
        build_name="Nightly",
        build_number="18",
        platform="android",
    )

    assert prepared == output_root
    assert prepared.exists()
    assert not stale_file.exists()


def test_prepare_code_upload_source_keeps_script_and_required_infos(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / SCRIPT_DIRNAME / "hotfix").mkdir(parents=True)
    (platform_dir / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").write_bytes(b"dll")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    (platform_dir / ASSETS_INFO_FILENAME).write_text("assets", encoding="utf-8")
    (platform_dir / ASSETS_SUBPACK_INFO_FILENAME).write_text("subpack", encoding="utf-8")

    prepared = prepare_code_upload_source(
        "android",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert (prepared / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").read_bytes() == b"dll"
    assert (prepared / PACKAGE_BUILD_INFO_FILENAME).read_text(encoding="utf-8") == "pkg"
    assert (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8") == "assets"
    assert (prepared / ASSETS_SUBPACK_INFO_FILENAME).read_text(encoding="utf-8") == "subpack"


def test_prepare_code_upload_source_requires_package_build_info(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "ios"
    (platform_dir / SCRIPT_DIRNAME / "hotfix").mkdir(parents=True)
    (platform_dir / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").write_bytes(b"dll")
    (platform_dir / ASSETS_INFO_FILENAME).write_text("assets", encoding="utf-8")

    with pytest.raises(ClientResourceArtifactsError, match="package_build.info"):
        prepare_code_upload_source(
            "ios",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_assetbundle_upload_source_keeps_art_assets_and_infos(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "windows"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "catalog.bytes").write_bytes(b"catalog")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    (platform_dir / ASSETS_INFO_FILENAME).write_text(
        "1,100,art_assets/catalog.bytes,0.1\n",
        encoding="utf-8",
    )

    prepared = prepare_assetbundle_upload_source(
        "windows",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert (prepared / ART_ASSETS_DIRNAME / "catalog.bytes").read_bytes() == b"catalog"
    assert (prepared / PACKAGE_BUILD_INFO_FILENAME).read_text(encoding="utf-8") == "pkg"
    assert "art_assets/catalog.bytes" in (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8")
    assert not (prepared / ASSETS_SUBPACK_INFO_FILENAME).exists()


def test_prepare_assetbundle_upload_source_requires_declared_art_assets_files(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "ios"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "existing.bundle").write_bytes(b"bundle")
    (platform_dir / ART_ASSETS_DIRNAME / "buildlogtep.json").write_text("{}", encoding="utf-8")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    (platform_dir / ASSETS_INFO_FILENAME).write_text(
        "1,100,art_assets/existing.bundle,0.1\n2,101,art_assets/real.bundle,0.2\n3,102,art_assets/buildlogtep.json,0.3\n",
        encoding="utf-8",
    )

    with pytest.raises(ClientResourceArtifactsError, match="missing declared art_assets files"):
        prepare_assetbundle_upload_source(
            "ios",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_assetbundle_upload_source_requires_real_payload_file(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "buildlogtep.json").write_text("{}", encoding="utf-8")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    (platform_dir / ASSETS_INFO_FILENAME).write_text("header-only", encoding="utf-8")

    with pytest.raises(ClientResourceArtifactsError, match="does not contain any real art_assets payload files"):
        prepare_assetbundle_upload_source(
            "android",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_table_upload_source_renames_local_db_to_client_db(tmp_path: Path) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "osx"
    (output_root / SERVER_DATA_DIRNAME).mkdir(parents=True)
    platform_dir.mkdir(parents=True)
    (platform_dir / LOCAL_DB_FILENAME).write_bytes(b"local-db")
    (output_root / SERVER_DATA_DIRNAME / SERVER_DB_FILENAME).write_bytes(b"server-db")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")

    prepared = prepare_table_upload_source(
        "osx",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert (prepared / CLIENT_DB_FILENAME).read_bytes() == b"local-db"
    assert (prepared / SERVER_DB_FILENAME).read_bytes() == b"server-db"
    assert (prepared / PACKAGE_BUILD_INFO_FILENAME).read_text(encoding="utf-8") == "pkg"


def test_build_upload_summary_uses_new_remote_layout_names(tmp_path: Path) -> None:
    prepared_dir = tmp_path / "prepared"
    prepared_dir.mkdir()
    (prepared_dir / "client.db").write_bytes(b"client")
    summary = build_upload_summary(
        prepared_dir,
        artifact_type="table",
        build_label="501",
    )

    assert summary.remote_root == "ClientRes_Table/501"
    assert summary.file_count == 1
    assert summary.total_bytes == len(b"client")
    assert list_source_files(prepared_dir) == [prepared_dir / "client.db"]


def test_parse_assetbundle_manifest_paths_extracts_art_assets_only(tmp_path: Path) -> None:
    info_path = tmp_path / ASSETS_INFO_FILENAME
    info_path.write_text(
        "1,100,art_assets/real.bundle,0.1\n2,101,other/path.bin,0.2\n3,102,art_assets/buildlogtep.json,0.3\n",
        encoding="utf-8",
    )

    assert parse_assetbundle_manifest_paths(info_path) == {
        "art_assets/real.bundle",
        "art_assets/buildlogtep.json",
    }


def test_has_real_assetbundle_payload_ignores_metadata_only_entries() -> None:
    metadata_only = {f"art_assets/{name}" for name in ART_ASSET_METADATA_FILENAMES}
    assert has_real_assetbundle_payload(metadata_only) is False
    assert has_real_assetbundle_payload({*metadata_only, "art_assets/real.bundle"}) is True


def test_validate_uploaded_artifacts_checks_remote_listing_and_logs_success(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    prepared_dir = tmp_path / "prepared"
    (prepared_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    first_file = prepared_dir / ART_ASSETS_DIRNAME / "first.bundle"
    second_file = prepared_dir / PACKAGE_BUILD_INFO_FILENAME
    third_file = prepared_dir / ASSETS_INFO_FILENAME
    first_file.write_bytes(b"bundle")
    second_file.write_text("pkg", encoding="utf-8")
    third_file.write_text("1,100,art_assets/first.bundle,0.1\n", encoding="utf-8")

    summary = build_upload_summary(
        prepared_dir,
        artifact_type="asset-bundle",
        platform="android",
        build_label="42",
    )
    expected_files = build_expected_remote_files(prepared_dir, remote_root=summary.remote_root)
    results = [
        UploadedArtifact(
            local_path=local_path,
            remote_path=remote_path,
            size=local_path.stat().st_size,
            sha256=f"sha-{index}",
            status_code=201,
            integrity_status="verified",
        )
        for index, (local_path, remote_path) in enumerate(expected_files, start=1)
    ]

    def fake_fetch_remote_listing(*, prefix, settings, recursive, limit, timeout_seconds):
        assert prefix == summary.remote_root
        assert recursive is True
        assert limit >= summary.file_count
        return 200, {
            "count": len(expected_files),
            "entries": [
                {
                    "path": remote_path,
                    "type": "file",
                    "size": local_path.stat().st_size,
                }
                for local_path, remote_path in expected_files
            ],
        }, b""

    monkeypatch.setattr(
        "Common.client_resource_artifacts.fetch_remote_listing",
        fake_fetch_remote_listing,
    )

    validate_uploaded_artifacts(
        summary,
        results=results,
        settings=FileServerClientSettings(base_url="http://fileserver", token=None, config_path=None),
        log_prefix="[TestUpload]",
    )

    output = capsys.readouterr().out
    assert "[TestUpload] uploadVerifiedFiles=3" in output
    assert "[TestUpload] uploadVerifiedRemoteFiles=3" in output


def test_validate_uploaded_artifacts_raises_when_remote_listing_misses_file(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    prepared_dir = tmp_path / "prepared"
    (prepared_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    first_file = prepared_dir / ART_ASSETS_DIRNAME / "first.bundle"
    second_file = prepared_dir / PACKAGE_BUILD_INFO_FILENAME
    third_file = prepared_dir / ASSETS_INFO_FILENAME
    first_file.write_bytes(b"bundle")
    second_file.write_text("pkg", encoding="utf-8")
    third_file.write_text("1,100,art_assets/first.bundle,0.1\n", encoding="utf-8")

    summary = build_upload_summary(
        prepared_dir,
        artifact_type="asset-bundle",
        platform="windows",
        build_label="501",
    )
    expected_files = build_expected_remote_files(prepared_dir, remote_root=summary.remote_root)
    results = [
        UploadedArtifact(
            local_path=local_path,
            remote_path=remote_path,
            size=local_path.stat().st_size,
            sha256=f"sha-{index}",
            status_code=201,
            integrity_status="verified",
        )
        for index, (local_path, remote_path) in enumerate(expected_files, start=1)
    ]

    def fake_fetch_remote_listing(*, prefix, settings, recursive, limit, timeout_seconds):
        return 200, {
            "count": 1,
            "entries": [
                {
                    "path": expected_files[0][1],
                    "type": "file",
                    "size": expected_files[0][0].stat().st_size,
                }
            ],
        }, b""

    monkeypatch.setattr(
        "Common.client_resource_artifacts.fetch_remote_listing",
        fake_fetch_remote_listing,
    )

    with pytest.raises(ClientResourceArtifactsError, match="Missing remote uploaded files"):
        validate_uploaded_artifacts(
            summary,
            results=results,
            settings=FileServerClientSettings(base_url="http://fileserver", token=None, config_path=None),
            log_prefix="[TestUpload]",
        )


def test_upload_client_res_assetbundle_invokes_aggregate_validation(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "catalog.bytes").write_bytes(b"catalog")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    (platform_dir / ASSETS_INFO_FILENAME).write_text(
        "1,100,art_assets/catalog.bytes,0.1\n",
        encoding="utf-8",
    )

    settings = FileServerClientSettings(base_url="http://fileserver", token=None, config_path=None)
    captured: dict[str, object] = {}

    monkeypatch.setattr(
        "Common.client_resource_artifacts.resolve_file_server_settings",
        lambda: settings,
    )

    def fake_upload_asset_bundle(*args, **kwargs):
        source_path = Path(args[0])
        files = list_source_files(source_path)
        remote_root = "ClientRes_Assetbundle_android/77"
        return [
            UploadedArtifact(
                local_path=file_path,
                remote_path=f"{remote_root}/{file_path.relative_to(source_path).as_posix()}",
                size=file_path.stat().st_size,
                sha256=f"sha-{index}",
                status_code=201,
                integrity_status="verified",
            )
            for index, file_path in enumerate(files, start=1)
        ]

    def fake_validate(summary, *, results, settings, log_prefix):
        captured["summary"] = summary
        captured["results"] = results
        captured["settings"] = settings
        captured["log_prefix"] = log_prefix

    monkeypatch.setattr(
        "Common.client_resource_artifacts.upload_asset_bundle",
        fake_upload_asset_bundle,
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.validate_uploaded_artifacts",
        fake_validate,
    )

    results = upload_client_res_assetbundle(
        "android",
        output_root=output_root,
        build_number="77",
        fallback_build_label="0.1.77",
        log_prefix="[BuildAssetbundle][Android]",
    )

    assert len(results) == 3
    assert captured["settings"] == settings
    assert captured["log_prefix"] == "[BuildAssetbundle][Android]"
    assert captured["summary"].remote_root == "ClientRes_Assetbundle_android/77"