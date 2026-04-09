from __future__ import annotations

from pathlib import Path
import sys

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.client_resource_artifacts import (  # noqa: E402
    ART_ASSETS_DIRNAME,
    ASSETS_INFO_FILENAME,
    ASSETS_SUBPACK_INFO_FILENAME,
    CLIENT_DB_FILENAME,
    ClientResourceArtifactsError,
    LOCAL_DB_FILENAME,
    PACKAGE_BUILD_INFO_FILENAME,
    SCRIPT_DIRNAME,
    SERVER_DATA_DIRNAME,
    SERVER_DB_FILENAME,
    build_upload_summary,
    list_source_files,
    prepare_assetbundle_upload_source,
    prepare_clean_ci_output_root,
    prepare_code_upload_source,
    prepare_table_upload_source,
)


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
    (platform_dir / ASSETS_INFO_FILENAME).write_text("assets", encoding="utf-8")

    prepared = prepare_assetbundle_upload_source(
        "windows",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert (prepared / ART_ASSETS_DIRNAME / "catalog.bytes").read_bytes() == b"catalog"
    assert (prepared / PACKAGE_BUILD_INFO_FILENAME).read_text(encoding="utf-8") == "pkg"
    assert (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8") == "assets"
    assert not (prepared / ASSETS_SUBPACK_INFO_FILENAME).exists()


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