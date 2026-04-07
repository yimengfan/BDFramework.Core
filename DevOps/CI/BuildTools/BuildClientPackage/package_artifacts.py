from __future__ import annotations

"""BuildClientPackage 输出目录与母包上传辅助。

职责边界：
1. 这里只处理母包默认输出目录的准备、扫描和上传。
2. Unity 可执行文件查找、BatchMode 命令拼接、日志流式输出仍由 unity3d_batchmode.py 负责。
3. 业务入口仍然是各平台 build_xxx.py，避免重新堆回 generic common.py。
"""

from collections.abc import Callable
import shutil
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
import zipfile


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
IOS_INFO_PLIST_FILENAME = "Info.plist"
WINDOWS_LAUNCHER_FILENAME = "Launcher.exe"
WINDOWS_DO_NOT_PUBLISH_DIRNAME = "不要发布"
WINDOWS_BURST_DO_NOT_SHIP_SUFFIX = "_BurstDebugInformation_DoNotShip"


@dataclass(frozen=True)
class PublishPackageSummary:
    """描述一次母包上传前的本地输出概况。"""

    source_dir: Path
    upload_source_path: Path
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


def _is_relative_to(path: Path, parent: Path) -> bool:
    """兼容 Python 3.9 的 Path.is_relative_to 判定。"""
    try:
        path.relative_to(parent)
    except ValueError:
        return False
    return True


def _find_unique_publish_dir(
    source_dir: Path,
    *,
    description: str,
    predicate: Callable[[Path], bool],
) -> Path:
    """从母包输出目录中定位唯一的目标目录。"""
    if predicate(source_dir):
        return source_dir

    candidates = [
        child_path
        for child_path in sorted(source_dir.iterdir())
        if child_path.is_dir() and predicate(child_path)
    ]
    if not candidates:
        raise UnityBatchModeError(f"{description} directory does not exist under: {source_dir}")
    if len(candidates) > 1:
        candidate_names = ", ".join(candidate.name for candidate in candidates)
        raise UnityBatchModeError(
            f"Multiple {description} directories found under {source_dir}: {candidate_names}"
        )
    return candidates[0]


def find_ios_xcode_project_dir(source_dir: Path) -> Path:
    """定位 iOS 导出的 Xcode 工程目录。"""
    return _find_unique_publish_dir(
        source_dir,
        description="iOS Xcode project",
        predicate=lambda candidate: (candidate / IOS_INFO_PLIST_FILENAME).is_file(),
    )


def find_windows_runtime_dir(source_dir: Path) -> Path:
    """定位 Windows 可运行母包目录。"""
    return _find_unique_publish_dir(
        source_dir,
        description="Windows runtime",
        predicate=lambda candidate: (candidate / WINDOWS_LAUNCHER_FILENAME).is_file(),
    )


def find_windows_do_not_publish_dirs(runtime_dir: Path) -> list[Path]:
    """收集 Windows 输出中需要单独归档的“不要发布”目录。"""
    normalized_chinese_dirname = WINDOWS_DO_NOT_PUBLISH_DIRNAME.casefold()
    normalized_burst_suffix = WINDOWS_BURST_DO_NOT_SHIP_SUFFIX.casefold()

    return [
        candidate
        for candidate in sorted(runtime_dir.rglob("*"))
        if candidate.is_dir()
        and (
            candidate.name.casefold() == normalized_chinese_dirname
            or candidate.name.casefold().endswith(normalized_burst_suffix)
        )
    ]


def create_zip_archive(
    zip_path: Path,
    *,
    source_root: Path,
    file_paths: list[Path],
    archive_root: PurePosixPath,
) -> Path:
    """按给定根目录把一组选中文件打成 zip。"""
    members: list[tuple[Path, PurePosixPath]] = []
    for file_path in sorted(file_paths):
        if not file_path.is_file() or file_path.name in SKIPPED_OUTPUT_FILENAMES:
            continue
        relative_path = PurePosixPath(file_path.relative_to(source_root).as_posix())
        members.append((file_path, archive_root / relative_path))

    if not members:
        raise UnityBatchModeError(f"No files found to archive from: {source_root}")

    zip_path.parent.mkdir(parents=True, exist_ok=True)
    if zip_path.exists():
        zip_path.unlink()

    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for local_path, archive_path in members:
            archive.write(local_path, archive_path.as_posix())

    return zip_path


