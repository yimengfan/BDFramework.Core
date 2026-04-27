"""TeamCity TestClientRes 编排入口。

该脚本负责把 ClientRes 下载校验链路收敛成 TeamCity 可复用的三段流程：
1. 为目标平台解析或排队 Code / AssetBundle / Table 上游构建。
2. 等待上游构建结束，并导出三段 build.number。
3. 在当前 TestClientRes 父构建内直接执行平台对应的 VerifyClientRes 本地校验脚本。

示例：
    python Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/test_client_res.py resolve-builds \
        --platform android \
        --client-version 0.1 \
        --branch v4/v-4.0.0 \
        --vcs-revision abcdef1234
"""

from __future__ import annotations

import argparse
import json
import os
from dataclasses import dataclass
from pathlib import Path
import shlex
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from typing import Any


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
WORKSPACE_ROOT = BUILD_TOOLS_ROOT.parents[4]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.buildtools_config import BuildToolsConfigError, load_buildtools_external_config  # noqa: E402


DEFAULT_RECENT_BUILD_COUNT = 50
DEFAULT_WAIT_TIMEOUT_SECONDS = 5400
DEFAULT_WAIT_POLL_INTERVAL_SECONDS = 10
DEFAULT_WAIT_HEARTBEAT_SECONDS = 60
DEFAULT_GET_RETRY_ATTEMPTS = 5
DEFAULT_GET_RETRY_MAX_DELAY_SECONDS = 5
UPSTREAM_BUILD_TYPE_ORDER = ("code", "assetbundle", "table")

UPSTREAM_BUILD_ID_PARAM_BY_KIND = {
    "code": "test.clientres.code.build.id",
    "assetbundle": "test.clientres.assetbundle.build.id",
    "table": "test.clientres.table.build.id",
}
UPSTREAM_BUILD_NUMBER_PARAM_BY_KIND = {
    "code": "test.clientres.code.build.number",
    "assetbundle": "test.clientres.assetbundle.build.number",
    "table": "test.clientres.table.build.number",
}
VERIFY_BUILD_ID_PARAM = "test.clientres.verify.build.id"
VERIFY_BUILD_NUMBER_PARAM = "test.clientres.verify.build.number"
EXPECTED_VERSION_INFO_PARAM = "test.clientres.expected.version.info"
EXPECTED_CODE_VERSION_PARAM = "test.clientres.expected.code.version"
EXPECTED_ASSETBUNDLE_VERSION_PARAM = "test.clientres.expected.assetbundle.version"
EXPECTED_TABLE_VERSION_PARAM = "test.clientres.expected.table.version"


class TestClientResError(RuntimeError):
    """Raised when the TestClientRes TeamCity orchestration flow fails."""


def log_line(message: str) -> None:
    """以 TeamCity/Windows 控制台兼容方式输出一行日志，并强制立即刷新。"""
    try:
        print(message, flush=True)
    except UnicodeEncodeError:
        safe_message = message.encode("utf-8", errors="replace").decode("utf-8", errors="replace")
        try:
            sys.stdout.buffer.write(safe_message.encode(sys.stdout.encoding or "utf-8", errors="replace"))
            sys.stdout.buffer.write(b"\n")
            sys.stdout.buffer.flush()
        except (AttributeError, OSError):
            print(safe_message.encode("ascii", errors="replace").decode("ascii"), flush=True)


@dataclass(frozen=True)
class TeamCityRuntimeConfig:
    """Resolved TeamCity connection details consumed by BuildTools runtime orchestrators."""

    base_url: str
    token: str
    config_path: Path | None


@dataclass(frozen=True)
class PlatformBuildMatrix:
    """Maps one logical ClientRes platform to its upstream and local-check TeamCity build types."""

    platform_key: str
    platform_label: str
    code_build_type_id: str
    assetbundle_build_type_id: str
    table_build_type_id: str
    verify_build_type_id: str


@dataclass(frozen=True)
class BuildHandle:
    """Normalized TeamCity build metadata required by the orchestration steps."""

    build_id: int
    build_type_id: str
    number: str
    state: str
    status: str
    status_text: str
    branch_name: str
    web_url: str | None
    revision: str
    client_version: str
    build_extra_args: str
    running_stage_text: str = ""
    running_percentage: str = ""
    probably_hanging: str = ""

    @property
    def is_finished_success(self) -> bool:
        """Return whether the build finished successfully."""
        return self.state.lower() == "finished" and self.status.upper() == "SUCCESS"

    @property
    def is_inflight(self) -> bool:
        """Return whether the build is still queued or running."""
        return self.state.lower() in {"queued", "running"}


PLATFORM_BUILD_MATRIX_BY_KEY = {
    "android": PlatformBuildMatrix(
        platform_key="android",
        platform_label="Android",
        code_build_type_id="BDFrameworkCore_BuildCodeAndroid",
        assetbundle_build_type_id="BDFrameworkCore_BuildAssetbundleAndroid",
        table_build_type_id="BDFrameworkCore_BuildTable",
        verify_build_type_id="BDFrameworkCore_VerifyClientResAndroid",
    ),
    "ios": PlatformBuildMatrix(
        platform_key="ios",
        platform_label="iOS",
        code_build_type_id="BDFrameworkCore_BuildCodeIos",
        assetbundle_build_type_id="BDFrameworkCore_BuildAssetbundleIos",
        table_build_type_id="BDFrameworkCore_BuildTable",
        verify_build_type_id="BDFrameworkCore_VerifyClientResIos",
    ),
    "windows": PlatformBuildMatrix(
        platform_key="windows",
        platform_label="Windows",
        code_build_type_id="BDFrameworkCore_BuildCodeWindows",
        assetbundle_build_type_id="BDFrameworkCore_BuildAssetbundleWindows",
        table_build_type_id="BDFrameworkCore_BuildTable",
        verify_build_type_id="BDFrameworkCore_VerifyClientResWindows",
    ),
}


