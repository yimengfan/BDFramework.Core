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
import time
from pathlib import Path
from typing import Sequence

from config.settings import SETTINGS


SCRIPT_DIR = Path(__file__).resolve().parent
LOG_DIR = SCRIPT_DIR / SETTINGS["log_dir_name"]
LOG_DIR.mkdir(parents=True, exist_ok=True)
DEFAULT_CI_LOG_ROOT_NAME = "CILog"
UNITY_LOG_POLL_INTERVAL_SECONDS = 1.0
UNITY_SUCCESS_LOG_MARKERS = (
    "===>5.构建结束",
    "Build Success :",
    "打包Exe成功~",
    "build finished successfully",
)
UNITY_FAILURE_LOG_MARKERS = (
    "打包失败!",
    "【CI】构建母包失败",
    "Package not exsit",
    "Scripts have compiler errors.",
    "Build Finished, Result: Failure.",
    "executeMethod method",
)
UNITY_EXIT_LOG_MARKERS = (
    "Application will terminate with return code",
    "Exiting without the bug reporter.",
    "BatchMode quit successfully invoked",
)


class UnityBatchModeError(RuntimeError):
    """Unity BatchMode 调用相关错误。"""


class UnityLogStreamingState:
    """维护 Unity 日志增量读取状态。"""

    def __init__(self) -> None:
        self.offset = 0
        self.partial_line = ""
        self.last_activity_at = time.monotonic()
        self.saw_completion_marker = False
        self.completed_successfully: bool | None = None


def safe_console_print(message: object = "") -> None:
    """向当前控制台输出文本，遇到宿主编码不兼容时自动降级替换字符。"""
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


def configure_live_console_output() -> None:
    """尽量让 CI 下的 stdout/stderr 按行刷新，避免 TeamCity 长时间看不到脚本日志。"""
    for stream_name in ("stdout", "stderr"):
        stream = getattr(sys, stream_name, None)
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            reconfigure(line_buffering=True)


def get_default_project_dir() -> Path:
    """获取默认 Unity 工程目录。

    规则：
    1. 如果 config 中显式配置了 default_project_dir，则优先使用。
    2. 否则从当前脚本目录开始向上查找，找到首个包含 Unity 工程标记目录的祖先目录。
    """
    configured_project_dir = get_project_settings().get("default_dir")
    if configured_project_dir:
        return Path(configured_project_dir).expanduser().resolve()

    markers = get_project_settings().get("markers", [])
    search_candidates = (SCRIPT_DIR, *SCRIPT_DIR.parents)
    for candidate_dir in search_candidates:
        if all((candidate_dir / marker).exists() for marker in markers):
            return candidate_dir

    raise UnityBatchModeError(
        "Failed to infer Unity project directory from script location. "
        f"script_dir={SCRIPT_DIR}, required_markers={markers}"
    )


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


def dedupe_candidates(candidates: Sequence[str]) -> list[str]:
    """按声明顺序去重候选路径。"""
    deduped_candidates: list[str] = []
    seen_candidates: set[str] = set()
    for candidate in candidates:
        if candidate in seen_candidates:
            continue
        seen_candidates.add(candidate)
        deduped_candidates.append(candidate)
    return deduped_candidates


