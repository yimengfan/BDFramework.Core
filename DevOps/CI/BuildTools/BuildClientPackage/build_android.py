"""Android 母包 CI 入口。

注意：
1. 本文件是 Android 母包脚本的执行主体。
2. 流程控制、参数校验、日志输出都放在这里。
3. 只有 Unity BatchMode 命令拼装/执行相关能力放到 unity3d_batchmode.py。
4. 调用方可以显式传入 Unity 版本和工程目录；不传时走默认配置。
"""

from __future__ import annotations

import argparse

from package_artifacts import clear_publish_package_dir, upload_publish_package

from unity3d_batchmode import (
    configure_live_console_output,
    UnityBatchModeError,
    build_batchmode_command,
    compose_client_version,
    detect_host_os,
    ensure_platform_allowed,
    get_execute_method,
    get_log_path,
    insert_command_argument,
    read_log_tail,
    resolve_build_metadata,
    resolve_unity_executable,
    resolve_project_dir,
    run_batchmode,
)


PLATFORM_KEY = "android"
LOG_PREFIX = "[BuildClientPackage][Android]"


def parse_args() -> argparse.Namespace:
    """解析脚本参数。

    当前只要求一个必要参数：clientVersion。
    其他参数为可选增强能力：
    1. --unity-version: 指定使用哪个 Unity 版本。
    2. --project-dir: 指定 Unity 工程目录。
    3. --dry-run: 只输出命令，不真正执行 Unity。
    """
    parser = argparse.ArgumentParser(
        description="Build Android client package via Unity batchmode."
    )
    parser.add_argument(
        "--client-version",
        required=True,
        help="Client package major.minor version, for example: 0.1",
    )
    parser.add_argument(
        "--build-name",
        dest="build_name",
        default=None,
        help="Optional CI build name. If omitted, try resolving from environment.",
    )
    parser.add_argument(
        "--build-number",
        dest="build_number",
        default=None,
        help="Optional CI build number. If provided, Unity clientVersion becomes major.minor.buildNumber.",
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
        "--debug-build",
        default="false",
        choices=["true", "false"],
        help="Whether to request debug-capable Unity build flow and Talos E2E compilation symbols.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Only print the final Unity command, do not execute Unity.",
    )
    return parser.parse_args()


def validate_client_version(client_version: str) -> str:
    """校验并规范化 clientVersion。"""
    normalized = client_version.strip()
    if not normalized:
        raise UnityBatchModeError("clientVersion is empty")

    if any(ch in normalized for ch in ('\n', '\r', '\t')):
        raise UnityBatchModeError(
            f"clientVersion contains unsupported whitespace characters: {normalized!r}"
        )

    return normalized


def main() -> int:
    """执行 Android 母包构建、日志回收和产物上传主流程。"""
    configure_live_console_output()
    print(f"{LOG_PREFIX} ===== Step 1/7: parse args =====")
    args = parse_args()
    client_version_prefix = validate_client_version(args.client_version)
    build_name, build_number = resolve_build_metadata(
        args.build_name,
        args.build_number,
    )
    client_version = compose_client_version(client_version_prefix, build_number)
    debug_build = str(getattr(args, "debug_build", "false")).strip().lower()

    print(f"{LOG_PREFIX} ===== Step 2/7: validate host =====")
    host_os = detect_host_os()
    ensure_platform_allowed(PLATFORM_KEY)
    print(f"{LOG_PREFIX} host_os={host_os}")
    print(f"{LOG_PREFIX} clientVersionPrefix={client_version_prefix}")
    print(f"{LOG_PREFIX} clientVersion={client_version}")
    print(f"{LOG_PREFIX} debugBuild={debug_build}")
    if build_name:
        print(f"{LOG_PREFIX} buildName={build_name}")
    if build_number:
        print(f"{LOG_PREFIX} buildNumber={build_number}")

    print(f"{LOG_PREFIX} ===== Step 3/7: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(
        args.unity_version,
        allow_missing=args.dry_run,
    )
    project_dir = resolve_project_dir(args.project_dir)
    execute_method = get_execute_method(PLATFORM_KEY)
    log_path = get_log_path(
        PLATFORM_KEY,
        client_version,
        project_dir=project_dir,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{LOG_PREFIX} unity={unity_path}")
    print(f"{LOG_PREFIX} unityVersion={actual_unity_version}")
    print(f"{LOG_PREFIX} projectDir={project_dir}")
    print(f"{LOG_PREFIX} method={execute_method}")
    print(f"{LOG_PREFIX} log={log_path}")

    print(f"{LOG_PREFIX} ===== Step 4/7: reset output dir =====")
    if args.dry_run:
        print(f"{LOG_PREFIX} dry-run enabled, skip clearing publish output directory")
    else:
        publish_output_dir = clear_publish_package_dir(PLATFORM_KEY, project_dir=project_dir)
        print(f"{LOG_PREFIX} publishOutputDir={publish_output_dir}")

    print(f"{LOG_PREFIX} ===== Step 5/7: build Unity command =====")
    command = build_batchmode_command(
        unity_path=unity_path,
        project_dir=project_dir,
        execute_method=execute_method,
        client_version=client_version,
        log_path=log_path,
    )
    command = insert_command_argument(command, flag="-buildDebug", value=debug_build)

    print(f"{LOG_PREFIX} ===== Step 6/7: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        print(read_log_tail(log_path))
        raise UnityBatchModeError(
            "Android client package build failed. "
            f"exit_code={return_code}, log={log_path}"
        )

    print(f"{LOG_PREFIX} ===== Step 7/7: upload client package =====")
    if args.dry_run:
        print(f"{LOG_PREFIX} dry-run enabled, skip client package upload")
    else:
        upload_publish_package(
            PLATFORM_KEY,
            project_dir=project_dir,
            build_number=build_number,
            client_version=client_version,
            log_prefix=LOG_PREFIX,
        )

    print(f"{LOG_PREFIX} build finished successfully")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except UnityBatchModeError as exc:
        print(f"{LOG_PREFIX}[ERROR] {exc}")
        raise SystemExit(2)