def parse_args() -> argparse.Namespace:
    """解析 TestClientRes 编排入口的子命令与共享参数。"""
    parser = argparse.ArgumentParser(
        description="解析或排队 ClientRes 上游构建，并在当前构建内直接执行本地校验脚本。"
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    resolve_parser = subparsers.add_parser(
        "resolve-builds",
        help="Resolve or queue Code / AssetBundle / Table TeamCity builds for the requested platform.",
    )
    add_platform_arg(resolve_parser)
    add_client_version_arg(resolve_parser)
    add_branch_arg(resolve_parser)
    add_vcs_revision_arg(resolve_parser)
    add_config_arg(resolve_parser)
    add_source_build_id_arg(resolve_parser)
    resolve_parser.add_argument(
        "--upstream-build-extra-args",
        default="",
        help="Optional build.extra.args forwarded to queued upstream TeamCity builds.",
    )
    resolve_parser.add_argument(
        "--search-count",
        type=int,
        default=DEFAULT_RECENT_BUILD_COUNT,
        help="How many recent TeamCity builds per buildType to inspect when reusing the current revision.",
    )

    wait_parser = subparsers.add_parser(
        "wait-builds",
        help="Wait until the queued/reused upstream TeamCity builds finish and export their build numbers.",
    )
    add_platform_arg(wait_parser)
    add_config_arg(wait_parser)
    add_wait_args(wait_parser)
    wait_parser.add_argument("--code-build-id", required=True, help="Resolved TeamCity build id for Code.")
    wait_parser.add_argument(
        "--assetbundle-build-id",
        required=True,
        help="Resolved TeamCity build id for AssetBundle.",
    )
    wait_parser.add_argument("--table-build-id", required=True, help="Resolved TeamCity build id for Table.")

    verify_parser = subparsers.add_parser(
        "queue-verify-build",
        help="Run the platform-specific VerifyClientRes local-check script directly inside the current build.",
    )
    add_platform_arg(verify_parser)
    add_client_version_arg(verify_parser)
    add_branch_arg(verify_parser)
    add_vcs_revision_arg(verify_parser)
    add_config_arg(verify_parser)
    add_source_build_id_arg(verify_parser)
    add_wait_args(verify_parser)
    verify_parser.add_argument(
        "--expected-code-version",
        required=True,
        help="Resolved Code build.number passed to the VerifyClientRes local-check task.",
    )
    verify_parser.add_argument(
        "--expected-assetbundle-version",
        required=True,
        help="Resolved AssetBundle build.number passed to the VerifyClientRes local-check task.",
    )
    verify_parser.add_argument(
        "--expected-table-version",
        required=True,
        help="Resolved Table build.number passed to the VerifyClientRes local-check task.",
    )
    verify_parser.add_argument(
        "--verify-build-extra-args",
        default="",
        help="Optional extra args forwarded directly to the platform VerifyClientRes local-check script.",
    )
    return parser.parse_args()


def add_platform_arg(parser: argparse.ArgumentParser) -> None:
    """Add the shared platform selector required by every TestClientRes subcommand."""
    parser.add_argument(
        "--platform",
        choices=tuple(PLATFORM_BUILD_MATRIX_BY_KEY.keys()),
        required=True,
        help="ClientRes platform key: android, ios, or windows.",
    )


def add_client_version_arg(parser: argparse.ArgumentParser) -> None:
    """Add the shared build.client.version argument used by queued TeamCity builds."""
    parser.add_argument(
        "--client-version",
        required=True,
        help="ClientRes major.minor version forwarded to upstream and local-check TeamCity builds.",
    )


def add_branch_arg(parser: argparse.ArgumentParser) -> None:
    """Add the optional TeamCity branch argument shared by queued/reused build lookups."""
    parser.add_argument(
        "--branch",
        default="",
        help="Optional TeamCity branch name used to match and queue builds for the current VCS branch.",
    )


def add_vcs_revision_arg(parser: argparse.ArgumentParser) -> None:
    """Add the exact VCS revision argument used to reuse or queue the current commit."""
    parser.add_argument(
        "--vcs-revision",
        required=True,
        help="Exact VCS revision that must be reused or queued for upstream/local-check TeamCity builds.",
    )


def add_config_arg(parser: argparse.ArgumentParser) -> None:
    """Add the optional BuildTools config-path argument shared by TeamCity-aware BuildTools scripts."""
    parser.add_argument(
        "--config",
        default=None,
        help="Optional BuildTools config path used to resolve TeamCity endpoint and token env.",
    )


def add_source_build_id_arg(parser: argparse.ArgumentParser) -> None:
    """Add the optional source TeamCity build id used for traceable queue comments."""
    parser.add_argument(
        "--source-build-id",
        default="",
        help="Optional TeamCity build id of the parent TestClientRes task for queue-comment traceability.",
    )


def add_wait_args(parser: argparse.ArgumentParser) -> None:
    """Add the shared timeout and poll-interval arguments for waiting TeamCity builds."""
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=DEFAULT_WAIT_TIMEOUT_SECONDS,
        help="Maximum seconds to wait for TeamCity builds to finish.",
    )
    parser.add_argument(
        "--poll-interval-seconds",
        type=int,
        default=DEFAULT_WAIT_POLL_INTERVAL_SECONDS,
        help="Polling interval in seconds while waiting for TeamCity builds to finish.",
    )


def normalize_required_value(raw_value: str, *, field_name: str) -> str:
    """Normalize a required command-line value into a non-empty trimmed string."""
    normalized = str(raw_value or "").strip()
    if not normalized:
        raise TestClientResError(f"{field_name} is empty")
    return normalized


def normalize_optional_value(raw_value: str | None) -> str:
    """Normalize an optional command-line value into a trimmed string."""
    return str(raw_value or "").strip()


def normalize_branch_name(raw_value: str | None) -> str:
    """Normalize TeamCity branch values so REST responses and queued branches can be compared safely."""
    normalized = normalize_optional_value(raw_value)
    if normalized.startswith("refs/heads/"):
        normalized = normalized[len("refs/heads/") :]
    if normalized in {"<default>", "<default branch>", "default", "(default)"}:
        return ""
    return normalized


def build_vcs_branch_name(branch_name: str) -> str | None:
    """Translate a normalized TeamCity branch name into the VCS branch format accepted by REST queue payloads."""
    normalized = normalize_branch_name(branch_name)
    if not normalized:
        return None
    if normalized.startswith("refs/"):
        return normalized
    return f"refs/heads/{normalized}"


def resolve_current_build_metadata(source_build_id: str | None) -> tuple[str | None, str | None, str | None]:
    """解析当前父构建的 id、名称和编号，供本地 step3 回写 TeamCity 参数。"""
    resolved_build_id = normalize_optional_value(source_build_id) or None
    if resolved_build_id is None:
        for env_name in ("TEAMCITY_BUILD_ID", "BUILD_ID", "TC_BUILD_ID"):
            env_value = normalize_optional_value(os.environ.get(env_name)) or None
            if env_value is not None:
                resolved_build_id = env_value
                break

    resolved_build_name = None
    for env_name in ("CI_BUILD_NAME", "BUILD_NAME", "TEAMCITY_BUILDCONF_NAME", "JOB_NAME", "TC_BUILD_NAME"):
        env_value = normalize_optional_value(os.environ.get(env_name)) or None
        if env_value is not None:
            resolved_build_name = env_value
            break

    resolved_build_number = None
    for env_name in (
        "CI_BUILD_NUMBER",
        "BUILD_NUMBER",
        "TEAMCITY_BUILD_NUMBER",
        "GITHUB_RUN_NUMBER",
        "TC_BUILD_NUMBER",
    ):
        env_value = normalize_optional_value(os.environ.get(env_name)) or None
        if env_value is not None:
            resolved_build_number = env_value
            break

    return resolved_build_id, resolved_build_name, resolved_build_number


