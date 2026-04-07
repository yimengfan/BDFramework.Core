from __future__ import annotations

"""BuildClientPackage Unity 执行与日志流式输出 helpers。

这个模块只处理 Unity 进程调用和 -logFile 增量镜像，不承载平台业务判断。
真正的阶段编排、参数校验和上传逻辑仍留在各平台入口脚本里。
"""

import subprocess
import time
from dataclasses import dataclass
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


@dataclass
class UnityLogStreamingState:
    """维护 Unity 日志增量读取状态。"""

    offset: int = 0
    partial_line: str = ""


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

    for line in emitted_lines:
        safe_console_print(line)


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