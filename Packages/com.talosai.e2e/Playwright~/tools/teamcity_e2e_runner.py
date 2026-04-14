"""Talos TeamCity E2E 远程编排入口。

职责：
1. 复用或触发远端 TeamCity 母包构建。
2. 从文件服务器下载对应平台的可执行包体。
3. 调用 Playwright 工具脚本执行 BaseFlow 或指定测试文件。
4. 把测试报告稳定落到 Playwright~/test-results，供 TeamCity Artifact 收集。
"""

from __future__ import annotations

import argparse
import base64
import io
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import sys
import time
from dataclasses import dataclass
import urllib.error
import urllib.parse
import urllib.request
import zipfile


TOOL_DIR = Path(__file__).resolve().parent
PLAYWRIGHT_DIR = TOOL_DIR.parent
REPO_ROOT = Path(__file__).resolve().parents[4]
BUILD_TOOLS_ROOT = REPO_ROOT / "DevOps" / "CI" / "BuildTools"

if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.artifact_uploader import (  # noqa: E402
    ArtifactUploadError,
    build_authorization_headers,
    build_download_request_path,
    create_http_connection,
    fetch_remote_listing,
    resolve_file_server_settings,
)
from Common.buildtools_config import BuildToolsConfigError, load_buildtools_external_config  # noqa: E402


LOG_PREFIX = "[TalosTeamCityE2E]"
DEFAULT_CLIENT_VERSION = "0.1"
DEFAULT_BUILD_DEBUG = "true"
DEFAULT_TIMEOUT_SECONDS = 5400
DEFAULT_POLL_INTERVAL_SECONDS = 10
DEFAULT_DOWNLOAD_TIMEOUT_SECONDS = 600
DEFAULT_GET_RETRY_ATTEMPTS = 5
DEFAULT_GET_RETRY_MAX_DELAY_SECONDS = 5
DEFAULT_NODE_VERSION = "20.18.0"
DEFAULT_NODE_DOWNLOAD_TIMEOUT_SECONDS = 300
WINDOWS_SKIPPED_PACKAGE_MARKERS = (
    "_BurstDebugInformation_DoNotShip",
    "不要发布",
)
NODE_TOOLCACHE_DIR = PLAYWRIGHT_DIR / ".toolcache" / "node"


class TalosTeamCityE2EError(RuntimeError):
    """Talos TeamCity E2E 编排失败时抛出的统一错误。"""


@dataclass(frozen=True)
class TeamCityRuntimeConfig:
    """保存 TeamCity REST 所需的连接信息。"""

    base_url: str
    token: str | None
    username: str | None
    password: str | None
    config_path: Path | None


@dataclass(frozen=True)
class BuildHandle:
    """归一化后的 TeamCity 构建摘要。"""

    build_id: int
    build_type_id: str
    number: str
    state: str
    status: str
    status_text: str
    branch_name: str
    web_url: str | None

    @property
    def is_finished_success(self) -> bool:
        """判断构建是否已经成功结束。"""
        return self.state.lower() == "finished" and self.status.upper() == "SUCCESS"


@dataclass(frozen=True)
class PlatformProfile:
    """描述一个平台在 TeamCity 构建、文件下载和工具脚本层面的约定。"""

    platform_key: str
    package_build_type_id: str | None
    remote_root_prefix: str | None
    allowed_suffixes: tuple[str, ...]
    disallowed_name_markers: tuple[str, ...]
    tool_script_name: str
    package_arg_name: str


@dataclass(frozen=True)
class RemotePackageEntry:
    """描述一个从文件服务器目录列表中挑出的目标包体。"""

    remote_path: str
    file_name: str


@dataclass(frozen=True)
class NodeTooling:
    """保存 Playwright 工具脚本执行所需的 Node/npm 路径。"""

    node_home: Path
    node_bin: Path
    npm_bin: Path


PLATFORM_PROFILE_BY_KEY = {
    "windows": PlatformProfile(
        platform_key="windows",
        package_build_type_id="BDFrameworkCore_BuildClientPackageWindows",
        remote_root_prefix="ClientPackage_windows",
        allowed_suffixes=(".zip",),
        disallowed_name_markers=WINDOWS_SKIPPED_PACKAGE_MARKERS,
        tool_script_name="test-pc.sh",
        package_arg_name="--exe",
    ),
    "android": PlatformProfile(
        platform_key="android",
        package_build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        remote_root_prefix="ClientPackage_android",
        allowed_suffixes=(".apk",),
        disallowed_name_markers=(),
        tool_script_name="test-android.sh",
        package_arg_name="--apk",
    ),
    "macos": PlatformProfile(
        platform_key="macos",
        package_build_type_id=None,
        remote_root_prefix=None,
        allowed_suffixes=(".app", ".zip"),
        disallowed_name_markers=(),
        tool_script_name="test-pc.sh",
        package_arg_name="--exe",
    ),
}


