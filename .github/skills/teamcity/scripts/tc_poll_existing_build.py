#!/usr/bin/env python3
"""TeamCity Build Poller - Poll existing build by ID. TeamCity 构建轮询 - 按ID轮询已存在的构建。

用法 / Usage:
    python3 tc_poll_existing_build.py <build_id> [--timeout 3600] [--interval 60]

用于轮询已入队的构建，而不触发新构建。用于 shell 脚本集成。
Poll an already queued build without triggering a new one. Used for shell script integration.
"""

import argparse
import os
import sys
import time
from pathlib import Path

# Add parent directory for imports
sys.path.insert(0, str(Path(__file__).resolve().parent))

from update_project_settings import (
    DEFAULT_ENV_FILE,
    DEFAULT_WAIT_HEARTBEAT_SECONDS,
    TeamCityConfig,
    TeamCityApiError,
    build_config,
    get_build,
    get_build_log_tail,
    print_build_summary,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Manual diagnostics only: poll an existing TeamCity build by ID without triggering a new build."
    )
    parser.add_argument(
        "build_id",
        type=int,
        help="TeamCity build ID to poll.",
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=3600,
        help="Maximum wait time in seconds. Default: 3600 (1 hour).",
    )
    parser.add_argument(
        "--interval",
        type=int,
        default=30,
        help="Poll interval in seconds. Default: 30.",
    )
    parser.add_argument(
        "--env-file",
        default=str(DEFAULT_ENV_FILE),
        help="TeamCity .env file path. Existing shell env values win over file values.",
    )
    parser.add_argument(
        "--log-tail-lines",
        type=int,
        default=80,
        help="Lines of log tail to print on failure. Default: 80.",
    )
    return parser.parse_args()


def poll_build(
    config: TeamCityConfig,
    build_id: int,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
) -> int:
    """轮询构建直到完成，返回退出码。 Poll build until completion, return exit code."""
    
    deadline = time.monotonic() + max(timeout_seconds, 1)
    poll_interval = max(poll_interval_seconds, 1)
    heartbeat_interval_seconds = max(DEFAULT_WAIT_HEARTBEAT_SECONDS, poll_interval)
    last_state: tuple | None = None
    last_report_time: float | None = None
    start_time = time.monotonic()

    print(f"[poll_existing] Build ID: {build_id}")
    print(f"[poll_existing] Timeout: {timeout_seconds}s")
    print(f"[poll_existing] Interval: {poll_interval}s")
    print(f"[poll_existing] ========================================")

    while True:
        current_time = time.monotonic()
        elapsed = int(current_time - start_time)
        
        try:
            build = get_build(config, build_id)
        except TeamCityApiError as exc:
            print(f"[poll_existing][{elapsed}s] ERROR: {exc}")
            if current_time >= deadline:
                raise
            time.sleep(poll_interval)
            continue

        running_info = build.get("running-info") or {}
        current_state = (
            build.get("state"),
            build.get("status"),
            build.get("statusText"),
            running_info.get("currentStageText"),
            running_info.get("percentageComplete"),
        )

        # Print when state changes or heartbeat interval elapsed
        if (
            current_state != last_state
            or last_report_time is None
            or current_time - last_report_time >= heartbeat_interval_seconds
        ):
            print(f"[poll_existing][{elapsed}s] ========================================")
            print_build_summary(config, build)
            last_state = current_state
            last_report_time = current_time

        # Check completion
        if build.get("state") == "finished":
            if build.get("status") != "SUCCESS":
                print(f"[poll_existing][{elapsed}s] ========================================")
                print("[poll_existing] Build FAILED, printing log tail...")
                print(get_build_log_tail(config, build_id, log_tail_lines))
                return 1
            print(f"[poll_existing][{elapsed}s] ========================================")
            print("[poll_existing] Build SUCCESS!")
            return 0

        # Check timeout
        if current_time >= deadline:
            print(f"[poll_existing][{elapsed}s] ========================================")
            raise TeamCityApiError(
                f"Timed out waiting for build {build_id} after {timeout_seconds} seconds"
            )

        time.sleep(poll_interval)


def main() -> int:
    args = parse_args()
    
    # Build minimal config for API calls
    class MinimalArgs:
        def __init__(self, args):
            self.env_file = args.env_file
            self.base_url = None
            self.project_id = None
    
    minimal_args = MinimalArgs(args)
    
    try:
        config = build_config(minimal_args)
        return poll_build(
            config,
            build_id=args.build_id,
            timeout_seconds=args.timeout,
            poll_interval_seconds=args.interval,
            log_tail_lines=args.log_tail_lines,
        )
    except TeamCityApiError as exc:
        print(f"[poll_existing][ERROR] {exc}")
        return 2


if __name__ == "__main__":
    sys.exit(main())