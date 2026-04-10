"""BuildClientPackage Unity 与工程路径解析 helpers。

宿主机差异、Unity 版本候选和工程目录推导是最容易继续膨胀的部分，
所以单独拆到这里维护。新增路径规则时，只改本模块，不要把分支再塞回门面文件。
"""

from __future__ import annotations

import os
from pathlib import Path
from typing import Sequence

from _unity_batchmode_shared import (
    SCRIPT_DIR,
    UnityBatchModeError,
    detect_host_os,
    get_project_settings,
    get_unity_settings,
    safe_console_print,
)


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


def get_default_project_dir() -> Path:
    """获取默认 Unity 工程目录。"""
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
    """解析并校验 Unity 工程目录。"""
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


def get_default_unity_version() -> str:
    """获取默认 Unity 版本。"""
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
    """按优先级解析 Unity 可执行文件路径，并返回实际使用的版本。"""
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
    selected_unity_version = unity_version.strip() if unity_version else get_default_unity_version()

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