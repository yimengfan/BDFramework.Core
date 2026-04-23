from __future__ import annotations

"""TeamCity Web API 辅助脚本。 TeamCity Web API helper script.

用途 / Purpose:
1. 读取指定 project 的当前信息与 Versioned Settings。 Read the current project metadata and Versioned Settings.
2. 导出当前 Versioned Settings，作为后续修改的基线。 Export the current Versioned Settings as the update baseline.
3. 读取用户自定义 JSON，再通过 Web API PUT 回 TeamCity。 Load user-provided JSON and PUT it back through the Web API.
4. 更新后立即再次读取并打印关键字段，便于确认是否已经从 VCS 设置生效。 Re-read key fields after updates so VCS-backed settings activation can be verified immediately.

安全约束 / Security constraints:
- 不在仓库中保存真实密钥。 Never persist real secrets in the repository.
- 优先通过 TEAMCITY_TOKEN 使用 Bearer Token。 Prefer TEAMCITY_TOKEN as a Bearer token.
- 如果必须使用账号密码，请在本地 .env 中填写，不要提交到 Git。 If basic auth is required, keep credentials in a local .env file and never commit them.
"""

import argparse
import json
import os
import shutil
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
SKILL_DIR = SCRIPT_DIR.parent
REPO_ROOT = SCRIPT_DIR.parents[3]
PLAYWRIGHT_DIR = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~"
TALOS_BATCHMODE_SCRIPT = PLAYWRIGHT_DIR / "tools" / "test-batchmode.sh"
DEFAULT_ENV_FILE = REPO_ROOT / ".test-DevOps" / ".teamcity" / ".env"
DEFAULT_OUTPUT_DIR_NAME = "output"
DEFAULT_GET_RETRY_ATTEMPTS = 5
DEFAULT_GET_RETRY_MAX_DELAY_SECONDS = 5
DEFAULT_WAIT_HEARTBEAT_SECONDS = 60
DEFAULT_TALOS_BASEFLOW_BUILD_TYPE_ID = "BDFrameworkCore_TalosAIStep01BaseFlowTest"
DEFAULT_TALOS_BASEFLOW_TEST_FILE = "tests/testBaseFlow-e2e.spec.ts"
DEFAULT_TALOS_CLIENT_VERSION = "0.1"
DEFAULT_TALOS_BUILD_DEBUG = "true"
CPOLAR_TRANSIENT_404_MARKERS = (
    "cpolar.com",
    "domain doesn't exist",
)


class TeamCityApiError(RuntimeError):
    """表示 TeamCity Web API 请求失败。"""


@dataclass
class TeamCityConfig:
    """保存脚本运行所需的 TeamCity 连接与输出配置。"""

    base_url: str
    project_id: str
    token: str | None
    username: str | None
    password: str | None
    output_dir: Path
    resolved_project_id: str | None = None
    resolved_project_match: str | None = None


@dataclass
class TeamCityResponse:
    """包装 TeamCity Web API 的状态码与解析结果。"""

    status_code: int
    data: Any


@dataclass
class VersionedSettingsHandle:
    """统一保存 Versioned Settings 的原始结构、标准化字段和更新入口。"""

    raw: dict[str, Any]
    normalized: dict[str, Any]
    source_mode: str
    update_path: str


@dataclass
class QueuedBuildHandle:
    """保存已入队构建的关键标识，便于后续轮询与日志查询。"""

    build_id: int
    href: str | None
    web_url: str | None


@dataclass(frozen=True)
class AgentHandle:
    """描述可用于调度决策的 agent 状态快照。"""

    agent_id: int
    name: str
    enabled: bool
    authorized: bool
    connected: bool
    running_build_count: int


@dataclass(frozen=True)
class BuildDispatchPlan:
    """描述批量触发构建时的调度模式与 agent 分配结果。"""

    mode: str
    reason: str
    compatible_agent_ids_by_build_type: dict[str, tuple[int, ...]]
    idle_agent_ids_by_build_type: dict[str, tuple[int, ...]]
    parallel_assignment: dict[str, int]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Read or update TeamCity project versioned settings via Web API."
    )
    parser.add_argument(
        "command",
        choices=(
            "show-project",
            "verify-vcs",
            "export-current",
            "apply",
            "run-build",
            "run-build-group",
            "run-talos-baseflow-chain",
        ),
        help="show-project: print current project + versioned settings; verify-vcs: print a VCS-loading oriented summary; export-current: save current versioned settings; apply: PUT a user-provided payload and verify it; run-build: queue a single build, optionally wait for completion, and print a log tail on failure; run-build-group: inspect compatible agents, choose parallel or sequential dispatch automatically, then queue multiple builds; run-talos-baseflow-chain: run a local Talos batchmode gate first, then rebuild the player package, then queue the matching remote BaseFlow validation.",
    )
    parser.add_argument(
        "--env-file",
        default=str(DEFAULT_ENV_FILE),
        help="Optional .env file path. Default: %(default)s",
    )
    parser.add_argument(
        "--base-url",
        default=None,
        help="Override TEAMCITY_BASE_URL.",
    )
    parser.add_argument(
        "--project-id",
        default=None,
        help="Override TEAMCITY_PROJECT_ID.",
    )
    parser.add_argument(
        "--payload",
        default=None,
        help="JSON file to PUT to /app/rest/projects/id:{projectId}/versionedSettings. Required for apply.",
    )
    parser.add_argument(
        "--output",
        default=None,
        help="Output JSON path. export-current defaults to .github/skills/teamcity/output/current-versioned-settings.json",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the request target and payload without sending PUT.",
    )
    parser.add_argument(
        "--build-type-id",
        dest="build_type_ids",
        action="append",
        default=[],
        help="Build configuration id. Repeat this option for run-build-group, for example: --build-type-id BDFrameworkCore_BuildClientPackageIos --build-type-id BDFrameworkCore_BuildClientPackageWindows",
    )
    parser.add_argument(
        "--dispatch-mode",
        choices=("auto", "parallel", "sequential"),
        default="auto",
        help="For run-build-group, choose how to dispatch builds. auto prefers parallel only when enough idle compatible agents exist. Default: %(default)s.",
    )
    parser.add_argument(
        "--branch",
        default=None,
        help="Optional branch name for run-build.",
    )
    parser.add_argument(
        "--comment",
        default=None,
        help="Optional TeamCity build comment. If omitted, queued builds automatically include a test-target comment.",
    )
    parser.add_argument(
        "--tag",
        dest="tags",
        action="append",
        default=[],
        help="Optional TeamCity build tag. Repeat this option or use comma-separated values. Only add essential metadata tags such as platform (e.g. 'win64', 'android'). Test targets and branch info belong in --comment, not in tags. No default tag is injected automatically.",
    )
    parser.add_argument(
        "--property",
        dest="properties",
        action="append",
        default=[],
        help="Optional build property override in name=value form. Repeat for multiple values.",
    )
    parser.add_argument(
        "--wait",
        action="store_true",
        help="For run-build, wait for the build to finish and print the final status.",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=900,
        help="For run-build --wait, maximum time to wait. Default: %(default)s seconds.",
    )
    parser.add_argument(
        "--poll-interval-seconds",
        type=int,
        default=5,
        help="For run-build --wait, polling interval. Default: %(default)s seconds.",
    )
    parser.add_argument(
        "--log-tail-lines",
        type=int,
        default=80,
        help="For run-build failures, print the last N build log lines. Default: %(default)s.",
    )
    parser.add_argument(
        "--platform",
        choices=("android", "windows"),
        default=None,
        help="For run-talos-baseflow-chain, target platform. Default: android.",
    )
    parser.add_argument(
        "--unity-path",
        default=None,
        help="For run-talos-baseflow-chain, local Unity executable path. Defaults to UNITY_PATH when omitted.",
    )
    parser.add_argument(
        "--project-path",
        default=None,
        help="For run-talos-baseflow-chain, local Unity project path. Defaults to the current repository root.",
    )
    parser.add_argument(
        "--test-file",
        default=DEFAULT_TALOS_BASEFLOW_TEST_FILE,
        help="For run-talos-baseflow-chain, Playwright test file relative to Playwright~. Default: %(default)s.",
    )
    parser.add_argument(
        "--local-batchmode-mode",
        choices=("tcp", "sync"),
        default="tcp",
        help="For run-talos-baseflow-chain, local Talos batchmode mode. tcp keeps exact Playwright-spec parity; sync is a broader fallback. Default: %(default)s.",
    )
    parser.add_argument(
        "--allow-local-sync-fallback",
        action="store_true",
        help="For run-talos-baseflow-chain, explicitly allow the less-consistent local sync fallback.",
    )
    parser.add_argument(
        "--client-version",
        default=DEFAULT_TALOS_CLIENT_VERSION,
        help="For run-talos-baseflow-chain, build.client.version override. Default: %(default)s.",
    )
    parser.add_argument(
        "--build-debug",
        default=DEFAULT_TALOS_BUILD_DEBUG,
        help="For run-talos-baseflow-chain, build.debugBuild override. Default: %(default)s.",
    )
    parser.add_argument(
        "--adb-serial",
        default=None,
        help="For run-talos-baseflow-chain Android reruns, Talos adb serial override.",
    )
    parser.add_argument(
        "--adb-connect-targets",
        default=None,
        help="For run-talos-baseflow-chain Android reruns, Talos adb connect targets override.",
    )
    parser.add_argument(
        "--emulator-type",
        default=None,
        help="For run-talos-baseflow-chain Android reruns, Talos emulator type override.",
    )
    parser.add_argument(
        "--baseflow-build-type-id",
        default=DEFAULT_TALOS_BASEFLOW_BUILD_TYPE_ID,
        help="For run-talos-baseflow-chain, the TeamCity BaseFlow build type id. Default: %(default)s.",
    )
    parser.add_argument(
        "--package-build-type-id",
        default=None,
        help="For run-talos-baseflow-chain, optional explicit package build type id override.",
    )
    return parser.parse_args()


