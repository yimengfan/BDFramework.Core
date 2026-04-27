"""Shared BuildTools external-integration config loader.

Terminology:
1. External integration config: business-independent settings for external systems consumed by BuildTools.
2. External service config: endpoint/auth settings for file servers or CI servers.
3. External signing config: certificate or signing metadata required by external toolchains.
4. External test config: switches and timing options for smoke tests that talk to external systems.
"""

from __future__ import annotations

import json
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable, Iterator

try:
    import tomllib
except ModuleNotFoundError:  # pragma: no cover - Python < 3.11 fallback
    try:
        import tomli as tomllib
    except ModuleNotFoundError:  # pragma: no cover - TeamCity old Python fallback
        tomllib = None


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG_PATH = BUILD_TOOLS_ROOT / "buildtools.toml"
DEFAULT_CONFIG_EXAMPLE_PATH = BUILD_TOOLS_ROOT / "buildtools.toml.example"
DEFAULT_REMOTE_ARTIFACT_TEST_BUILD_NUMBER = "remote-smoke-tests"
DEFAULT_REMOTE_ARTIFACT_TEST_FILENAME = "artifact_uploader_remote_test.txt"


class BuildToolsConfigError(RuntimeError):
    """BuildTools external-integration config related errors."""


@dataclass(frozen=True)
class BuildToolsFileServerConfig:
    """Business-independent file-server endpoint and upload settings."""

    base_url: str | None = None
    host: str | None = None
    port: int | None = None
    scheme: str | None = None
    token: str | None = None
    tokens: tuple[str, ...] = ()
    upload_chunk_size_kb: int | None = None
    hash_chunk_size_kb: int | None = None


@dataclass(frozen=True)
class BuildToolsCiServerConfig:
    """Business-independent CI server endpoint config reserved for TeamCity/Jenkins style integrations."""

    provider: str | None = None
    base_url: str | None = None
    project_name: str | None = None
    token: str | None = None
    token_env: str | None = None


@dataclass(frozen=True)
class BuildToolsIosXcodeSigningConfig:
    """External iOS signing config shared by the Xcode archive/export helper."""

    signing_style: str | None = None
    team_id: str | None = None
    bundle_identifier: str | None = None
    code_sign_identity: str | None = None
    provisioning_profile_specifier: str | None = None
    provisioning_profile: str | None = None


@dataclass(frozen=True)
class BuildToolsRemoteArtifactTestConfig:
    """External smoke-test config for remote artifact uploader validation."""

    enabled: bool = False
    build_number: str = DEFAULT_REMOTE_ARTIFACT_TEST_BUILD_NUMBER
    filename: str = DEFAULT_REMOTE_ARTIFACT_TEST_FILENAME
    request_timeout_seconds: int = 20
    listing_timeout_seconds: int = 15
    poll_interval_seconds: int = 1


@dataclass(frozen=True)
class BuildToolsTalosE2EConfig:
    """Talos E2E 编排默认参数，从 buildtools.toml [talos.e2e] 读取。

    Talos E2E orchestration defaults read from buildtools.toml [talos.e2e].
    当 TeamCity DSL 调用 teamcity_e2e_runner.py 时，
    这些参数提供合理的默认值，减少 DSL 行长度与维护成本。
    These parameters provide sensible defaults when TeamCity DSL invokes teamcity_e2e_runner.py,
    reducing DSL line length and maintenance cost.
    """

    client_version: str = "0.1"
    build_debug: str = "true"
    timeout_seconds: int = 5400
    poll_interval_seconds: int = 10
    download_timeout_seconds: int = 600
    unity_host: str = "127.0.0.1"
    unity_port: int = 10002


@dataclass(frozen=True)
class BuildToolsExternalConfig:
    """Typed view over the shared BuildTools external-integration config file."""

    config_path: Path | None
    file_server: BuildToolsFileServerConfig
    ci_server: BuildToolsCiServerConfig
    ios_xcode: BuildToolsIosXcodeSigningConfig
    remote_artifact_test: BuildToolsRemoteArtifactTestConfig
    talos_e2e: BuildToolsTalosE2EConfig


def parse_token_values(value: Any) -> tuple[str, ...]:
    """Normalize a token field into a de-duplicated tuple of strings."""
    if value is None:
        return ()

    if isinstance(value, str):
        items = [part.strip() for part in value.replace(";", ",").split(",") if part.strip()]
    elif isinstance(value, (list, tuple)):
        items = [str(part).strip() for part in value if str(part).strip()]
    else:
        raise BuildToolsConfigError(f"Unsupported token value: {value!r}")

    unique_items: list[str] = []
    for item in items:
        if item not in unique_items:
            unique_items.append(item)
    return tuple(unique_items)


