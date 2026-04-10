"""Shared BatchMode flow for ClientRes Code / Assetbundle / Table scripts."""

from __future__ import annotations

import argparse
from pathlib import Path
import sys

from Common.client_resource_artifacts import (
    ClientResourceArtifactsError,
    prepare_clean_ci_output_root,
    upload_client_res_assetbundle,
    upload_client_res_code,
    upload_client_res_table,
)
from Common.artifact_uploader import ArtifactUploadError


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
BUILD_CLIENT_PACKAGE_ROOT = BUILD_TOOLS_ROOT / "BuildClientPackage"
if str(BUILD_CLIENT_PACKAGE_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_CLIENT_PACKAGE_ROOT))

from unity3d_batchmode import (  # noqa: E402
    UnityBatchModeError,
    build_batchmode_command,
    compose_client_version,
    configure_live_console_output,
    detect_host_os,
    ensure_platform_allowed,
    get_log_path,
    read_log_tail,
    resolve_build_metadata,
    resolve_project_dir,
    resolve_unity_executable,
    run_batchmode,
)


TABLE_OUTPUT_PLATFORM_BY_HOST = {
    "mac": "osx",
    "windows": "windows",
    "linux": "linux",
}

UNITY_BATCHMODE_BUILD_TARGET_BY_PLATFORM = {
    "android": "Android",
    "ios": "iOS",
    "windows": "Win64",
}


def parse_platform_args(description: str) -> argparse.Namespace:
    """解析 Code / Assetbundle 三端任务的公共参数。"""
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument(
        "--client-version",
        required=True,
        help="Client resource major.minor version, for example: 0.1",
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
        help="Optional Unity version, for example: 2021.3.58f1.",
    )
    parser.add_argument(
        "--project-dir",
        default=None,
        help="Optional Unity project directory. If omitted, infer the current repo root.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Only print the final Unity command, do not execute Unity or upload files.",
    )
    return parser.parse_args()