def split_local_verify_extra_args(raw_args: str) -> list[str]:
    """把透传给本地校验脚本的额外参数拆成 argv，避免继续依赖 TeamCity 子任务。"""
    normalized = normalize_optional_value(raw_args)
    if not normalized:
        return []

    try:
        return shlex.split(normalized, posix=os.name != "nt")
    except ValueError as exc:
        raise TestClientResError(f"verifyBuildExtraArgs is invalid: {exc}") from exc


def build_local_verify_command(
    matrix: PlatformBuildMatrix,
    *,
    client_version: str,
    expected_code_version: str,
    expected_assetbundle_version: str,
    expected_table_version: str,
    config_path: str | None,
    build_name: str | None,
    build_number: str | None,
    verify_build_extra_args: str,
) -> list[str]:
    """组装当前父构建内直接执行的平台校验脚本命令。"""
    verify_script_path = Path(__file__).resolve().with_name(f"verify_{matrix.platform_key}.py")
    if not verify_script_path.is_file():
        raise TestClientResError(f"Local verify script is missing: {verify_script_path}")

    command = [
        sys.executable or "python",
        str(verify_script_path),
        "--client-version",
        client_version,
        "--expected-code-version",
        expected_code_version,
        "--expected-assetbundle-version",
        expected_assetbundle_version,
        "--expected-table-version",
        expected_table_version,
    ]
    if config_path:
        command.extend(["--config", config_path])
    if build_name:
        command.extend(["--build-name", build_name])
    if build_number:
        command.extend(["--build-number", build_number])
    command.extend(split_local_verify_extra_args(verify_build_extra_args))
    return command


def format_local_verify_command(command: list[str]) -> str:
    """把本地校验命令格式化成单行日志，方便在 TeamCity 中直接复制复现。"""
    if os.name == "nt":
        return subprocess.list2cmdline(command)
    return shlex.join(command)


def terminate_local_verify_process(process: subprocess.Popen, *, log_prefix: str) -> None:
    """在本地校验超时后尽量优雅结束子进程，必要时再强制 kill。"""
    log_line(f"{log_prefix} localVerifyTimeoutAction=terminate")
    try:
        process.terminate()
    except OSError:
        return

    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        log_line(f"{log_prefix} localVerifyTimeoutAction=kill")
        process.kill()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            log_line(f"{log_prefix} localVerifyTimeoutAction=kill-wait-expired")


def run_local_verify_command(
    command: list[str],
    *,
    log_prefix: str,
    timeout_seconds: int,
    heartbeat_seconds: int,
) -> None:
    """在当前父构建进程内直接运行平台校验脚本，并输出心跳与超时诊断。"""
    log_line(f"{log_prefix} localVerifyCwd={WORKSPACE_ROOT}")
    log_line(f"{log_prefix} localVerifyCommand={format_local_verify_command(command)}")
    log_line(f"{log_prefix} localVerifyLaunch=started waitingForUnityOutput=true")
    log_line(
        f"{log_prefix} localVerifyMonitor heartbeatSeconds={heartbeat_seconds} "
        f"timeoutSeconds={timeout_seconds if timeout_seconds > 0 else 'disabled'}"
    )
    try:
        process = subprocess.Popen(
            command,
            cwd=str(WORKSPACE_ROOT),
        )
    except OSError as exc:
        raise TestClientResError(f"Failed to start local verify script: {exc}") from exc

    wait_started_at = time.monotonic()
    deadline = wait_started_at + timeout_seconds if timeout_seconds > 0 else None
    while True:
        current_wait_timeout = heartbeat_seconds
        now = time.monotonic()
        if deadline is not None:
            remaining_seconds = deadline - now
            if remaining_seconds <= 0:
                terminate_local_verify_process(process, log_prefix=log_prefix)
                raise TestClientResError(
                    f"Local verify script timed out after {timeout_seconds}s without completing"
                )
            current_wait_timeout = max(1, min(heartbeat_seconds, int(remaining_seconds)))

        try:
            return_code = process.wait(timeout=current_wait_timeout)
            break
        except subprocess.TimeoutExpired:
            now = time.monotonic()
            elapsed_seconds = int(now - wait_started_at)
            remaining_text = f"{max(0, int(deadline - now))}s" if deadline is not None else "disabled"
            log_line(
                f"{log_prefix} localVerifyHeartbeat state=running elapsed={elapsed_seconds}s "
                f"remaining={remaining_text}"
            )
            continue

    if return_code != 0:
        raise TestClientResError(f"Local verify script failed with exit code {return_code}")


def resolve_platform_build_matrix(platform_key: str) -> PlatformBuildMatrix:
    """Resolve one platform key into the TeamCity build-type mapping used by TestClientRes."""
    matrix = PLATFORM_BUILD_MATRIX_BY_KEY.get(normalize_required_value(platform_key, field_name="platform"))
    if matrix is None:
        raise TestClientResError(f"Unsupported platform: {platform_key}")
    return matrix


def resolve_teamcity_runtime_config(config_path: str | None) -> TeamCityRuntimeConfig:
    """Resolve TeamCity endpoint and token from shared BuildTools config plus the configured token env."""
    try:
        external_config = load_buildtools_external_config(config_path=config_path)
    except BuildToolsConfigError as exc:  # pragma: no cover - delegated loader is unit-tested separately
        raise TestClientResError(str(exc)) from exc

    ci_server = external_config.ci_server
    provider = normalize_optional_value(ci_server.provider).lower()
    if provider and provider != "teamcity":
        raise TestClientResError(f"Unsupported ci_server.provider for TestClientRes: {ci_server.provider!r}")

    base_url = normalize_optional_value(ci_server.base_url or os.environ.get("TEAMCITY_BASE_URL"))
    if not base_url:
        raise TestClientResError(
            "TeamCity base_url is empty. Configure [ci_server].base_url in buildtools.toml or TEAMCITY_BASE_URL."
        )

    token = normalize_optional_value(ci_server.token)
    token_env_name = normalize_optional_value(ci_server.token_env) or "TEAMCITY_TOKEN"
    if not token:
        token = normalize_optional_value(os.environ.get(token_env_name))
    if not token:
        raise TestClientResError(
            "TeamCity token is empty. Configure [ci_server].token or [ci_server].token_env in buildtools.toml, "
            f"then provide the env var {token_env_name}."
        )

    return TeamCityRuntimeConfig(
        base_url=base_url.rstrip("/"),
        token=token,
        config_path=external_config.config_path,
    )