def load_env_file(env_file: Path) -> None:
    if not env_file.exists():
        return

    for raw_line in env_file.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line:
            raise TeamCityApiError(f"Invalid .env line: {raw_line!r}")
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip())


def build_config(args: argparse.Namespace) -> TeamCityConfig:
    env_file = Path(args.env_file).expanduser().resolve()
    load_env_file(env_file)

    base_url = (args.base_url or os.environ.get("TEAMCITY_BASE_URL", "")).strip()
    project_id = (args.project_id or os.environ.get("TEAMCITY_PROJECT_ID", "")).strip()
    token = os.environ.get("TEAMCITY_TOKEN", "").strip() or None
    username = os.environ.get("TEAMCITY_USERNAME", "").strip() or None
    password = os.environ.get("TEAMCITY_PASSWORD", "").strip() or None
    output_dir_name = os.environ.get("TEAMCITY_OUTPUT_DIR", DEFAULT_OUTPUT_DIR_NAME).strip() or DEFAULT_OUTPUT_DIR_NAME

    if not base_url:
        raise TeamCityApiError("TEAMCITY_BASE_URL is required")
    if not project_id:
        raise TeamCityApiError("TEAMCITY_PROJECT_ID is required")
    if not token and not (username and password):
        raise TeamCityApiError(
            "Provide TEAMCITY_TOKEN or TEAMCITY_USERNAME + TEAMCITY_PASSWORD"
        )

    output_dir = (SKILL_DIR / output_dir_name).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    return TeamCityConfig(
        base_url=base_url.rstrip("/"),
        project_id=project_id,
        token=token,
        username=username,
        password=password,
        output_dir=output_dir,
    )


def build_headers(config: TeamCityConfig, *, has_json_body: bool = False) -> dict[str, str]:
    headers = {
        "Accept": "application/json",
    }
    if has_json_body:
        headers["Content-Type"] = "application/json"

    if config.token:
        headers["Authorization"] = f"Bearer {config.token}"
    else:
        credentials = f"{config.username}:{config.password}".encode("utf-8")
        import base64

        headers["Authorization"] = "Basic " + base64.b64encode(credentials).decode("ascii")

    return headers


def project_locator(project_id: str) -> str:
    encoded = urllib.parse.quote(project_id, safe="")
    return f"id:{encoded}"


def normalize_public_teamcity_url(
    config: TeamCityConfig,
    server_url: str | None,
) -> str | None:
    """把 TeamCity 服务端返回的内网 URL 重写成当前配置的公开访问基址。
    Rewrite a TeamCity server-returned intranet URL onto the currently configured public base URL.

    TeamCity Web API 常会返回服务器自己声明的 `webUrl`，当操作者通过外网反向代理访问时，
    这些地址通常仍指向内网主机名或端口，导致日志里的链接无法直接打开。
    The TeamCity Web API often returns the server-declared `webUrl`, and when operators access TeamCity through a public reverse proxy,
    those links usually still point at intranet hosts or ports, which makes the logged links unusable.
    """
    if not server_url:
        return None

    parsed_base_url = urllib.parse.urlparse(config.base_url)
    if not parsed_base_url.scheme or not parsed_base_url.netloc:
        return server_url

    parsed_server_url = urllib.parse.urlparse(server_url)
    server_path = parsed_server_url.path or ""
    if not parsed_server_url.scheme and not parsed_server_url.netloc:
        server_path = server_url if server_url.startswith("/") else f"/{server_url}"

    base_path_prefix = parsed_base_url.path.rstrip("/")
    rebased_path = f"{base_path_prefix}{server_path}" if base_path_prefix else server_path
    if not rebased_path.startswith("/"):
        rebased_path = f"/{rebased_path}"

    return urllib.parse.urlunparse(
        (
            parsed_base_url.scheme,
            parsed_base_url.netloc,
            rebased_path,
            parsed_server_url.params,
            parsed_server_url.query,
            parsed_server_url.fragment,
        )
    )


def get_request_retry_attempts(method: str) -> int:
    return DEFAULT_GET_RETRY_ATTEMPTS if method.upper() == "GET" else 1


def get_retry_delay_seconds(attempt: int) -> int:
    return min(max(attempt, 1), DEFAULT_GET_RETRY_MAX_DELAY_SECONDS)


def is_transient_http_error(status_code: int, response_body: str) -> bool:
    if status_code == 502:
        return True
    if status_code != 404:
        return False

    normalized_body = response_body.lower()
    return all(marker in normalized_body for marker in CPOLAR_TRANSIENT_404_MARKERS)


def should_retry_http_error(method: str, status_code: int, response_body: str) -> bool:
    return method.upper() == "GET" and is_transient_http_error(status_code, response_body)


def should_retry_url_error(method: str) -> bool:
    return method.upper() == "GET"


def api_request(
    config: TeamCityConfig,
    method: str,
    path: str,
    *,
    payload: Any | None = None,
) -> TeamCityResponse:
    url = f"{config.base_url}{path}"
    body: bytes | None = None
    if payload is not None:
        body = json.dumps(payload, ensure_ascii=False, indent=2).encode("utf-8")

    retry_attempts = get_request_retry_attempts(method)
    for attempt in range(1, retry_attempts + 1):
        request = urllib.request.Request(
            url=url,
            method=method,
            data=body,
            headers=build_headers(config, has_json_body=payload is not None),
        )

        try:
            with urllib.request.urlopen(request) as response:
                raw = response.read().decode("utf-8")
                data = json.loads(raw) if raw else {}
                return TeamCityResponse(status_code=response.status, data=data)
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", errors="replace")
            if attempt < retry_attempts and should_retry_http_error(method, exc.code, raw):
                delay_seconds = get_retry_delay_seconds(attempt)
                print(
                    "[TeamCitySkill] transient API error, retrying. "
                    f"method={method} url={url} status={exc.code} attempt={attempt}/{retry_attempts} "
                    f"delaySeconds={delay_seconds}"
                )
                time.sleep(delay_seconds)
                continue
            raise TeamCityApiError(
                f"TeamCity API request failed. method={method} url={url} status={exc.code} body={raw}"
            ) from exc
        except urllib.error.URLError as exc:
            if attempt < retry_attempts and should_retry_url_error(method):
                delay_seconds = get_retry_delay_seconds(attempt)
                print(
                    "[TeamCitySkill] transient connection error, retrying. "
                    f"method={method} url={url} error={exc.reason} attempt={attempt}/{retry_attempts} "
                    f"delaySeconds={delay_seconds}"
                )
                time.sleep(delay_seconds)
                continue
            raise TeamCityApiError(f"Failed to connect to TeamCity: {exc}") from exc

    raise TeamCityApiError(f"TeamCity API request exhausted retries. method={method} url={url}")


