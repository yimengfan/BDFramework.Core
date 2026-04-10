"""BuildClientPackage Unity batchmode shared helpers.

这里放不依赖具体平台流程的通用能力：
1. 配置读取与公共异常。
2. CI 元数据、日志目录与命令行参数拼装。
3. 供路径解析和日志执行模块复用的纯辅助函数。

平台主流程仍必须留在 build_android.py / build_ios.py / build_windows.py。
"""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path
from typing import Sequence

from config.settings import SETTINGS


SCRIPT_DIR = Path(__file__).resolve().parent
LOG_DIR = SCRIPT_DIR / SETTINGS["log_dir_name"]
LOG_DIR.mkdir(parents=True, exist_ok=True)
DEFAULT_CI_LOG_ROOT_NAME = "CILog"


class UnityBatchModeError(RuntimeError):
    """Unity BatchMode 调用相关错误。"""


def safe_console_print(message: object = "") -> None:
    """向当前控制台输出文本，宿主编码不兼容时自动降级替换字符。"""
    text = str(message)
    encoding = getattr(sys.stdout, "encoding", None) or "utf-8"
    data = (text + "\n").encode(encoding, errors="replace")
    stdout_buffer = getattr(sys.stdout, "buffer", None)
    if stdout_buffer is not None:
        stdout_buffer.write(data)
        stdout_buffer.flush()
        return

    sys.stdout.write(data.decode(encoding, errors="replace"))
    sys.stdout.flush()


def get_project_settings() -> dict:
    """获取工程相关配置。"""
    return SETTINGS["project"]


def get_unity_settings() -> dict:
    """获取 Unity 相关配置。"""
    return SETTINGS["unity"]


def get_platform_settings(platform_key: str) -> dict:
    """获取目标平台相关配置。"""
    platforms = SETTINGS["platforms"]
    if platform_key not in platforms:
        raise UnityBatchModeError(f"Unsupported platform key: {platform_key}")
    return platforms[platform_key]


def detect_host_os() -> str:
    """识别当前 CI 宿主机系统。"""
    if sys.platform.startswith("darwin"):
        return "mac"
    if sys.platform.startswith("win"):
        return "windows"
    if sys.platform.startswith("linux"):
        return "linux"

    raise UnityBatchModeError(f"Unsupported host OS: {sys.platform}")


def configure_live_console_output() -> None:
    """尽量让 CI 下的 stdout/stderr 按行刷新，避免长时间没有日志。"""
    for stream_name in ("stdout", "stderr"):
        stream = getattr(sys, stream_name, None)
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            reconfigure(line_buffering=True)


def ensure_platform_allowed(platform_key: str) -> None:
    """检查当前宿主环境是否允许执行指定平台流程。"""
    host_os = detect_host_os()
    rule = get_platform_settings(platform_key)
    allowed_hosts = rule["allowed_hosts"]
    if host_os not in allowed_hosts:
        raise UnityBatchModeError(
            f"Platform '{platform_key}' is not allowed on host '{host_os}'. "
            f"Allowed hosts: {allowed_hosts}"
        )


def sanitize_for_filename(raw_value: str) -> str:
    """把版本号、构建名等字符串转成安全文件名片段。"""
    if not raw_value:
        return "unknown"

    sanitized = raw_value.strip()
    for bad_char in ('/', '\\', ':', '*', '?', '"', '<', '>', '|', ' '):
        sanitized = sanitized.replace(bad_char, '_')
    return sanitized or "unknown"


def resolve_build_metadata(
    build_name: str | None,
    build_number: str | None,
) -> tuple[str | None, str | None]:
    """解析当前 CI 构建名和构建号。"""
    resolved_build_name = (build_name or "").strip() or None
    resolved_build_number = (build_number or "").strip() or None

    if resolved_build_name is None:
        for env_name in (
            "CI_BUILD_NAME",
            "BUILD_NAME",
            "TEAMCITY_BUILDCONF_NAME",
            "JOB_NAME",
            "TC_BUILD_NAME",
        ):
            env_value = os.environ.get(env_name, "").strip()
            if env_value:
                resolved_build_name = env_value
                break

    if resolved_build_number is None:
        for env_name in (
            "CI_BUILD_NUMBER",
            "BUILD_NUMBER",
            "TEAMCITY_BUILD_NUMBER",
            "GITHUB_RUN_NUMBER",
            "TC_BUILD_NUMBER",
        ):
            env_value = os.environ.get(env_name, "").strip()
            if env_value:
                resolved_build_number = env_value
                break

    return resolved_build_name, resolved_build_number


def compose_client_version(client_version: str, build_number: str | None) -> str:
    """按 CI 构建号组装 Unity 使用的 clientVersion。"""
    normalized = client_version.strip()
    if not normalized:
        raise UnityBatchModeError("clientVersion is empty")

    if any(ch in normalized for ch in ('\n', '\r', '\t')):
        raise UnityBatchModeError(
            f"clientVersion contains unsupported whitespace characters: {normalized!r}"
        )

    normalized_build_number = (build_number or "").strip()
    if not normalized_build_number:
        return normalized

    if any(ch in normalized_build_number for ch in ('\n', '\r', '\t', ' ')):
        raise UnityBatchModeError(
            f"buildNumber contains unsupported whitespace characters: {normalized_build_number!r}"
        )

    version_parts = [segment.strip() for segment in normalized.split(".") if segment.strip()]
    if len(version_parts) < 2:
        raise UnityBatchModeError(
            "clientVersion must provide at least major.minor when build number is enabled. "
            f"Received: {normalized!r}"
        )

    return f"{version_parts[0]}.{version_parts[1]}.{normalized_build_number}"


