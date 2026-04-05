from __future__ import annotations

"""iOS 母包 CI 入口。

说明：
1. iOS 只能在 macOS 宿主机上执行，因为最终需要导出 Xcode 工程 / IPA。
2. 本文件自己管理流程日志、参数解析和边界处理。
3. 仅把 Unity BatchMode 共通能力交给 unity3d_batchmode.py。
4. 调用方可以选择 Unity 版本，并指定需要构建的 Unity 工程目录。
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


PLATFORM_KEY = "ios"


def parse_args() -> argparse.Namespace:
    """解析 iOS 母包构建参数。"""
    parser = argparse.ArgumentParser(
        description="Build iOS client package via Unity batchmode."
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
    """校验 iOS 构建的 clientVersion。"""
    normalized = client_version.strip()
    if not normalized:
        raise UnityBatchModeError("clientVersion is empty")

    if any(ch in normalized for ch in ('\n', '\r', '\t')):
        raise UnityBatchModeError(
            f"clientVersion contains unsupported whitespace characters: {normalized!r}"
        )

    return normalized


def main() -> int:
    print("[BuildClientPackage][iOS] ===== Step 1/5: parse args =====")
    args = parse_args()
    client_version = validate_client_version(args.client_version)

    print("[BuildClientPackage][iOS] ===== Step 2/5: validate host =====")
    host_os = detect_host_os()
    ensure_platform_allowed(PLATFORM_KEY)
    print(f"[BuildClientPackage][iOS] host_os={host_os}")
    print(f"[BuildClientPackage][iOS] clientVersion={client_version}")

    print("[BuildClientPackage][iOS] ===== Step 3/5: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(args.unity_version)
    project_dir = resolve_project_dir(args.project_dir)
    execute_method = get_execute_method(PLATFORM_KEY)
    log_path = get_log_path(PLATFORM_KEY, client_version)
    print(f"[BuildClientPackage][iOS] unity={unity_path}")
    print(f"[BuildClientPackage][iOS] unityVersion={actual_unity_version}")
    print(f"[BuildClientPackage][iOS] projectDir={project_dir}")
    print(f"[BuildClientPackage][iOS] method={execute_method}")
    print(f"[BuildClientPackage][iOS] log={log_path}")

    print("[BuildClientPackage][iOS] ===== Step 4/5: build Unity command =====")
    command = build_batchmode_command(
        unity_path=unity_path,
        project_dir=project_dir,
        execute_method=execute_method,
        client_version=client_version,
        log_path=log_path,
    )

    print("[BuildClientPackage][iOS] ===== Step 5/5: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        raise UnityBatchModeError(
            "iOS client package build failed. "
            f"exit_code={return_code}, log={log_path}"
        )

    print("[BuildClientPackage][iOS] build finished successfully")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except UnityBatchModeError as exc:
        print(f"[BuildClientPackage][iOS][ERROR] {exc}")
        raise SystemExit(2)