def api_request_text(
    config: TeamCityConfig,
    method: str,
    path: str,
) -> str:
    url = f"{config.base_url}{path}"
    retry_attempts = get_request_retry_attempts(method)
    for attempt in range(1, retry_attempts + 1):
        request = urllib.request.Request(
            url=url,
            method=method,
            headers=build_headers(config),
        )

        try:
            with urllib.request.urlopen(request) as response:
                return response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", errors="replace")
            if attempt < retry_attempts and should_retry_http_error(method, exc.code, raw):
                delay_seconds = get_retry_delay_seconds(attempt)
                print(
                    "[TeamCitySkill] transient API error, retrying text request. "
                    f"method={method} url={url} status={exc.code} attempt={attempt}/{retry_attempts} "
                    f"delaySeconds={delay_seconds}"
                )
                time.sleep(delay_seconds)
                continue
            raise TeamCityApiError(
                f"TeamCity API request failed. method={method} url={url} status={exc.code} body={raw}"
            ) from exc
        except urllib.error.URLError as exc:
            if attempt < retry_attempts and should_retry_url_error(method):
                delay_seconds = get_retry_delay_seconds(attempt)
                print(
                    "[TeamCitySkill] transient connection error, retrying text request. "
                    f"method={method} url={url} error={exc.reason} attempt={attempt}/{retry_attempts} "
                    f"delaySeconds={delay_seconds}"
                )
                time.sleep(delay_seconds)
                continue
            raise TeamCityApiError(f"Failed to connect to TeamCity: {exc}") from exc

    raise TeamCityApiError(f"TeamCity API request exhausted retries. method={method} url={url}")


def get_project(config: TeamCityConfig) -> dict[str, Any]:
    locator = project_locator(resolve_project_id(config))
    response = api_request(
        config,
        "GET",
        f"/app/rest/projects/{locator}?fields=id,name,externalId,parentProject(id,name),href",
    )
    return response.data


def get_versioned_settings(config: TeamCityConfig) -> dict[str, Any]:
    locator = project_locator(resolve_project_id(config))
    try:
        response = api_request(
            config,
            "GET",
            f"/app/rest/projects/{locator}/versionedSettings",
        )
        return response.data
    except TeamCityApiError as exc:
        if "status=404" not in str(exc):
            raise
    return get_project_feature_versioned_settings(config)


def get_project_feature_versioned_settings(config: TeamCityConfig) -> dict[str, Any]:
    locator = project_locator(resolve_project_id(config))
    response = api_request(
        config,
        "GET",
        f"/app/rest/projects/{locator}?fields=projectFeatures(projectFeature(id,type,href,properties(property(name,value),count,href)),count)",
    )
    project_features = response.data.get("projectFeatures", {})
    features = project_features.get("projectFeature", [])
    if not isinstance(features, list):
        features = []

    for feature in features:
        if feature.get("type") == "versionedSettings":
            return feature

    raise TeamCityApiError(
        f"No projectFeatures(type=versionedSettings) found for project {resolve_project_id(config)}"
    )


def normalize_versioned_settings(raw_settings: dict[str, Any]) -> dict[str, Any]:
    normalized = dict(raw_settings)

    properties = raw_settings.get("properties")
    if raw_settings.get("type") == "versionedSettings" and isinstance(properties, dict):
        property_list = properties.get("property", [])
        if isinstance(property_list, list):
            for item in property_list:
                name = item.get("name")
                if isinstance(name, str):
                    normalized[name] = item.get("value")
            normalized["propertyKeys"] = sorted(
                item.get("name") for item in property_list if isinstance(item.get("name"), str)
            )
            normalized["featureId"] = raw_settings.get("id")

    return normalized


def get_versioned_settings_handle(config: TeamCityConfig) -> VersionedSettingsHandle:
    locator = project_locator(resolve_project_id(config))
    raw_settings = get_versioned_settings(config)
    normalized = normalize_versioned_settings(raw_settings)

    if raw_settings.get("type") == "versionedSettings" and raw_settings.get("id"):
        feature_id = urllib.parse.quote(str(raw_settings["id"]), safe="")
        update_path = f"/app/rest/projects/{locator}/projectFeatures/id:{feature_id}"
        source_mode = "projectFeature"
    else:
        update_path = f"/app/rest/projects/{locator}/versionedSettings"
        source_mode = "legacyEndpoint"

    return VersionedSettingsHandle(
        raw=raw_settings,
        normalized=normalized,
        source_mode=source_mode,
        update_path=update_path,
    )


def list_projects(config: TeamCityConfig) -> list[dict[str, Any]]:
    response = api_request(
        config,
        "GET",
        "/app/rest/projects?fields=project(id,name,externalId,parentProjectId,href),count",
    )
    projects = response.data.get("project", [])
    return projects if isinstance(projects, list) else []


def resolve_project_id(config: TeamCityConfig) -> str:
    if config.resolved_project_id:
        return config.resolved_project_id

    requested_project_id = config.project_id
    direct_locator = project_locator(requested_project_id)
    try:
        response = api_request(
            config,
            "GET",
            f"/app/rest/projects/{direct_locator}?fields=id,name,externalId,parentProject(id,name),href",
        )
        project = response.data
        config.resolved_project_id = project.get("id") or requested_project_id
        config.resolved_project_match = "direct-id"
        return config.resolved_project_id
    except TeamCityApiError as exc:
        if "status=404" not in str(exc):
            raise

    projects = list_projects(config)
    for field in ("id", "externalId", "name"):
        for project in projects:
            value = project.get(field)
            if isinstance(value, str) and value == requested_project_id:
                config.resolved_project_id = project.get("id") or requested_project_id
                config.resolved_project_match = f"lookup:{field}"
                return config.resolved_project_id

    sample_projects = ", ".join(
        filter(None, (project.get("id") for project in projects[:10]))
    )
    raise TeamCityApiError(
        "Failed to resolve TeamCity project. "
        f"requested={requested_project_id!r}. "
        "Tried direct id lookup and fallback matching by id/externalId/name. "
        f"Sample available project ids: {sample_projects or '<empty>'}"
    )


def write_json(file_path: Path, data: Any) -> None:
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def read_json(file_path: Path) -> Any:
    return json.loads(file_path.read_text(encoding="utf-8"))


def summarize_project(project: dict[str, Any]) -> None:
    print("[TeamCitySkill] project summary")
    print(f"  id={project.get('id')}")
    print(f"  name={project.get('name')}")
    if "externalId" in project:
        print(f"  externalId={project.get('externalId')}")
    parent = project.get("parentProject") or {}
    print(f"  parent={parent.get('name') or '-'}")
    print(f"  href={project.get('href')}")


def summarize_versioned_settings(settings: dict[str, Any]) -> None:
    normalized = normalize_versioned_settings(settings)
    print("[TeamCitySkill] versioned settings summary")
    for key in (
        "enabled",
        "format",
        "settingsPath",
        "synchronizationMode",
        "buildSettings",
        "allowUIEditing",
        "buildSettingsMode",
        "showChanges",
        "rootId",
    ):
        if key in normalized:
            print(f"  {key}={normalized.get(key)!r}")

    vcs_root = normalized.get("vcsRootInstance") or normalized.get("vcsRoot") or {}
    if isinstance(vcs_root, dict) and vcs_root:
        print(
            "  vcsRoot="
            f"{vcs_root.get('id') or vcs_root.get('name') or vcs_root.get('href') or vcs_root}"
        )

    if normalized.get("featureId"):
        print(f"  featureId={normalized.get('featureId')}")
    if normalized.get("propertyKeys"):
        print(f"  propertyKeys={normalized.get('propertyKeys')}")

    print(f"  topLevelKeys={sorted(settings.keys())}")


def print_resolution_summary(config: TeamCityConfig) -> None:
    if not config.resolved_project_id:
        return
    print("[TeamCitySkill] project locator resolution")
    print(f"  requested={config.project_id}")
    print(f"  resolved={config.resolved_project_id}")
    print(f"  match={config.resolved_project_match or 'unknown'}")


def normalize_truthy(value: Any) -> bool:
    if isinstance(value, bool):
        return value
    if isinstance(value, str):
        return value.strip().lower() in {"true", "yes", "enabled", "on"}
    return False


def summarize_vcs_loading(settings: dict[str, Any]) -> None:
    normalized = normalize_versioned_settings(settings)
    enabled = normalize_truthy(normalized.get("enabled"))
    settings_path = normalized.get("settingsPath")
    synchronization_mode = normalized.get("synchronizationMode")
    build_settings = normalized.get("buildSettings")
    root_id = normalized.get("rootId")
    vcs_root = normalized.get("vcsRootInstance") or normalized.get("vcsRoot")
    has_vcs_root = bool(vcs_root or root_id)

    prefers_vcs = isinstance(build_settings, str) and build_settings.upper() in {
        "PREFER_VCS",
        "USE_VCS",
        "USE_SETTINGS_FROM_VCS",
    }
    prefers_current = isinstance(build_settings, str) and build_settings.upper() in {
        "PREFER_CURRENT",
        "USE_CURRENT",
        "USE_CURRENT_SETTINGS",
    }

    likely_loaded_from_vcs = enabled and has_vcs_root and (
        bool(settings_path) or bool(synchronization_mode) or prefers_vcs
    )

    print("[TeamCitySkill] versioned settings VCS check")
    print(f"  enabled={enabled}")
    print(f"  settingsPath={settings_path!r}")
    print(f"  synchronizationMode={synchronization_mode!r}")
    print(f"  buildSettings={build_settings!r}")
    print(f"  rootId={root_id!r}")
    print(f"  hasVcsRoot={has_vcs_root}")
    print(f"  likelyLoadedFromVcs={likely_loaded_from_vcs}")
    if prefers_current:
        print("  note=当前策略更偏向保留 TeamCity 当前设置，而不是优先采用 VCS 里的新设置。")
    elif not likely_loaded_from_vcs:
        print(
            "  note=当前返回结构未能明确证明已从 VCS 加载；请结合 topLevelKeys 和导出 JSON 继续检查。"
        )