def build_windows_unity_candidates(
    unity_version: str,
    invalid_env_unity_path: str | None = None,
) -> list[str]:
    """构建 Windows 平台的 Unity 路径候选。"""
    root_candidates: list[Path] = []

    for env_name in ("ProgramFiles", "ProgramFiles(x86)", "ProgramW6432"):
        env_value = os.environ.get(env_name, "").strip()
        if env_value:
            root_candidates.append(Path(env_value))

    for drive in ("C:\\", "D:\\", "E:\\"):
        root_candidates.append(Path(drive))
        root_candidates.append(Path(drive) / "Program Files")

    if invalid_env_unity_path:
        invalid_path = Path(invalid_env_unity_path)
        for candidate in (invalid_path.parent, *invalid_path.parents):
            if str(candidate) in (invalid_path.anchor, "."):
                continue
            root_candidates.append(candidate)
            root_candidates.append(candidate.parent)

    roots = dedupe_candidates([str(candidate) for candidate in root_candidates if str(candidate)])
    subpath_templates = [
        r"Unity\Hub\Editor\{version}\Editor\Unity.exe",
        r"Unity Hub\Editor\{version}\Editor\Unity.exe",
        r"Unity3d\Hub\Editor\{version}\Editor\Unity.exe",
        r"Unity3d\{version}\Editor\Unity.exe",
        r"Unity\{version}\Editor\Unity.exe",
        r"UnityEditors\{version}\Editor\Unity.exe",
        r"Editor\Unity\{version}\Editor\Unity.exe",
        r"Unity {version}\Editor\Unity.exe",
    ]

    candidates: list[str] = []
    for root in roots:
        normalized_root = root.rstrip("\\/")
        for subpath_template in subpath_templates:
            candidates.append(rf"{normalized_root}\{subpath_template.format(version=unity_version)}")

    return dedupe_candidates(candidates)


def build_unity_candidates(
    host_os: str,
    unity_version: str,
    invalid_env_unity_path: str | None = None,
) -> list[str]:
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
    if host_os == "windows":
        candidates.extend(
            build_windows_unity_candidates(
                unity_version,
                invalid_env_unity_path=invalid_env_unity_path,
            )
        )

    return dedupe_candidates(candidates)