def parse_table_args(description: str) -> argparse.Namespace:
    """解析统一 BuildTable 任务参数。"""
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument(
        "--client-version",
        default=None,
        help="Optional version label for logs and fallback upload label, for example: 0.1",
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
        help="Optional CI build number used as the upload directory label.",
    )
    parser.add_argument(
        "--unity-version",
        default=None,
        help="Optional Unity version, for example: 2021.3.58f1.",
    )
    parser.add_argument(
        "--project-dir",
        default=None,
        help="Optional Unity project directory. If omitted, infer the current repo root.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Only print the final Unity command, do not execute Unity or upload files.",
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


def resolve_table_client_version_label(client_version: str | None) -> str:
    """BuildTable 没有强制版本号时，为日志和本地调试提供稳定标签。"""
    normalized = (client_version or "").strip()
    if normalized:
        return validate_client_version(normalized)
    return "table"


def insert_command_argument(command: list[str], *, flag: str, value: str) -> list[str]:
    """把额外命令参数插到 -quit 之前，避免 Unity 参数顺序混乱。"""
    resolved_command = list(command)
    if resolved_command and resolved_command[-1] == "-quit":
        resolved_command[-1:-1] = [flag, value]
        return resolved_command

    resolved_command.extend([flag, value])
    return resolved_command


def run_platform_resource_build(
    *,
    platform_key: str,
    log_prefix: str,
    description: str,
    execute_method: str,
    build_kind: str,
    artifact_kind: str,
) -> int:
    """执行三端 Code / Assetbundle 资源构建主流程。"""
    configure_live_console_output()
    print(f"{log_prefix} ===== Step 1/7: parse args =====")
    args = parse_platform_args(description)
    client_version_prefix = validate_client_version(args.client_version)
    build_name, build_number = resolve_build_metadata(args.build_name, args.build_number)
    client_version = compose_client_version(client_version_prefix, build_number)

    print(f"{log_prefix} ===== Step 2/7: validate host =====")
    host_os = detect_host_os()
    ensure_platform_allowed(platform_key)
    unity_build_target = UNITY_BATCHMODE_BUILD_TARGET_BY_PLATFORM.get(platform_key)
    if unity_build_target is None:
        raise UnityBatchModeError(f"Unsupported Unity build target platform: {platform_key}")

    print(f"{log_prefix} host_os={host_os}")
    print(f"{log_prefix} unityBuildTarget={unity_build_target}")
    print(f"{log_prefix} clientVersionPrefix={client_version_prefix}")
    print(f"{log_prefix} clientVersion={client_version}")
    if build_name:
        print(f"{log_prefix} buildName={build_name}")
    if build_number:
        print(f"{log_prefix} buildNumber={build_number}")

    print(f"{log_prefix} ===== Step 3/7: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(
        args.unity_version,
        allow_missing=args.dry_run,
    )
    project_dir = resolve_project_dir(args.project_dir)
    log_path = get_log_path(
        platform_key,
        client_version,
        project_dir=project_dir,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{log_prefix} unity={unity_path}")
    print(f"{log_prefix} unityVersion={actual_unity_version}")
    print(f"{log_prefix} projectDir={project_dir}")
    print(f"{log_prefix} method={execute_method}")
    print(f"{log_prefix} log={log_path}")

    print(f"{log_prefix} ===== Step 4/7: prepare output dir =====")
    ci_output_root = prepare_clean_ci_output_root(
        project_dir,
        build_kind=build_kind,
        build_name=build_name,
        build_number=build_number,
        platform=platform_key,
    )
    print(f"{log_prefix} ciOutputRoot={ci_output_root}")

    print(f"{log_prefix} ===== Step 5/7: build Unity command =====")
    command = insert_command_argument(
        insert_command_argument(
            build_batchmode_command(
                unity_path=unity_path,
                project_dir=project_dir,
                execute_method=execute_method,
                client_version=client_version,
                log_path=log_path,
            ),
            flag="-buildTarget",
            value=unity_build_target,
        ),
        flag="-ciOutputRoot",
        value=str(ci_output_root),
    )

    print(f"{log_prefix} ===== Step 6/7: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        print(read_log_tail(log_path))
        raise UnityBatchModeError(
            f"{artifact_kind} build failed. exit_code={return_code}, log={log_path}"
        )

    print(f"{log_prefix} ===== Step 7/7: upload =====")
    if args.dry_run:
        print(f"{log_prefix} dry-run enabled, skip artifact upload")
    elif artifact_kind == "code":
        upload_client_res_code(
            platform_key,
            output_root=ci_output_root,
            build_number=build_number,
            fallback_build_label=client_version,
            log_prefix=log_prefix,
        )
    elif artifact_kind == "assetbundle":
        upload_client_res_assetbundle(
            platform_key,
            output_root=ci_output_root,
            build_number=build_number,
            fallback_build_label=client_version,
            log_prefix=log_prefix,
        )
    else:
        raise UnityBatchModeError(f"Unsupported artifact kind: {artifact_kind}")

    print(f"{log_prefix} build finished successfully")
    return 0


def run_table_resource_build(
    *,
    log_prefix: str,
    description: str,
    execute_method: str,
    build_kind: str,
) -> int:
    """执行统一 BuildTable 任务主流程。"""
    configure_live_console_output()
    print(f"{log_prefix} ===== Step 1/7: parse args =====")
    args = parse_table_args(description)
    build_name, build_number = resolve_build_metadata(args.build_name, args.build_number)
    client_version = resolve_table_client_version_label(args.client_version)

    print(f"{log_prefix} ===== Step 2/7: validate host =====")
    host_os = detect_host_os()
    local_platform_dir = TABLE_OUTPUT_PLATFORM_BY_HOST.get(host_os, host_os)
    print(f"{log_prefix} host_os={host_os}")
    print(f"{log_prefix} localDbPlatformDir={local_platform_dir}")
    print(f"{log_prefix} clientVersion={client_version}")
    if build_name:
        print(f"{log_prefix} buildName={build_name}")
    if build_number:
        print(f"{log_prefix} buildNumber={build_number}")

    print(f"{log_prefix} ===== Step 3/7: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(
        args.unity_version,
        allow_missing=args.dry_run,
    )
    project_dir = resolve_project_dir(args.project_dir)
    log_path = get_log_path(
        "table",
        client_version,
        project_dir=project_dir,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{log_prefix} unity={unity_path}")
    print(f"{log_prefix} unityVersion={actual_unity_version}")
    print(f"{log_prefix} projectDir={project_dir}")
    print(f"{log_prefix} method={execute_method}")
    print(f"{log_prefix} log={log_path}")

    print(f"{log_prefix} ===== Step 4/7: prepare output dir =====")
    ci_output_root = prepare_clean_ci_output_root(
        project_dir,
        build_kind=build_kind,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{log_prefix} ciOutputRoot={ci_output_root}")

    print(f"{log_prefix} ===== Step 5/7: build Unity command =====")
    command = insert_command_argument(
        build_batchmode_command(
            unity_path=unity_path,
            project_dir=project_dir,
            execute_method=execute_method,
            client_version=client_version,
            log_path=log_path,
        ),
        flag="-ciOutputRoot",
        value=str(ci_output_root),
    )

    print(f"{log_prefix} ===== Step 6/7: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        print(read_log_tail(log_path))
        raise UnityBatchModeError(
            f"table build failed. exit_code={return_code}, log={log_path}"
        )

    print(f"{log_prefix} ===== Step 7/7: upload =====")
    if args.dry_run:
        print(f"{log_prefix} dry-run enabled, skip artifact upload")
    else:
        upload_client_res_table(
            local_platform_dir,
            output_root=ci_output_root,
            build_number=build_number,
            fallback_build_label=client_version,
            log_prefix=log_prefix,
        )

    print(f"{log_prefix} build finished successfully")
    return 0


__all__ = [
    "ArtifactUploadError",
    "ClientResourceArtifactsError",
    "UnityBatchModeError",
    "run_platform_resource_build",
    "run_table_resource_build",
]