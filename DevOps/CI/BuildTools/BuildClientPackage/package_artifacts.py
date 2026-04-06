from __future__ import annotations

"""BuildClientPackage 输出目录与母包上传辅助。

职责边界：
1. 这里只处理母包默认输出目录的准备、扫描和上传。
2. Unity 可执行文件查找、BatchMode 命令拼接、日志流式输出仍由 unity3d_batchmode.py 负责。
3. 业务入口仍然是各平台 build_xxx.py，避免重新堆回 generic common.py。
"""

import shutil
import sys
from dataclasses import dataclass
from pathlib import Path


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.artifact_uploader import (  # noqa: E402
    UploadedArtifact,
    build_artifact_remote_root,
    resolve_file_server_settings,
    upload_client_package,
)
from unity3d_batchmode import UnityBatchModeError  # noqa: E402


SKIPPED_OUTPUT_FILENAMES = {".DS_Store"}


@dataclass(frozen=True)
class PublishPackageSummary:
    """描述一次母包上传前的本地输出概况。"""

    source_dir: Path
    build_label: str
    remote_root: str
    file_count: int
    total_bytes: int


def get_publish_package_dir(platform_key: str, *, project_dir: Path) -> Path:
    """返回 Unity 母包默认输出目录。"""
    return project_dir / "DevOps" / "PublishPackages" / platform_key


def clear_publish_package_dir(platform_key: str, *, project_dir: Path) -> Path:
    """构建前清空母包输出目录，避免旧文件污染本次结果。"""
    output_dir = get_publish_package_dir(platform_key, project_dir=project_dir)
    if output_dir.exists():
        if output_dir.is_dir():
            shutil.rmtree(output_dir)
        else:
            output_dir.unlink()

    output_dir.mkdir(parents=True, exist_ok=True)
    return output_dir


def list_publish_package_files(source_dir: Path) -> list[Path]:
    """按稳定顺序收集待上传的母包文件。"""
    if not source_dir.exists():
        raise UnityBatchModeError(f"Client package output directory does not exist: {source_dir}")
    if not source_dir.is_dir():
        raise UnityBatchModeError(f"Client package output path is not a directory: {source_dir}")

    files = [
        file_path
        for file_path in sorted(source_dir.rglob("*"))
        if file_path.is_file() and file_path.name not in SKIPPED_OUTPUT_FILENAMES
    ]
    if not files:
        raise UnityBatchModeError(f"Client package output directory is empty: {source_dir}")
    return files


def resolve_upload_build_label(build_number: str | None, client_version: str) -> str:
    """解析上传目录版本段。

    CI 下优先使用 buildNumber，保证同一条流水线的远端目录稳定；
    本地手工触发如果没有 buildNumber，则退回到 clientVersion。
    """
    normalized_build_number = (build_number or "").strip()
    if normalized_build_number:
        return normalized_build_number

    normalized_client_version = client_version.strip()
    if normalized_client_version:
        return normalized_client_version

    raise UnityBatchModeError("Upload build label is empty. Provide buildNumber or clientVersion.")


def build_publish_package_summary(
    platform_key: str,
    *,
    project_dir: Path,
    build_number: str | None,
    client_version: str,
) -> PublishPackageSummary:
    """扫描本地母包输出目录，并生成上传摘要。"""
    source_dir = get_publish_package_dir(platform_key, project_dir=project_dir)
    files = list_publish_package_files(source_dir)
    build_label = resolve_upload_build_label(build_number, client_version)
    remote_root = build_artifact_remote_root(
        "client-package",
        platform=platform_key,
        build_number=build_label,
    )
    total_bytes = sum(file_path.stat().st_size for file_path in files)
    return PublishPackageSummary(
        source_dir=source_dir,
        build_label=build_label,
        remote_root=remote_root,
        file_count=len(files),
        total_bytes=total_bytes,
    )


def upload_publish_package(
    platform_key: str,
    *,
    project_dir: Path,
    build_number: str | None,
    client_version: str,
    log_prefix: str,
) -> list[UploadedArtifact]:
    """上传 Unity 默认输出目录下的母包，并输出适合 CI 观察的进度日志。"""
    summary = build_publish_package_summary(
        platform_key,
        project_dir=project_dir,
        build_number=build_number,
        client_version=client_version,
    )
    settings = resolve_file_server_settings()

    print(f"{log_prefix} uploadSourceDir={summary.source_dir}")
    print(f"{log_prefix} uploadBuildLabel={summary.build_label}")
    print(f"{log_prefix} uploadRemoteRoot={summary.remote_root}")
    print(f"{log_prefix} uploadServerUrl={settings.base_url}")
    if settings.config_path is not None:
        print(f"{log_prefix} uploadConfig={settings.config_path}")
    print(f"{log_prefix} uploadFileCount={summary.file_count}")
    print(f"{log_prefix} uploadTotalBytes={summary.total_bytes}")

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

    results = upload_client_package(
        summary.source_dir,
        platform=platform_key,
        build_number=summary.build_label,
        settings=settings,
        on_uploading=on_uploading,
        on_uploaded=on_uploaded,
    )
    print(f"{log_prefix} uploadedFiles={len(results)}")
    return results