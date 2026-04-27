"""ClientRes 产物暂存与上传集成辅助函数测试。

测试重点：
1. 暂存目录只保留每种资源类型所需的文件。
2. 上传封装在成功上传后发布共享的文件服务器版本指针。
3. 代码、AssetBundle、表格三种资源的暂存规则和校验逻辑。
4. 远端列表校验和上传后验证流程。
"""

from __future__ import annotations

from pathlib import Path
from types import SimpleNamespace
import sys

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.client_resource_artifacts import (  # noqa: E402
    ART_ASSETS_DIRNAME,
    ART_ASSET_METADATA_FILENAMES,
    AssetInfoEntry,
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
    normalize_remote_listing_path,
    resolve_table_platform_output_dir,
    upload_client_res_code,
    upload_client_res_table,
    relativize_asset_info_entries,
    upload_client_res_assetbundle,
    validate_uploaded_artifacts,
)
from Common.artifact_uploader import FileServerClientSettings, UploadedArtifact  # noqa: E402


def write_asset_info(target_path: Path, rows: list[tuple[str, str, str, str]]) -> None:
    """写入最小化的 assets.info CSV 文件，用于 ClientRes 产物测试。

    参数：
        target_path: 目标文件路径。
        rows: CSV 数据行列表，每行格式为 (Id, HashName, LocalPath, FileSize)。
    """
    content = ["Id,HashName,LocalPath,FileSize"]
    content.extend(",".join(row) for row in rows)
    target_path.write_text("\n".join(content) + "\n", encoding="utf-8")