def normalize_shell_path(path: Path) -> str:
    """把本机路径归一化成 shell 子进程可直接消费的统一文本格式。"""
    return str(path.resolve()).replace("\\", "/")


def resolve_node_tooling_from_home(home: Path) -> NodeTooling | None:
    """从一个候选 Node 安装目录里解析 node 与 npm 可执行文件。"""
    resolved_home = home.expanduser()
    node_bin = next((candidate.resolve() for candidate in (resolved_home / "node.exe", resolved_home / "node") if candidate.is_file()), None)
    npm_bin = next((candidate.resolve() for candidate in (resolved_home / "npm.cmd", resolved_home / "npm") if candidate.is_file()), None)
    if node_bin is None or npm_bin is None:
        return None
    return NodeTooling(node_home=resolved_home.resolve(), node_bin=node_bin, npm_bin=npm_bin)


def resolve_existing_node_tooling() -> NodeTooling | None:
    """优先从显式环境变量、本机 PATH 或标准安装目录解析现成的 Node 工具链。"""
    explicit_node_bin = normalize_optional_value(os.getenv("NODE_BIN"))
    explicit_npm_bin = normalize_optional_value(os.getenv("NPM_BIN"))
    if explicit_node_bin and explicit_npm_bin:
        node_path = Path(explicit_node_bin).expanduser()
        npm_path = Path(explicit_npm_bin).expanduser()
        if node_path.is_file() and npm_path.is_file():
            return NodeTooling(node_home=node_path.parent.resolve(), node_bin=node_path.resolve(), npm_bin=npm_path.resolve())

    for home_var_name in ("TALOS_NODEJS_HOME", "NODEJS_HOME"):
        home_value = normalize_optional_value(os.getenv(home_var_name))
        if not home_value:
            continue
        tooling = resolve_node_tooling_from_home(Path(home_value))
        if tooling is not None:
            return tooling

    system_node = shutil.which("node")
    system_npm = shutil.which("npm") or shutil.which("npm.cmd")
    if system_node and system_npm:
        node_path = Path(system_node)
        npm_path = Path(system_npm)
        return NodeTooling(node_home=node_path.parent.resolve(), node_bin=node_path.resolve(), npm_bin=npm_path.resolve())

    for home in (Path("C:/Program Files/nodejs"), Path("C:/Program Files (x86)/nodejs")):
        tooling = resolve_node_tooling_from_home(home)
        if tooling is not None:
            return tooling

    node_version = normalize_optional_value(os.getenv("TALOS_NODE_VERSION")) or DEFAULT_NODE_VERSION
    cached_home = NODE_TOOLCACHE_DIR / f"node-v{node_version}-win-x64"
    return resolve_node_tooling_from_home(cached_home)


def ensure_windows_portable_node_tooling(node_version: str | None = None) -> NodeTooling:
    """当 Windows agent 未预装 Node 时，下载并缓存一份 portable Node 工具链。"""
    resolved_version = normalize_optional_value(node_version) or normalize_optional_value(os.getenv("TALOS_NODE_VERSION")) or DEFAULT_NODE_VERSION
    install_home = NODE_TOOLCACHE_DIR / f"node-v{resolved_version}-win-x64"
    cached_tooling = resolve_node_tooling_from_home(install_home)
    if cached_tooling is not None:
        return cached_tooling

    NODE_TOOLCACHE_DIR.mkdir(parents=True, exist_ok=True)
    archive_url = f"https://nodejs.org/dist/v{resolved_version}/node-v{resolved_version}-win-x64.zip"
    print(f"{LOG_PREFIX} nodeBootstrap=download")
    print(f"{LOG_PREFIX} nodeArchiveUrl={archive_url}")
    request = urllib.request.Request(archive_url, headers={"User-Agent": "TalosTeamCityE2E/1.0"})
    try:
        with urllib.request.urlopen(request, timeout=DEFAULT_NODE_DOWNLOAD_TIMEOUT_SECONDS) as response:
            archive_bytes = response.read()
    except (urllib.error.HTTPError, urllib.error.URLError) as exc:
        raise TalosTeamCityE2EError(f"Failed to download Windows Node.js runtime: {exc}") from exc

    if install_home.exists():
        shutil.rmtree(install_home)

    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
        archive.extractall(NODE_TOOLCACHE_DIR)

    installed_tooling = resolve_node_tooling_from_home(install_home)
    if installed_tooling is None:
        raise TalosTeamCityE2EError(f"Downloaded Node.js archive did not produce expected tools under: {install_home}")
    return installed_tooling