def resolve_unity_executable(
    unity_version: str | None = None,
    *,
    allow_missing: bool = False,
) -> tuple[Path, str]:
    """按优先级解析 Unity 可执行文件路径，并返回实际使用的版本。

    优先级：
    1. 环境变量 UNITY_PATH
    2. --unity-version 指定的版本
    3. config/settings.py 中按宿主系统配置的默认版本
    """
    env_unity_path = os.environ.get("UNITY_PATH", "").strip()
    invalid_env_unity_path: str | None = None
    if env_unity_path:
        unity_path = Path(env_unity_path)
        if unity_path.exists():
            return unity_path, unity_version or "ENV:UNITY_PATH"
        if allow_missing:
            safe_console_print(
                "[UnityBatchMode] UNITY_PATH does not exist, but dry-run allows continuing. "
                f"path={env_unity_path}"
            )
            return unity_path, unity_version or "ENV:UNITY_PATH"
        invalid_env_unity_path = env_unity_path
        safe_console_print(
            "[UnityBatchMode] UNITY_PATH does not exist, continue searching fallback candidates. "
            f"path={env_unity_path}"
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

    candidates = build_unity_candidates(
        host_os,
        selected_unity_version,
        invalid_env_unity_path=invalid_env_unity_path,
    )
    if not candidates:
        raise UnityBatchModeError(
            f"No Unity executable candidates configured for version={selected_unity_version}, host_os={host_os}"
        )

    for candidate in candidates:
        candidate_path = Path(candidate)
        if candidate_path.exists():
            return candidate_path, selected_unity_version

    if allow_missing:
        fallback_candidate = Path(candidates[0])
        safe_console_print(
            "[UnityBatchMode] Unity executable not found on disk, but dry-run allows continuing. "
            f"candidate={fallback_candidate}"
        )
        return fallback_candidate, selected_unity_version

    raise UnityBatchModeError(
        "Unity executable not found. "
        f"Host OS: {host_os}. "
        f"Requested Unity version: {selected_unity_version}. "
        f"Candidates: {candidates}. "
        f"Invalid UNITY_PATH: {invalid_env_unity_path}. "
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

    if os.name != "nt":
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
    raw_value = os.environ.get("CI_LOG_ROOT_NAME", "").strip() or DEFAULT_CI_LOG_ROOT_NAME
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


def extract_log_path_from_command(command: Sequence[str]) -> Path | None:
    """从 Unity 命令行参数中提取 -logFile 对应路径。"""
    for index, value in enumerate(command[:-1]):
        if value == "-logFile":
            return Path(command[index + 1])
    return None


def classify_unity_log_line(line: str, state: UnityLogStreamingState) -> None:
    """根据日志内容识别构建完成/失败标记。"""
    normalized_line = line.casefold()

    if any(marker.casefold() in normalized_line for marker in UNITY_SUCCESS_LOG_MARKERS):
        state.saw_completion_marker = True
        state.completed_successfully = True
        return

    if any(marker.casefold() in normalized_line for marker in UNITY_FAILURE_LOG_MARKERS):
        state.saw_completion_marker = True
        state.completed_successfully = False
        return

    if any(marker.casefold() in normalized_line for marker in UNITY_EXIT_LOG_MARKERS):
        state.saw_completion_marker = True
        if "return code 0" in normalized_line:
            state.completed_successfully = True
        elif "return code" in normalized_line and "return code 0" not in normalized_line:
            state.completed_successfully = False


def emit_unity_log_updates(
    log_path: Path,
    state: UnityLogStreamingState,
    *,
    flush_partial: bool = False,
) -> None:
    """把 Unity -logFile 的新增内容实时输出到当前控制台。"""
    if not log_path.exists():
        return

    try:
        file_size = log_path.stat().st_size
    except OSError:
        return

    if file_size < state.offset:
        state.offset = 0
        state.partial_line = ""

    try:
        with log_path.open("r", encoding="utf-8", errors="replace") as handle:
            handle.seek(state.offset)
            chunk = handle.read()
            state.offset = handle.tell()
    except OSError:
        return

    if not chunk and not flush_partial:
        return

    text = state.partial_line + chunk
    state.partial_line = ""
    emitted_lines: list[str] = []

    for line in text.splitlines(keepends=True):
        if line.endswith(("\n", "\r")):
            emitted_lines.append(line.rstrip("\r\n"))
        elif flush_partial:
            emitted_lines.append(line.rstrip("\r\n"))
        else:
            state.partial_line = line

    if not emitted_lines:
        return

    state.last_activity_at = time.monotonic()
    for line in emitted_lines:
        safe_console_print(line)
        classify_unity_log_line(line, state)


def run_batchmode(command: Sequence[str], *, dry_run: bool = False) -> int:
    """执行 Unity BatchMode。

    dry_run=True 时只打印命令，不真正执行。
    这个模式适合：
    - 本地联调
    - CI 脚本自检
    - 排查参数拼接问题
    """
    safe_console_print("[UnityBatchMode] command=")
    safe_console_print(" ".join(quote_argument(arg) for arg in command))

    if dry_run:
        safe_console_print("[UnityBatchMode] dry-run enabled, skip Unity execution.")
        return 0

    log_path = extract_log_path_from_command(command)
    if log_path is None:
        completed = subprocess.run(command)
        return completed.returncode

    safe_console_print(f"[UnityBatchMode] streaming log file: {log_path}")
    process = subprocess.Popen(command)
    state = UnityLogStreamingState()

    while True:
        emit_unity_log_updates(log_path, state)

        return_code = process.poll()
        if return_code is not None:
            emit_unity_log_updates(log_path, state, flush_partial=True)
            return return_code

        time.sleep(UNITY_LOG_POLL_INTERVAL_SECONDS)


def read_log_tail(log_path: Path, *, max_lines: int = 120) -> str:
    """读取 Unity 日志尾部，方便在 CI 失败时直接输出关键信息。"""
    if not log_path.exists():
        return f"[UnityBatchMode] log file does not exist: {log_path}"

    try:
        raw_lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError as exc:
        return f"[UnityBatchMode] failed to read log file: {log_path}. error={exc}"

    if max_lines <= 0:
        tail_lines = raw_lines
    else:
        tail_lines = raw_lines[-max_lines:]

    header = f"[UnityBatchMode] log tail ({len(tail_lines)}/{len(raw_lines)} lines) from {log_path}"
    return "\n".join([header, *tail_lines])


def quote_argument(value: str) -> str:
    """输出适合日志展示的命令参数。"""
    if any(ch in value for ch in (" ", "\t", "\n", '"')):
        escaped = value.replace('"', '\\"')
        return f'"{escaped}"'
    return value