def split_toml_value_and_comment(line: str) -> str:
    """Strip inline TOML comments while ignoring # inside string literals."""
    in_single_quote = False
    in_double_quote = False

    for index, char in enumerate(line):
        if char == '"' and not in_single_quote:
            is_escaped = index > 0 and line[index - 1] == "\\"
            if not is_escaped:
                in_double_quote = not in_double_quote
        elif char == "'" and not in_double_quote:
            in_single_quote = not in_single_quote
        elif char == "#" and not in_single_quote and not in_double_quote:
            return line[:index].rstrip()

    return line.rstrip()


def find_toml_delimiter(value: str, delimiter: str) -> int:
    """Find a delimiter while ignoring quoted or nested array content."""
    in_single_quote = False
    in_double_quote = False
    array_depth = 0

    for index, char in enumerate(value):
        if char == '"' and not in_single_quote:
            is_escaped = index > 0 and value[index - 1] == "\\"
            if not is_escaped:
                in_double_quote = not in_double_quote
        elif char == "'" and not in_double_quote:
            in_single_quote = not in_single_quote
        elif not in_single_quote and not in_double_quote:
            if char == "[":
                array_depth += 1
            elif char == "]":
                array_depth = max(0, array_depth - 1)
            elif char == delimiter and array_depth == 0:
                return index

    return -1


def split_toml_array_items(raw_value: str) -> list[str]:
    """Split a TOML array by commas while respecting quoted content."""
    items: list[str] = []
    current: list[str] = []
    in_single_quote = False
    in_double_quote = False
    array_depth = 0

    for index, char in enumerate(raw_value):
        if char == '"' and not in_single_quote:
            is_escaped = index > 0 and raw_value[index - 1] == "\\"
            if not is_escaped:
                in_double_quote = not in_double_quote
            current.append(char)
            continue

        if char == "'" and not in_double_quote:
            in_single_quote = not in_single_quote
            current.append(char)
            continue

        if not in_single_quote and not in_double_quote:
            if char == "[":
                array_depth += 1
            elif char == "]":
                array_depth = max(0, array_depth - 1)
            elif char == "," and array_depth == 0:
                item = "".join(current).strip()
                if not item:
                    raise BuildToolsConfigError("Minimal TOML parser does not support empty array items.")
                items.append(item)
                current = []
                continue

        current.append(char)

    last_item = "".join(current).strip()
    if last_item:
        items.append(last_item)
    return items


def parse_minimal_toml_value(raw_value: str) -> Any:
    """Parse the minimal TOML value types required by current BuildTools config."""
    value = raw_value.strip()
    if not value:
        raise BuildToolsConfigError("Minimal TOML parser received an empty value.")

    if value.startswith("["):
        if not value.endswith("]"):
            raise BuildToolsConfigError(f"Minimal TOML parser found an unterminated array: {value!r}")
        inner = value[1:-1].strip()
        if not inner:
            return []
        return [parse_minimal_toml_value(item) for item in split_toml_array_items(inner)]

    if value.startswith('"'):
        try:
            return json.loads(value)
        except json.JSONDecodeError as exc:
            raise BuildToolsConfigError(
                f"Minimal TOML parser failed to decode string: {value!r}"
            ) from exc

    if value.startswith("'") and value.endswith("'") and len(value) >= 2:
        return value[1:-1]

    if value in {"true", "false"}:
        return value == "true"

    numeric_value = value[1:] if value.startswith(("+", "-")) else value
    if numeric_value.isdigit():
        return int(value, 10)

    raise BuildToolsConfigError(
        "Minimal TOML parser only supports strings, integers, booleans, and arrays. "
        f"Unsupported value: {value!r}"
    )


def ensure_toml_table(root: dict[str, Any], section_name: str) -> dict[str, Any]:
    """Create and return a nested TOML table for a dotted section name."""
    section_parts = [part.strip() for part in section_name.split(".")]
    if not section_parts or any(not part for part in section_parts):
        raise BuildToolsConfigError(f"Minimal TOML parser found an invalid table name: {section_name!r}")

    current: dict[str, Any] = root
    for part in section_parts:
        next_value = current.get(part)
        if next_value is None:
            next_table: dict[str, Any] = {}
            current[part] = next_table
            current = next_table
            continue
        if not isinstance(next_value, dict):
            raise BuildToolsConfigError(
                "Minimal TOML parser cannot redefine a value as a table. "
                f"section={section_name!r}"
            )
        current = next_value
    return current