def command_show_project(config: TeamCityConfig) -> int:
    print("[TeamCitySkill] ===== Step 1/2: fetch project =====")
    project = get_project(config)
    print_resolution_summary(config)
    summarize_project(project)

    print("[TeamCitySkill] ===== Step 2/2: fetch versioned settings =====")
    settings_handle = get_versioned_settings_handle(config)
    print(f"[TeamCitySkill] versioned settings source={settings_handle.source_mode}")
    summarize_versioned_settings(settings_handle.raw)
    return 0


def command_verify_vcs(config: TeamCityConfig) -> int:
    print("[TeamCitySkill] ===== Step 1/2: fetch project =====")
    project = get_project(config)
    print_resolution_summary(config)
    summarize_project(project)

    print("[TeamCitySkill] ===== Step 2/2: verify versioned settings from VCS =====")
    settings_handle = get_versioned_settings_handle(config)
    print(f"[TeamCitySkill] versioned settings source={settings_handle.source_mode}")
    summarize_versioned_settings(settings_handle.raw)
    summarize_vcs_loading(settings_handle.raw)
    return 0


def command_export_current(config: TeamCityConfig, output_path: Path) -> int:
    print("[TeamCitySkill] ===== Step 1/3: fetch project =====")
    project = get_project(config)
    print_resolution_summary(config)
    summarize_project(project)

    print("[TeamCitySkill] ===== Step 2/3: fetch current versioned settings =====")
    settings_handle = get_versioned_settings_handle(config)
    print(f"[TeamCitySkill] versioned settings source={settings_handle.source_mode}")
    summarize_versioned_settings(settings_handle.raw)

    print("[TeamCitySkill] ===== Step 3/3: write json =====")
    write_json(output_path, settings_handle.raw)
    print(f"[TeamCitySkill] current settings exported to: {output_path}")
    return 0


def command_apply(
    config: TeamCityConfig,
    payload_path: Path,
    output_path: Path,
    *,
    dry_run: bool,
) -> int:
    if not payload_path.exists():
        raise TeamCityApiError(f"Payload file does not exist: {payload_path}")

    print("[TeamCitySkill] ===== Step 1/5: fetch project =====")
    project = get_project(config)
    print_resolution_summary(config)
    summarize_project(project)

    print("[TeamCitySkill] ===== Step 2/5: backup current versioned settings =====")
    before_handle = get_versioned_settings_handle(config)
    print(f"[TeamCitySkill] versioned settings source={before_handle.source_mode}")
    summarize_versioned_settings(before_handle.raw)
    write_json(output_path, before_handle.raw)
    print(f"[TeamCitySkill] backup saved to: {output_path}")

    print("[TeamCitySkill] ===== Step 3/5: load desired payload =====")
    payload = read_json(payload_path)
    print(f"[TeamCitySkill] payload file={payload_path}")

    target_path = before_handle.update_path
    if dry_run:
        print("[TeamCitySkill] dry-run enabled, skip PUT request")
        print(f"[TeamCitySkill] target={config.base_url}{target_path}")
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    print("[TeamCitySkill] ===== Step 4/5: PUT versioned settings =====")
    response = api_request(config, "PUT", target_path, payload=payload)
    print(f"[TeamCitySkill] update status={response.status_code}")

    print("[TeamCitySkill] ===== Step 5/5: verify after update =====")
    after_handle = get_versioned_settings_handle(config)
    print(f"[TeamCitySkill] versioned settings source={after_handle.source_mode}")
    summarize_versioned_settings(after_handle.raw)
    return 0


def parse_build_properties(raw_properties: list[str]) -> list[dict[str, str]]:
    properties: list[dict[str, str]] = []
    for raw_property in raw_properties:
        if "=" not in raw_property:
            raise TeamCityApiError(
                f"Invalid --property value: {raw_property!r}. Expected name=value"
            )
        name, value = raw_property.split("=", 1)
        normalized_name = name.strip()
        if not normalized_name:
            raise TeamCityApiError(
                f"Invalid --property value: {raw_property!r}. Property name is empty"
            )
        properties.append({"name": normalized_name, "value": value})
    return properties


def merge_named_properties(
    base_properties: list[dict[str, str]],
    override_properties: list[dict[str, str]],
) -> list[dict[str, str]]:
    """按属性名合并参数，显式覆盖项优先。 Merge build properties by name and let explicit overrides win."""

    merged_properties = [dict(property_item) for property_item in base_properties]
    index_by_name = {
        (property_item.get("name") or "").strip(): index
        for index, property_item in enumerate(merged_properties)
        if isinstance(property_item, dict)
    }

    for property_item in override_properties:
        if not isinstance(property_item, dict):
            continue
        property_name = (property_item.get("name") or "").strip()
        if not property_name:
            continue
        if property_name in index_by_name:
            merged_properties[index_by_name[property_name]] = dict(property_item)
            continue
        index_by_name[property_name] = len(merged_properties)
        merged_properties.append(dict(property_item))

    return merged_properties


def parse_build_type_ids(raw_build_type_ids: list[str]) -> list[str]:
    normalized_build_type_ids: list[str] = []
    for raw_value in raw_build_type_ids:
        for part in raw_value.split(","):
            normalized = part.strip()
            if normalized:
                normalized_build_type_ids.append(normalized)
    return normalized_build_type_ids


def parse_build_tags(raw_tags: list[str]) -> list[str]:
    normalized_tags: list[str] = []
    seen_tags: set[str] = set()
    for raw_value in raw_tags:
        for part in raw_value.split(","):
            normalized = part.strip()
            if normalized and normalized not in seen_tags:
                normalized_tags.append(normalized)
                seen_tags.add(normalized)
    return normalized_tags


def build_queue_comment(
    *,
    build_type_id: str,
    branch: str | None,
    comment: str | None,
) -> str:
    parts: list[str] = []
    normalized_comment = (comment or "").strip()
    if normalized_comment:
        parts.append(normalized_comment)

    parts.append(f"测试目标: {build_type_id}")
    if branch:
        parts.append(f"分支: {branch}")
    return " | ".join(parts)


def build_queue_tags(
    *,
    build_type_id: str,
    tags: list[str],
) -> list[str]:
    """规范化用户显式传入的 TeamCity build tags。

    Normalize the TeamCity build tags provided explicitly by the caller.

    不再自动注入 teamcityskill 或 buildTypeId；
    测试目标、分支与触发来源等信息统一留在 comment 中。
    The helper no longer injects teamcityskill or buildTypeId automatically;
    target, branch, and trigger-source details stay in the comment instead.
    """
    merged_tags: list[str] = []
    seen_tags: set[str] = set()
    for tag in tags:
        normalized = tag.strip()
        if normalized and normalized not in seen_tags:
            merged_tags.append(normalized)
            seen_tags.add(normalized)
    return merged_tags


def build_queue_payload(
    *,
    build_type_id: str,
    branch: str | None,
    properties: list[dict[str, str]],
    comment: str | None,
    tags: list[str],
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "buildType": {"id": build_type_id},
        "comment": {
            "text": build_queue_comment(
                build_type_id=build_type_id,
                branch=branch,
                comment=comment,
            )
        },
    }
    if branch:
        payload["branchName"] = branch
    if properties:
        payload["properties"] = {"property": properties}

    resolved_tags = build_queue_tags(build_type_id=build_type_id, tags=tags)
    if resolved_tags:
        payload["tags"] = {
            "tag": [{"name": tag} for tag in resolved_tags]
        }
    return payload


def resolve_single_build_type_id(raw_build_type_ids: list[str]) -> str:
    build_type_ids = parse_build_type_ids(raw_build_type_ids)
    if len(build_type_ids) != 1:
        raise TeamCityApiError(
            "run-build requires exactly one --build-type-id. "
            f"got={build_type_ids!r}"
        )
    return build_type_ids[0]