def build_headers(config: TeamCityRuntimeConfig) -> dict[str, str]:
    """Build the JSON-oriented TeamCity REST headers used by TestClientRes requests."""
    return {
        "Accept": "application/json",
        "Content-Type": "application/json",
        "Authorization": f"Bearer {config.token}",
    }


def api_request_json(
    config: TeamCityRuntimeConfig,
    method: str,
    path: str,
    *,
    payload: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """Send a TeamCity REST request and decode the JSON payload with safe GET retry handling."""
    request_url = f"{config.base_url}{path}"
    request_body = None if payload is None else json.dumps(payload).encode("utf-8")
    max_attempts = DEFAULT_GET_RETRY_ATTEMPTS if method.upper() == "GET" else 1

    for attempt in range(1, max_attempts + 1):
        request = urllib.request.Request(
            url=request_url,
            method=method.upper(),
            headers=build_headers(config),
            data=request_body,
        )
        try:
            with urllib.request.urlopen(request) as response:
                raw_payload = response.read().decode("utf-8", errors="replace")
                return json.loads(raw_payload) if raw_payload else {}
        except urllib.error.HTTPError as exc:
            response_body = exc.read().decode("utf-8", errors="replace")
            if method.upper() == "GET" and exc.code == 502 and attempt < max_attempts:
                time.sleep(min(attempt, DEFAULT_GET_RETRY_MAX_DELAY_SECONDS))
                continue
            raise TestClientResError(
                f"TeamCity {method.upper()} {path} failed with HTTP {exc.code}: {response_body[:500]}"
            ) from exc
        except urllib.error.URLError as exc:
            if method.upper() == "GET" and attempt < max_attempts:
                time.sleep(min(attempt, DEFAULT_GET_RETRY_MAX_DELAY_SECONDS))
                continue
            raise TestClientResError(f"TeamCity {method.upper()} {path} failed: {exc}") from exc

    raise TestClientResError(f"TeamCity {method.upper()} {path} exhausted all retries")


def api_request_text(config: TeamCityRuntimeConfig, method: str, path: str) -> str:
    """Send a TeamCity REST request and return the raw text body with safe GET retry handling."""
    request_url = f"{config.base_url}{path}"
    max_attempts = DEFAULT_GET_RETRY_ATTEMPTS if method.upper() == "GET" else 1

    for attempt in range(1, max_attempts + 1):
        request = urllib.request.Request(url=request_url, method=method.upper(), headers=build_headers(config))
        try:
            with urllib.request.urlopen(request) as response:
                return response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            response_body = exc.read().decode("utf-8", errors="replace")
            if method.upper() == "GET" and exc.code == 502 and attempt < max_attempts:
                time.sleep(min(attempt, DEFAULT_GET_RETRY_MAX_DELAY_SECONDS))
                continue
            raise TestClientResError(
                f"TeamCity {method.upper()} {path} failed with HTTP {exc.code}: {response_body[:500]}"
            ) from exc
        except urllib.error.URLError as exc:
            if method.upper() == "GET" and attempt < max_attempts:
                time.sleep(min(attempt, DEFAULT_GET_RETRY_MAX_DELAY_SECONDS))
                continue
            raise TestClientResError(f"TeamCity {method.upper()} {path} failed: {exc}") from exc

    raise TestClientResError(f"TeamCity {method.upper()} {path} exhausted all retries")


def build_handle_from_response(build_data: dict[str, Any]) -> BuildHandle:
    """Normalize one TeamCity build JSON object into the BuildHandle used by orchestration steps."""
    running_info = build_data.get("running-info") or {}
    return BuildHandle(
        build_id=int(build_data.get("id")),
        build_type_id=normalize_optional_value(
            build_data.get("buildTypeId") or (build_data.get("buildType") or {}).get("id")
        ),
        number=normalize_optional_value(build_data.get("number")),
        state=normalize_optional_value(build_data.get("state")),
        status=normalize_optional_value(build_data.get("status")),
        status_text=normalize_optional_value(build_data.get("statusText")),
        branch_name=normalize_branch_name(build_data.get("branchName")),
        web_url=normalize_optional_value(build_data.get("webUrl")) or None,
        revision=extract_build_revision(build_data),
        client_version=extract_build_property(build_data, "build.client.version"),
        build_extra_args=extract_build_property(build_data, "build.extra.args"),
        running_stage_text=normalize_optional_value(running_info.get("currentStageText")).replace("\n", " "),
        running_percentage=normalize_optional_value(running_info.get("percentageComplete")),
        probably_hanging=normalize_optional_value(running_info.get("probablyHanging")),
    )


def extract_build_revision(build_data: dict[str, Any]) -> str:
    """Read the first TeamCity revision value from a build payload."""
    revisions = (build_data.get("revisions") or {}).get("revision", [])
    if isinstance(revisions, dict):
        revisions = [revisions]
    for revision in revisions:
        version = normalize_optional_value((revision or {}).get("version"))
        if version:
            return version
    return ""


def extract_build_property(build_data: dict[str, Any], property_name: str) -> str:
    """Read one build property from TeamCity build JSON returned with nested properties fields."""
    properties = (build_data.get("properties") or {}).get("property", [])
    if isinstance(properties, dict):
        properties = [properties]
    for item in properties:
        if normalize_optional_value((item or {}).get("name")) == property_name:
            return normalize_optional_value((item or {}).get("value"))
    return ""


def list_recent_builds(
    config: TeamCityRuntimeConfig,
    *,
    build_type_id: str,
    search_count: int,
) -> list[dict[str, Any]]:
    """List recent TeamCity builds for one buildType with the revision and property fields needed for reuse checks."""
    encoded_build_type_id = urllib.parse.quote(build_type_id, safe="")
    locator = (
        "/app/rest/builds?locator="
        f"buildType:(id:{encoded_build_type_id}),state:any,defaultFilter:false,count:{search_count}"
        "&fields=build(id,buildTypeId,number,state,status,statusText,branchName,webUrl,"
        "revisions(revision(version,vcsBranchName)),properties(property(name,value))),count"
    )
    response = api_request_json(config, "GET", locator)
    builds = response.get("build", [])
    if isinstance(builds, list):
        return builds
    if isinstance(builds, dict):
        return [builds]
    return []


def list_build_type_vcs_root_instances(
    config: TeamCityRuntimeConfig,
    *,
    build_type_id: str,
) -> list[dict[str, Any]]:
    """List VCS root instances for one TeamCity buildType so queued revisions can reference an exact root instance."""
    encoded_build_type_id = urllib.parse.quote(build_type_id, safe="")
    response = api_request_json(
        config,
        "GET",
        "/app/rest/buildTypes/id:{buildTypeId}/vcsRootInstances?fields="
        "vcs-root-instance(id,default,properties(property(name,value)))".format(buildTypeId=encoded_build_type_id),
    )
    vcs_root_instances = response.get("vcs-root-instance", [])
    if isinstance(vcs_root_instances, list):
        return vcs_root_instances
    if isinstance(vcs_root_instances, dict):
        return [vcs_root_instances]
    return []


def extract_vcs_root_instance_branch(vcs_root_instance: dict[str, Any]) -> str:
    """Read the configured branch property from one TeamCity VCS root instance payload."""
    properties = (vcs_root_instance.get("properties") or {}).get("property", [])
    if isinstance(properties, dict):
        properties = [properties]
    for item in properties:
        if normalize_optional_value((item or {}).get("name")) != "branch":
            continue
        return normalize_optional_value((item or {}).get("value"))
    return ""


def resolve_build_type_vcs_root_instance_id(
    config: TeamCityRuntimeConfig,
    *,
    build_type_id: str,
    branch_name: str,
) -> str:
    """Resolve the TeamCity VCS root instance id used by queued revisions for one buildType and branch."""
    vcs_root_instances = list_build_type_vcs_root_instances(config, build_type_id=build_type_id)
    if not vcs_root_instances:
        raise TestClientResError(f"No TeamCity VCS root instances found for buildTypeId={build_type_id}")

    expected_vcs_branch_name = build_vcs_branch_name(branch_name) or ""
    for vcs_root_instance in vcs_root_instances:
        branch_value = extract_vcs_root_instance_branch(vcs_root_instance)
        if expected_vcs_branch_name and branch_value != expected_vcs_branch_name:
            continue
        instance_id = normalize_optional_value(vcs_root_instance.get("id"))
        if instance_id:
            return instance_id

    for vcs_root_instance in vcs_root_instances:
        if not bool(vcs_root_instance.get("default", False)):
            continue
        instance_id = normalize_optional_value(vcs_root_instance.get("id"))
        if instance_id:
            return instance_id

    fallback_instance_id = normalize_optional_value(vcs_root_instances[0].get("id"))
    if fallback_instance_id:
        return fallback_instance_id

    raise TestClientResError(f"TeamCity VCS root instance id is empty for buildTypeId={build_type_id}")


def build_queue_properties(client_version: str, build_extra_args: str) -> list[dict[str, str]]:
    """Build the property list forwarded to queued upstream TeamCity builds."""
    properties = [{"name": "build.client.version", "value": normalize_required_value(client_version, field_name="clientVersion") }]
    normalized_extra_args = normalize_optional_value(build_extra_args)
    if normalized_extra_args:
        properties.append({"name": "build.extra.args", "value": normalized_extra_args})
    return properties


def build_verify_queue_properties(
    *,
    client_version: str,
    expected_code_version: str,
    expected_assetbundle_version: str,
    expected_table_version: str,
    verify_build_extra_args: str,
) -> list[dict[str, str]]:
    """Build the property list forwarded to the platform-specific VerifyClientRes local-check task."""
    properties = [
        {"name": "build.client.version", "value": normalize_required_value(client_version, field_name="clientVersion")},
        {"name": EXPECTED_CODE_VERSION_PARAM, "value": normalize_required_value(expected_code_version, field_name="expectedCodeVersion")},
        {"name": EXPECTED_ASSETBUNDLE_VERSION_PARAM, "value": normalize_required_value(expected_assetbundle_version, field_name="expectedAssetbundleVersion")},
        {"name": EXPECTED_TABLE_VERSION_PARAM, "value": normalize_required_value(expected_table_version, field_name="expectedTableVersion")},
    ]
    normalized_extra_args = normalize_optional_value(verify_build_extra_args)
    if normalized_extra_args:
        properties.append({"name": "build.extra.args", "value": normalized_extra_args})
    return properties


def find_reusable_build(
    recent_builds: list[dict[str, Any]],
    *,
    branch_name: str,
    vcs_revision: str,
    client_version: str,
    build_extra_args: str,
    required_properties: dict[str, str] | None = None,
) -> BuildHandle | None:
    """从最近构建里挑出可复用的 TeamCity 构建。

    复用条件包括：同分支、同 revision、同客户端版本、同 build.extra.args，
    以及调用方显式要求匹配的附加属性（例如 VerifyClientRes 的三段 expected version）。
    """
    normalized_branch = normalize_branch_name(branch_name)
    normalized_revision = normalize_required_value(vcs_revision, field_name="vcsRevision")
    normalized_client_version = normalize_required_value(client_version, field_name="clientVersion")
    normalized_build_extra_args = normalize_optional_value(build_extra_args)
    normalized_required_properties = {
        normalize_required_value(property_name, field_name="requiredPropertyName"): normalize_optional_value(property_value)
        for property_name, property_value in (required_properties or {}).items()
    }

    matching_finished_success: list[BuildHandle] = []
    matching_inflight: list[BuildHandle] = []
    for build_data in recent_builds:
        handle = build_handle_from_response(build_data)
        if handle.revision != normalized_revision:
            continue
        if normalized_branch and handle.branch_name != normalized_branch:
            continue
        if handle.client_version != normalized_client_version:
            continue
        if handle.build_extra_args != normalized_build_extra_args:
            continue
        if any(
            extract_build_property(build_data, property_name) != property_value
            for property_name, property_value in normalized_required_properties.items()
        ):
            continue
        if handle.is_finished_success:
            matching_finished_success.append(handle)
            continue
        if handle.is_inflight:
            matching_inflight.append(handle)

    if matching_finished_success:
        return matching_finished_success[0]
    if matching_inflight:
        return matching_inflight[0]
    return None


def queue_build(
    config: TeamCityRuntimeConfig,
    *,
    build_type_id: str,
    branch_name: str,
    vcs_revision: str,
    properties: list[dict[str, str]],
    comment: str,
) -> BuildHandle:
    """Queue one TeamCity build for the exact branch/revision and return the queued build handle."""
    payload: dict[str, Any] = {
        "buildType": {"id": build_type_id},
        "comment": {"text": comment},
    }
    normalized_branch = normalize_branch_name(branch_name)
    if normalized_branch:
        payload["branchName"] = normalized_branch
    if properties:
        payload["properties"] = {"property": properties}

    normalized_revision = normalize_required_value(vcs_revision, field_name="vcsRevision")
    vcs_root_instance_id = resolve_build_type_vcs_root_instance_id(
        config,
        build_type_id=build_type_id,
        branch_name=normalized_branch,
    )
    revision_payload = {
        "version": normalized_revision,
        "vcs-root-instance": {"id": vcs_root_instance_id},
    }
    vcs_branch_name = build_vcs_branch_name(normalized_branch)
    if vcs_branch_name:
        revision_payload["vcsBranchName"] = vcs_branch_name
    payload["revisions"] = {"failOnMissingRevisions": True, "revision": [revision_payload]}

    response = api_request_json(config, "POST", "/app/rest/buildQueue", payload=payload)
    build_id = response.get("id")
    if not isinstance(build_id, int):
        raise TestClientResError(f"Unexpected TeamCity queue response without build id: {response!r}")
    build_type = response.get("buildType") or {}
    return BuildHandle(
        build_id=build_id,
        build_type_id=normalize_optional_value(build_type.get("id") or build_type_id),
        number=normalize_optional_value(response.get("number")),
        state=normalize_optional_value(response.get("state") or "queued"),
        status=normalize_optional_value(response.get("status")),
        status_text=normalize_optional_value(response.get("statusText")),
        branch_name=normalize_branch_name(response.get("branchName") or normalized_branch),
        web_url=normalize_optional_value(response.get("webUrl")) or None,
        revision=normalized_revision,
        client_version=extract_build_property(response, "build.client.version") or extract_property_value(properties, "build.client.version"),
        build_extra_args=extract_build_property(response, "build.extra.args") or extract_property_value(properties, "build.extra.args"),
    )


def extract_property_value(properties: list[dict[str, str]], property_name: str) -> str:
    """Read one property value from a queued-build property payload."""
    for item in properties:
        if normalize_optional_value(item.get("name")) == property_name:
            return normalize_optional_value(item.get("value"))
    return ""


def get_build(config: TeamCityRuntimeConfig, build_id: int) -> BuildHandle:
    """Read one TeamCity build by id with the fields required for wait and failure reporting."""
    response = api_request_json(
        config,
        "GET",
        "/app/rest/builds/id:{buildId}?fields="
        "id,buildTypeId,number,state,status,statusText,branchName,webUrl,"
        "revisions(revision(version,vcsBranchName)),properties(property(name,value)),"
        "running-info(currentStageText,percentageComplete,probablyHanging)".format(buildId=build_id),
    )
    return build_handle_from_response(response)


def read_build_log_tail(config: TeamCityRuntimeConfig, build_id: int, *, line_count: int = 80) -> str:
    """Read the tail of a TeamCity build log for focused nested-build failure diagnostics."""
    log_text = api_request_text(config, "GET", f"/downloadBuildLog.html?buildId={build_id}")
    lines = log_text.splitlines()
    if len(lines) <= line_count:
        return log_text
    return "\n".join(lines[-line_count:])


def format_waiting_build_summary(handle: BuildHandle) -> str:
    """格式化等待日志里使用的 TeamCity 构建状态摘要。"""
    summary = (
        f"buildId={handle.build_id} buildTypeId={handle.build_type_id} "
        f"state={handle.state or 'unknown'} status={handle.status or 'unknown'} "
        f"statusText={handle.status_text or '<empty>'} number={handle.number or 'pending'}"
    )
    if handle.running_percentage:
        summary += f" progress={handle.running_percentage}%"
    if handle.probably_hanging:
        summary += f" hanging={handle.probably_hanging}"
    if handle.running_stage_text:
        summary += f" stage={handle.running_stage_text[:180]}"
    return summary


def read_build_log_tail_for_diagnostics(
    config: TeamCityRuntimeConfig,
    build_id: int,
    *,
    line_count: int = 40,
) -> str:
    """尝试读取构建日志尾部；若读取失败则返回可直接拼接到异常里的说明文本。"""
    try:
        return read_build_log_tail(config, build_id, line_count=line_count)
    except TestClientResError as exc:
        return f"<failed to read build log tail: {exc}>"


def wait_for_build_success(
    config: TeamCityRuntimeConfig,
    *,
    build_id: int,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_prefix: str,
) -> BuildHandle:
    """等待一个 TeamCity 构建成功结束，并在超时或失败时输出可直接定位问题的诊断信息。"""
    wait_started_at = time.monotonic()
    deadline = wait_started_at + timeout_seconds
    last_state_key: tuple[str, str, str, str] | None = None
    last_progress_log_at = wait_started_at
    while True:
        handle = get_build(config, build_id)
        now = time.monotonic()
        state_key = (handle.state, handle.status, handle.status_text, handle.number)
        if state_key != last_state_key:
            log_line(f"{log_prefix} {format_waiting_build_summary(handle)}")
            if handle.web_url:
                log_line(f"{log_prefix} webUrl={handle.web_url}")
            last_state_key = state_key
            last_progress_log_at = now
        elif now - last_progress_log_at >= DEFAULT_WAIT_HEARTBEAT_SECONDS:
            elapsed_seconds = int(now - wait_started_at)
            remaining_seconds = max(0, int(deadline - now))
            log_line(
                f"{log_prefix} stillWaiting {format_waiting_build_summary(handle)} "
                f"elapsed={elapsed_seconds}s remaining={remaining_seconds}s"
            )
            if handle.web_url:
                log_line(f"{log_prefix} webUrl={handle.web_url}")
            last_progress_log_at = now

        if handle.is_finished_success:
            return handle

        if handle.state.lower() == "finished":
            log_tail = read_build_log_tail_for_diagnostics(config, build_id)
            raise TestClientResError(
                "Nested TeamCity build failed. "
                f"{format_waiting_build_summary(handle)}"
                f"{f' webUrl={handle.web_url}' if handle.web_url else ''}\n{log_tail}"
            )

        if now >= deadline:
            elapsed_seconds = int(now - wait_started_at)
            log_tail = read_build_log_tail_for_diagnostics(config, build_id)
            web_url_line = f"\nwebUrl={handle.web_url}" if handle.web_url else ""
            raise TestClientResError(
                "Timed out waiting for TeamCity build "
                f"{format_waiting_build_summary(handle)} after {timeout_seconds}s "
                f"elapsed={elapsed_seconds}s{web_url_line}\n{log_tail}"
            )

        time.sleep(poll_interval_seconds)


def resolve_remaining_wait_timeout_seconds(wait_deadline: float) -> int:
    """根据共享 deadline 计算当前子构建还能使用的剩余等待预算。"""
    return max(0, int(wait_deadline - time.monotonic()))


def build_queue_comment(
    *,
    scope_label: str,
    platform_label: str,
    source_build_id: str,
) -> str:
    """Build a traceable TeamCity queue comment for upstream and local-check nested builds."""
    parts = [f"Triggered by TestClientRes", f"scope={scope_label}", f"platform={platform_label}"]
    normalized_source_build_id = normalize_optional_value(source_build_id)
    if normalized_source_build_id:
        parts.append(f"sourceBuildId={normalized_source_build_id}")
    return " | ".join(parts)


def teamcity_service_escape(value: str) -> str:
    """Escape one value for TeamCity service-message usage."""
    return (
        str(value)
        .replace("|", "||")
        .replace("'", "|'")
        .replace("\n", "|n")
        .replace("\r", "|r")
        .replace("[", "|[")
        .replace("]", "|]")
    )


def emit_teamcity_parameter(name: str, value: str) -> None:
    """输出一个 TeamCity service message 参数，并立即刷新到当前构建日志。"""
    log_line(
        "##teamcity[setParameter name='{}' value='{}']".format(
            teamcity_service_escape(name),
            teamcity_service_escape(value),
        )
    )


def emit_build_id_parameter(kind: str, build_id: int) -> None:
    """Publish one upstream TeamCity build id for the next orchestration step."""
    emit_teamcity_parameter(UPSTREAM_BUILD_ID_PARAM_BY_KIND[kind], str(build_id))


def emit_build_number_parameter(kind: str, build_number: str) -> None:
    """Publish one upstream TeamCity build.number for the next orchestration step or nested verify task."""
    emit_teamcity_parameter(
        UPSTREAM_BUILD_NUMBER_PARAM_BY_KIND[kind],
        normalize_required_value(build_number, field_name=f"{kind}BuildNumber"),
    )


def command_resolve_builds(args: argparse.Namespace) -> int:
    """Resolve or queue the three upstream ClientRes builds required by TestClientRes for one platform."""
    matrix = resolve_platform_build_matrix(args.platform)
    log_prefix = f"[TestClientRes][{matrix.platform_label}]"

    print(f"{log_prefix} ===== Phase 1/4: parse args =====")
    client_version = normalize_required_value(args.client_version, field_name="clientVersion")
    branch_name = normalize_branch_name(args.branch)
    vcs_revision = normalize_required_value(args.vcs_revision, field_name="vcsRevision")
    upstream_build_extra_args = normalize_optional_value(args.upstream_build_extra_args)
    search_count = max(1, int(args.search_count))

    print(f"{log_prefix} ===== Phase 2/4: resolve TeamCity config =====")
    config = resolve_teamcity_runtime_config(args.config)
    print(f"{log_prefix} teamcityBaseUrl={config.base_url}")
    if config.config_path is not None:
        print(f"{log_prefix} buildtoolsConfig={config.config_path}")
    print(f"{log_prefix} branch={branch_name or '<default>'}")
    print(f"{log_prefix} vcsRevision={vcs_revision}")
    print(f"{log_prefix} clientVersion={client_version}")
    print(f"{log_prefix} upstreamBuildExtraArgs={upstream_build_extra_args or '<empty>'}")

    print(f"{log_prefix} ===== Phase 3/4: resolve or queue upstream builds =====")
    requested_build_types = {
        "code": matrix.code_build_type_id,
        "assetbundle": matrix.assetbundle_build_type_id,
        "table": matrix.table_build_type_id,
    }

    for kind in UPSTREAM_BUILD_TYPE_ORDER:
        build_type_id = requested_build_types[kind]
        recent_builds = list_recent_builds(config, build_type_id=build_type_id, search_count=search_count)
        reusable = find_reusable_build(
            recent_builds,
            branch_name=branch_name,
            vcs_revision=vcs_revision,
            client_version=client_version,
            build_extra_args=upstream_build_extra_args,
        )
        if reusable is not None:
            print(
                f"{log_prefix} reuse {kind} buildTypeId={reusable.build_type_id} buildId={reusable.build_id} "
                f"number={reusable.number or 'pending'} state={reusable.state or 'unknown'} status={reusable.status or 'unknown'}"
            )
            if reusable.web_url:
                print(f"{log_prefix} reuseWebUrl={reusable.web_url}")
            emit_build_id_parameter(kind, reusable.build_id)
            if reusable.number:
                emit_build_number_parameter(kind, reusable.number)
            continue

        queued = queue_build(
            config,
            build_type_id=build_type_id,
            branch_name=branch_name,
            vcs_revision=vcs_revision,
            properties=build_queue_properties(client_version, upstream_build_extra_args),
            comment=build_queue_comment(
                scope_label=f"upstream-{kind}",
                platform_label=matrix.platform_label,
                source_build_id=args.source_build_id,
            ),
        )
        print(
            f"{log_prefix} queued {kind} buildTypeId={queued.build_type_id} buildId={queued.build_id} "
            f"number={queued.number or 'pending'}"
        )
        if queued.web_url:
            print(f"{log_prefix} queuedWebUrl={queued.web_url}")
        emit_build_id_parameter(kind, queued.build_id)
        if queued.number:
            emit_build_number_parameter(kind, queued.number)

    print(f"{log_prefix} ===== Phase 4/4: export TeamCity parameters =====")
    print(f"{log_prefix} upstream build resolution finished")
    return 0


def command_wait_builds(args: argparse.Namespace) -> int:
    """等待三段上游构建共用同一个总超时预算，并导出最终版本号。"""
    matrix = resolve_platform_build_matrix(args.platform)
    log_prefix = f"[TestClientRes][{matrix.platform_label}]"

    print(f"{log_prefix} ===== Phase 1/3: parse args =====")
    code_build_id = int(normalize_required_value(args.code_build_id, field_name="codeBuildId"))
    assetbundle_build_id = int(
        normalize_required_value(args.assetbundle_build_id, field_name="assetbundleBuildId")
    )
    table_build_id = int(normalize_required_value(args.table_build_id, field_name="tableBuildId"))
    timeout_seconds = max(1, int(args.timeout_seconds))
    poll_interval_seconds = max(1, int(args.poll_interval_seconds))

    print(f"{log_prefix} ===== Phase 2/3: resolve TeamCity config =====")
    config = resolve_teamcity_runtime_config(args.config)
    print(f"{log_prefix} teamcityBaseUrl={config.base_url}")
    if config.config_path is not None:
        print(f"{log_prefix} buildtoolsConfig={config.config_path}")
    print(f"{log_prefix} timeoutSeconds={timeout_seconds}")
    print(f"{log_prefix} pollIntervalSeconds={poll_interval_seconds}")

    print(f"{log_prefix} ===== Phase 3/3: wait upstream builds =====")
    wait_deadline = time.monotonic() + timeout_seconds
    pending_build_ids = {
        "code": code_build_id,
        "assetbundle": assetbundle_build_id,
        "table": table_build_id,
    }
    resolved_handles: dict[str, BuildHandle] = {}
    for kind in UPSTREAM_BUILD_TYPE_ORDER:
        remaining_timeout_seconds = resolve_remaining_wait_timeout_seconds(wait_deadline)
        if remaining_timeout_seconds <= 0:
            raise TestClientResError(
                f"Timed out waiting for upstream TeamCity builds after {timeout_seconds}s before {kind} build finished"
            )
        current_build_id = pending_build_ids[kind]
        print(
            f"{log_prefix} waitUpstream kind={kind} buildId={current_build_id} "
            f"remainingTimeoutSeconds={remaining_timeout_seconds}"
        )
        resolved_handles[kind] = wait_for_build_success(
            config,
            build_id=current_build_id,
            timeout_seconds=remaining_timeout_seconds,
            poll_interval_seconds=poll_interval_seconds,
            log_prefix=log_prefix,
        )

    for kind in UPSTREAM_BUILD_TYPE_ORDER:
        emit_build_number_parameter(kind, resolved_handles[kind].number)

    expected_version_info = "{code}.{assetbundle}.{table}".format(
        code=resolved_handles["code"].number,
        assetbundle=resolved_handles["assetbundle"].number,
        table=resolved_handles["table"].number,
    )
    emit_teamcity_parameter(EXPECTED_VERSION_INFO_PARAM, expected_version_info)
    print(f"{log_prefix} expectedVersionInfo={expected_version_info}")
    print(f"{log_prefix} upstream build wait finished")
    return 0


def command_queue_verify_build(args: argparse.Namespace) -> int:
    """在当前 TestClientRes 父构建内直接执行平台对应的 VerifyClientRes 本地校验脚本。"""
    matrix = resolve_platform_build_matrix(args.platform)
    log_prefix = f"[TestClientRes][{matrix.platform_label}]"

    log_line(f"{log_prefix} ===== Phase 1/4: parse args =====")
    client_version = normalize_required_value(args.client_version, field_name="clientVersion")
    branch_name = normalize_branch_name(args.branch)
    vcs_revision = normalize_required_value(args.vcs_revision, field_name="vcsRevision")
    config_path = normalize_optional_value(args.config) or None
    expected_code_version = normalize_required_value(args.expected_code_version, field_name="expectedCodeVersion")
    expected_assetbundle_version = normalize_required_value(
        args.expected_assetbundle_version,
        field_name="expectedAssetbundleVersion",
    )
    expected_table_version = normalize_required_value(args.expected_table_version, field_name="expectedTableVersion")
    timeout_seconds = max(0, int(args.timeout_seconds))
    heartbeat_seconds = max(1, int(args.poll_interval_seconds))
    verify_build_extra_args = normalize_optional_value(args.verify_build_extra_args)
    current_build_id, current_build_name, current_build_number = resolve_current_build_metadata(args.source_build_id)

    log_line(f"{log_prefix} ===== Phase 2/4: resolve local verify command =====")
    if config_path is not None:
        log_line(f"{log_prefix} buildtoolsConfig={config_path}")
    log_line(f"{log_prefix} branch={branch_name or '<default>'}")
    log_line(f"{log_prefix} vcsRevision={vcs_revision}")
    log_line(
        f"{log_prefix} expectedVersionInfo={expected_code_version}.{expected_assetbundle_version}.{expected_table_version}"
    )
    log_line(f"{log_prefix} localVerifyTimeoutSeconds={timeout_seconds if timeout_seconds > 0 else 'disabled'}")
    log_line(f"{log_prefix} localVerifyHeartbeatSeconds={heartbeat_seconds}")
    log_line(f"{log_prefix} verifyBuildExtraArgs={verify_build_extra_args or '<empty>'}")
    log_line(f"{log_prefix} currentBuildId={current_build_id or '<empty>'}")
    log_line(f"{log_prefix} currentBuildName={current_build_name or '<empty>'}")
    log_line(f"{log_prefix} currentBuildNumber={current_build_number or '<empty>'}")

    local_verify_command = build_local_verify_command(
        matrix,
        client_version=client_version,
        expected_code_version=expected_code_version,
        expected_assetbundle_version=expected_assetbundle_version,
        expected_table_version=expected_table_version,
        config_path=config_path,
        build_name=current_build_name,
        build_number=current_build_number,
        verify_build_extra_args=verify_build_extra_args,
    )

    log_line(f"{log_prefix} ===== Phase 3/4: export current build parameters =====")
    if current_build_id is not None:
        emit_teamcity_parameter(VERIFY_BUILD_ID_PARAM, current_build_id)
    if current_build_number is not None:
        emit_teamcity_parameter(VERIFY_BUILD_NUMBER_PARAM, current_build_number)

    log_line(f"{log_prefix} ===== Phase 4/4: run local verify task in current build =====")
    run_local_verify_command(
        local_verify_command,
        log_prefix=log_prefix,
        timeout_seconds=timeout_seconds,
        heartbeat_seconds=heartbeat_seconds,
    )
    log_line(f"{log_prefix} local verify finished successfully in current build")
    return 0


def main() -> int:
    """Dispatch one TestClientRes orchestration subcommand."""
    args = parse_args()
    if args.command == "resolve-builds":
        return command_resolve_builds(args)
    if args.command == "wait-builds":
        return command_wait_builds(args)
    if args.command == "queue-verify-build":
        return command_queue_verify_build(args)
    raise TestClientResError(f"Unsupported command: {args.command}")


def _safe_print_error(exc: TestClientResError) -> None:
    """以 Windows 控制台兼容方式输出 TestClientRes 异常。"""
    log_line(f"[TestClientRes][ERROR] {exc}")


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except TestClientResError as exc:
        _safe_print_error(exc)
        raise SystemExit(2)