from __future__ import annotations

"""BuildClientPackage Unity batchmode 公共导出层。

维护约束：
1. 这个文件只保留稳定导出接口和职责说明，不再承载实现细节。
2. 路径与宿主机差异逻辑放到 _unity_batchmode_paths.py。
3. Unity 执行与日志镜像逻辑放到 _unity_batchmode_logs.py。
4. 纯共享 helper 放到 _unity_batchmode_shared.py。

如果某段逻辑需要知道平台业务流程、上传策略或阶段编排，它就不应该进入这里。
"""

from _unity_batchmode_logs import (
    UnityLogStreamingState,
    emit_unity_log_updates,
    read_log_tail,
    run_batchmode,
)
from _unity_batchmode_paths import (
    build_unity_candidates,
    build_windows_unity_candidates,
    dedupe_candidates,
    get_default_project_dir,
    get_default_unity_version,
    get_unity_path_groups,
    normalize_named_paths,
    resolve_project_dir,
    resolve_unity_executable,
)
from _unity_batchmode_shared import (
    UnityBatchModeError,
    build_batchmode_command,
    cleanup_stale_hybridclr_outputs,
    compose_client_version,
    configure_live_console_output,
    detect_host_os,
    ensure_platform_allowed,
    extract_log_path_from_command,
    extract_project_dir_from_command,
    get_ci_log_root_name,
    get_disk_root,
    get_execute_method,
    get_log_path,
    get_platform_settings,
    get_project_settings,
    get_unity_settings,
    quote_argument,
    resolve_build_metadata,
    safe_console_print,
    sanitize_for_filename,
)


__all__ = [
    "UnityBatchModeError",
    "UnityLogStreamingState",
    "build_batchmode_command",
    "build_unity_candidates",
    "build_windows_unity_candidates",
    "cleanup_stale_hybridclr_outputs",
    "compose_client_version",
    "configure_live_console_output",
    "dedupe_candidates",
    "detect_host_os",
    "emit_unity_log_updates",
    "ensure_platform_allowed",
    "extract_log_path_from_command",
    "extract_project_dir_from_command",
    "get_ci_log_root_name",
    "get_default_project_dir",
    "get_default_unity_version",
    "get_disk_root",
    "get_execute_method",
    "get_log_path",
    "get_platform_settings",
    "get_project_settings",
    "get_unity_path_groups",
    "get_unity_settings",
    "normalize_named_paths",
    "quote_argument",
    "read_log_tail",
    "resolve_build_metadata",
    "resolve_project_dir",
    "resolve_unity_executable",
    "run_batchmode",
    "safe_console_print",
    "sanitize_for_filename",
]





