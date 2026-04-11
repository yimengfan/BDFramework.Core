"""Shared BatchMode flow for ClientRes build and verification scripts."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys

from Common.client_resource_artifacts import (
    ClientResourceArtifactsError,
    prepare_clean_ci_output_root,
    upload_client_res_assetbundle,
    upload_client_res_code,
    upload_client_res_table,
)
from Common.artifact_uploader import ArtifactUploadError, resolve_file_server_settings


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


def has_ci_build_metadata(build_name: str | None, build_number: str | None) -> bool:
    """只在真实 CI 上启用平台隔离工程，避免本地临时调试也被强制迁移目录。"""
    return any(
        (
            (build_name or "").strip(),
            (build_number or "").strip(),
            os.environ.get("TEAMCITY_VERSION", "").strip(),
        )
    )


def run_git_command(*, repo_dir: Path, args: list[str]) -> str:
    """ClientRes CI 平台隔离依赖 git worktree；这里统一封装命令和错误出口。"""
    completed = subprocess.run(
        ["git", *args],
        cwd=repo_dir,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if completed.returncode != 0:
        command_text = " ".join(["git", *args])
        raise UnityBatchModeError(
            "Failed to prepare isolated CI project directory. "
            f"command={command_text}, cwd={repo_dir}, stderr={completed.stderr.strip()}"
        )

    return completed.stdout.strip()


def parse_git_worktree_paths(raw_output: str) -> set[Path]:
    """解析 git worktree list --porcelain，识别当前仓库已经注册的 worktree 目录。"""
    worktree_paths: set[Path] = set()
    for line in raw_output.splitlines():
        if not line.startswith("worktree "):
            continue
        worktree_paths.add(Path(line[len("worktree ") :]).resolve())
    return worktree_paths


def remove_existing_project_dir(project_dir: Path) -> None:
    """删除旧的隔离工程目录，确保每次 CI 都从当前 revision 重新展开。"""
    if not project_dir.exists():
        return

    if project_dir.is_dir():
        shutil.rmtree(project_dir)
        return

    project_dir.unlink()


def prepare_platform_ci_project_dir(
    *,
    base_project_dir: Path,
    platform_key: str,
    build_name: str | None,
    build_number: str | None,
    log_prefix: str,
) -> Path:
    """Assetbundle CI 下优先复用平台隔离 checkout；必要时回退到 sibling worktree 隔离跨平台 Library/Temp。"""
    resolved_base_project_dir = base_project_dir.resolve()

    if not has_ci_build_metadata(build_name, build_number):
        print(f"{log_prefix} ciProjectIsolation=disabled")
        return resolved_base_project_dir

    if resolved_base_project_dir.parent.name.lower() == platform_key.lower():
        print(f"{log_prefix} ciProjectIsolation=already_isolated")
        print(f"{log_prefix} ciProjectRoot={resolved_base_project_dir}")
        print(f"{log_prefix} ciProjectAction=use_existing_checkout")
        return resolved_base_project_dir

    repo_root = Path(
        run_git_command(
            repo_dir=resolved_base_project_dir,
            args=["rev-parse", "--show-toplevel"],
        )
    ).resolve()
    isolated_project_dir = (
        repo_root.parent / platform_key / resolved_base_project_dir.name
    ).resolve()
    if isolated_project_dir == repo_root:
        print(f"{log_prefix} ciProjectIsolation=base_project")
        return repo_root

    print(f"{log_prefix} ciProjectIsolation=enabled")
    print(f"{log_prefix} ciProjectRoot={isolated_project_dir}")

    run_git_command(repo_dir=repo_root, args=["worktree", "prune"])
    registered_worktrees = parse_git_worktree_paths(
        run_git_command(repo_dir=repo_root, args=["worktree", "list", "--porcelain"])
    )
    if isolated_project_dir in registered_worktrees:
        print(f"{log_prefix} ciProjectAction=remove_existing_worktree")
        run_git_command(
            repo_dir=repo_root,
            args=["worktree", "remove", "--force", str(isolated_project_dir)],
        )
    elif isolated_project_dir.exists():
        print(f"{log_prefix} ciProjectAction=remove_existing_path")
        remove_existing_project_dir(isolated_project_dir)

    isolated_project_dir.parent.mkdir(parents=True, exist_ok=True)
    print(f"{log_prefix} ciProjectAction=create_worktree")
    run_git_command(
        repo_dir=repo_root,
        args=["worktree", "add", "--force", "--detach", str(isolated_project_dir), "HEAD"],
    )
    return isolated_project_dir


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


def parse_platform_verify_args(description: str) -> argparse.Namespace:
    """解析 ClientRes 文件服务器验证任务的公共参数。"""
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument(
        "--client-version",
        required=True,
        help="Client resource major.minor version, for example: 0.1",
    )
    parser.add_argument(
        "--expected-code-version",
        required=True,
        help="Expected ClientRes_Code build number already published on the file server.",
    )
    parser.add_argument(
        "--expected-assetbundle-version",
        required=True,
        help="Expected ClientRes_Assetbundle build number already published on the file server.",
    )
    parser.add_argument(
        "--expected-table-version",
        required=True,
        help="Expected ClientRes_Table build number already published on the file server.",
    )
    parser.add_argument(
        "--server-url",
        default=None,
        help="Optional override for the file-server base URL. If omitted, resolve from BuildTools config.",
    )
    parser.add_argument(
        "--config",
        default=None,
        help="Optional BuildTools config path used to resolve file-server settings.",
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
        help="Only print the final Unity command, do not execute Unity.",
    )
    return parser.parse_args()


def validate_required_batch_value(raw_value: str, *, field_name: str) -> str:
    """校验并规范化 BatchMode 必填参数。"""
    normalized = str(raw_value or "").strip()
    if not normalized:
        raise UnityBatchModeError(f"{field_name} is empty")

    if any(ch in normalized for ch in ('\n', '\r', '\t')):
        raise UnityBatchModeError(
            f"{field_name} contains unsupported whitespace characters: {normalized!r}"
        )

    return normalized


def validate_client_version(client_version: str) -> str:
    """校验并规范化 clientVersion。"""
    return validate_required_batch_value(client_version, field_name="clientVersion")


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


REVERT_PRESERVE_DIRS = frozenset({
    "Library",
    "Temp",
    "Obj",
    "Logs",
})


def revert_and_snapshot_changes(
    *,
    repo_dir: Path,
    log_prefix: str,
    log_dir: Path,
) -> None:
    """Revert 本地变更（保留 Library 等大缓存目录），并将变更文件列表写入 change.log。"""
    completed = subprocess.run(
        ["git", "status", "--porcelain"],
        cwd=repo_dir,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if completed.returncode != 0:
        raise UnityBatchModeError(
            f"git status failed. cwd={repo_dir}, stderr={completed.stderr.strip()}"
        )

    raw_status = completed.stdout.strip()
    if not raw_status:
        print(f"{log_prefix} revertAndSnapshot=no_local_changes")
        change_log_path = log_dir / "change.log"
        change_log_path.parent.mkdir(parents=True, exist_ok=True)
        change_log_path.write_text("", encoding="utf-8")
        return

    changed_files: list[str] = []
    revert_targets: list[str] = []
    for line in raw_status.splitlines():
        if len(line) < 4:
            continue
        file_path = line[3:].strip()
        if not file_path:
            continue

        top_dir_name = file_path.replace("\\", "/").split("/")[0]
        if top_dir_name in REVERT_PRESERVE_DIRS:
            continue

        changed_files.append(line.rstrip())
        revert_targets.append(file_path)

    change_log_path = log_dir / "change.log"
    change_log_path.parent.mkdir(parents=True, exist_ok=True)
    change_log_path.write_text("\n".join(changed_files) + "\n", encoding="utf-8")
    print(f"{log_prefix} revertAndSnapshot={len(revert_targets)} files")
    print(f"{log_prefix} changeLog={change_log_path}")

    if not revert_targets:
        return

    checkout_args = ["git", "checkout", "--"] + revert_targets
    completed = subprocess.run(
        checkout_args,
        cwd=repo_dir,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if completed.returncode != 0:
        raise UnityBatchModeError(
            f"git checkout -- failed. cwd={repo_dir}, stderr={completed.stderr.strip()}"
        )

    clean_args = ["git", "clean", "-fd", "--"] + revert_targets
    subprocess.run(
        clean_args,
        cwd=repo_dir,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    print(f"{log_prefix} revert completed")


def should_isolate_platform_project_dir(artifact_kind: str) -> bool:
    """只有 Assetbundle 构建需要额外的平台工程隔离，避免跨平台复用 Unity 缓存。"""
    return artifact_kind == "assetbundle"


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
    print(f"{log_prefix} ===== Step 1/8: parse args =====")
    args = parse_platform_args(description)
    client_version_prefix = validate_client_version(args.client_version)
    build_name, build_number = resolve_build_metadata(args.build_name, args.build_number)
    client_version = compose_client_version(client_version_prefix, build_number)

    print(f"{log_prefix} ===== Step 2/8: validate host =====")
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

    print(f"{log_prefix} ===== Step 3/8: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(
        args.unity_version,
        allow_missing=args.dry_run,
    )
    base_project_dir = resolve_project_dir(args.project_dir)
    if should_isolate_platform_project_dir(artifact_kind):
        project_dir = prepare_platform_ci_project_dir(
            base_project_dir=base_project_dir,
            platform_key=platform_key,
            build_name=build_name,
            build_number=build_number,
            log_prefix=log_prefix,
        )
    else:
        project_dir = base_project_dir
        print(f"{log_prefix} ciProjectIsolation=skipped")
        print(f"{log_prefix} ciProjectIsolationReason=assetbundle_only")
    log_path = get_log_path(
        platform_key,
        client_version,
        project_dir=project_dir,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{log_prefix} unity={unity_path}")
    print(f"{log_prefix} unityVersion={actual_unity_version}")
    print(f"{log_prefix} baseProjectDir={base_project_dir}")
    print(f"{log_prefix} projectDir={project_dir}")
    print(f"{log_prefix} method={execute_method}")
    print(f"{log_prefix} log={log_path}")

    print(f"{log_prefix} ===== Step 4/8: revert local changes =====")
    if not args.dry_run:
        revert_and_snapshot_changes(
            repo_dir=project_dir,
            log_prefix=log_prefix,
            log_dir=log_path.parent,
        )
    else:
        print(f"{log_prefix} dry-run enabled, skip revert")

    print(f"{log_prefix} ===== Step 5/8: prepare output dir =====")
    ci_output_root = prepare_clean_ci_output_root(
        project_dir,
        build_kind=build_kind,
        build_name=build_name,
        build_number=build_number,
        platform=platform_key,
    )
    print(f"{log_prefix} ciOutputRoot={ci_output_root}")

    print(f"{log_prefix} ===== Step 6/8: build Unity command =====")
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

    print(f"{log_prefix} ===== Step 7/8: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        print(read_log_tail(log_path))
        raise UnityBatchModeError(
            f"{artifact_kind} build failed. exit_code={return_code}, log={log_path}"
        )

    print(f"{log_prefix} ===== Step 8/8: upload =====")
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
    table_platform_hint = TABLE_OUTPUT_PLATFORM_BY_HOST.get(host_os, host_os)
    print(f"{log_prefix} host_os={host_os}")
    print(f"{log_prefix} tableUploadPlatformHint={table_platform_hint}")
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
            table_platform_hint,
            output_root=ci_output_root,
            build_number=build_number,
            fallback_build_label=client_version,
            log_prefix=log_prefix,
        )

    print(f"{log_prefix} build finished successfully")
    return 0


def run_platform_resource_verify(
    *,
    platform_key: str,
    log_prefix: str,
    description: str,
    execute_method: str,
) -> int:
    """执行平台化 ClientRes 文件服务器验证主流程。"""
    configure_live_console_output()
    print(f"{log_prefix} ===== Step 1/5: parse args =====")
    args = parse_platform_verify_args(description)
    client_version_prefix = validate_client_version(args.client_version)
    expected_code_version = validate_required_batch_value(
        args.expected_code_version,
        field_name="expectedCodeVersion",
    )
    expected_assetbundle_version = validate_required_batch_value(
        args.expected_assetbundle_version,
        field_name="expectedAssetbundleVersion",
    )
    expected_table_version = validate_required_batch_value(
        args.expected_table_version,
        field_name="expectedTableVersion",
    )
    build_name, build_number = resolve_build_metadata(args.build_name, args.build_number)
    client_version = compose_client_version(client_version_prefix, build_number)

    print(f"{log_prefix} ===== Step 2/5: validate host and resolve file server =====")
    host_os = detect_host_os()
    ensure_platform_allowed(platform_key)
    unity_build_target = UNITY_BATCHMODE_BUILD_TARGET_BY_PLATFORM.get(platform_key)
    if unity_build_target is None:
        raise UnityBatchModeError(f"Unsupported Unity build target platform: {platform_key}")

    resolved_settings = resolve_file_server_settings(
        server_url=args.server_url,
        config_path=args.config,
    )
    resolved_server_url = resolved_settings.base_url.rstrip("/")

    print(f"{log_prefix} host_os={host_os}")
    print(f"{log_prefix} unityBuildTarget={unity_build_target}")
    print(f"{log_prefix} clientVersionPrefix={client_version_prefix}")
    print(f"{log_prefix} clientVersion={client_version}")
    print(f"{log_prefix} fileServerUrl={resolved_server_url}")
    print(
        f"{log_prefix} expectedVersionInfo={expected_code_version}.{expected_assetbundle_version}.{expected_table_version}"
    )
    if resolved_settings.config_path is not None:
        print(f"{log_prefix} buildtoolsConfig={resolved_settings.config_path}")
    if build_name:
        print(f"{log_prefix} buildName={build_name}")
    if build_number:
        print(f"{log_prefix} buildNumber={build_number}")

    print(f"{log_prefix} ===== Step 3/5: resolve Unity =====")
    unity_path, actual_unity_version = resolve_unity_executable(
        args.unity_version,
        allow_missing=args.dry_run,
    )
    base_project_dir = resolve_project_dir(args.project_dir)
    project_dir = base_project_dir
    print(f"{log_prefix} ciProjectIsolation=skipped")
    print(f"{log_prefix} ciProjectIsolationReason=assetbundle_build_only")
    log_path = get_log_path(
        platform_key,
        client_version,
        project_dir=project_dir,
        build_name=build_name,
        build_number=build_number,
    )
    print(f"{log_prefix} unity={unity_path}")
    print(f"{log_prefix} unityVersion={actual_unity_version}")
    print(f"{log_prefix} baseProjectDir={base_project_dir}")
    print(f"{log_prefix} projectDir={project_dir}")
    print(f"{log_prefix} method={execute_method}")
    print(f"{log_prefix} log={log_path}")

    print(f"{log_prefix} ===== Step 4/5: build Unity command =====")
    command = insert_command_argument(
        insert_command_argument(
            insert_command_argument(
                insert_command_argument(
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
                    flag="-fileServerUrl",
                    value=resolved_server_url,
                ),
                flag="-expectedCodeVersion",
                value=expected_code_version,
            ),
            flag="-expectedAssetbundleVersion",
            value=expected_assetbundle_version,
        ),
        flag="-expectedTableVersion",
        value=expected_table_version,
    )

    print(f"{log_prefix} ===== Step 5/5: execute =====")
    return_code = run_batchmode(command, dry_run=args.dry_run)
    if return_code != 0:
        print(read_log_tail(log_path))
        raise UnityBatchModeError(
            "ClientRes file-server verification failed. "
            f"exit_code={return_code}, log={log_path}"
        )

    print(f"{log_prefix} verification finished successfully")
    return 0


__all__ = [
    "ArtifactUploadError",
    "ClientResourceArtifactsError",
    "UnityBatchModeError",
    "run_platform_resource_build",
    "run_table_resource_build",
]