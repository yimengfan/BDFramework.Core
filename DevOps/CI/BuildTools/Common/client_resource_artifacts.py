from __future__ import annotations

"""Shared helpers for ClientRes Code / Assetbundle / Table CI scripts.

职责边界：
1. 这里只处理隔离输出目录、产物筛选、临时整理与上传。
2. Unity BatchMode 参数解析、日志和进程控制统一复用 BuildClientPackage 的共享 facade。
3. TeamCity DSL 只调度 Python 入口脚本，真正的产物组织与上传规则放在这里收敛。
"""

import csv
from collections.abc import Callable
import shutil
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
import tempfile

from Common.artifact_uploader import (
    DEFAULT_TIMEOUT_SECONDS,
    FileServerClientSettings,
    UploadedArtifact,
    build_artifact_remote_root,
    fetch_remote_listing,
    resolve_file_server_settings,
    upload_asset_bundle,
    upload_code,
    upload_table,
)


SKIPPED_OUTPUT_FILENAMES = {".DS_Store"}
PACKAGE_BUILD_INFO_FILENAME = "package_build.info"
ASSETS_INFO_FILENAME = "assets.info"
ASSETS_SUBPACK_INFO_FILENAME = "assets_subpack.info"
SCRIPT_DIRNAME = "script"
ART_ASSETS_DIRNAME = "art_assets"
ART_ASSET_METADATA_FILENAMES = {"art_asset_type.info", "art_assets.info", "buildlogtep.json"}
ART_ASSET_TYPE_INFO_LOCAL_PATH = f"{ART_ASSETS_DIRNAME}/art_asset_type.info"
ART_ASSETS_INFO_LOCAL_PATH = f"{ART_ASSETS_DIRNAME}/art_assets.info"
ART_ASSET_BUILD_LOG_LOCAL_PATH = f"{ART_ASSETS_DIRNAME}/buildlogtep.json"
SCRIPT_METADATA_FILENAMES = set()
LOCAL_DB_FILENAME = "local.db"
CLIENT_DB_FILENAME = "client.db"
SERVER_DATA_DIRNAME = "server_data"
SERVER_DB_FILENAME = "server.db"
DEFAULT_BUILD_NAME_LABEL = "local"
DEFAULT_BUILD_NUMBER_LABEL = "manual"


class ClientResourceArtifactsError(RuntimeError):
    """ClientRes 产物整理与上传前校验错误。"""


@dataclass(frozen=True)
class ClientResourceUploadSummary:
    """描述一次 ClientRes 上传前的本地产物概况。"""

    source_path: Path
    prepared_source_path: Path
    build_label: str
    remote_root: str
    file_count: int
    total_bytes: int


@dataclass(frozen=True)
class AssetInfoEntry:
    """描述 assets.info 中的一条服务器资源映射。"""

    asset_id: str
    hash_name: str
    local_path: str
    file_size: str


def sanitize_path_fragment(raw_value: str | None, *, fallback: str) -> str:
    """把构建名、构建号等字符串转成稳定目录片段。"""
    normalized = (raw_value or "").strip()
    if not normalized:
        return fallback

    sanitized = normalized
    for bad_char in ('/', '\\', ':', '*', '?', '"', '<', '>', '|', ' '):
        sanitized = sanitized.replace(bad_char, '_')

    return sanitized or fallback


def get_ci_output_root(
    project_dir: Path,
    *,
    build_kind: str,
    build_name: str | None,
    build_number: str | None,
    platform: str | None = None,
) -> Path:
    """生成当前任务的隔离输出目录。"""
    output_root = (
        project_dir
        / "Library"
        / "CIOutputs"
        / sanitize_path_fragment(build_kind, fallback="clientres")
        / sanitize_path_fragment(build_name, fallback=DEFAULT_BUILD_NAME_LABEL)
        / sanitize_path_fragment(build_number, fallback=DEFAULT_BUILD_NUMBER_LABEL)
    )
    if platform:
        output_root /= sanitize_path_fragment(platform, fallback="unknown")
    return output_root


def prepare_clean_ci_output_root(
    project_dir: Path,
    *,
    build_kind: str,
    build_name: str | None,
    build_number: str | None,
    platform: str | None = None,
) -> Path:
    """构建前重建隔离输出目录，避免旧产物重复上传。"""
    output_root = get_ci_output_root(
        project_dir,
        build_kind=build_kind,
        build_name=build_name,
        build_number=build_number,
        platform=platform,
    )
    if output_root.exists():
        if output_root.is_dir():
            shutil.rmtree(output_root)
        else:
            output_root.unlink()

    output_root.mkdir(parents=True, exist_ok=True)
    return output_root