def merge_queue_properties_with_ci_credentials(
    config: TeamCityConfig,
    properties: list[dict[str, str]],
) -> list[dict[str, str]]:
    """为排队构建补齐当前会话凭据，同时保留显式传入属性优先级。 Inject current-session CI credentials into queued builds while preserving explicit caller overrides."""

    merged_properties = list(properties)
    property_names = {
        (property_item.get("name") or "").strip()
        for property_item in properties
        if isinstance(property_item, dict)
    }

    if config.token and "env.TEAMCITY_TOKEN" not in property_names:
        merged_properties.append({"name": "env.TEAMCITY_TOKEN", "value": config.token})
        property_names.add("env.TEAMCITY_TOKEN")

    if not config.token and config.username and config.password:
        if "env.TEAMCITY_USERNAME" not in property_names:
            merged_properties.append(
                {"name": "env.TEAMCITY_USERNAME", "value": config.username}
            )
            property_names.add("env.TEAMCITY_USERNAME")
        if "env.TEAMCITY_PASSWORD" not in property_names:
            merged_properties.append(
                {"name": "env.TEAMCITY_PASSWORD", "value": config.password}
            )

    return merged_properties


def queue_build(
    config: TeamCityConfig,
    *,
    build_type_id: str,
    branch: str | None,
    properties: list[dict[str, str]],
    comment: str | None,
    tags: list[str],
) -> QueuedBuildHandle:
    """排队单个 TeamCity 构建，并把当前 API 会话凭据透传给远端 step。 Queue a single TeamCity build and forward the current API-session credentials to the remote step."""

    resolved_properties = merge_queue_properties_with_ci_credentials(config, properties)
    payload = build_queue_payload(
        build_type_id=build_type_id,
        branch=branch,
        properties=resolved_properties,
        comment=comment,
        tags=tags,
    )

    response = api_request(config, "POST", "/app/rest/buildQueue", payload=payload)
    build_id = response.data.get("id")
    if not isinstance(build_id, int):
        raise TeamCityApiError(
            f"Unexpected queue response, missing build id: {response.data!r}"
        )

    return QueuedBuildHandle(
        build_id=build_id,
        href=response.data.get("href"),
        web_url=response.data.get("webUrl"),
    )


def get_build(config: TeamCityConfig, build_id: int) -> dict[str, Any]:
    """读取单个构建的状态快照，并包含等待阶段需要的 running-info 字段。
    Read a single build status snapshot and include the running-info fields needed by the wait loop.
    """
    response = api_request(
        config,
        "GET",
        "/app/rest/builds/id:{buildId}?fields="
        "id,buildTypeId,number,state,status,statusText,branchName,webUrl,"
        "queuedDate,startDate,finishDate,agent(name),comment(text),tags(tag(name)),"
        "running-info(percentageComplete,currentStageText,probablyHanging),properties(property(name,value))".format(
            buildId=build_id
        ),
    )
    return response.data


def get_ready_agents(config: TeamCityConfig) -> list[dict[str, Any]]:
    response = api_request(
        config,
        "GET",
        "/app/rest/agents?locator=connected:true,authorized:true,enabled:true&fields="
        "agent(id,name,enabled,authorized,connected,href),count",
    )
    agents = response.data.get("agent", [])
    return agents if isinstance(agents, list) else []


def get_running_builds(config: TeamCityConfig) -> list[dict[str, Any]]:
    response = api_request(
        config,
        "GET",
        "/app/rest/builds?locator=state:running&fields="
        "build(id,buildTypeId,state,status,statusText,webUrl,agent(id,name)),count",
    )
    builds = response.data.get("build", [])
    return builds if isinstance(builds, list) else []


def get_running_build_counts_by_agent(config: TeamCityConfig) -> dict[int, int]:
    counts: dict[int, int] = {}
    for build in get_running_builds(config):
        agent = build.get("agent") or {}
        agent_id = agent.get("id")
        if isinstance(agent_id, int):
            counts[agent_id] = counts.get(agent_id, 0) + 1
    return counts


def build_ready_agent_handles(config: TeamCityConfig) -> dict[int, AgentHandle]:
    running_counts = get_running_build_counts_by_agent(config)
    ready_agents: dict[int, AgentHandle] = {}
    for agent in get_ready_agents(config):
        agent_id = agent.get("id")
        if not isinstance(agent_id, int):
            continue
        ready_agents[agent_id] = AgentHandle(
            agent_id=agent_id,
            name=str(agent.get("name") or f"agent-{agent_id}"),
            enabled=bool(agent.get("enabled", False)),
            authorized=bool(agent.get("authorized", False)),
            connected=bool(agent.get("connected", False)),
            running_build_count=running_counts.get(agent_id, 0),
        )
    return ready_agents


def get_compatible_agents_for_build_type(
    config: TeamCityConfig,
    build_type_id: str,
    *,
    ready_agent_ids: set[int] | None = None,
) -> list[dict[str, Any]]:
    encoded_build_type_id = urllib.parse.quote(build_type_id, safe="")
    response = api_request(
        config,
        "GET",
        "/app/rest/buildTypes/id:{buildTypeId}?fields="
        "id,name,compatibleAgents(agent(id,name,enabled,authorized,connected,href),count)".format(
            buildTypeId=encoded_build_type_id
        ),
    )
    compatible_agents = (response.data.get("compatibleAgents") or {}).get("agent", [])
    if not isinstance(compatible_agents, list):
        compatible_agents = []

    if ready_agent_ids is None:
        return compatible_agents

    return [
        agent
        for agent in compatible_agents
        if isinstance(agent.get("id"), int) and agent["id"] in ready_agent_ids
    ]


def build_compatible_agent_ids_by_build_type(
    config: TeamCityConfig,
    build_type_ids: list[str],
    *,
    ready_agents: dict[int, AgentHandle],
) -> dict[str, tuple[int, ...]]:
    ready_agent_ids = set(ready_agents)
    compatible: dict[str, tuple[int, ...]] = {}
    for build_type_id in build_type_ids:
        agents = get_compatible_agents_for_build_type(
            config,
            build_type_id,
            ready_agent_ids=ready_agent_ids,
        )
        compatible[build_type_id] = tuple(
            sorted(
                agent["id"]
                for agent in agents
                if isinstance(agent.get("id"), int)
            )
        )
    return compatible


def find_parallel_assignment(
    build_type_ids: list[str],
    idle_agent_ids_by_build_type: dict[str, tuple[int, ...]],
) -> dict[str, int]:
    matched_build_by_agent: dict[int, str] = {}

    def try_assign(build_type_id: str, visited_agent_ids: set[int]) -> bool:
        for agent_id in idle_agent_ids_by_build_type.get(build_type_id, ()): 
            if agent_id in visited_agent_ids:
                continue
            visited_agent_ids.add(agent_id)
            current_build_type_id = matched_build_by_agent.get(agent_id)
            if current_build_type_id is None or try_assign(current_build_type_id, visited_agent_ids):
                matched_build_by_agent[agent_id] = build_type_id
                return True
        return False

    for build_type_id in build_type_ids:
        try_assign(build_type_id, set())

    return {
        build_type_id: agent_id
        for agent_id, build_type_id in matched_build_by_agent.items()
    }


def build_dispatch_plan(
    build_type_ids: list[str],
    *,
    compatible_agent_ids_by_build_type: dict[str, tuple[int, ...]],
    ready_agents: dict[int, AgentHandle],
    dispatch_mode: str,
) -> BuildDispatchPlan:
    idle_agent_ids_by_build_type = {
        build_type_id: tuple(
            agent_id
            for agent_id in compatible_agent_ids_by_build_type.get(build_type_id, ())
            if ready_agents[agent_id].running_build_count == 0
        )
        for build_type_id in build_type_ids
    }
    parallel_assignment = find_parallel_assignment(build_type_ids, idle_agent_ids_by_build_type)
    can_run_all_in_parallel = len(parallel_assignment) == len(build_type_ids)

    if dispatch_mode == "parallel" and not can_run_all_in_parallel:
        raise TeamCityApiError(
            "dispatch-mode=parallel requires enough idle compatible agents for all builds. "
            f"idleAgentIdsByBuildType={idle_agent_ids_by_build_type!r}"
        )

    if dispatch_mode == "parallel":
        mode = "parallel"
        reason = "forced by --dispatch-mode parallel"
    elif dispatch_mode == "sequential":
        mode = "sequential"
        reason = "forced by --dispatch-mode sequential"
    elif can_run_all_in_parallel and len(build_type_ids) > 1:
        mode = "parallel"
        reason = "enough idle compatible agents are available for all requested builds"
    else:
        mode = "sequential"
        if len(build_type_ids) <= 1:
            reason = "only one build was requested"
        else:
            reason = "not enough idle compatible agents for a full parallel dispatch"

    return BuildDispatchPlan(
        mode=mode,
        reason=reason,
        compatible_agent_ids_by_build_type=compatible_agent_ids_by_build_type,
        idle_agent_ids_by_build_type=idle_agent_ids_by_build_type,
        parallel_assignment=parallel_assignment,
    )


