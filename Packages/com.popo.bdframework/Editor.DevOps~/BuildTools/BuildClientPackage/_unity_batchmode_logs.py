"""BuildClientPackage Unity 执行与日志流式输出 helpers。

这个模块只处理 Unity 进程调用和 -logFile 增量镜像，不承载平台业务判断。
真正的阶段编排、参数校验和上传逻辑仍留在各平台入口脚本里。
"""

from __future__ import annotations

import subprocess
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Sequence

from _unity_batchmode_shared import (
    cleanup_stale_hybridclr_outputs,
    extract_log_path_from_command,
    extract_project_dir_from_command,
    quote_argument,
    safe_console_print,
)


UNITY_LOG_POLL_INTERVAL_SECONDS = 1.0
UNITY_COMPLETION_STALL_GRACE_SECONDS = 30.0
UNITY_FORCE_TERMINATE_WAIT_SECONDS = 10.0
UNITY_FORCE_KILL_WAIT_SECONDS = 5.0
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
    "Batchmode quit successfully invoked",
)


@dataclass
class UnityLogStreamingState:
    """维护 Unity 日志增量读取状态。"""

    offset: int = 0
    partial_line: str = ""
    last_activity_at: float = field(default_factory=time.monotonic)
    saw_completion_marker: bool = False
    completed_successfully: bool | None = None


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
        if "return code 0" in normalized_line or "quit successfully" in normalized_line:
            state.completed_successfully = True
        elif "return code" in normalized_line and "return code 0" not in normalized_line:
            state.completed_successfully = False


def resolve_stuck_process_return_code(
    process_return_code: int | None,
    state: UnityLogStreamingState,
) -> int:
    """根据已识别的完成状态，为被强制结束的 Unity 进程返回稳定结果。"""
    if state.completed_successfully is True:
        return 0
    if process_return_code is not None and process_return_code != 0:
        return process_return_code
    if state.completed_successfully is False:
        return 1
    return process_return_code or 1


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
            # Unity 偶尔会把一行拆成多次写入，先缓冲到下次或进程结束再输出。
            state.partial_line = line

    if not emitted_lines:
        return

    state.last_activity_at = time.monotonic()
    for line in emitted_lines:
        safe_console_print(line)
        classify_unity_log_line(line, state)


def run_batchmode(command: Sequence[str], *, dry_run: bool = False) -> int:
    """执行 Unity BatchMode。"""
    safe_console_print("[UnityBatchMode] command=")
    safe_console_print(" ".join(quote_argument(arg) for arg in command))

    if dry_run:
        safe_console_print("[UnityBatchMode] dry-run enabled, skip Unity execution.")
        return 0

    project_dir = extract_project_dir_from_command(command)
    if project_dir is not None:
        removed_paths = cleanup_stale_hybridclr_outputs(project_dir)
        for removed_path in removed_paths:
            safe_console_print(f"[UnityBatchMode] removed stale HybridCLR output: {removed_path}")

    log_path = extract_log_path_from_command(command)
    if log_path is None:
        completed = subprocess.run(command)
        return completed.returncode

    # Unity 在 batchmode 下最稳定的详细输出入口是 -logFile，这里把新增内容镜像回 CI 控制台。
    safe_console_print(f"[UnityBatchMode] streaming log file: {log_path}")
    process = subprocess.Popen(command)
    state = UnityLogStreamingState()

    while True:
        emit_unity_log_updates(log_path, state)

        return_code = process.poll()
        if return_code is not None:
            emit_unity_log_updates(log_path, state, flush_partial=True)
            return return_code

        if state.saw_completion_marker:
            idle_seconds = time.monotonic() - state.last_activity_at
            if idle_seconds >= UNITY_COMPLETION_STALL_GRACE_SECONDS:
                safe_console_print(
                    "[UnityBatchMode] completion marker detected but Unity process is still running; "
                    f"idle_seconds={idle_seconds:.1f}, attempting graceful termination."
                )
                process.terminate()
                forced_return_code: int | None = None
                try:
                    forced_return_code = process.wait(timeout=UNITY_FORCE_TERMINATE_WAIT_SECONDS)
                except subprocess.TimeoutExpired:
                    safe_console_print(
                        "[UnityBatchMode] Unity process did not exit after terminate; killing it now."
                    )
                    process.kill()
                    try:
                        forced_return_code = process.wait(timeout=UNITY_FORCE_KILL_WAIT_SECONDS)
                    except subprocess.TimeoutExpired:
                        safe_console_print(
                            "[UnityBatchMode] Unity process still did not exit after kill; "
                            "falling back to completion-marker result."
                        )

                emit_unity_log_updates(log_path, state, flush_partial=True)
                return resolve_stuck_process_return_code(forced_return_code, state)

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