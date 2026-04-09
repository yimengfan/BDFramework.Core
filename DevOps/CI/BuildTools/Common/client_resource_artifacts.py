from __future__ import annotations

"""Shared helpers for ClientRes Code / Assetbundle / Table CI scripts.

职责边界：
1. 这里只处理隔离输出目录、产物筛选、临时整理与上传。
2. Unity BatchMode 参数解析、日志和进程控制统一复用 BuildClientPackage 的共享 facade。
3. TeamCity DSL 只调度 Python 入口脚本，真正的产物组织与上传规则放在这里收敛。
"""

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

    copy_path(
        ensure_existing_path(platform_dir / SCRIPT_DIRNAME, description="ClientRes code script directory"),
        prepared_dir / SCRIPT_DIRNAME,
    )
    copy_path(
        ensure_existing_path(
            platform_dir / PACKAGE_BUILD_INFO_FILENAME,
            description="ClientRes code package_build.info",
        ),
        prepared_dir / PACKAGE_BUILD_INFO_FILENAME,
    )
    copy_path(
        ensure_existing_path(platform_dir / ASSETS_INFO_FILENAME, description="ClientRes code assets.info"),
        prepared_dir / ASSETS_INFO_FILENAME,
    )

    optional_subpack = platform_dir / ASSETS_SUBPACK_INFO_FILENAME
    if optional_subpack.exists():
        copy_path(optional_subpack, prepared_dir / ASSETS_SUBPACK_INFO_FILENAME)

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

    copy_path(
        ensure_existing_path(
            platform_dir / ART_ASSETS_DIRNAME,
            description="ClientRes assetbundle art_assets directory",
        ),
        prepared_dir / ART_ASSETS_DIRNAME,
    )
    copy_path(
        ensure_existing_path(
            platform_dir / PACKAGE_BUILD_INFO_FILENAME,
            description="ClientRes assetbundle package_build.info",
        ),
        prepared_dir / PACKAGE_BUILD_INFO_FILENAME,
    )
    copy_path(
        ensure_existing_path(
            platform_dir / ASSETS_INFO_FILENAME,
            description="ClientRes assetbundle assets.info",
        ),
        prepared_dir / ASSETS_INFO_FILENAME,
    )

    optional_subpack = platform_dir / ASSETS_SUBPACK_INFO_FILENAME
    if optional_subpack.exists():
        copy_path(optional_subpack, prepared_dir / ASSETS_SUBPACK_INFO_FILENAME)

    return prepared_dir


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
        ensure_existing_path(platform_dir / LOCAL_DB_FILENAME, description="ClientRes table local.db"),
        prepared_dir / CLIENT_DB_FILENAME,
    )
    copy_path(
        ensure_existing_path(
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