def print_agent_summary(ready_agents: dict[int, AgentHandle]) -> None:
    print("[TeamCitySkill] ready agents")
    if not ready_agents:
        print("  <empty>")
        return
    for agent in ready_agents.values():
        print(
            f"  id={agent.agent_id} name={agent.name!r} "
            f"connected={agent.connected} authorized={agent.authorized} "
            f"enabled={agent.enabled} runningBuildCount={agent.running_build_count}"
        )


def print_dispatch_plan(
    plan: BuildDispatchPlan,
    *,
    ready_agents: dict[int, AgentHandle],
) -> None:
    print("[TeamCitySkill] build group dispatch plan")
    print(f"  mode={plan.mode}")
    print(f"  reason={plan.reason}")
    for build_type_id in plan.compatible_agent_ids_by_build_type:
        compatible_names = [
            ready_agents[agent_id].name
            for agent_id in plan.compatible_agent_ids_by_build_type[build_type_id]
            if agent_id in ready_agents
        ]
        idle_names = [
            ready_agents[agent_id].name
            for agent_id in plan.idle_agent_ids_by_build_type[build_type_id]
            if agent_id in ready_agents
        ]
        print(
            f"  buildTypeId={build_type_id!r} "
            f"compatibleAgents={compatible_names or ['<empty>']} "
            f"idleAgents={idle_names or ['<empty>']}"
        )
    if plan.parallel_assignment:
        assignment_labels = [
            f"{build_type_id}->{ready_agents[agent_id].name}"
            for build_type_id, agent_id in sorted(plan.parallel_assignment.items())
        ]
        print(f"  parallelAssignment={assignment_labels}")


def _fetch_build_state_only(config: TeamCityConfig, build_id: int) -> dict[str, Any]:
    """仅查询构建的 state/status/finishDate，用于快速判断是否已结束。
    Fetch only state/status/finishDate for a fast completion check without the heavy running-info payload.
    """
    response = api_request(
        config,
        "GET",
        "/app/rest/builds/id:{buildId}?fields=id,state,status,finishDate".format(buildId=build_id),
    )
    return response.data


def wait_for_build_completion(
    config: TeamCityConfig,
    *,
    build_id: int,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
) -> int:
    """轮询构建直到完成，并在长时间等待时输出稳定心跳与阶段信息。
    Poll the build until completion and emit steady heartbeats plus stage information during long waits.

    首次轮询前先做一次轻量 state-only 检查：如果构建已经 finished，直接跳过详细轮询。
    Before the first full poll, perform a lightweight state-only check so already-finished builds
    are detected immediately without waiting for the heartbeat interval.
    """
    deadline = time.monotonic() + max(timeout_seconds, 1)
    poll_interval = max(poll_interval_seconds, 1)
    heartbeat_interval_seconds = max(DEFAULT_WAIT_HEARTBEAT_SECONDS, poll_interval)
    last_state: tuple[Any, Any, Any, Any, Any] | None = None
    last_report_time: float | None = None

    # 快速检查：如果构建已结束，直接打印完整摘要并返回。
    # Fast-path: if the build is already finished, print a full summary and return immediately.
    quick = _fetch_build_state_only(config, build_id)
    if quick.get("state") == "finished":
        build = get_build(config, build_id)
        print_build_summary(config, build)
        if build.get("status") != "SUCCESS":
            print("[TeamCitySkill] build log tail")
            print(get_build_log_tail(config, build_id, log_tail_lines))
            return 1
        return 0

    while True:
        current_time = time.monotonic()
        build = get_build(config, build_id)
        running_info = build.get("running-info") or {}
        current_state = (
            build.get("state"),
            build.get("status"),
            build.get("statusText"),
            running_info.get("currentStageText"),
            running_info.get("probablyHanging"),
        )
        if (
            current_state != last_state
            or last_report_time is None
            or current_time - last_report_time >= heartbeat_interval_seconds
        ):
            print_build_summary(config, build)
            last_state = current_state
            last_report_time = current_time

        if build.get("state") == "finished":
            if build.get("status") != "SUCCESS":
                print("[TeamCitySkill] build log tail")
                print(get_build_log_tail(config, build_id, log_tail_lines))
                return 1
            return 0

        if current_time >= deadline:
            raise TeamCityApiError(
                f"Timed out waiting for build {build_id} after {timeout_seconds} seconds"
            )

        time.sleep(poll_interval)


def get_build_log_tail(config: TeamCityConfig, build_id: int, tail_lines: int) -> str:
    raw_log = api_request_text(config, "GET", f"/downloadBuildLog.html?buildId={build_id}")
    if tail_lines <= 0:
        return raw_log
    lines = raw_log.splitlines()
    return "\n".join(lines[-tail_lines:])


def print_build_summary(config: TeamCityConfig, build: dict[str, Any]) -> None:
    """打印构建摘要，并把链接重写成当前配置下可直接访问的公开地址。
    Print a build summary and rewrite links into the currently configured publicly reachable base URL.
    """
    print("[TeamCitySkill] build summary")
    summary_keys = (
        "id",
        "buildTypeId",
        "number",
        "state",
        "status",
        "statusText",
        "branchName",
        "queuedDate",
        "startDate",
        "finishDate",
    )
    for key in summary_keys:
        if key in build:
            print(f"  {key}={build.get(key)!r}")

    web_url = build.get("webUrl")
    if web_url:
        normalized_web_url = normalize_public_teamcity_url(config, web_url)
        if normalized_web_url:
            print(f"  webUrl={normalized_web_url!r}")
            if normalized_web_url != web_url:
                print(f"  serverWebUrl={web_url!r}")

    running_info = build.get("running-info") or {}
    if isinstance(running_info, dict):
        if running_info.get("percentageComplete") is not None:
            print(f"  progress={running_info.get('percentageComplete')!r}")
        if running_info.get("probablyHanging") is not None:
            print(f"  hanging={running_info.get('probablyHanging')!r}")
        if running_info.get("currentStageText"):
            print(f"  stage={running_info.get('currentStageText')!r}")

    agent = build.get("agent") or {}
    if isinstance(agent, dict) and agent.get("name"):
        print(f"  agent={agent.get('name')!r}")
    comment = build.get("comment") or {}
    if isinstance(comment, dict) and comment.get("text"):
        print(f"  comment={comment.get('text')!r}")
    tags = (build.get("tags") or {}).get("tag", [])
    if isinstance(tags, list):
        tag_names = [tag.get("name") for tag in tags if isinstance(tag, dict) and tag.get("name")]
        if tag_names:
            print(f"  tags={tag_names!r}")


def resolve_bash_executable() -> str:
    """解析本机 bash 路径，供本地 Talos gate 稳定执行。 Resolve the local bash executable path for the Talos preflight gate."""

    resolved_bash = shutil.which("bash")
    if resolved_bash:
        return resolved_bash
    raise TeamCityApiError("bash is required for the local Talos batchmode preflight gate")


def resolve_talos_package_build_type_id(
    platform: str,
    package_build_type_id: str | None,
) -> str:
    """按平台解析 Talos BaseFlow 所依赖的母包 BuildType。 Resolve the package BuildType required by the Talos BaseFlow chain for the selected platform."""

    normalized_override = (package_build_type_id or "").strip()
    if normalized_override:
        return normalized_override

    if platform == "android":
        return "BDFrameworkCore_BuildClientPackageAndroid"
    if platform == "windows":
        return "BDFrameworkCore_BuildClientPackageWindows"

    raise TeamCityApiError(f"Unsupported Talos BaseFlow chain platform: {platform!r}")