def test_prepare_clean_ci_output_root_recreates_existing_directory(tmp_path: Path) -> None:
    """验证 CI 输出根目录被重新创建且不包含残留文件。"""
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
    """验证代码暂存同时保留显式 package_build.info、哈希负载文件和必需元数据。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / SCRIPT_DIRNAME / "hotfix").mkdir(parents=True)
    (platform_dir / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").write_bytes(b"dll")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "script/hotfix/Assembly-CSharp.dll.bytes", "0.2"),
        ],
    )
    (platform_dir / ASSETS_SUBPACK_INFO_FILENAME).write_text("subpack", encoding="utf-8")

    prepared = prepare_code_upload_source(
        "android",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert (prepared / PACKAGE_BUILD_INFO_FILENAME).read_text(encoding="utf-8") == "pkg"
    assert (prepared / "100").read_text(encoding="utf-8") == "pkg"
    assert (prepared / "101").read_bytes() == b"dll"
    assert not (prepared / SCRIPT_DIRNAME).exists()
    assert "HashName,LocalPath" in (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8")
    assert (prepared / ASSETS_SUBPACK_INFO_FILENAME).read_text(encoding="utf-8") == "subpack"


def test_prepare_code_upload_source_requires_package_build_info(tmp_path: Path) -> None:
    """验证代码暂存在缺少 package_build.info 文件时报错。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "ios"
    (platform_dir / SCRIPT_DIRNAME / "hotfix").mkdir(parents=True)
    (platform_dir / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").write_bytes(b"dll")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "script/hotfix/Assembly-CSharp.dll.bytes", "0.2"),
        ],
    )

    with pytest.raises(ClientResourceArtifactsError, match="package_build.info"):
        prepare_code_upload_source(
            "ios",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_code_upload_source_requires_declared_script_files(tmp_path: Path) -> None:
    """验证代码暂存拒绝在 assets.info 中声明但磁盘上缺失的脚本文件。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "ios"
    (platform_dir / SCRIPT_DIRNAME / "hotfix").mkdir(parents=True)
    (platform_dir / SCRIPT_DIRNAME / "hotfix" / "Assembly-CSharp.dll.bytes").write_bytes(b"dll")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "script/hotfix/Assembly-CSharp.dll.bytes", "0.2"),
            ("3", "102", "script/hotfix/Assembly-CSharp.pdb.bytes", "0.3"),
        ],
    )

    with pytest.raises(ClientResourceArtifactsError, match="Assembly-CSharp.pdb.bytes"):
        prepare_code_upload_source(
            "ios",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_assetbundle_upload_source_filters_editor_only_metadata(tmp_path: Path) -> None:
    """验证 AssetBundle 暂存会过滤 package_build.info 与 editor-only 构建元数据。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "windows"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "catalog.bytes").write_bytes(b"catalog")
    (platform_dir / ART_ASSETS_DIRNAME / "buildlogtep.json").write_text("{}", encoding="utf-8")
    (platform_dir / ART_ASSETS_DIRNAME / "EditorBuild.Info").write_text("editor", encoding="utf-8")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "art_assets/catalog.bytes", "0.2"),
            ("3", "102", "art_assets/buildlogtep.json", "0.3"),
            ("4", "103", "art_assets/EditorBuild.Info", "0.4"),
        ],
    )

    prepared = prepare_assetbundle_upload_source(
        "windows",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert not (prepared / PACKAGE_BUILD_INFO_FILENAME).exists()
    assert not (prepared / "100").exists()
    assert (prepared / "101").read_bytes() == b"catalog"
    assert not (prepared / "102").exists()
    assert not (prepared / "103").exists()
    assert not (prepared / ART_ASSETS_DIRNAME).exists()
    prepared_assets_info = (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8")
    assert "art_assets/catalog.bytes" in prepared_assets_info
    assert PACKAGE_BUILD_INFO_FILENAME not in prepared_assets_info
    assert "art_assets/buildlogtep.json" not in prepared_assets_info
    assert "art_assets/EditorBuild.Info" not in prepared_assets_info
    assert not (prepared / ASSETS_SUBPACK_INFO_FILENAME).exists()


def test_prepare_assetbundle_upload_source_falls_back_to_art_assets_info_when_assets_info_has_no_payload(
    tmp_path: Path,
) -> None:
    """验证当 assets.info 中没有真实负载文件时，AssetBundle 暂存会回退到 art_assets.info 解析。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "catalog.bytes").write_bytes(b"catalog")
    (platform_dir / ART_ASSETS_DIRNAME / "art_asset_type.info").write_text("type", encoding="utf-8")
    (platform_dir / ART_ASSETS_DIRNAME / "art_assets.info").write_text(
        "Id,AssetType,LoadPath,GUID,AssetBundleLoadType,AssetBundlePath,Hash,AssetsPackSourceHash,Mix,DependAssetIds\n"
        '1,-1,,,0,catalog.bytes,555,123,0,"[]"\n',
        encoding="utf-8",
    )
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1000001", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("1000002", "101", "art_assets/art_asset_type.info", "0.2"),
            ("1000003", "102", "art_assets/art_assets.info", "0.3"),
        ],
    )

    prepared = prepare_assetbundle_upload_source(
        "android",
        output_root=output_root,
        staging_dir=tmp_path / "staging",
    )

    assert not (prepared / PACKAGE_BUILD_INFO_FILENAME).exists()
    assert not (prepared / "100").exists()
    assert (prepared / "101").read_text(encoding="utf-8") == "type"
    assert "catalog.bytes" in (prepared / "102").read_text(encoding="utf-8")
    assert (prepared / "555").read_bytes() == b"catalog"
    assert "art_assets/catalog.bytes" in (prepared / ASSETS_INFO_FILENAME).read_text(encoding="utf-8")


def test_prepare_assetbundle_upload_source_requires_declared_art_assets_files(tmp_path: Path) -> None:
    """验证 AssetBundle 暂存拒绝在 assets.info 中声明但磁盘上缺失的 art_assets 文件。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "ios"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "existing.bundle").write_bytes(b"bundle")
    (platform_dir / ART_ASSETS_DIRNAME / "buildlogtep.json").write_text("{}", encoding="utf-8")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "art_assets/existing.bundle", "0.2"),
            ("3", "102", "art_assets/real.bundle", "0.3"),
            ("4", "103", "art_assets/buildlogtep.json", "0.4"),
        ],
    )

    with pytest.raises(ClientResourceArtifactsError, match="art_assets/real.bundle"):
        prepare_assetbundle_upload_source(
            "ios",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_assetbundle_upload_source_requires_real_payload_file(tmp_path: Path) -> None:
    """验证 AssetBundle 暂存拒绝仅包含元数据的 art_assets 输出（缺少真实负载文件）。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "buildlogtep.json").write_text("{}", encoding="utf-8")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "art_assets/buildlogtep.json", "0.2"),
        ],
    )

    with pytest.raises(ClientResourceArtifactsError, match="does not contain any real art_assets payload files"):
        prepare_assetbundle_upload_source(
            "android",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_prepare_table_upload_source_renames_local_db_to_client_db(tmp_path: Path) -> None:
    """验证表格暂存将 local.db 重命名为 client.db 并保留 server.db。"""
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


def test_prepare_table_upload_source_requires_non_empty_databases(tmp_path: Path) -> None:
    """验证表格暂存拒绝空的 local.db 负载文件。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "osx"
    (output_root / SERVER_DATA_DIRNAME).mkdir(parents=True)
    platform_dir.mkdir(parents=True)
    (platform_dir / LOCAL_DB_FILENAME).write_bytes(b"")
    (output_root / SERVER_DATA_DIRNAME / SERVER_DB_FILENAME).write_bytes(b"server-db")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")

    with pytest.raises(ClientResourceArtifactsError, match="local.db must not be empty"):
        prepare_table_upload_source(
            "osx",
            output_root=output_root,
            staging_dir=tmp_path / "staging",
        )


def test_resolve_table_platform_output_dir_falls_back_to_detected_platform(tmp_path: Path) -> None:
    """验证表格上传在主机平台提示过时时，回退到唯一已生成的平台目录。"""
    output_root = tmp_path / "output"
    detected_platform_dir = output_root / "android"
    detected_platform_dir.mkdir(parents=True)
    (detected_platform_dir / LOCAL_DB_FILENAME).write_bytes(b"local-db")
    (detected_platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")

    resolved = resolve_table_platform_output_dir("windows", output_root=output_root)

    assert resolved == detected_platform_dir


def test_build_upload_summary_uses_new_remote_layout_names(tmp_path: Path) -> None:
    """验证上传摘要使用新的共享远端根目录命名规则。"""
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


def test_normalize_remote_listing_path_maps_legacy_root_case_to_expected_root() -> None:
    """验证列表路径在仅根目录大小写不同时，复用当前的远端根目录拼写。"""
    normalized_path, alias_root = normalize_remote_listing_path(
        "ClientRes_table/501/client.db",
        expected_root="ClientRes_Table/501",
    )

    assert normalized_path == "ClientRes_Table/501/client.db"
    assert alias_root == "ClientRes_table/501"


def test_upload_client_res_code_publishes_shared_version_manifest(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证代码上传在验证通过后发布共享版本指针到文件服务器。"""
    settings = SimpleNamespace(base_url="http://fileserver", config_path=None)
    prepared_dir = tmp_path / "prepared_code"
    prepared_dir.mkdir(parents=True)
    captured_publish: dict[str, object] = {}
    captured_validate: dict[str, object] = {}

    monkeypatch.setattr("Common.client_resource_artifacts.resolve_file_server_settings", lambda: settings)
    monkeypatch.setattr(
        "Common.client_resource_artifacts.prepare_code_upload_source",
        lambda *args, **kwargs: prepared_dir,
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.build_upload_summary",
        lambda *args, **kwargs: SimpleNamespace(
            prepared_source_path=prepared_dir,
            build_label="238",
            remote_root="ClientRes_Code_android/238",
            file_count=1,
            total_bytes=8,
        ),
    )
    monkeypatch.setattr("Common.client_resource_artifacts.log_upload_summary", lambda *args, **kwargs: None)
    monkeypatch.setattr("Common.client_resource_artifacts.emit_upload_callbacks", lambda _prefix: (None, None))
    monkeypatch.setattr("Common.client_resource_artifacts.upload_code", lambda *args, **kwargs: ["uploaded"])
    monkeypatch.setattr(
        "Common.client_resource_artifacts.validate_uploaded_artifacts",
        lambda summary, *, results, settings, log_prefix: captured_validate.update(
            {
                "summary": summary,
                "results": results,
                "settings": settings,
                "log_prefix": log_prefix,
            }
        ),
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.publish_client_resource_version_manifest",
        lambda platform, **kwargs: captured_publish.update({"platform": platform, **kwargs})
        or SimpleNamespace(to_text=lambda: "238.0.0"),
    )

    results = upload_client_res_code(
        "android",
        output_root=tmp_path / "output",
        build_number="238",
        fallback_build_label="0.1.238",
        log_prefix="[BuildCode][Android]",
    )

    assert results == ["uploaded"]
    assert captured_publish == {
        "platform": "android",
        "component_kind": "code",
        "build_label": "238",
        "settings": settings,
    }
    assert captured_validate["results"] == ["uploaded"]
    assert captured_validate["settings"] == settings
    assert captured_validate["log_prefix"] == "[BuildCode][Android]"
    assert captured_validate["summary"].remote_root == "ClientRes_Code_android/238"


def test_upload_client_res_table_publishes_all_platform_manifests(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证表格上传为所有运行时平台发布共享版本指针。"""
    settings = SimpleNamespace(base_url="http://fileserver", config_path=None)
    prepared_dir = tmp_path / "prepared_table"
    prepared_dir.mkdir(parents=True)
    captured_publish: dict[str, object] = {}
    captured_validate: dict[str, object] = {}

    monkeypatch.setattr("Common.client_resource_artifacts.resolve_file_server_settings", lambda: settings)
    monkeypatch.setattr(
        "Common.client_resource_artifacts.resolve_table_platform_output_dir",
        lambda *args, **kwargs: tmp_path / "output" / "osx",
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.prepare_table_upload_source",
        lambda *args, **kwargs: prepared_dir,
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.build_upload_summary",
        lambda *args, **kwargs: SimpleNamespace(
            prepared_source_path=prepared_dir,
            build_label="501",
            remote_root="ClientRes_Table/501",
            file_count=2,
            total_bytes=16,
        ),
    )
    monkeypatch.setattr("Common.client_resource_artifacts.log_upload_summary", lambda *args, **kwargs: None)
    monkeypatch.setattr("Common.client_resource_artifacts.emit_upload_callbacks", lambda _prefix: (None, None))
    monkeypatch.setattr("Common.client_resource_artifacts.upload_table", lambda *args, **kwargs: ["uploaded"])
    monkeypatch.setattr(
        "Common.client_resource_artifacts.validate_uploaded_artifacts",
        lambda summary, *, results, settings, log_prefix: captured_validate.update(
            {
                "summary": summary,
                "results": results,
                "settings": settings,
                "log_prefix": log_prefix,
            }
        ),
    )
    monkeypatch.setattr(
        "Common.client_resource_artifacts.publish_table_version_manifests",
        lambda **kwargs: captured_publish.update(kwargs)
        or {"android": SimpleNamespace(to_text=lambda: "0.0.501")},
    )

    results = upload_client_res_table(
        "osx",
        output_root=tmp_path / "output",
        build_number="501",
        fallback_build_label="table",
        log_prefix="[BuildTable]",
    )

    assert results == ["uploaded"]
    assert captured_publish == {
        "build_label": "501",
        "settings": settings,
    }
    assert captured_validate["results"] == ["uploaded"]
    assert captured_validate["settings"] == settings
    assert captured_validate["log_prefix"] == "[BuildTable]"
    assert captured_validate["summary"].remote_root == "ClientRes_Table/501"


def test_parse_assetbundle_manifest_paths_extracts_art_assets_only(tmp_path: Path) -> None:
    """验证 AssetBundle 清单解析只返回 art_assets 目录下的条目。"""
    info_path = tmp_path / ASSETS_INFO_FILENAME
    write_asset_info(
        info_path,
        [
            ("1", "100", "art_assets/real.bundle", "0.1"),
            ("2", "101", "other/path.bin", "0.2"),
            ("3", "102", "art_assets/buildlogtep.json", "0.3"),
        ],
    )

    assert parse_assetbundle_manifest_paths(info_path) == {
        "art_assets/real.bundle",
        "art_assets/buildlogtep.json",
    }


def test_relativize_asset_info_entries_rewrites_windows_absolute_paths() -> None:
    """验证 Windows 绝对路径的资产条目被转换为相对于平台输出根目录的相对路径。"""
    entries = [
        AssetInfoEntry(
            asset_id="1",
            hash_name="100",
            local_path="D:/ci/clientres/android/android/art_assets/catalog.bytes",
            file_size="0.1",
        )
    ]

    normalized_entries = relativize_asset_info_entries(
        Path("D:/ci/clientres/android/android"),
        entries,
    )

    assert normalized_entries[0].local_path == "art_assets/catalog.bytes"


def test_has_real_assetbundle_payload_ignores_metadata_only_entries() -> None:
    """验证仅包含元数据的 AssetBundle 条目不计为真实负载文件。"""
    metadata_only = {f"art_assets/{name}" for name in ART_ASSET_METADATA_FILENAMES}
    assert has_real_assetbundle_payload(metadata_only) is False
    assert has_real_assetbundle_payload({*metadata_only, "art_assets/real.bundle"}) is True


def test_validate_uploaded_artifacts_checks_remote_listing_and_logs_success(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证上传产物校验会检查远端列表并记录已验证的文件数量。"""
    prepared_dir = tmp_path / "prepared"
    prepared_dir.mkdir(parents=True)
    first_file = prepared_dir / "100"
    second_file = prepared_dir / "101"
    third_file = prepared_dir / ASSETS_INFO_FILENAME
    first_file.write_bytes(b"bundle")
    second_file.write_text("pkg", encoding="utf-8")
    write_asset_info(
        third_file,
        [
            ("1", "100", "art_assets/first.bundle", "0.1"),
            ("2", "101", PACKAGE_BUILD_INFO_FILENAME, "0.2"),
        ],
    )

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
    """验证当远端列表缺少期望文件时，上传产物校验失败。"""
    prepared_dir = tmp_path / "prepared"
    prepared_dir.mkdir(parents=True)
    first_file = prepared_dir / "100"
    second_file = prepared_dir / "101"
    third_file = prepared_dir / ASSETS_INFO_FILENAME
    first_file.write_bytes(b"bundle")
    second_file.write_text("pkg", encoding="utf-8")
    write_asset_info(
        third_file,
        [
            ("1", "100", "art_assets/first.bundle", "0.1"),
            ("2", "101", PACKAGE_BUILD_INFO_FILENAME, "0.2"),
        ],
    )

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


def test_validate_uploaded_artifacts_accepts_remote_listing_with_legacy_root_case(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify uploaded artifact validation tolerates listing roots that preserve a legacy directory case."""
    prepared_dir = tmp_path / "prepared"
    prepared_dir.mkdir(parents=True)
    client_db = prepared_dir / CLIENT_DB_FILENAME
    server_db = prepared_dir / SERVER_DB_FILENAME
    package_info = prepared_dir / PACKAGE_BUILD_INFO_FILENAME
    client_db.write_bytes(b"client-db")
    server_db.write_bytes(b"server-db")
    package_info.write_text("pkg", encoding="utf-8")

    summary = build_upload_summary(
        prepared_dir,
        artifact_type="table",
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
        assert prefix == summary.remote_root
        return 200, {
            "count": len(expected_files),
            "entries": [
                {
                    "path": remote_path.replace("ClientRes_Table", "ClientRes_table", 1),
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
    assert "[TestUpload] uploadVerifiedRootAliases=ClientRes_table/501" in output
    assert "[TestUpload] uploadVerifiedFiles=3" in output


def test_upload_client_res_assetbundle_invokes_aggregate_validation(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证 AssetBundle 上传会先做聚合校验，再发布共享版本指针。"""
    output_root = tmp_path / "output"
    platform_dir = output_root / "android"
    (platform_dir / ART_ASSETS_DIRNAME).mkdir(parents=True)
    (platform_dir / ART_ASSETS_DIRNAME / "catalog.bytes").write_bytes(b"catalog")
    (platform_dir / PACKAGE_BUILD_INFO_FILENAME).write_text("pkg", encoding="utf-8")
    write_asset_info(
        platform_dir / ASSETS_INFO_FILENAME,
        [
            ("1", "100", PACKAGE_BUILD_INFO_FILENAME, "0.1"),
            ("2", "101", "art_assets/catalog.bytes", "0.2"),
        ],
    )

    settings = FileServerClientSettings(base_url="http://fileserver", token=None, config_path=None)
    captured: dict[str, object] = {}
    captured_publish: dict[str, object] = {}

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
    monkeypatch.setattr(
        "Common.client_resource_artifacts.publish_client_resource_version_manifest",
        lambda platform, **kwargs: captured_publish.update({"platform": platform, **kwargs})
        or SimpleNamespace(to_text=lambda: "0.77.0"),
    )

    results = upload_client_res_assetbundle(
        "android",
        output_root=output_root,
        build_number="77",
        fallback_build_label="0.1.77",
        log_prefix="[BuildAssetbundle][Android]",
    )

    assert len(results) == 2
    assert all(not result.remote_path.endswith(f"/{PACKAGE_BUILD_INFO_FILENAME}") for result in results)
    assert captured["settings"] == settings
    assert captured["log_prefix"] == "[BuildAssetbundle][Android]"
    assert captured["summary"].remote_root == "ClientRes_Assetbundle_android/77"
    assert captured_publish == {
        "platform": "android",
        "component_kind": "assetbundle",
        "build_label": "77",
        "settings": settings,
    }
