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
from pathlib import Path
import tempfile

from Common.artifact_uploader import (
    UploadedArtifact,
    build_artifact_remote_root,
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
        print(f"{log_prefix} uploadedFiles={len(results)}")
        return results