def run_local_talos_batchmode_preflight(
    *,
    unity_path: str | None,
    project_path: Path | None,
    test_file: str | None,
    local_batchmode_mode: str,
) -> int:
    """执行本地 Talos batchmode gate，并把结果作为远端链路的前置条件。 Run the local Talos batchmode gate and use its exit code as the prerequisite for the remote chain."""

    resolved_unity_path = (unity_path or os.environ.get("UNITY_PATH", "")).strip()
    if not resolved_unity_path:
        raise TeamCityApiError(
            "run-talos-baseflow-chain requires --unity-path or UNITY_PATH so the local Talos batchmode gate can run"
        )

    resolved_project_path = (
        project_path.expanduser().resolve()
        if project_path is not None
        else REPO_ROOT
    )
    normalized_test_file = (test_file or "").strip()

    if not TALOS_BATCHMODE_SCRIPT.is_file():
        raise TeamCityApiError(
            f"Talos batchmode script is missing: {TALOS_BATCHMODE_SCRIPT}"
        )

    environment = os.environ.copy()
    environment["UNITY_PATH"] = resolved_unity_path
    environment["PROJECT_PATH"] = str(resolved_project_path)
    if local_batchmode_mode == "sync":
        environment["TALOS_MODE"] = "sync"
        if normalized_test_file:
            print(
                "[TeamCitySkill] local sync fallback note: sync mode executes the full exported Talos suite set instead of Playwright-spec filtering"
            )

    command = [resolve_bash_executable(), str(TALOS_BATCHMODE_SCRIPT)]
    if normalized_test_file:
        command.extend(["--test-file", normalized_test_file])

    print("[TeamCitySkill] ===== Step 1/3: local Talos batchmode gate =====")
    print(f"[TeamCitySkill] localMode={local_batchmode_mode!r}")
    print(f"[TeamCitySkill] unityPath={resolved_unity_path!r}")
    print(f"[TeamCitySkill] projectPath={str(resolved_project_path)!r}")
    if normalized_test_file:
        print(f"[TeamCitySkill] testFile={normalized_test_file!r}")

    completed_process = subprocess.run(
        command,
        cwd=str(PLAYWRIGHT_DIR),
        env=environment,
        check=False,
    )
    if completed_process.returncode != 0:
        print(
            f"[TeamCitySkill][ERROR] local Talos batchmode gate failed with exit code {completed_process.returncode}"
        )
    return completed_process.returncode


def queue_and_wait_build(
    config: TeamCityConfig,
    *,
    build_type_id: str,
    branch: str | None,
    properties: list[dict[str, str]],
    comment: str | None,
    tags: list[str],
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
    step_title: str,
) -> tuple[int, QueuedBuildHandle]:
    """排队并等待单个 TeamCity 构建完成，返回退出码和构建句柄。 Queue and wait for a single TeamCity build, then return both the exit code and queued handle."""

    print(f"[TeamCitySkill] ===== {step_title}: queue build =====")
    queued = queue_build(
        config,
        build_type_id=build_type_id,
        branch=branch,
        properties=properties,
        comment=comment,
        tags=tags,
    )
    print(f"[TeamCitySkill] queued build id={queued.build_id}")
    if queued.web_url:
        normalized_web_url = normalize_public_teamcity_url(config, queued.web_url)
        print(f"[TeamCitySkill] webUrl={normalized_web_url or queued.web_url}")
        if normalized_web_url and normalized_web_url != queued.web_url:
            print(f"[TeamCitySkill] serverWebUrl={queued.web_url}")

    print(f"[TeamCitySkill] ===== {step_title}: wait for build =====")
    result = wait_for_build_completion(
        config,
        build_id=queued.build_id,
        timeout_seconds=timeout_seconds,
        poll_interval_seconds=poll_interval_seconds,
        log_tail_lines=log_tail_lines,
    )
    return result, queued


def command_run_talos_baseflow_chain(
    config: TeamCityConfig,
    *,
    platform: str,
    unity_path: str | None,
    project_path: Path | None,
    local_batchmode_mode: str,
    allow_local_sync_fallback: bool,
    test_file: str | None,
    client_version: str,
    build_debug: str,
    branch: str | None,
    comment: str | None,
    tags: list[str],
    adb_serial: str | None,
    adb_connect_targets: str | None,
    emulator_type: str | None,
    baseflow_build_type_id: str,
    package_build_type_id: str | None,
    extra_properties: list[dict[str, str]],
    dry_run: bool,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
) -> int:
    """按“本地 gate -> 母包 -> BaseFlow”顺序执行 Talos 链路。 Run the Talos chain in the strict order of local gate -> package build -> BaseFlow."""

    if dry_run:
        raise TeamCityApiError(
            "run-talos-baseflow-chain does not support --dry-run because the BaseFlow step depends on the real package build id"
        )

    normalized_platform = platform.strip().lower()
    if local_batchmode_mode == "sync" and not allow_local_sync_fallback:
        raise TeamCityApiError(
            "local sync fallback is less consistent than TCP mode; pass --allow-local-sync-fallback to opt into it explicitly"
        )

    local_gate_result = run_local_talos_batchmode_preflight(
        unity_path=unity_path,
        project_path=project_path,
        test_file=test_file,
        local_batchmode_mode=local_batchmode_mode,
    )
    if local_gate_result != 0:
        return local_gate_result

    resolved_package_build_type_id = resolve_talos_package_build_type_id(
        normalized_platform,
        package_build_type_id,
    )
    normalized_test_file = (test_file or DEFAULT_TALOS_BASEFLOW_TEST_FILE).strip() or DEFAULT_TALOS_BASEFLOW_TEST_FILE
    normalized_tags = build_queue_tags(
        build_type_id=baseflow_build_type_id,
        tags=tags,
    )
    if normalized_platform not in normalized_tags:
        normalized_tags.append(normalized_platform)

    package_properties = merge_named_properties(
        [
            {"name": "build.client.version", "value": client_version},
            {"name": "build.debugBuild", "value": build_debug},
        ],
        extra_properties,
    )
    package_result, queued_package = queue_and_wait_build(
        config,
        build_type_id=resolved_package_build_type_id,
        branch=branch,
        properties=package_properties,
        comment=(comment or "Talos BaseFlow package rebuild").strip(),
        tags=normalized_tags,
        timeout_seconds=timeout_seconds,
        poll_interval_seconds=poll_interval_seconds,
        log_tail_lines=log_tail_lines,
        step_title="Step 2/3: package build",
    )
    if package_result != 0:
        return package_result

    baseflow_defaults = [
        {"name": "build.client.version", "value": client_version},
        {"name": "build.debugBuild", "value": build_debug},
        {"name": "talos.e2e.platform", "value": normalized_platform},
        {"name": "talos.e2e.package.build.id", "value": str(queued_package.build_id)},
        {"name": "talos.e2e.package.build.type.id", "value": resolved_package_build_type_id},
        {"name": "talos.e2e.test.file", "value": normalized_test_file},
    ]
    if adb_serial:
        baseflow_defaults.append({"name": "talos.e2e.adb.serial", "value": adb_serial.strip()})
    if adb_connect_targets:
        baseflow_defaults.append(
            {"name": "talos.e2e.adb.connect.targets", "value": adb_connect_targets.strip()}
        )
    if emulator_type:
        baseflow_defaults.append(
            {"name": "talos.e2e.emulator.type", "value": emulator_type.strip()}
        )

    baseflow_properties = merge_named_properties(baseflow_defaults, extra_properties)
    baseflow_result, queued_baseflow = queue_and_wait_build(
        config,
        build_type_id=baseflow_build_type_id,
        branch=branch,
        properties=baseflow_properties,
        comment=(comment or "Talos BaseFlow validation").strip(),
        tags=normalized_tags,
        timeout_seconds=timeout_seconds,
        poll_interval_seconds=poll_interval_seconds,
        log_tail_lines=log_tail_lines,
        step_title="Step 3/3: remote BaseFlow",
    )
    if baseflow_result == 0:
        print("[TeamCitySkill] Talos BaseFlow chain completed successfully")
        print(f"[TeamCitySkill] packageBuildId={queued_package.build_id}")
        print(f"[TeamCitySkill] baseFlowBuildId={queued_baseflow.build_id}")
    return baseflow_result


def command_run_build(
    config: TeamCityConfig,
    *,
    build_type_id: str,
    branch: str | None,
    properties: list[dict[str, str]],
    comment: str | None,
    tags: list[str],
    dry_run: bool,
    wait: bool,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
) -> int:
    if not build_type_id:
        raise TeamCityApiError("--build-type-id is required for run-build")

    print("[TeamCitySkill] ===== Step 1/2: queue build =====")
    if dry_run:
        preview = build_queue_payload(
            build_type_id=build_type_id,
            branch=branch,
            properties=properties,
            comment=comment,
            tags=tags,
        )
        print("[TeamCitySkill] dry-run enabled, skip POST request")
        print(f"[TeamCitySkill] target={config.base_url}/app/rest/buildQueue")
        print(json.dumps(preview, ensure_ascii=False, indent=2))
        return 0

    queued = queue_build(
        config,
        build_type_id=build_type_id,
        branch=branch,
        properties=properties,
        comment=comment,
        tags=tags,
    )
    print(f"[TeamCitySkill] queued build id={queued.build_id}")
    if queued.web_url:
        normalized_web_url = normalize_public_teamcity_url(config, queued.web_url)
        print(f"[TeamCitySkill] webUrl={normalized_web_url or queued.web_url}")
        if normalized_web_url and normalized_web_url != queued.web_url:
            print(f"[TeamCitySkill] serverWebUrl={queued.web_url}")

    if not wait:
        return 0

    print("[TeamCitySkill] ===== Step 2/2: wait for build =====")
    return wait_for_build_completion(
        config,
        build_id=queued.build_id,
        timeout_seconds=timeout_seconds,
        poll_interval_seconds=poll_interval_seconds,
        log_tail_lines=log_tail_lines,
    )


