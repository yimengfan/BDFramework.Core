from __future__ import annotations

"""Windows 母包 CI 入口。

说明：
1. 本文件负责 Windows 母包的完整流程控制。
2. CI 宿主机可能是 Windows / macOS / Linux，是否可真正完成构建由 Unity 模块能力和 CI 环境共同决定。
3. 为了便于排查问题，这里保留完整步骤日志和显式异常信息。
4. 调用方可以显式选择 Unity 版本，并单独传入工程目录。
"""

import argparse

from unity3d_batchmode import (
    UnityBatchModeError,
    build_batchmode_command,
    detect_host_os,
    ensure_platform_allowed,
    get_execute_method,
    get_log_path,
    resolve_unity_executable,
    resolve_project_dir,
    run_batchmode,
)


PLATFORM_KEY = "windows"


def parse_args() -> argparse.Namespace:
    """解析 Windows 母包构建参数。"""
    parser = argparse.ArgumentParser(
        description="Build Windows client package via Unity batchmode."
    )
    parser.add_argument(
        "--client-version",
        required=True,
        help="Client package version, for example: 0.1.0",
    )
    parser.add_argument(
        "--unity-version",
        default=None,
        help="Optional Unity version, for example: 2021.3.58f1. If omitted, use the configured default version.",
    )
    parser.add_argument(
        "--project-dir",
        default=None,
        help="Optional Unity project directory. If omitted, infer the default project root from config/script location.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Only print the final Unity command, do not execute Unity.",
    )
    return parser.parse_args()


def validate_client_version(client_version: str) -> str:
    """校验 Windows 构建的 clientVersion。"""
    normalized = client_version.strip()
    if not normalized:
        raise UnityBatchModeError("clientVersion is empty")

    if any(ch in normalized for ch in ('\n', '\r', '\t')):
        raise UnityBatchModeError(
            f"clientVersion contains unsupported whitespace characters: {normalized!r}"
        )

    return normalized


def main() -> int:
    print("[BuildClientPackage][Windows] ===== Step 1/5: parse args =====")
    args = parse_args()
    client_version = validate_client_version(args.client_version)

    print("[BuildClientPackage][Windows] ===== Step 2/5: validate host =====")
    host_os = detect_host_os()
    ensure_platform_allowed(PLATFORM_KEY)
    print(f"[BuildClientPackage][Windows] host_os={host_os}")
    print(f"[BuildClientPackage][Windows] clientVersion={client_version}")

    print("[BuildClientPackage][Windows] ===== Step 3/5: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(args.unity_version)
    project_dir = resolve_project_dir(args.project_dir)
    execute_method = get_execute_method(PLATFORM_KEY)
    log_path = get_log_path(PLATFORM_KEY, client_version)
    print(f"[BuildClientPackage][Windows] unity={unity_path}")
    print(f"[BuildClientPackage][Windows] unityVersion={actual_unity_version}")
    print(f"[BuildClientPackage][Windows] projectDir={project_dir}")
    print(f"[BuildClientPackage][Windows] method={execute_method}")
    print(f"[BuildClientPackage][Windows] log={log_path}")

    print("[BuildClientPackage][Windows] ===== Step 4/5: build Unity command =====")
    command = build_batchmode_command(
        unity_path=unity_path,
        project_dir=project_dir,
        execute_method=execute_method,
        client_version=client_version,
        log_path=log_path,
    )

    print("[BuildClientPackage][Windows] ===== Step 5/5: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        raise UnityBatchModeError(
            "Windows client package build failed. "
            f"exit_code={return_code}, log={log_path}"
        )

    print("[BuildClientPackage][Windows] build finished successfully")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except UnityBatchModeError as exc:
        print(f"[BuildClientPackage][Windows][ERROR] {exc}")
        raise SystemExit(2)