def ensure_node_tooling() -> NodeTooling:
    """确保 Playwright 工具脚本执行前已经拿到可用的 Node/npm。"""
    existing_tooling = resolve_existing_node_tooling()
    if existing_tooling is not None:
        return existing_tooling

    if os.name == "nt":
        return ensure_windows_portable_node_tooling()

    raise TalosTeamCityE2EError(
        "Node.js tooling is missing. Install Node.js or configure NODE_BIN / NPM_BIN / TALOS_NODEJS_HOME before running Talos Playwright tools."
    )


def build_test_tool_environment(node_tooling: NodeTooling) -> dict[str, str]:
    """为平台工具脚本补齐 Node/npm 环境变量，避免远端 agent 依赖 PATH。"""
    environment = os.environ.copy()
    environment["TALOS_NODEJS_HOME"] = normalize_shell_path(node_tooling.node_home)
    environment["NODE_BIN"] = normalize_shell_path(node_tooling.node_bin)
    environment["NPM_BIN"] = normalize_shell_path(node_tooling.npm_bin)
    return environment


def parse_args() -> argparse.Namespace:
    """解析 TeamCity E2E 编排入口参数。"""
    parser = argparse.ArgumentParser(
        description="Resolve TeamCity package builds, download the player package, and run Talos Playwright E2E tests."
    )
    parser.add_argument("--platform", default="windows", help="目标平台：windows / android / macos。")
    parser.add_argument("--client-version", default=DEFAULT_CLIENT_VERSION, help="上游母包构建使用的 major.minor 版本号。")
    parser.add_argument(
        "--build-debug",
        default=DEFAULT_BUILD_DEBUG,
        choices=["true", "false"],
        help="是否要求上游母包以 debug 模式构建，并带上 Talos E2E 编译宏。",
    )
    parser.add_argument("--package-build-id", default=None, help="可选：直接复用已存在的 TeamCity 母包构建 id。")
    parser.add_argument("--package-build-type-id", default=None, help="可选：覆盖平台默认母包 buildTypeId。")
    parser.add_argument("--package-build-extra-args", default="", help="透传给远端母包构建的 build.extra.args。")
    parser.add_argument("--package-path", default=None, help="可选：直接使用本地包体路径，跳过 TeamCity 与文件服务器下载。")
    parser.add_argument("--branch", default=None, help="可选：远端母包构建排队时使用的 TeamCity 分支名。")
    parser.add_argument("--config", default=None, help="可选：BuildTools 外部配置路径，用于解析 TeamCity 与文件服务器地址。")
    parser.add_argument("--file-server-url", default=None, help="可选：覆盖文件服务器访问地址。")
    parser.add_argument("--file-server-token", default=None, help="可选：覆盖文件服务器访问 token。")
    parser.add_argument("--test-file", default="", help="可选：只运行指定的 Playwright 测试文件。")
    parser.add_argument("--unity-host", default="127.0.0.1", help="桌面平台 TCP 连接地址。")
    parser.add_argument("--unity-port", type=int, default=10002, help="Unity E2E TCP 端口。")
    parser.add_argument("--adb-serial", default="", help="Android 多设备场景下的 ADB 序列号。")
    parser.add_argument("--timeout-seconds", type=int, default=DEFAULT_TIMEOUT_SECONDS, help="等待上游 TeamCity 构建完成的超时时间。")
    parser.add_argument("--poll-interval-seconds", type=int, default=DEFAULT_POLL_INTERVAL_SECONDS, help="轮询 TeamCity 构建状态的时间间隔。")
    parser.add_argument("--download-timeout-seconds", type=int, default=DEFAULT_DOWNLOAD_TIMEOUT_SECONDS, help="下载文件服务器包体的单次请求超时时间。")
    return parser.parse_args()


def normalize_optional_value(raw_value: object | None) -> str:
    """把可选文本统一规整为去空白字符串。"""
    return str(raw_value or "").strip()