def command_run_build_group(
    config: TeamCityConfig,
    *,
    build_type_ids: list[str],
    branch: str | None,
    properties: list[dict[str, str]],
    comment: str | None,
    tags: list[str],
    dispatch_mode: str,
    dry_run: bool,
    wait: bool,
    timeout_seconds: int,
    poll_interval_seconds: int,
    log_tail_lines: int,
) -> int:
    if not build_type_ids:
        raise TeamCityApiError("run-build-group requires at least one --build-type-id")

    print("[TeamCitySkill] ===== Step 1/3: inspect agents =====")
    ready_agents = build_ready_agent_handles(config)
    print_agent_summary(ready_agents)

    print("[TeamCitySkill] ===== Step 2/3: inspect compatible agents =====")
    compatible_agent_ids_by_build_type = build_compatible_agent_ids_by_build_type(
        config,
        build_type_ids,
        ready_agents=ready_agents,
    )
    dispatch_plan = build_dispatch_plan(
        build_type_ids,
        compatible_agent_ids_by_build_type=compatible_agent_ids_by_build_type,
        ready_agents=ready_agents,
        dispatch_mode=dispatch_mode,
    )
    print_dispatch_plan(dispatch_plan, ready_agents=ready_agents)

    print("[TeamCitySkill] ===== Step 3/3: queue builds =====")
    preview = []
    for build_type_id in build_type_ids:
        preview.append(
            build_queue_payload(
                build_type_id=build_type_id,
                branch=branch,
                properties=properties,
                comment=comment,
                tags=tags,
            )
        )

    if dry_run:
        print("[TeamCitySkill] dry-run enabled, skip POST requests")
        print(f"[TeamCitySkill] dispatchMode={dispatch_plan.mode}")
        print(json.dumps(preview, ensure_ascii=False, indent=2))
        return 0

    queued_builds: list[tuple[str, QueuedBuildHandle]] = []
    if dispatch_plan.mode == "parallel":
        for build_type_id in build_type_ids:
            queued = queue_build(
                config,
                build_type_id=build_type_id,
                branch=branch,
                properties=properties,
                comment=comment,
                tags=tags,
            )
            queued_builds.append((build_type_id, queued))
            print(f"[TeamCitySkill] queued buildTypeId={build_type_id} buildId={queued.build_id}")
            if queued.web_url:
                normalized_web_url = normalize_public_teamcity_url(config, queued.web_url)
                print(f"[TeamCitySkill] webUrl={normalized_web_url or queued.web_url}")
                if normalized_web_url and normalized_web_url != queued.web_url:
                    print(f"[TeamCitySkill] serverWebUrl={queued.web_url}")
    else:
        for build_type_id in build_type_ids:
            queued = queue_build(
                config,
                build_type_id=build_type_id,
                branch=branch,
                properties=properties,
                comment=comment,
                tags=tags,
            )
            queued_builds.append((build_type_id, queued))
            print(f"[TeamCitySkill] queued buildTypeId={build_type_id} buildId={queued.build_id}")
            if queued.web_url:
                normalized_web_url = normalize_public_teamcity_url(config, queued.web_url)
                print(f"[TeamCitySkill] webUrl={normalized_web_url or queued.web_url}")
                if normalized_web_url and normalized_web_url != queued.web_url:
                    print(f"[TeamCitySkill] serverWebUrl={queued.web_url}")

            if wait:
                result = wait_for_build_completion(
                    config,
                    build_id=queued.build_id,
                    timeout_seconds=timeout_seconds,
                    poll_interval_seconds=poll_interval_seconds,
                    log_tail_lines=log_tail_lines,
                )
                if result != 0:
                    return result
        return 0

    if not wait:
        return 0

    final_result = 0
    for build_type_id, queued in queued_builds:
        print(
            f"[TeamCitySkill] waiting buildTypeId={build_type_id} buildId={queued.build_id}"
        )
        result = wait_for_build_completion(
            config,
            build_id=queued.build_id,
            timeout_seconds=timeout_seconds,
            poll_interval_seconds=poll_interval_seconds,
            log_tail_lines=log_tail_lines,
        )
        if result != 0:
            final_result = result
    return final_result


def default_export_path(config: TeamCityConfig) -> Path:
    return config.output_dir / "current-versioned-settings.json"


def default_backup_path(config: TeamCityConfig) -> Path:
    return config.output_dir / "versioned-settings.before-update.json"


def main() -> int:
    args = parse_args()
    config = build_config(args)

    if args.command == "show-project":
        return command_show_project(config)

    if args.command == "verify-vcs":
        return command_verify_vcs(config)

    if args.command == "export-current":
        output_path = (
            Path(args.output).expanduser().resolve()
            if args.output
            else default_export_path(config)
        )
        return command_export_current(config, output_path)

    if args.command == "apply":
        if not args.payload:
            raise TeamCityApiError("--payload is required for apply")
        payload_path = Path(args.payload).expanduser().resolve()
        output_path = (
            Path(args.output).expanduser().resolve()
            if args.output
            else default_backup_path(config)
        )
        return command_apply(
            config,
            payload_path,
            output_path,
            dry_run=args.dry_run,
        )

    if args.command == "run-build":
        return command_run_build(
            config,
            build_type_id=resolve_single_build_type_id(args.build_type_ids),
            branch=(args.branch or "").strip() or None,
            properties=parse_build_properties(args.properties),
            comment=(args.comment or "").strip() or None,
            tags=parse_build_tags(args.tags),
            dry_run=args.dry_run,
            wait=args.wait,
            timeout_seconds=args.timeout_seconds,
            poll_interval_seconds=args.poll_interval_seconds,
            log_tail_lines=args.log_tail_lines,
        )

    if args.command == "run-build-group":
        return command_run_build_group(
            config,
            build_type_ids=parse_build_type_ids(args.build_type_ids),
            branch=(args.branch or "").strip() or None,
            properties=parse_build_properties(args.properties),
            comment=(args.comment or "").strip() or None,
            tags=parse_build_tags(args.tags),
            dispatch_mode=args.dispatch_mode,
            dry_run=args.dry_run,
            wait=args.wait,
            timeout_seconds=args.timeout_seconds,
            poll_interval_seconds=args.poll_interval_seconds,
            log_tail_lines=args.log_tail_lines,
        )

    if args.command == "run-talos-baseflow-chain":
        return command_run_talos_baseflow_chain(
            config,
            platform=(args.platform or "android").strip() or "android",
            unity_path=(args.unity_path or "").strip() or None,
            project_path=(
                Path(args.project_path).expanduser().resolve()
                if args.project_path
                else None
            ),
            local_batchmode_mode=args.local_batchmode_mode,
            allow_local_sync_fallback=args.allow_local_sync_fallback,
            test_file=(args.test_file or "").strip() or DEFAULT_TALOS_BASEFLOW_TEST_FILE,
            client_version=(args.client_version or "").strip() or DEFAULT_TALOS_CLIENT_VERSION,
            build_debug=(args.build_debug or "").strip() or DEFAULT_TALOS_BUILD_DEBUG,
            branch=(args.branch or "").strip() or None,
            comment=(args.comment or "").strip() or None,
            tags=parse_build_tags(args.tags),
            adb_serial=(args.adb_serial or "").strip() or None,
            adb_connect_targets=(args.adb_connect_targets or "").strip() or None,
            emulator_type=(args.emulator_type or "").strip() or None,
            baseflow_build_type_id=(args.baseflow_build_type_id or "").strip() or DEFAULT_TALOS_BASEFLOW_BUILD_TYPE_ID,
            package_build_type_id=(args.package_build_type_id or "").strip() or None,
            extra_properties=parse_build_properties(args.properties),
            dry_run=args.dry_run,
            timeout_seconds=args.timeout_seconds,
            poll_interval_seconds=args.poll_interval_seconds,
            log_tail_lines=args.log_tail_lines,
        )

    raise TeamCityApiError(f"Unsupported command: {args.command}")


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except TeamCityApiError as exc:
        print(f"[TeamCitySkill][ERROR] {exc}")
        raise SystemExit(2)

