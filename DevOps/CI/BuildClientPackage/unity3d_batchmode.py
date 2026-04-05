from __future__ import annotations

"""
Unity BatchMode 辅助模块。

设计原则：
1. 这里只处理“Unity 命令行调用”相关的共通能力。
2. 不负责具体业务流程，业务流程仍由 build_android.py / build_ios.py / build_windows.py 主导。
3. 尽量多做边界检查，并输出清晰日志，方便在 CI 日志中快速定位问题。
"""

import os
import subprocess
import sys
from pathlib import Path
from typing import Sequence

from config.settings import SETTINGS


SCRIPT_DIR = Path(__file__).resolve().parent
LOG_DIR = SCRIPT_DIR / SETTINGS["log_dir_name"]
LOG_DIR.mkdir(parents=True, exist_ok=True)


class UnityBatchModeError(RuntimeError):
    """Unity BatchMode 调用相关错误。"""


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


def get_unity_path_groups(host_os: str) -> dict:
    """获取当前宿主机对应的 Unity 路径分组配置。"""
    unity_paths = get_unity_settings().get("paths", {})
    host_groups = unity_paths.get(host_os)
    if not isinstance(host_groups, dict):
        raise UnityBatchModeError(
            f"Invalid Unity path config for host_os={host_os}. Expect a dict at SETTINGS['unity']['paths'][host_os]"
        )
    return host_groups


def normalize_named_paths(
    *,
    host_os: str,
    group_name: str,
    raw_group: object,
    unity_version: str,
) -> list[str]:
    """把 key-value 形式的路径配置转换成有序候选路径列表。"""
    if raw_group is None:
        return []

    if not isinstance(raw_group, dict):
        raise UnityBatchModeError(
            f"Invalid Unity path group: host_os={host_os}, group={group_name}. Expect dict[name, path]"
        )

    candidates: list[str] = []
    for path_name, raw_path in raw_group.items():
        if not isinstance(raw_path, str) or not raw_path.strip():
            raise UnityBatchModeError(
                f"Invalid Unity path entry: host_os={host_os}, group={group_name}, name={path_name}, value={raw_path!r}"
            )

        normalized_path = raw_path.strip()
        if group_name == "versioned":
            if "{version}" not in normalized_path:
                raise UnityBatchModeError(
                    f"Versioned Unity path must contain '{{version}}': host_os={host_os}, name={path_name}, path={normalized_path}"
                )
            normalized_path = normalized_path.format(version=unity_version)

        candidates.append(normalized_path)

    return candidates


def detect_host_os() -> str:
    """识别当前 CI 宿主机系统。

    返回值限定为：mac / windows / linux
    """
    if sys.platform.startswith("darwin"):
        return "mac"
    if sys.platform.startswith("win"):
        return "windows"
    if sys.platform.startswith("linux"):
        return "linux"

    raise UnityBatchModeError(f"Unsupported host OS: {sys.platform}")


def get_default_project_dir() -> Path:
    """获取默认 Unity 工程目录。

    规则：
    1. 如果 config 中显式配置了 default_project_dir，则优先使用。
    2. 否则根据当前脚本目录自动推导工程根目录。
    """
    configured_project_dir = get_project_settings().get("default_dir")
    if configured_project_dir:
        return Path(configured_project_dir).expanduser().resolve()

    return SCRIPT_DIR.parents[2]


def resolve_project_dir(project_dir: str | None) -> Path:
    """解析并校验 Unity 工程目录。

    支持调用方通过 --project-dir 传入工程目录。
    若未传入，则使用配置或脚本相对路径推导的默认工程目录。
    """
    candidate = project_dir.strip() if project_dir else ""
    resolved_project_dir = (
        Path(candidate).expanduser().resolve() if candidate else get_default_project_dir()
    )

    if not resolved_project_dir.exists():
        raise UnityBatchModeError(
            f"Unity project directory does not exist: {resolved_project_dir}"
        )

    if not resolved_project_dir.is_dir():
        raise UnityBatchModeError(
            f"Unity project path is not a directory: {resolved_project_dir}"
        )

    markers = get_project_settings().get("markers", [])
    missing_markers = [
        marker for marker in markers if not (resolved_project_dir / marker).exists()
    ]
    if missing_markers:
        raise UnityBatchModeError(
            "Unity project directory validation failed. "
            f"Missing markers: {missing_markers}. project_dir={resolved_project_dir}"
        )

    return resolved_project_dir