def prepare_publish_package_upload_source(
    platform_key: str,
    *,
    source_dir: Path,
    staging_dir: Path,
) -> Path:
    """根据平台把待上传母包整理成最终上传源目录。"""
    if platform_key == "ios":
        prepared_dir = staging_dir / platform_key
        xcode_project_dir = find_ios_xcode_project_dir(source_dir)
        create_zip_archive(
            prepared_dir / f"{xcode_project_dir.name}.zip",
            source_root=xcode_project_dir,
            file_paths=list_publish_package_files(xcode_project_dir),
            archive_root=PurePosixPath(xcode_project_dir.name),
        )
        return prepared_dir

    if platform_key == "windows":
        prepared_dir = staging_dir / platform_key
        runtime_dir = find_windows_runtime_dir(source_dir)
        do_not_publish_dirs = find_windows_do_not_publish_dirs(runtime_dir)

        runtime_files = [
            file_path
            for file_path in list_publish_package_files(runtime_dir)
            if not any(_is_relative_to(file_path, skipped_dir) for skipped_dir in do_not_publish_dirs)
        ]
        create_zip_archive(
            prepared_dir / f"{runtime_dir.name}.zip",
            source_root=runtime_dir,
            file_paths=runtime_files,
            archive_root=PurePosixPath(runtime_dir.name),
        )

        for skipped_dir in do_not_publish_dirs:
            zip_suffix = skipped_dir.relative_to(runtime_dir).as_posix().replace("/", "_")
            create_zip_archive(
                prepared_dir / f"{runtime_dir.name}_{zip_suffix}.zip",
                source_root=runtime_dir,
                file_paths=list_publish_package_files(skipped_dir),
                archive_root=PurePosixPath(runtime_dir.name),
            )

        return prepared_dir

    return source_dir


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
    upload_source_path: Path | None = None,
) -> PublishPackageSummary:
    """扫描本地母包输出目录，并生成上传摘要。"""
    source_dir = get_publish_package_dir(platform_key, project_dir=project_dir)
    resolved_upload_source_path = upload_source_path or source_dir
    files = list_publish_package_files(resolved_upload_source_path)
    build_label = resolve_upload_build_label(build_number, client_version)
    remote_root = build_artifact_remote_root(
        "client-package",
        platform=platform_key,
        build_number=build_label,
    )
    total_bytes = sum(file_path.stat().st_size for file_path in files)
    return PublishPackageSummary(
        source_dir=source_dir,
        upload_source_path=resolved_upload_source_path,
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
    source_dir = get_publish_package_dir(platform_key, project_dir=project_dir)
    settings = resolve_file_server_settings()

    with tempfile.TemporaryDirectory(prefix=f"buildclientpackage_{platform_key}_") as temp_dir:
        prepared_source_path = prepare_publish_package_upload_source(
            platform_key,
            source_dir=source_dir,
            staging_dir=Path(temp_dir),
        )
        summary = build_publish_package_summary(
            platform_key,
            project_dir=project_dir,
            build_number=build_number,
            client_version=client_version,
            upload_source_path=prepared_source_path,
        )

        print(f"{log_prefix} uploadSourceDir={summary.source_dir}")
        if summary.upload_source_path != summary.source_dir:
            print(f"{log_prefix} uploadPreparedSource={summary.upload_source_path}")
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
            summary.upload_source_path,
            platform=platform_key,
            build_number=summary.build_label,
            settings=settings,
            on_uploading=on_uploading,
            on_uploaded=on_uploaded,
        )
        print(f"{log_prefix} uploadedFiles={len(results)}")
        return results