def load_minimal_toml(content: str) -> dict[str, Any]:
    """Parse the TOML subset required by BuildTools config on old Python runtimes."""
    result: dict[str, Any] = {}
    current_table = result

    for line_number, raw_line in enumerate(content.splitlines(), start=1):
        line = split_toml_value_and_comment(raw_line).strip()
        if not line:
            continue

        if line.startswith("["):
            if not line.endswith("]"):
                raise BuildToolsConfigError(
                    f"Minimal TOML parser found an unterminated table header at line {line_number}: {raw_line!r}"
                )
            section_name = line[1:-1].strip()
            current_table = ensure_toml_table(result, section_name)
            continue

        delimiter_index = find_toml_delimiter(line, "=")
        if delimiter_index < 0:
            raise BuildToolsConfigError(
                f"Minimal TOML parser expected key/value assignment at line {line_number}: {raw_line!r}"
            )

        key = line[:delimiter_index].strip()
        if not key:
            raise BuildToolsConfigError(f"Minimal TOML parser found an empty key at line {line_number}.")

        value = line[delimiter_index + 1 :].strip()
        current_table[key] = parse_minimal_toml_value(value)

    return result


def load_toml_file(path: Path) -> dict[str, Any]:
    """Load a BuildTools TOML config file."""
    if tomllib is not None:
        with path.open("rb") as handle:
            data = tomllib.load(handle)
    else:
        data = load_minimal_toml(path.read_text(encoding="utf-8"))

    if not isinstance(data, dict):
        raise BuildToolsConfigError(f"Config root must be a TOML table: {path}")
    return data


def resolve_buildtools_config_path(
    config_path: str | os.PathLike[str] | None,
    *,
    env_var_names: Iterable[str] = (),
) -> Path | None:
    """Resolve the shared BuildTools config path with explicit, env, and default fallbacks."""
    candidate_values: list[str] = []
    if config_path is not None:
        candidate_values.append(str(config_path).strip())

    for env_name in ("BUILDTOOLS_CONFIG", "BUILD_TOOLS_CONFIG", *tuple(env_var_names)):
        env_value = os.environ.get(env_name, "").strip()
        if env_value:
            candidate_values.append(env_value)

    for raw_candidate in candidate_values:
        if not raw_candidate:
            continue
        candidate = Path(raw_candidate).expanduser().resolve()
        if not candidate.exists():
            raise BuildToolsConfigError(f"BuildTools config does not exist: {candidate}")
        return candidate

    if DEFAULT_CONFIG_PATH.exists():
        return DEFAULT_CONFIG_PATH
    if DEFAULT_CONFIG_EXAMPLE_PATH.exists():
        return DEFAULT_CONFIG_EXAMPLE_PATH
    return None


def get_config_section(config_data: dict[str, Any], section_name: str) -> dict[str, Any]:
    """Safely read a dotted TOML table path from the shared BuildTools config."""
    current: Any = config_data
    for part in section_name.split("."):
        if current in (None, ""):
            return {}
        if not isinstance(current, dict):
            raise BuildToolsConfigError(f"Config section must be a TOML table: {section_name}")
        current = current.get(part, {})

    if current in (None, ""):
        return {}
    if not isinstance(current, dict):
        raise BuildToolsConfigError(f"Config section must be a TOML table: {section_name}")
    return current


def coerce_optional_string(value: Any) -> str | None:
    """Normalize a config value into an optional stripped string."""
    if value is None:
        return None
    normalized = str(value).strip()
    return normalized or None


def coerce_optional_int(value: Any) -> int | None:
    """Normalize a config value into an optional integer."""
    if value in (None, ""):
        return None
    return int(value)


def coerce_optional_bool(value: Any) -> bool | None:
    """Normalize a config value into an optional boolean."""
    if value in (None, ""):
        return None
    if isinstance(value, bool):
        return value
    normalized = str(value).strip().lower()
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    raise BuildToolsConfigError(f"Unsupported boolean config value: {value!r}")