def get_default_unity_version(host_os: str) -> str:
    """获取默认 Unity 版本。

    为了降低配置维护成本，默认版本不再按宿主系统拆开维护，统一只有一个默认值。
    如果某台 CI 机器需要特殊版本，优先通过 --unity-version 或 UNITY_PATH 覆盖。
    """
    unity_version = get_unity_settings().get("default_version")
    if not unity_version:
        raise UnityBatchModeError("No default Unity version configured")
    return unity_version


def build_unity_candidates(host_os: str, unity_version: str) -> list[str]:
    """根据宿主系统和 Unity 版本生成候选 Unity 可执行路径。"""
    host_groups = get_unity_path_groups(host_os)
    candidates = normalize_named_paths(
        host_os=host_os,
        group_name="versioned",
        raw_group=host_groups.get("versioned"),
        unity_version=unity_version,
    )
    candidates.extend(
        normalize_named_paths(
            host_os=host_os,
            group_name="fallback",
            raw_group=host_groups.get("fallback"),
            unity_version=unity_version,
        )
    )
    return candidates


def resolve_unity_executable(unity_version: str | None = None) -> tuple[Path, str]:
    """按优先级解析 Unity 可执行文件路径，并返回实际使用的版本。

    优先级：
    1. 环境变量 UNITY_PATH
    2. --unity-version 指定的版本
    3. config/settings.py 中按宿主系统配置的默认版本
    """
    env_unity_path = os.environ.get("UNITY_PATH", "").strip()
    if env_unity_path:
        unity_path = Path(env_unity_path)
        if unity_path.exists():
            return unity_path, unity_version or "ENV:UNITY_PATH"
        raise UnityBatchModeError(
            f"UNITY_PATH is set but does not exist: {env_unity_path}"
        )

    host_os = detect_host_os()
    selected_unity_version = unity_version.strip() if unity_version else get_default_unity_version(host_os)

    unity_settings = get_unity_settings()
    supported_versions = unity_settings.get("supported_versions", [])
    if selected_unity_version not in supported_versions:
        raise UnityBatchModeError(
            f"Unsupported Unity version: {selected_unity_version}. "
            f"Supported versions: {supported_versions}"
        )

    candidates = build_unity_candidates(host_os, selected_unity_version)
    if not candidates:
        raise UnityBatchModeError(
            f"No Unity executable candidates configured for version={selected_unity_version}, host_os={host_os}"
        )

    for candidate in candidates:
        candidate_path = Path(candidate)
        if candidate_path.exists():
            return candidate_path, selected_unity_version

    raise UnityBatchModeError(
        "Unity executable not found. "
        f"Host OS: {host_os}. "
        f"Requested Unity version: {selected_unity_version}. "
        f"Candidates: {candidates}. "
        "Please set env UNITY_PATH or update SETTINGS['unity']['paths'] in config/settings.py"
    )


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
    """把版本号等字符串转成安全文件名片段。"""
    if not raw_value:
        return "unknown"

    sanitized = raw_value.strip()
    for bad_char in ('/', '\\', ':', '*', '?', '"', '<', '>', '|', ' '):
        sanitized = sanitized.replace(bad_char, '_')
    return sanitized or "unknown"


def get_log_path(platform_key: str, client_version: str) -> Path:
    """生成该次构建的日志文件路径。"""
    safe_version = sanitize_for_filename(client_version)
    return LOG_DIR / f"{platform_key}_{safe_version}.log"


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
    """拼接 Unity BatchMode 命令。

    注意：
    - 这里只负责命令拼接，不负责业务参数校验。
    - `clientVersion` 会透传给 C# CI 入口 `PublishPipeLineCI`。
    """
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


def run_batchmode(command: Sequence[str], *, dry_run: bool = False) -> int:
    """执行 Unity BatchMode。

    dry_run=True 时只打印命令，不真正执行。
    这个模式适合：
    - 本地联调
    - CI 脚本自检
    - 排查参数拼接问题
    """
    print("[UnityBatchMode] command=")
    print(" ".join(quote_argument(arg) for arg in command))

    if dry_run:
        print("[UnityBatchMode] dry-run enabled, skip Unity execution.")
        return 0

    completed = subprocess.run(command)
    return completed.returncode


def quote_argument(value: str) -> str:
    """输出适合日志展示的命令参数。"""
    if any(ch in value for ch in (" ", "\t", "\n", '"')):
        escaped = value.replace('"', '\\"')
        return f'"{escaped}"'
    return value