def ensure_existing_path(path: Path, *, description: str) -> Path:
    """校验并返回必须存在的路径。"""
    if not path.exists():
        raise ClientResourceArtifactsError(f"{description} does not exist: {path}")
    return path


def ensure_non_empty_file(path: Path, *, description: str) -> Path:
    """校验文件存在且非空。"""
    resolved_path = ensure_existing_path(path, description=description)
    if not resolved_path.is_file():
        raise ClientResourceArtifactsError(f"{description} is not a file: {resolved_path}")
    if resolved_path.stat().st_size <= 0:
        raise ClientResourceArtifactsError(f"{description} must not be empty: {resolved_path}")
    return resolved_path


def copy_path(source_path: Path, destination_path: Path) -> None:
    """复制文件或目录到 staging 目录。"""
    if source_path.is_dir():
        shutil.copytree(source_path, destination_path, dirs_exist_ok=True)
        return

    destination_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source_path, destination_path)


def list_source_files(source_path: Path) -> list[Path]:
    """按稳定顺序收集 staging 目录中的上传文件。"""
    if not source_path.exists():
        raise ClientResourceArtifactsError(f"Prepared source path does not exist: {source_path}")
    if source_path.is_file():
        return [source_path]

    files = [
        file_path
        for file_path in sorted(source_path.rglob("*"))
        if file_path.is_file() and file_path.name not in SKIPPED_OUTPUT_FILENAMES
    ]
    if not files:
        raise ClientResourceArtifactsError(f"No files found in prepared source path: {source_path}")
    return files


def resolve_upload_build_label(build_number: str | None, fallback_label: str | None) -> str:
    """优先使用 TeamCity build number，缺失时回退到业务标签。"""
    normalized_build_number = (build_number or "").strip()
    if normalized_build_number:
        return normalized_build_number

    normalized_fallback = (fallback_label or "").strip()
    if normalized_fallback:
        return normalized_fallback

    raise ClientResourceArtifactsError("Upload build label is empty. Provide build number or fallback label.")


def build_upload_summary(
    prepared_source_path: Path,
    *,
    artifact_type: str,
    build_label: str,
    platform: str | None = None,
) -> ClientResourceUploadSummary:
    """生成上传前摘要，统一输出日志与统计。"""
    files = list_source_files(prepared_source_path)
    remote_root = build_artifact_remote_root(
        artifact_type,
        platform=platform,
        build_number=build_label,
    )
    total_bytes = sum(file_path.stat().st_size for file_path in files)
    return ClientResourceUploadSummary(
        source_path=prepared_source_path,
        prepared_source_path=prepared_source_path,
        build_label=build_label,
        remote_root=remote_root,
        file_count=len(files),
        total_bytes=total_bytes,
    )


def log_upload_summary(
    summary: ClientResourceUploadSummary,
    *,
    log_prefix: str,
    settings_base_url: str,
    config_path: Path | None,
) -> None:
    """输出适合 CI 观察的上传摘要日志。"""
    print(f"{log_prefix} uploadPreparedSource={summary.prepared_source_path}")
    print(f"{log_prefix} uploadBuildLabel={summary.build_label}")
    print(f"{log_prefix} uploadRemoteRoot={summary.remote_root}")
    print(f"{log_prefix} uploadServerUrl={settings_base_url}")
    if config_path is not None:
        print(f"{log_prefix} uploadConfig={config_path}")
    print(f"{log_prefix} uploadFileCount={summary.file_count}")
    print(f"{log_prefix} uploadTotalBytes={summary.total_bytes}")


def emit_upload_callbacks(log_prefix: str) -> tuple[
    Callable[[int, int, Path, str], None],
    Callable[[int, int, UploadedArtifact], None],
]:
    """生成统一的上传进度日志回调。"""

    def on_uploading(index: int, total: int, local_path: Path, remote_path: str) -> None:
        print(
            f"{log_prefix} uploadProgress={index}/{total} state=uploading "
            f"local={local_path} remote={remote_path}"
        )

    def on_uploaded(index: int, total: int, result: UploadedArtifact) -> None:
        integrity_status = result.integrity_status or "unknown"
        print(
            f"{log_prefix} uploadProgress={index}/{total} state=uploaded "
            f"remote={result.remote_path} size={result.size} integrity={integrity_status}"
        )

    return on_uploading, on_uploaded