def load_buildtools_external_config(
    config_path: str | os.PathLike[str] | None = None,
    *,
    env_var_names: Iterable[str] = (),
) -> BuildToolsExternalConfig:
    """Load the shared BuildTools external-integration config into typed data structures."""
    resolved_config_path = resolve_buildtools_config_path(config_path, env_var_names=env_var_names)
    config_data = load_toml_file(resolved_config_path) if resolved_config_path else {}

    file_server_section = get_config_section(config_data, "artifact_file_server")
    ci_server_section = get_config_section(config_data, "ci_server")
    ios_xcode_section = get_config_section(config_data, "ios_xcode")
    remote_artifact_test_section = get_config_section(config_data, "tests.remote_artifact")
    talos_e2e_section = get_config_section(config_data, "talos.e2e")

    return BuildToolsExternalConfig(
        config_path=resolved_config_path,
        file_server=BuildToolsFileServerConfig(
            base_url=coerce_optional_string(file_server_section.get("base_url")),
            host=coerce_optional_string(file_server_section.get("ip") or file_server_section.get("host")),
            port=coerce_optional_int(file_server_section.get("port")),
            scheme=coerce_optional_string(file_server_section.get("scheme")),
            token=coerce_optional_string(file_server_section.get("token")),
            tokens=parse_token_values(file_server_section.get("tokens")),
            upload_chunk_size_kb=coerce_optional_int(file_server_section.get("upload_chunk_size_kb")),
            hash_chunk_size_kb=coerce_optional_int(file_server_section.get("hash_chunk_size_kb")),
        ),
        ci_server=BuildToolsCiServerConfig(
            provider=coerce_optional_string(ci_server_section.get("provider")),
            base_url=coerce_optional_string(ci_server_section.get("base_url")),
            project_name=coerce_optional_string(ci_server_section.get("project_name")),
            token=coerce_optional_string(ci_server_section.get("token")),
            token_env=coerce_optional_string(ci_server_section.get("token_env")),
        ),
        ios_xcode=BuildToolsIosXcodeSigningConfig(
            signing_style=coerce_optional_string(ios_xcode_section.get("signing_style")),
            team_id=coerce_optional_string(ios_xcode_section.get("team_id")),
            bundle_identifier=coerce_optional_string(ios_xcode_section.get("bundle_identifier")),
            code_sign_identity=coerce_optional_string(ios_xcode_section.get("code_sign_identity")),
            provisioning_profile_specifier=coerce_optional_string(
                ios_xcode_section.get("provisioning_profile_specifier")
            ),
            provisioning_profile=coerce_optional_string(ios_xcode_section.get("provisioning_profile")),
        ),
        remote_artifact_test=BuildToolsRemoteArtifactTestConfig(
            enabled=coerce_optional_bool(remote_artifact_test_section.get("enabled")) or False,
            build_number=coerce_optional_string(remote_artifact_test_section.get("build_number"))
            or DEFAULT_REMOTE_ARTIFACT_TEST_BUILD_NUMBER,
            filename=coerce_optional_string(remote_artifact_test_section.get("filename"))
            or DEFAULT_REMOTE_ARTIFACT_TEST_FILENAME,
            request_timeout_seconds=coerce_optional_int(
                remote_artifact_test_section.get("request_timeout_seconds")
            )
            or 20,
            listing_timeout_seconds=coerce_optional_int(
                remote_artifact_test_section.get("listing_timeout_seconds")
            )
            or 15,
            poll_interval_seconds=coerce_optional_int(
                remote_artifact_test_section.get("poll_interval_seconds")
            )
            or 1,
        ),
        talos_e2e=BuildToolsTalosE2EConfig(
            client_version=coerce_optional_string(talos_e2e_section.get("client_version")) or "0.1",
            build_debug=coerce_optional_string(talos_e2e_section.get("build_debug")) or "true",
            timeout_seconds=coerce_optional_int(talos_e2e_section.get("timeout_seconds")) or 5400,
            poll_interval_seconds=coerce_optional_int(talos_e2e_section.get("poll_interval_seconds")) or 10,
            download_timeout_seconds=coerce_optional_int(talos_e2e_section.get("download_timeout_seconds")) or 600,
            unity_host=coerce_optional_string(talos_e2e_section.get("unity_host")) or "127.0.0.1",
            unity_port=coerce_optional_int(talos_e2e_section.get("unity_port")) or 10002,
        ),
    )


def iter_ios_xcode_shell_pairs(
    signing_config: BuildToolsIosXcodeSigningConfig,
) -> Iterator[tuple[str, str]]:
    """Emit the iOS signing keys that the Xcode shell helper understands."""
    for key, value in (
        ("signing_style", signing_config.signing_style),
        ("team_id", signing_config.team_id),
        ("bundle_identifier", signing_config.bundle_identifier),
        ("code_sign_identity", signing_config.code_sign_identity),
        ("provisioning_profile_specifier", signing_config.provisioning_profile_specifier),
        ("provisioning_profile", signing_config.provisioning_profile),
    ):
        if value is None:
            continue
        yield key, value


__all__ = [
    "BUILD_TOOLS_ROOT",
    "DEFAULT_CONFIG_EXAMPLE_PATH",
    "DEFAULT_CONFIG_PATH",
    "DEFAULT_REMOTE_ARTIFACT_TEST_BUILD_NUMBER",
    "DEFAULT_REMOTE_ARTIFACT_TEST_FILENAME",
    "BuildToolsCiServerConfig",
    "BuildToolsConfigError",
    "BuildToolsExternalConfig",
    "BuildToolsFileServerConfig",
    "BuildToolsIosXcodeSigningConfig",
    "BuildToolsRemoteArtifactTestConfig",
    "BuildToolsTalosE2EConfig",
    "find_toml_delimiter",
    "get_config_section",
    "iter_ios_xcode_shell_pairs",
    "load_buildtools_external_config",
    "load_minimal_toml",
    "load_toml_file",
    "parse_token_values",
    "resolve_buildtools_config_path",
    "split_toml_array_items",
    "split_toml_value_and_comment",
]