def configure_console_streams() -> None:
    """为标准输出流增加编码兜底，避免 Windows agent 因不可编码字符直接中断。"""
    for stream in (sys.stdout, sys.stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if not callable(reconfigure):
            continue

        current_encoding = normalize_optional_value(getattr(stream, "encoding", "")) or "utf-8"
        candidate_encodings = [current_encoding]
        if current_encoding.lower() != "utf-8":
            candidate_encodings.append("utf-8")

        for candidate_encoding in candidate_encodings:
            try:
                reconfigure(encoding=candidate_encoding, errors="backslashreplace")
                break
            except (LookupError, OSError, ValueError):
                continue


def normalize_required_value(raw_value: object | None, *, field_name: str) -> str:
    """把必填文本统一规整为去空白字符串，并在缺失时抛错。"""
    normalized = normalize_optional_value(raw_value)
    if not normalized:
        raise TalosTeamCityE2EError(f"{field_name} is empty")
    return normalized


def normalize_bool_flag(raw_value: object | None, *, field_name: str = "buildDebug") -> str:
    """把布尔风格参数统一标准化为 true 或 false。"""
    normalized = normalize_optional_value(raw_value).lower()
    if normalized in {"1", "true", "yes", "on"}:
        return "true"
    if normalized in {"0", "false", "no", "off", ""}:
        return "false"
    raise TalosTeamCityE2EError(f"{field_name} contains unsupported value: {raw_value!r}")


def normalize_branch_name(raw_value: object | None) -> str:
    """统一 TeamCity 分支值，避免把空字符串继续下传到 REST 层。"""
    return normalize_optional_value(raw_value)


def resolve_platform_profile(platform_key: str) -> PlatformProfile:
    """根据平台标识返回 Talos E2E 运行配置。"""
    profile = PLATFORM_PROFILE_BY_KEY.get(normalize_required_value(platform_key, field_name="platform").lower())
    if profile is None:
        raise TalosTeamCityE2EError(f"Unsupported platform: {platform_key}")
    return profile


def load_external_config_if_available(config_path: str | None):
    """按需读取 BuildTools 外部配置；失败时统一转成 Talos 编排错误。"""
    try:
        return load_buildtools_external_config(config_path=config_path)
    except BuildToolsConfigError as exc:
        raise TalosTeamCityE2EError(str(exc)) from exc


def resolve_teamcity_runtime_config(config_path: str | None) -> TeamCityRuntimeConfig:
    """解析 TeamCity 地址与凭据，优先使用环境变量，其次复用 BuildTools 外部配置。"""
    external_config = load_external_config_if_available(config_path)
    ci_server = external_config.ci_server
    provider = normalize_optional_value(ci_server.provider).lower()
    if provider and provider != "teamcity":
        raise TalosTeamCityE2EError(f"Unsupported ci_server.provider: {ci_server.provider!r}")

    base_url = normalize_optional_value(
        os.environ.get("TEAMCITY_BASE_URL")
        or os.environ.get("TEAMCITY_SERVER_URL")
        or ci_server.base_url
    )
    if not base_url:
        raise TalosTeamCityE2EError(
            "TeamCity base_url is empty. Configure TEAMCITY_BASE_URL / TEAMCITY_SERVER_URL or [ci_server].base_url."
        )

    token_env_name = normalize_optional_value(ci_server.token_env) or "TEAMCITY_TOKEN"
    token = normalize_optional_value(os.environ.get(token_env_name) or ci_server.token) or None
    username = normalize_optional_value(os.environ.get("TEAMCITY_USERNAME")) or None
    password = normalize_optional_value(os.environ.get("TEAMCITY_PASSWORD")) or None
    if not token and not (username and password):
        raise TalosTeamCityE2EError(
            "TeamCity credential is empty. Provide TEAMCITY_TOKEN / ci_server.token, or TEAMCITY_USERNAME + TEAMCITY_PASSWORD."
        )

    return TeamCityRuntimeConfig(
        base_url=base_url.rstrip("/"),
        token=token,
        username=username,
        password=password,
        config_path=external_config.config_path,
    )


def build_headers(config: TeamCityRuntimeConfig, *, has_json_body: bool) -> dict[str, str]:
    """构造 TeamCity REST 请求头。"""
    headers = {"Accept": "application/json"}
    if has_json_body:
        headers["Content-Type"] = "application/json"

    if config.token:
        headers["Authorization"] = f"Bearer {config.token}"
        return headers

    credentials = f"{config.username}:{config.password}".encode("utf-8")
    headers["Authorization"] = "Basic " + base64.b64encode(credentials).decode("ascii")
    return headers


def api_request_json(
    config: TeamCityRuntimeConfig,
    method: str,
    path: str,
    *,
    payload: dict[str, object] | None = None,
) -> dict[str, object]:
    """发起 TeamCity JSON REST 请求，并对 GET 做轻量重试。"""
    request_url = f"{config.base_url}{path}"
    request_body = None if payload is None else json.dumps(payload).encode("utf-8")
    max_attempts = DEFAULT_GET_RETRY_ATTEMPTS if method.upper() == "GET" else 1

    for attempt in range(1, max_attempts + 1):
        request = urllib.request.Request(
            url=request_url,
            method=method.upper(),
            headers=build_headers(config, has_json_body=payload is not None),
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
            raise TalosTeamCityE2EError(
                f"TeamCity {method.upper()} {path} failed with HTTP {exc.code}: {response_body[:500]}"
            ) from exc
        except urllib.error.URLError as exc:
            if method.upper() == "GET" and attempt < max_attempts:
                time.sleep(min(attempt, DEFAULT_GET_RETRY_MAX_DELAY_SECONDS))
                continue
            raise TalosTeamCityE2EError(f"TeamCity {method.upper()} {path} failed: {exc}") from exc

    raise TalosTeamCityE2EError(f"TeamCity {method.upper()} {path} exhausted all retries")


def api_request_text(config: TeamCityRuntimeConfig, method: str, path: str) -> str:
    """读取 TeamCity 文本响应，用于失败时抓取构建日志尾部。"""
    request = urllib.request.Request(
        url=f"{config.base_url}{path}",
        method=method.upper(),
        headers=build_headers(config, has_json_body=False),
    )
    with urllib.request.urlopen(request) as response:
        return response.read().decode("utf-8", errors="replace")


def build_handle_from_response(build_data: dict[str, object]) -> BuildHandle:
    """把 TeamCity REST 返回值标准化成更稳定的构建句柄。"""
    return BuildHandle(
        build_id=int(build_data.get("id") or 0),
        build_type_id=normalize_required_value(
            build_data.get("buildTypeId") or (build_data.get("buildType") or {}).get("id"),
            field_name="buildTypeId",
        ),
        number=normalize_optional_value(build_data.get("number")),
        state=normalize_optional_value(build_data.get("state") or "queued"),
        status=normalize_optional_value(build_data.get("status")),
        status_text=normalize_optional_value(build_data.get("statusText")),
        branch_name=normalize_branch_name(build_data.get("branchName")),
        web_url=normalize_optional_value(build_data.get("webUrl")) or None,
    )


def get_build(config: TeamCityRuntimeConfig, build_id: int) -> BuildHandle:
    """按 id 读取一个 TeamCity 构建状态。"""
    response = api_request_json(
        config,
        "GET",
        "/app/rest/builds/id:{buildId}?fields=id,buildTypeId,number,state,status,statusText,branchName,webUrl".format(
            buildId=build_id
        ),
    )
    return build_handle_from_response(response)


def read_build_log_tail(config: TeamCityRuntimeConfig, build_id: int, *, line_count: int = 80) -> str:
    """读取构建日志尾部，便于上游构建失败时直接定位问题。"""
    log_text = api_request_text(config, "GET", f"/downloadBuildLog.html?buildId={build_id}")
    lines = log_text.splitlines()
    if len(lines) <= line_count:
        return log_text
    return "\n".join(lines[-line_count:])


def build_queue_properties(client_version: str, build_debug: str, build_extra_args: str) -> list[dict[str, str]]:
    """构造远端母包构建排队时需要透传的 TeamCity 参数。"""
    properties = [
        {"name": "build.client.version", "value": normalize_required_value(client_version, field_name="clientVersion")},
        {"name": "build.debugBuild", "value": normalize_bool_flag(build_debug)},
    ]
    normalized_extra_args = normalize_optional_value(build_extra_args)
    if normalized_extra_args:
        properties.append({"name": "build.extra.args", "value": normalized_extra_args})
    return properties


def queue_build(
    config: TeamCityRuntimeConfig,
    *,
    build_type_id: str,
    branch_name: str,
    properties: list[dict[str, str]],
    comment: str,
) -> BuildHandle:
    """排队一个 TeamCity 构建，并返回最小构建句柄。"""
    payload: dict[str, object] = {
        "buildType": {"id": build_type_id},
        "comment": {"text": comment},
    }
    normalized_branch = normalize_branch_name(branch_name)
    if normalized_branch:
        payload["branchName"] = normalized_branch
    if properties:
        payload["properties"] = {"property": properties}

    response = api_request_json(config, "POST", "/app/rest/buildQueue", payload=payload)
    build_id = int(response.get("id") or 0)
    if build_id <= 0:
        raise TalosTeamCityE2EError(f"Unexpected TeamCity queue response: {response!r}")
    return build_handle_from_response(response)


def wait_for_build_success(
    config: TeamCityRuntimeConfig,
    *,
    build_id: int,
    timeout_seconds: int,
    poll_interval_seconds: int,
) -> BuildHandle:
    """等待远端构建成功结束，并在失败时给出可直接排查的诊断。"""
    started_at = time.monotonic()
    deadline = started_at + timeout_seconds
    last_summary = ""

    while True:
        handle = get_build(config, build_id)
        summary = (
            f"buildId={handle.build_id} buildTypeId={handle.build_type_id} "
            f"state={handle.state or 'unknown'} status={handle.status or 'unknown'} "
            f"statusText={handle.status_text or '<empty>'} number={handle.number or 'pending'}"
        )
        if summary != last_summary:
            print(f"{LOG_PREFIX} {summary}")
            if handle.web_url:
                print(f"{LOG_PREFIX} webUrl={handle.web_url}")
            last_summary = summary

        if handle.is_finished_success:
            return handle

        if handle.state.lower() == "finished":
            raise TalosTeamCityE2EError(
                "Upstream TeamCity build failed. "
                f"{summary}\n{read_build_log_tail(config, build_id)}"
            )

        if time.monotonic() >= deadline:
            raise TalosTeamCityE2EError(
                "Timed out waiting for upstream TeamCity build. "
                f"{summary}\n{read_build_log_tail(config, build_id, line_count=40)}"
            )

        time.sleep(poll_interval_seconds)


def teamcity_service_escape(value: str) -> str:
    """转义 TeamCity service message 中的参数值。"""
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
    """把关键的上游构建信息回写到当前 TeamCity 构建参数。"""
    if not normalize_optional_value(os.environ.get("TEAMCITY_VERSION")):
        return
    print(
        "##teamcity[setParameter name='{}' value='{}']".format(
            teamcity_service_escape(name),
            teamcity_service_escape(value),
        )
    )


def build_remote_root(remote_root_prefix: str, build_number: str) -> str:
    """根据平台远端根和构建号拼出文件服务器目录。"""
    return str(PurePosixPath(remote_root_prefix) / normalize_required_value(build_number, field_name="buildNumber"))


def select_remote_package_entry(profile: PlatformProfile, entries: list[dict[str, object]]) -> RemotePackageEntry:
    """从文件服务器目录列表中挑出平台真正可执行的包体文件。"""
    candidates: list[RemotePackageEntry] = []
    lowered_suffixes = tuple(suffix.lower() for suffix in profile.allowed_suffixes)
    lowered_markers = tuple(marker.casefold() for marker in profile.disallowed_name_markers)

    for entry in entries:
        if str(entry.get("type") or "").strip().lower() != "file":
            continue
        remote_path = normalize_optional_value(entry.get("path"))
        if not remote_path:
            continue
        file_name = PurePosixPath(remote_path).name
        lowered_name = file_name.lower()
        if not lowered_name.endswith(lowered_suffixes):
            continue
        if any(marker in file_name.casefold() for marker in lowered_markers):
            continue
        candidates.append(RemotePackageEntry(remote_path=remote_path, file_name=file_name))

    if not candidates:
        raise TalosTeamCityE2EError(
            f"No downloadable package file found for platform={profile.platform_key} in remote listing"
        )

    candidates.sort(key=lambda item: (len(item.file_name), item.file_name.casefold()))
    return candidates[0]


def download_remote_file(
    *,
    remote_path: str,
    destination_path: Path,
    server_url: str | None,
    server_token: str | None,
    config_path: str | None,
    timeout_seconds: int,
) -> Path:
    """从文件服务器下载单个包体文件到本地缓存目录。"""
    settings = resolve_file_server_settings(
        server_url=server_url,
        token=server_token,
        config_path=config_path,
    )
    request_path = build_download_request_path(settings.base_url, remote_path)
    connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
    try:
        connection.request("GET", request_path, headers=build_authorization_headers(settings))
        response = connection.getresponse()
        if response.status != 200:
            response_body = response.read().decode("utf-8", errors="replace")
            raise TalosTeamCityE2EError(
                f"Download package failed. remotePath={remote_path}, status={response.status}, detail={response_body[:500]}"
            )

        destination_path.parent.mkdir(parents=True, exist_ok=True)
        with destination_path.open("wb") as handle:
            while True:
                chunk = response.read(settings.hash_chunk_size_bytes)
                if not chunk:
                    break
                handle.write(chunk)
    finally:
        connection.close()

    return destination_path


def find_windows_launcher(extract_root: Path) -> Path:
    """从解压后的 Windows 包体中定位 Launcher.exe。"""
    preferred_launcher = sorted(extract_root.rglob("Launcher.exe"))
    if preferred_launcher:
        return preferred_launcher[0]

    fallback_executables = sorted(extract_root.rglob("*.exe"))
    if fallback_executables:
        return fallback_executables[0]

    raise TalosTeamCityE2EError(f"Windows launcher not found under extracted package: {extract_root}")


def prepare_local_package(profile: PlatformProfile, downloaded_path: Path) -> Path:
    """把下载后的包体整理成工具脚本可以直接消费的本地路径。"""
    if profile.platform_key == "windows":
        extract_root = PLAYWRIGHT_DIR / "test-results" / "packages" / "windows" / downloaded_path.stem
        if extract_root.exists():
            shutil.rmtree(extract_root)
        extract_root.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(downloaded_path, "r") as archive:
            archive.extractall(extract_root)
        return find_windows_launcher(extract_root)

    return downloaded_path


def resolve_bash_command() -> str:
    """定位当前环境中可用的 bash，可兼容 Windows Git Bash。"""
    configured = normalize_optional_value(os.environ.get("BASH_COMMAND"))
    if configured:
        return configured

    for candidate in (
        shutil.which("bash"),
        shutil.which("sh"),
        r"C:\Program Files\Git\bin\bash.exe",
        r"C:\Program Files\Git\usr\bin\bash.exe",
    ):
        if not candidate:
            continue
        if Path(candidate).exists():
            return str(candidate)

    raise TalosTeamCityE2EError("bash is not available. Configure BASH_COMMAND or install Bash/Git Bash.")


def build_test_command(profile: PlatformProfile, package_path: Path, args: argparse.Namespace) -> list[str]:
    """根据平台拼装最终的 Playwright 工具脚本调用命令。"""
    script_path = TOOL_DIR / profile.tool_script_name
    command = [resolve_bash_command(), str(script_path), profile.package_arg_name, str(package_path)]
    if args.test_file:
        command.extend(["--test-file", normalize_required_value(args.test_file, field_name="testFile")])
    command.extend(["--port", str(args.unity_port)])

    if profile.platform_key == "windows":
        command.extend(["--host", normalize_required_value(args.unity_host, field_name="unityHost")])
    if profile.platform_key == "android" and normalize_optional_value(args.adb_serial):
        command.extend(["--serial", normalize_optional_value(args.adb_serial)])
    if profile.platform_key == "macos":
        command.append("--macos")
        command.extend(["--host", normalize_required_value(args.unity_host, field_name="unityHost")])
    return command


def download_package_from_build(
    *,
    profile: PlatformProfile,
    build_number: str,
    args: argparse.Namespace,
) -> Path:
    """根据上游母包构建号，从文件服务器下载对应平台包体。"""
    if not profile.remote_root_prefix:
        raise TalosTeamCityE2EError(f"Platform does not support remote package download yet: {profile.platform_key}")

    settings = resolve_file_server_settings(
        server_url=args.file_server_url,
        token=args.file_server_token,
        config_path=args.config,
    )
    remote_root = build_remote_root(profile.remote_root_prefix, build_number)
    print(f"{LOG_PREFIX} packageRemoteRoot={remote_root}")
    print(f"{LOG_PREFIX} fileServerUrl={settings.base_url}")
    if settings.config_path is not None:
        print(f"{LOG_PREFIX} buildtoolsConfig={settings.config_path}")

    try:
        status, payload, raw_body = fetch_remote_listing(
            prefix=remote_root,
            settings=settings,
            recursive=True,
            limit=256,
            timeout_seconds=args.download_timeout_seconds,
        )
    except ArtifactUploadError as exc:
        raise TalosTeamCityE2EError(str(exc)) from exc

    if status != 200:
        detail = payload.get("detail") if isinstance(payload, dict) else None
        detail_text = detail or raw_body.decode("utf-8", errors="replace")
        raise TalosTeamCityE2EError(
            f"Remote listing failed. remoteRoot={remote_root}, status={status}, detail={detail_text}"
        )

    entries = payload.get("entries") if isinstance(payload, dict) else None
    if not isinstance(entries, list):
        raise TalosTeamCityE2EError(f"Remote listing payload is invalid: {payload!r}")

    selected_entry = select_remote_package_entry(profile, entries)
    print(f"{LOG_PREFIX} packageRemotePath={selected_entry.remote_path}")
    cached_file = PLAYWRIGHT_DIR / "test-results" / "packages" / profile.platform_key / selected_entry.file_name
    downloaded_path = download_remote_file(
        remote_path=selected_entry.remote_path,
        destination_path=cached_file,
        server_url=args.file_server_url,
        server_token=args.file_server_token,
        config_path=args.config,
        timeout_seconds=args.download_timeout_seconds,
    )
    print(f"{LOG_PREFIX} packageDownloadPath={downloaded_path}")
    return downloaded_path


def resolve_or_queue_package_build(args: argparse.Namespace, profile: PlatformProfile) -> tuple[int, str]:
    """解析上游母包构建，必要时远端排队并等待成功。"""
    config = resolve_teamcity_runtime_config(args.config)
    print(f"{LOG_PREFIX} teamcityBaseUrl={config.base_url}")
    if config.config_path is not None:
        print(f"{LOG_PREFIX} buildtoolsConfig={config.config_path}")

    if normalize_optional_value(args.package_build_id):
        build_handle = get_build(config, int(normalize_required_value(args.package_build_id, field_name="packageBuildId")))
    else:
        build_type_id = normalize_optional_value(args.package_build_type_id) or profile.package_build_type_id
        if not build_type_id:
            raise TalosTeamCityE2EError(
                f"Platform does not provide a default package buildTypeId, please pass --package-build-type-id: {profile.platform_key}"
            )
        properties = build_queue_properties(args.client_version, args.build_debug, args.package_build_extra_args)
        build_handle = queue_build(
            config,
            build_type_id=build_type_id,
            branch_name=args.branch,
            properties=properties,
            comment=f"TalosAI E2E package build | platform={profile.platform_key}",
        )

    print(f"{LOG_PREFIX} packageBuildId={build_handle.build_id}")
    if build_handle.number:
        print(f"{LOG_PREFIX} packageBuildNumber={build_handle.number}")
    if build_handle.web_url:
        print(f"{LOG_PREFIX} packageBuildWebUrl={build_handle.web_url}")

    completed_handle = build_handle
    if not build_handle.is_finished_success:
        completed_handle = wait_for_build_success(
            config,
            build_id=build_handle.build_id,
            timeout_seconds=int(args.timeout_seconds),
            poll_interval_seconds=int(args.poll_interval_seconds),
        )

    build_number = normalize_required_value(completed_handle.number, field_name="packageBuildNumber")
    emit_teamcity_parameter("talos.e2e.package.build.id", str(completed_handle.build_id))
    emit_teamcity_parameter("talos.e2e.package.build.number", build_number)
    return completed_handle.build_id, build_number


def run_test_tool(profile: PlatformProfile, package_path: Path, args: argparse.Namespace) -> int:
    """执行平台对应的 Playwright 工具脚本。"""
    command = build_test_command(profile, package_path, args)
    node_tooling = ensure_node_tooling()
    print(f"{LOG_PREFIX} nodeHome={normalize_shell_path(node_tooling.node_home)}")
    print(f"{LOG_PREFIX} nodeBin={normalize_shell_path(node_tooling.node_bin)}")
    print(f"{LOG_PREFIX} npmBin={normalize_shell_path(node_tooling.npm_bin)}")
    print(f"{LOG_PREFIX} testCommand={' '.join(command)}")
    completed = subprocess.run(command, cwd=REPO_ROOT, check=False, env=build_test_tool_environment(node_tooling))
    return int(completed.returncode)


def main() -> int:
    """执行 Talos TeamCity E2E 的完整编排流程。"""
    configure_console_streams()
    args = parse_args()
    profile = resolve_platform_profile(args.platform)
    args.build_debug = normalize_bool_flag(args.build_debug)

    print(f"{LOG_PREFIX} 测试目的=验证 Talos 远端母包构建、包体下载与 Playwright E2E 工具链闭环")
    print(f"{LOG_PREFIX} 实现手段=复用或排队 TeamCity 母包构建 -> 下载文件服务器包体 -> 调用 tools/{profile.tool_script_name}")
    print(f"{LOG_PREFIX} ===== Phase 1/5: parse args =====")
    print(f"{LOG_PREFIX} platform={profile.platform_key}")
    print(f"{LOG_PREFIX} clientVersion={normalize_required_value(args.client_version, field_name='clientVersion')}")
    print(f"{LOG_PREFIX} buildDebug={args.build_debug}")
    print(f"{LOG_PREFIX} packageBuildId={normalize_optional_value(args.package_build_id) or '<auto>'}")
    print(f"{LOG_PREFIX} testFile={normalize_optional_value(args.test_file) or '<all>'}")

    print(f"{LOG_PREFIX} ===== Phase 2/5: resolve package source =====")
    if normalize_optional_value(args.package_path):
        package_path = Path(normalize_required_value(args.package_path, field_name="packagePath")).expanduser().resolve()
        if not package_path.exists():
            raise TalosTeamCityE2EError(f"packagePath does not exist: {package_path}")
        print(f"{LOG_PREFIX} packageSource=local")
        print(f"{LOG_PREFIX} packagePath={package_path}")
    else:
        print(f"{LOG_PREFIX} packageSource=teamcity+file-server")
        _, package_build_number = resolve_or_queue_package_build(args, profile)
        downloaded_path = download_package_from_build(profile=profile, build_number=package_build_number, args=args)
        package_path = prepare_local_package(profile, downloaded_path)
        print(f"{LOG_PREFIX} localRunnablePath={package_path}")

    print(f"{LOG_PREFIX} ===== Phase 3/5: ensure report directories =====")
    report_root = PLAYWRIGHT_DIR / "test-results"
    report_root.mkdir(parents=True, exist_ok=True)
    print(f"{LOG_PREFIX} reportRoot={report_root}")

    print(f"{LOG_PREFIX} ===== Phase 4/5: run platform tool =====")
    exit_code = run_test_tool(profile, package_path, args)

    print(f"{LOG_PREFIX} ===== Phase 5/5: finish =====")
    print(f"{LOG_PREFIX} playwrightHtmlReport={PLAYWRIGHT_DIR / 'test-results' / 'html' / 'index.html'}")
    print(f"{LOG_PREFIX} playwrightJUnitReport={PLAYWRIGHT_DIR / 'test-results' / 'junit.xml'}")
    if exit_code != 0:
        raise TalosTeamCityE2EError(f"Talos E2E tool failed with exit code {exit_code}")
    print(f"{LOG_PREFIX} status=success")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except TalosTeamCityE2EError as exc:
        print(f"{LOG_PREFIX}[ERROR] {exc}")
        raise SystemExit(2)