def build_expected_remote_files(
    prepared_source_path: Path,
    *,
    remote_root: str,
) -> list[tuple[Path, str]]:
    """把本地 staging 文件映射到期望的远端路径。"""
    resolved_source_path = prepared_source_path.resolve()
    resolved_remote_root = PurePosixPath(remote_root)
    expected_files: list[tuple[Path, str]] = []

    for file_path in list_source_files(resolved_source_path):
        resolved_file_path = file_path.resolve()
        if resolved_source_path.is_file():
            remote_path = str(resolved_remote_root / resolved_file_path.name)
        else:
            remote_path = str(
                resolved_remote_root / PurePosixPath(resolved_file_path.relative_to(resolved_source_path).as_posix())
            )
        expected_files.append((resolved_file_path, remote_path))

    return expected_files


def fetch_remote_uploaded_file_entries(
    summary: ClientResourceUploadSummary,
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> dict[str, dict[str, object]]:
    """递归读取当前上传根目录下的远端文件清单。"""
    limit = min(max(summary.file_count * 4 + 16, 128), 2000)
    status, payload, raw_body = fetch_remote_listing(
        prefix=summary.remote_root,
        settings=settings,
        recursive=True,
        limit=limit,
        timeout_seconds=timeout_seconds,
    )
    if status != 200:
        detail = payload.get("detail") if isinstance(payload, dict) else None
        detail_text = detail or raw_body.decode("utf-8", errors="replace")
        raise ClientResourceArtifactsError(
            f"Remote upload listing failed: root={summary.remote_root}, status={status}, detail={detail_text}"
        )

    entries = payload.get("entries") if isinstance(payload, dict) else None
    if not isinstance(entries, list):
        raise ClientResourceArtifactsError(
            f"Remote upload listing returned invalid payload: root={summary.remote_root}, payload={payload!r}"
        )

    remote_files: dict[str, dict[str, object]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        if entry.get("type") != "file":
            continue

        remote_path = str(entry.get("path") or "").strip()
        if not remote_path:
            continue
        remote_files[remote_path] = entry

    return remote_files


def validate_uploaded_artifacts(
    summary: ClientResourceUploadSummary,
    *,
    results: list[UploadedArtifact],
    settings: FileServerClientSettings,
    log_prefix: str,
) -> None:
    """验证 staging 文件集合与远端已上传文件集合是否一致。"""
    expected_files = build_expected_remote_files(
        summary.prepared_source_path,
        remote_root=summary.remote_root,
    )
    expected_by_local_path = {local_path: remote_path for local_path, remote_path in expected_files}
    if len(expected_by_local_path) != summary.file_count:
        raise ClientResourceArtifactsError(
            "Upload summary file count mismatch. "
            f"expected={len(expected_by_local_path)}, summary={summary.file_count}, root={summary.remote_root}"
        )

    uploaded_by_local_path: dict[Path, UploadedArtifact] = {}
    duplicate_local_paths: list[str] = []
    for result in results:
        local_path = Path(result.local_path).resolve()
        if local_path in uploaded_by_local_path:
            duplicate_local_paths.append(str(local_path))
            continue
        uploaded_by_local_path[local_path] = result

    if duplicate_local_paths:
        raise ClientResourceArtifactsError(
            f"Duplicate uploaded artifact results detected: {sorted(duplicate_local_paths)}"
        )

    missing_local_paths = sorted(
        str(local_path)
        for local_path in expected_by_local_path
        if local_path not in uploaded_by_local_path
    )
    unexpected_local_paths = sorted(
        str(local_path)
        for local_path in uploaded_by_local_path
        if local_path not in expected_by_local_path
    )
    if missing_local_paths or unexpected_local_paths:
        raise ClientResourceArtifactsError(
            "Uploaded artifact set does not match local staging files. "
            f"missing={missing_local_paths}, unexpected={unexpected_local_paths}"
        )

    remote_path_mismatches: list[str] = []
    size_mismatches: list[str] = []
    uploaded_total_bytes = 0
    for local_path, expected_remote_path in expected_by_local_path.items():
        result = uploaded_by_local_path[local_path]
        if result.remote_path != expected_remote_path:
            remote_path_mismatches.append(
                f"local={local_path} expected={expected_remote_path} actual={result.remote_path}"
            )

        expected_size = local_path.stat().st_size
        if result.size != expected_size:
            size_mismatches.append(
                f"local={local_path} expected={expected_size} actual={result.size}"
            )
        uploaded_total_bytes += result.size

    if remote_path_mismatches or size_mismatches:
        raise ClientResourceArtifactsError(
            "Uploaded artifact metadata mismatch. "
            f"remotePaths={remote_path_mismatches}, sizes={size_mismatches}"
        )

    if uploaded_total_bytes != summary.total_bytes:
        raise ClientResourceArtifactsError(
            "Uploaded artifact byte total mismatch. "
            f"expected={summary.total_bytes}, actual={uploaded_total_bytes}, root={summary.remote_root}"
        )

    remote_files = fetch_remote_uploaded_file_entries(summary, settings=settings)
    expected_remote_paths = {remote_path for _, remote_path in expected_files}
    missing_remote_paths = sorted(
        remote_path for remote_path in expected_remote_paths if remote_path not in remote_files
    )
    if missing_remote_paths:
        raise ClientResourceArtifactsError(
            f"Missing remote uploaded files: root={summary.remote_root}, files={missing_remote_paths}"
        )

    remote_size_mismatches: list[str] = []
    for local_path, expected_remote_path in expected_by_local_path.items():
        remote_entry = remote_files.get(expected_remote_path, {})
        remote_size = remote_entry.get("size")
        if remote_size is None:
            continue
        expected_size = local_path.stat().st_size
        if int(remote_size) != expected_size:
            remote_size_mismatches.append(
                f"remote={expected_remote_path} expected={expected_size} actual={remote_size}"
            )

    if remote_size_mismatches:
        raise ClientResourceArtifactsError(
            f"Remote uploaded file size mismatch: root={summary.remote_root}, sizes={remote_size_mismatches}"
        )

    extra_remote_file_count = max(0, len(remote_files) - len(expected_remote_paths))
    print(f"{log_prefix} uploadVerifiedFiles={len(expected_remote_paths)}")
    print(f"{log_prefix} uploadVerifiedBytes={uploaded_total_bytes}")
    print(f"{log_prefix} uploadVerifiedRemoteFiles={len(remote_files)}")
    print(f"{log_prefix} uploadVerifiedExtraRemoteFiles={extra_remote_file_count}")


def is_asset_info_header(columns: list[str]) -> bool:
    """判断当前 CSV 行是否为标准 assets.info 表头。"""
    if len(columns) < 4:
        return False

    normalized_columns = [column.strip().lower() for column in columns[:4]]
    return normalized_columns == ["id", "hashname", "localpath", "filesize"]


def normalize_asset_hash_name(raw_value: str, *, info_file_path: Path, line_number: int) -> str:
    """规范化 assets.info 中的 HashName 字段。"""
    normalized_hash_name = raw_value.strip()
    if not normalized_hash_name:
        raise ClientResourceArtifactsError(
            f"Invalid assets.info hash name: file={info_file_path}, line={line_number}"
        )
    if "/" in normalized_hash_name or "\\" in normalized_hash_name:
        raise ClientResourceArtifactsError(
            "Invalid assets.info hash name, nested paths are not supported: "
            f"file={info_file_path}, line={line_number}, hash={normalized_hash_name}"
        )

    return normalized_hash_name


def normalize_asset_local_path(raw_value: str, *, info_file_path: Path, line_number: int) -> str:
    """规范化 assets.info 中的 LocalPath 字段。"""
    normalized_local_path = raw_value.strip().replace("\\", "/")
    pure_local_path = PurePosixPath(normalized_local_path)
    if not normalized_local_path or pure_local_path.is_absolute() or ".." in pure_local_path.parts:
        raise ClientResourceArtifactsError(
            f"Invalid assets.info local path: file={info_file_path}, line={line_number}, path={raw_value!r}"
        )

    normalized_local_path = pure_local_path.as_posix()
    if normalized_local_path in {"", "."}:
        raise ClientResourceArtifactsError(
            f"Invalid assets.info local path: file={info_file_path}, line={line_number}, path={raw_value!r}"
        )

    return normalized_local_path


def parse_asset_info_entries(info_file_path: Path) -> list[AssetInfoEntry]:
    """读取标准 assets.info，返回 HashName 和 LocalPath 的映射集合。"""
    if not info_file_path.exists():
        return []

    entries: list[AssetInfoEntry] = []
    with info_file_path.open("r", encoding="utf-8", newline="") as file_handle:
        for line_number, row in enumerate(csv.reader(file_handle), start=1):
            if not row:
                continue

            normalized_row = [column.strip() for column in row]
            if not any(normalized_row):
                continue
            if is_asset_info_header(normalized_row):
                continue
            if len(normalized_row) < 4:
                raise ClientResourceArtifactsError(
                    f"Invalid assets.info row: file={info_file_path}, line={line_number}, row={row!r}"
                )

            asset_id, hash_name, local_path, file_size = normalized_row[:4]
            entries.append(
                AssetInfoEntry(
                    asset_id=asset_id,
                    hash_name=normalize_asset_hash_name(
                        hash_name,
                        info_file_path=info_file_path,
                        line_number=line_number,
                    ),
                    local_path=normalize_asset_local_path(
                        local_path,
                        info_file_path=info_file_path,
                        line_number=line_number,
                    ),
                    file_size=file_size,
                )
            )

    return entries


def build_asset_file_size_label(file_path: Path) -> str:
    """按 Unity 侧 assets.info 的规则格式化文件大小字段。"""
    file_size_kb = int(((file_path.stat().st_size / 1024.0) * 100.0)) / 100.0
    if file_size_kb.is_integer():
        return str(int(file_size_kb))
    return str(file_size_kb)


def write_asset_info_entries(output_path: Path, asset_entries: list[AssetInfoEntry]) -> None:
    """把整理后的服务器资源映射重新写成标准 assets.info。"""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    rows = ["Id,HashName,LocalPath,FileSize"]
    rows.extend(
        f"{asset_entry.asset_id},{asset_entry.hash_name},{asset_entry.local_path},{asset_entry.file_size}"
        for asset_entry in asset_entries
    )
    output_path.write_text("\n".join(rows) + "\n", encoding="utf-8")


def parse_art_assets_info_entries(platform_dir: Path) -> list[AssetInfoEntry]:
    """从 art_assets.info 回退恢复 Assetbundle payload 的服务器资源映射。"""
    art_assets_info_path = platform_dir / ART_ASSETS_INFO_LOCAL_PATH
    if not art_assets_info_path.exists():
        return []

    asset_entries: list[AssetInfoEntry] = []
    with art_assets_info_path.open("r", encoding="utf-8", newline="") as file_handle:
        for line_number, row in enumerate(csv.reader(file_handle), start=1):
            if not row:
                continue

            normalized_row = [column.strip() for column in row]
            if not any(normalized_row):
                continue
            if normalized_row[:7] == [
                "Id",
                "AssetType",
                "LoadPath",
                "GUID",
                "AssetBundleLoadType",
                "AssetBundlePath",
                "Hash",
            ]:
                continue
            if len(normalized_row) < 7:
                raise ClientResourceArtifactsError(
                    f"Invalid art_assets.info row: file={art_assets_info_path}, line={line_number}, row={row!r}"
                )

            asset_id = normalized_row[0]
            asset_bundle_path = normalized_row[5]
            hash_name = normalized_row[6]
            if not asset_bundle_path or not hash_name:
                continue

            local_path = normalize_asset_local_path(
                f"{ART_ASSETS_DIRNAME}/{asset_bundle_path}",
                info_file_path=art_assets_info_path,
                line_number=line_number,
            )
            source_path = platform_dir / Path(local_path)
            file_size = build_asset_file_size_label(source_path) if source_path.exists() else "0"
            asset_entries.append(
                AssetInfoEntry(
                    asset_id=asset_id,
                    hash_name=normalize_asset_hash_name(
                        hash_name,
                        info_file_path=art_assets_info_path,
                        line_number=line_number,
                    ),
                    local_path=local_path,
                    file_size=file_size,
                )
            )

    return asset_entries


def resolve_assetbundle_asset_info_entries(platform_dir: Path, asset_entries: list[AssetInfoEntry]) -> list[AssetInfoEntry]:
    """必要时使用 art_assets.info 回退补齐 Assetbundle payload 映射。"""
    has_declared_art_payload = has_real_assetbundle_payload(
        {
            asset_entry.local_path
            for asset_entry in asset_entries
            if asset_entry.local_path.startswith(f"{ART_ASSETS_DIRNAME}/")
        }
    )
    if has_declared_art_payload:
        return asset_entries

    fallback_art_entries = parse_art_assets_info_entries(platform_dir)
    if not fallback_art_entries:
        return asset_entries

    merged_entries_by_local_path = {
        asset_entry.local_path: asset_entry
        for asset_entry in asset_entries
    }
    for asset_entry in fallback_art_entries:
        merged_entries_by_local_path.setdefault(asset_entry.local_path, asset_entry)

    return sorted(
        merged_entries_by_local_path.values(),
        key=lambda asset_entry: int(asset_entry.asset_id),
    )


def copy_manifest_hash_files(
    platform_dir: Path,
    prepared_dir: Path,
    *,
    asset_entries: list[AssetInfoEntry],
    source_description: str,
) -> None:
    """按 assets.info 把原始资源整理成服务器需要的 hash 文件布局。"""
    staged_hashes: dict[str, str] = {}
    for asset_entry in asset_entries:
        previous_local_path = staged_hashes.get(asset_entry.hash_name)
        if previous_local_path is not None:
            if previous_local_path != asset_entry.local_path:
                raise ClientResourceArtifactsError(
                    f"{source_description} assets.info contains duplicate hash names. "
                    f"hash={asset_entry.hash_name}, first={previous_local_path}, second={asset_entry.local_path}"
                )
            continue

        source_path = ensure_existing_path(
            platform_dir / Path(asset_entry.local_path),
            description=f"{source_description} manifest source file ({asset_entry.local_path})",
        )
        copy_path(source_path, prepared_dir / asset_entry.hash_name)
        staged_hashes[asset_entry.hash_name] = asset_entry.local_path


def collect_staged_hash_names(prepared_dir: Path) -> set[str]:
    """收集 staging 根目录下的 hash 文件名，并拒绝旧的目录式布局。"""
    staged_hash_names: set[str] = set()
    unexpected_nested_files: list[str] = []

    for file_path in list_source_files(prepared_dir):
        relative_path = file_path.relative_to(prepared_dir).as_posix()
        if relative_path in {ASSETS_INFO_FILENAME, ASSETS_SUBPACK_INFO_FILENAME}:
            continue
        if "/" in relative_path:
            unexpected_nested_files.append(relative_path)
            continue

        staged_hash_names.add(relative_path)

    if unexpected_nested_files:
        raise ClientResourceArtifactsError(
            "ClientRes staging contains unexpected nested files. "
            f"files={sorted(unexpected_nested_files)}"
        )

    return staged_hash_names


def describe_manifest_hashes(asset_entries: list[AssetInfoEntry], hash_names: set[str]) -> list[str]:
    """把缺失或多余的 hash 文件转换成便于定位的日志描述。"""
    local_path_by_hash = {
        asset_entry.hash_name: asset_entry.local_path
        for asset_entry in asset_entries
    }
    return [
        f"{hash_name}<-{local_path_by_hash.get(hash_name, '<unknown>')}"
        for hash_name in sorted(hash_names)
    ]


def validate_hashed_manifest_files(prepared_dir: Path, *, source_description: str) -> list[AssetInfoEntry]:
    """校验 staging 中的 hash 文件集合与 assets.info 的声明完全一致。"""
    asset_entries = parse_asset_info_entries(prepared_dir / ASSETS_INFO_FILENAME)
    declared_hash_names = {asset_entry.hash_name for asset_entry in asset_entries}
    actual_hash_names = collect_staged_hash_names(prepared_dir)

    missing_hash_names = declared_hash_names - actual_hash_names
    unexpected_hash_names = actual_hash_names - declared_hash_names
    if missing_hash_names or unexpected_hash_names:
        raise ClientResourceArtifactsError(
            f"{source_description} staging hash files do not match assets.info. "
            f"missing={describe_manifest_hashes(asset_entries, missing_hash_names)}, "
            f"unexpected={sorted(unexpected_hash_names)}"
        )

    return asset_entries


def prepare_code_upload_source(
    platform_key: str,
    *,
    output_root: Path,
    staging_dir: Path,
) -> Path:
    """整理热更代码需要上传的目录结构。"""
    platform_dir = ensure_existing_path(
        output_root / platform_key,
        description=f"ClientRes code platform output ({platform_key})",
    )
    prepared_dir = staging_dir / platform_key
    assets_info_path = ensure_existing_path(
        platform_dir / ASSETS_INFO_FILENAME,
        description="ClientRes code assets.info",
    )

    copy_path(
        assets_info_path,
        prepared_dir / ASSETS_INFO_FILENAME,
    )

    optional_subpack = platform_dir / ASSETS_SUBPACK_INFO_FILENAME
    if optional_subpack.exists():
        copy_path(optional_subpack, prepared_dir / ASSETS_SUBPACK_INFO_FILENAME)

    copy_manifest_hash_files(
        platform_dir,
        prepared_dir,
        asset_entries=parse_asset_info_entries(assets_info_path),
        source_description="ClientRes code",
    )

    validate_code_upload_source(prepared_dir)

    return prepared_dir


def prepare_assetbundle_upload_source(
    platform_key: str,
    *,
    output_root: Path,
    staging_dir: Path,
) -> Path:
    """整理热更 Assetbundle 需要上传的目录结构。"""
    platform_dir = ensure_existing_path(
        output_root / platform_key,
        description=f"ClientRes assetbundle platform output ({platform_key})",
    )
    prepared_dir = staging_dir / platform_key
    assets_info_path = ensure_existing_path(
        platform_dir / ASSETS_INFO_FILENAME,
        description="ClientRes assetbundle assets.info",
    )
    asset_entries = resolve_assetbundle_asset_info_entries(
        platform_dir,
        parse_asset_info_entries(assets_info_path),
    )

    write_asset_info_entries(prepared_dir / ASSETS_INFO_FILENAME, asset_entries)

    optional_subpack = platform_dir / ASSETS_SUBPACK_INFO_FILENAME
    if optional_subpack.exists():
        copy_path(optional_subpack, prepared_dir / ASSETS_SUBPACK_INFO_FILENAME)

    copy_manifest_hash_files(
        platform_dir,
        prepared_dir,
        asset_entries=asset_entries,
        source_description="ClientRes assetbundle",
    )

    validate_assetbundle_upload_source(prepared_dir)

    return prepared_dir


def parse_manifest_paths(info_file_path: Path, *, relative_prefix: str | None = None) -> set[str]:
    """从 assets.info 中提取声明的 LocalPath。"""
    declared_paths: set[str] = set()
    for asset_entry in parse_asset_info_entries(info_file_path):
        relative_path = asset_entry.local_path
        if relative_prefix is not None and not relative_path.startswith(relative_prefix):
            continue

        declared_paths.add(relative_path)

    return declared_paths


def parse_assetbundle_manifest_paths(info_file_path: Path) -> set[str]:
    """从 assets.info 中提取声明的 art_assets 路径。"""
    return parse_manifest_paths(info_file_path, relative_prefix=f"{ART_ASSETS_DIRNAME}/")


def parse_code_manifest_paths(info_file_path: Path) -> set[str]:
    """从 assets.info 中提取声明的 script 路径。"""
    return parse_manifest_paths(info_file_path, relative_prefix=f"{SCRIPT_DIRNAME}/")


def validate_code_upload_source(prepared_dir: Path) -> None:
    """校验 code staging 使用 hash 布局且包含真实脚本 payload。"""
    asset_entries = validate_hashed_manifest_files(
        prepared_dir,
        source_description="ClientRes code",
    )
    declared_script_paths = {
        asset_entry.local_path
        for asset_entry in asset_entries
        if asset_entry.local_path.startswith(f"{SCRIPT_DIRNAME}/")
    }

    if not any(
        PurePosixPath(relative_path).name not in SCRIPT_METADATA_FILENAMES
        for relative_path in declared_script_paths
    ):
        raise ClientResourceArtifactsError(
            "ClientRes code staging does not contain any script payload files. "
            f"declared={sorted(declared_script_paths)}"
        )


def has_real_assetbundle_payload(relative_paths: set[str]) -> bool:
    """判断 art_assets 集合里是否包含真正的资源文件，而不只是元数据文件。"""
    return any(PurePosixPath(relative_path).name not in ART_ASSET_METADATA_FILENAMES for relative_path in relative_paths)


def validate_assetbundle_upload_source(prepared_dir: Path) -> None:
    """校验 assetbundle staging 使用 hash 布局且包含真实资源 payload。"""
    asset_entries = validate_hashed_manifest_files(
        prepared_dir,
        source_description="ClientRes assetbundle",
    )
    declared_art_asset_paths = {
        asset_entry.local_path
        for asset_entry in asset_entries
        if asset_entry.local_path.startswith(f"{ART_ASSETS_DIRNAME}/")
    }

    if not has_real_assetbundle_payload(declared_art_asset_paths):
        raise ClientResourceArtifactsError(
            "ClientRes assetbundle staging does not contain any real art_assets payload files. "
            f"declared={sorted(declared_art_asset_paths)}"
        )


def prepare_table_upload_source(
    local_platform_dir: str,
    *,
    output_root: Path,
    staging_dir: Path,
) -> Path:
    """整理统一表格任务需要上传的 client.db / server.db。"""
    platform_dir = ensure_existing_path(
        output_root / local_platform_dir,
        description=f"ClientRes table local platform output ({local_platform_dir})",
    )
    prepared_dir = staging_dir / "table"
    prepared_dir.mkdir(parents=True, exist_ok=True)

    copy_path(
        ensure_non_empty_file(platform_dir / LOCAL_DB_FILENAME, description="ClientRes table local.db"),
        prepared_dir / CLIENT_DB_FILENAME,
    )
    copy_path(
        ensure_non_empty_file(
            output_root / SERVER_DATA_DIRNAME / SERVER_DB_FILENAME,
            description="ClientRes table server.db",
        ),
        prepared_dir / SERVER_DB_FILENAME,
    )
    copy_path(
        ensure_existing_path(
            platform_dir / PACKAGE_BUILD_INFO_FILENAME,
            description="ClientRes table package_build.info",
        ),
        prepared_dir / PACKAGE_BUILD_INFO_FILENAME,
    )

    return prepared_dir


def upload_client_res_code(
    platform_key: str,
    *,
    output_root: Path,
    build_number: str | None,
    fallback_build_label: str | None,
    log_prefix: str,
) -> list[UploadedArtifact]:
    """上传三端热更代码产物。"""
    settings = resolve_file_server_settings()
    build_label = resolve_upload_build_label(build_number, fallback_build_label)

    with tempfile.TemporaryDirectory(prefix=f"clientres_code_{platform_key}_") as temp_dir:
        prepared_source_path = prepare_code_upload_source(
            platform_key,
            output_root=output_root,
            staging_dir=Path(temp_dir),
        )
        summary = build_upload_summary(
            prepared_source_path,
            artifact_type="code",
            platform=platform_key,
            build_label=build_label,
        )
        log_upload_summary(
            summary,
            log_prefix=log_prefix,
            settings_base_url=settings.base_url,
            config_path=settings.config_path,
        )
        on_uploading, on_uploaded = emit_upload_callbacks(log_prefix)
        results = upload_code(
            prepared_source_path,
            platform=platform_key,
            build_number=summary.build_label,
            settings=settings,
            on_uploading=on_uploading,
            on_uploaded=on_uploaded,
        )
        validate_uploaded_artifacts(
            summary,
            results=results,
            settings=settings,
            log_prefix=log_prefix,
        )
        print(f"{log_prefix} uploadedFiles={len(results)}")
        return results


def upload_client_res_assetbundle(
    platform_key: str,
    *,
    output_root: Path,
    build_number: str | None,
    fallback_build_label: str | None,
    log_prefix: str,
) -> list[UploadedArtifact]:
    """上传三端热更 Assetbundle 产物。"""
    settings = resolve_file_server_settings()
    build_label = resolve_upload_build_label(build_number, fallback_build_label)

    with tempfile.TemporaryDirectory(prefix=f"clientres_assetbundle_{platform_key}_") as temp_dir:
        prepared_source_path = prepare_assetbundle_upload_source(
            platform_key,
            output_root=output_root,
            staging_dir=Path(temp_dir),
        )
        summary = build_upload_summary(
            prepared_source_path,
            artifact_type="asset-bundle",
            platform=platform_key,
            build_label=build_label,
        )
        log_upload_summary(
            summary,
            log_prefix=log_prefix,
            settings_base_url=settings.base_url,
            config_path=settings.config_path,
        )
        on_uploading, on_uploaded = emit_upload_callbacks(log_prefix)
        results = upload_asset_bundle(
            prepared_source_path,
            platform=platform_key,
            build_number=summary.build_label,
            settings=settings,
            on_uploading=on_uploading,
            on_uploaded=on_uploaded,
        )
        validate_uploaded_artifacts(
            summary,
            results=results,
            settings=settings,
            log_prefix=log_prefix,
        )
        print(f"{log_prefix} uploadedFiles={len(results)}")
        return results


def upload_client_res_table(
    local_platform_dir: str,
    *,
    output_root: Path,
    build_number: str | None,
    fallback_build_label: str | None,
    log_prefix: str,
) -> list[UploadedArtifact]:
    """上传统一表格任务产物。"""
    settings = resolve_file_server_settings()
    build_label = resolve_upload_build_label(build_number, fallback_build_label)

    with tempfile.TemporaryDirectory(prefix="clientres_table_") as temp_dir:
        prepared_source_path = prepare_table_upload_source(
            local_platform_dir,
            output_root=output_root,
            staging_dir=Path(temp_dir),
        )
        summary = build_upload_summary(
            prepared_source_path,
            artifact_type="table",
            build_label=build_label,
        )
        log_upload_summary(
            summary,
            log_prefix=log_prefix,
            settings_base_url=settings.base_url,
            config_path=settings.config_path,
        )
        on_uploading, on_uploaded = emit_upload_callbacks(log_prefix)
        results = upload_table(
            prepared_source_path,
            build_number=summary.build_label,
            settings=settings,
            on_uploading=on_uploading,
            on_uploaded=on_uploaded,
        )
        validate_uploaded_artifacts(
            summary,
            results=results,
            settings=settings,
            log_prefix=log_prefix,
        )
        print(f"{log_prefix} uploadedFiles={len(results)}")
        return results