def get_disk_root(reference_dir: Path) -> Path:
    """获取当前磁盘根目录。"""
    resolved_reference_dir = reference_dir.resolve()

    if os.name == "nt":
        return Path(resolved_reference_dir.anchor or os.path.abspath(os.sep))

    completed = subprocess.run(
        ["df", "-P", str(resolved_reference_dir)],
        check=False,
        capture_output=True,
        text=True,
    )
    if completed.returncode == 0:
        output_lines = [line.strip() for line in completed.stdout.splitlines() if line.strip()]
        if len(output_lines) >= 2:
            mount_point = output_lines[1].rsplit(maxsplit=1)[-1]
            if mount_point:
                return Path(mount_point)

    for candidate in (resolved_reference_dir, *resolved_reference_dir.parents):
        if candidate.is_mount():
            return candidate

    anchor = resolved_reference_dir.anchor or os.path.abspath(os.sep)
    return Path(anchor)


def get_ci_log_root_name() -> str:
    """返回共享 CI 日志根目录名，允许由外部 CI 覆盖。"""
    raw_value = os.environ.get("CI_LOG_ROOT_NAME", "").strip()
    if not raw_value and os.environ.get("TEAMCITY_VERSION", "").strip():
        raw_value = "TCLog"
    if not raw_value:
        raw_value = DEFAULT_CI_LOG_ROOT_NAME
    return sanitize_for_filename(raw_value)


def get_log_path(
    platform_key: str,
    client_version: str,
    *,
    project_dir: Path,
    build_name: str | None,
    build_number: str | None,
) -> Path:
    """生成该次 CI 构建的日志文件路径。"""
    safe_version = sanitize_for_filename(client_version)
    resolved_build_name, resolved_build_number = resolve_build_metadata(
        build_name,
        build_number,
    )

    # 有 CI 元数据时，把日志写到共享磁盘根目录，方便 TeamCity 跨工作区收集。
    if resolved_build_name and resolved_build_number:
        log_dir = (
            get_disk_root(project_dir)
            / get_ci_log_root_name()
            / sanitize_for_filename(resolved_build_name)
            / sanitize_for_filename(resolved_build_number)
        )
    else:
        log_dir = LOG_DIR

    try:
        log_dir.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        fallback_log_dir = LOG_DIR
        fallback_log_dir.mkdir(parents=True, exist_ok=True)
        safe_console_print(
            "[UnityBatchMode] failed to prepare CI log directory, "
            f"fallback to local logs. target={log_dir}, error={exc}"
        )
        log_dir = fallback_log_dir

    return log_dir / f"{platform_key}_{safe_version}.log"


def get_execute_method(platform_key: str) -> str:
    """读取平台对应的 Unity C# 入口。"""
    return get_platform_settings(platform_key)["method"]


def build_batchmode_command(
    *,
    unity_path: Path,
    project_dir: Path,
    execute_method: str,
    client_version: str,
    log_path: Path,
) -> list[str]:
    """拼接 Unity BatchMode 命令。"""
    return [
        str(unity_path),
        "-batchmode",
        "-projectPath",
        str(project_dir),
        "-executeMethod",
        execute_method,
        "-clientVersion",
        client_version,
        "-logFile",
        str(log_path),
        "-quit",
    ]


def extract_log_path_from_command(command: Sequence[str]) -> Path | None:
    """从 Unity 命令行参数中提取 -logFile 对应路径。"""
    for index, value in enumerate(command[:-1]):
        if value == "-logFile":
            return Path(command[index + 1])
    return None


def extract_project_dir_from_command(command: Sequence[str]) -> Path | None:
    """从 Unity 命令行参数中提取 -projectPath 对应目录。"""
    for index, value in enumerate(command[:-1]):
        if value == "-projectPath":
            return Path(command[index + 1])
    return None


def cleanup_stale_hybridclr_outputs(project_dir: Path) -> tuple[Path, ...]:
    """在启动 Unity 前清理旧的 HybridCLR 生成物，避免重复脚本编译。"""
    legacy_dir = project_dir / "Assets" / "HybridCLRGenerate"
    removed_paths: list[Path] = []

    for relative_path in ("AOTGenericReferences.cs", "link.xml"):
        legacy_path = legacy_dir / relative_path
        if legacy_path.exists():
            legacy_path.unlink()
            removed_paths.append(legacy_path)

        legacy_meta_path = Path(f"{legacy_path}.meta")
        if legacy_meta_path.exists():
            legacy_meta_path.unlink()
            removed_paths.append(legacy_meta_path)

    if legacy_dir.exists():
        try:
            legacy_dir.rmdir()
        except OSError:
            pass

    return tuple(removed_paths)


def quote_argument(value: str) -> str:
    """输出适合日志展示的命令参数。"""
    if any(ch in value for ch in (" ", "\t", "\n", '"')):
        escaped = value.replace('"', '\\"')
        return f'"{escaped}"'
    return value