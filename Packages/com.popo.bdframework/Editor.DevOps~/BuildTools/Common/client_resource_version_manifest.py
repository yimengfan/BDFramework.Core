"""ClientRes 共享版控指针 helper。

作用：
1. 维护文件服务器上的 `global_version.info`。
2. 文件内容为 JSON 数组，每条记录包含 `key`、`platform`、`version_num`、`game_server_ip`。
3. 通过 `platform` 检索对应的 `version_num`，其中 `version_num` 固定为 `code.assetbundle.table` 三段构建号。
4. 运行时 `AssetsVersionController.DevOps.cs` 会先读取这份指针文件，再分别下载 Code / Assetbundle / Table。
5. TeamCity 中 Code / Assetbundle / Table 会并发上传，因此这里需要在回写 `global_version.info` 时处理并发覆盖，确保只更新当前组件对应的那一段版本号。

示例：
    `global_version.info` ->
    `[{"key":"default","platform":"ios","version_num":"101.202.303","game_server_ip":"127.0.0.1"}]`
"""

from __future__ import annotations

import json
from dataclasses import dataclass, replace
from pathlib import Path
import tempfile

from Common.artifact_uploader import (
    DEFAULT_TIMEOUT_SECONDS,
    ArtifactUploadError,
    FileServerClientSettings,
    build_authorization_headers,
    build_download_request_path,
    create_http_connection,
    upload_single_file,
)


GLOBAL_VERSION_FILENAME = "global_version.info"
DEFAULT_VERSION_KEY = "default"
DEFAULT_GAME_SERVER_IP = "127.0.0.1"
KNOWN_CLIENT_RES_PLATFORMS = ("android", "ios", "windows")
GLOBAL_VERSION_PUBLISH_MAX_ATTEMPTS = 3


class ClientResourceVersionManifestError(RuntimeError):
    """Raised when the shared ClientRes version manifest cannot be parsed or published."""


@dataclass(frozen=True)
class GlobalVersionEntry:
    """Represents one entry in the global_version.info JSON array."""

    key: str = DEFAULT_VERSION_KEY
    platform: str = ""
    version_num: str = "0.0.0"
    game_server_ip: str = DEFAULT_GAME_SERVER_IP

    def to_dict(self) -> dict[str, str]:
        return {
            "key": self.key,
            "platform": self.platform,
            "version_num": self.version_num,
            "game_server_ip": self.game_server_ip,
        }


@dataclass(frozen=True)
class ClientResourceVersionManifest:
    """Represents the shared file-server version pointer for ClientRes.

    The version_num format is fixed to `code.assetbundle.table`, so each segment must not contain dots.
    """

    code_version: str = "0"
    assetbundle_version: str = "0"
    table_version: str = "0"

    def to_text(self) -> str:
        """Serialize the in-memory manifest back to ``code.assetbundle.table``."""
        return f"{self.code_version}.{self.assetbundle_version}.{self.table_version}"

    def with_component(self, component_kind: str, version_value: str) -> "ClientResourceVersionManifest":
        """Return a copy with one component version replaced.

        Args:
            component_kind: One of ``code``, ``assetbundle`` or ``table``.
            version_value: The build number that should be written into that segment.
        """
        if component_kind == "code":
            return ClientResourceVersionManifest(
                code_version=version_value,
                assetbundle_version=self.assetbundle_version,
                table_version=self.table_version,
            )
        if component_kind == "assetbundle":
            return ClientResourceVersionManifest(
                code_version=self.code_version,
                assetbundle_version=version_value,
                table_version=self.table_version,
            )
        if component_kind == "table":
            return ClientResourceVersionManifest(
                code_version=self.code_version,
                assetbundle_version=self.assetbundle_version,
                table_version=version_value,
            )

        raise ClientResourceVersionManifestError(f"Unsupported ClientRes component kind: {component_kind!r}")


def normalize_manifest_version_token(raw_value: str | None) -> str:
    """Validate a manifest token before writing it into the dot-delimited file format."""
    normalized = (raw_value or "").strip()
    if not normalized:
        raise ClientResourceVersionManifestError("ClientRes version token is empty.")
    if any(ch in normalized for ch in ("\n", "\r", "\t", "/", "\\")):
        raise ClientResourceVersionManifestError(f"ClientRes version token contains invalid characters: {normalized!r}")
    if "." in normalized:
        raise ClientResourceVersionManifestError(
            f"ClientRes version token cannot contain '.': {normalized!r}. "
            "Use CI build numbers for the shared manifest."
        )
    return normalized


def resolve_manifest_build_label(build_number: str | None, fallback_build_label: str | None) -> str | None:
    """Prefer CI build numbers because the manifest format does not support dotted labels."""
    normalized_build_number = (build_number or "").strip()
    if normalized_build_number and "." not in normalized_build_number:
        return normalized_build_number

    normalized_fallback = (fallback_build_label or "").strip()
    if normalized_fallback and "." not in normalized_fallback:
        return normalized_fallback

    return None


def parse_client_resource_version_manifest(content: str) -> ClientResourceVersionManifest:
    """Parse `code.assetbundle.table` from the server into a typed structure."""
    normalized = (content or "").strip()
    if not normalized:
        raise ClientResourceVersionManifestError("ClientRes version manifest is empty.")

    segments = [segment.strip() for segment in normalized.split(".")]
    if len(segments) != 3 or any(not segment for segment in segments):
        raise ClientResourceVersionManifestError(
            f"ClientRes version manifest must be 'code.assetbundle.table': {content!r}"
        )

    return ClientResourceVersionManifest(
        code_version=segments[0],
        assetbundle_version=segments[1],
        table_version=segments[2],
    )


def build_global_version_remote_path() -> str:
    """Build the remote path for the global version manifest file."""
    return GLOBAL_VERSION_FILENAME


def parse_global_version_entries(content: str) -> list[GlobalVersionEntry]:
    """Parse the global_version.info JSON content into a list of typed entries."""
    normalized = (content or "").strip()
    if not normalized:
        return []

    try:
        raw_entries = json.loads(normalized)
    except json.JSONDecodeError as exc:
        raise ClientResourceVersionManifestError(
            f"global_version.info is not valid JSON: {exc}"
        ) from exc

    if not isinstance(raw_entries, list):
        raise ClientResourceVersionManifestError(
            f"global_version.info root must be a JSON array, got: {type(raw_entries).__name__}"
        )

    entries: list[GlobalVersionEntry] = []
    for raw_entry in raw_entries:
        if not isinstance(raw_entry, dict):
            continue
        entries.append(GlobalVersionEntry(
            key=str(raw_entry.get("key", DEFAULT_VERSION_KEY) or DEFAULT_VERSION_KEY),
            platform=str(raw_entry.get("platform", "") or ""),
            version_num=str(raw_entry.get("version_num", "0.0.0") or "0.0.0"),
            game_server_ip=str(raw_entry.get("game_server_ip", DEFAULT_GAME_SERVER_IP) or DEFAULT_GAME_SERVER_IP),
        ))

    return entries


def serialize_global_version_entries(entries: list[GlobalVersionEntry]) -> str:
    """Serialize a list of GlobalVersionEntry back to JSON string."""
    return json.dumps(
        [entry.to_dict() for entry in entries],
        ensure_ascii=False,
        indent=2,
    )


def find_entry_by_platform(entries: list[GlobalVersionEntry], platform: str) -> GlobalVersionEntry | None:
    """Find the first entry matching the given platform (case-insensitive)."""
    normalized_platform = platform.strip().lower()
    for entry in entries:
        if entry.platform.strip().lower() == normalized_platform:
            return entry
    return None


def upsert_global_version_entry(
    entries: list[GlobalVersionEntry],
    platform: str,
    version_num: str,
    *,
    key: str = DEFAULT_VERSION_KEY,
    game_server_ip: str = DEFAULT_GAME_SERVER_IP,
) -> list[GlobalVersionEntry]:
    """Insert or update an entry for the given platform."""
    normalized_platform = platform.strip().lower()
    updated = False
    new_entries: list[GlobalVersionEntry] = []
    for entry in entries:
        if entry.platform.strip().lower() == normalized_platform:
            new_entries.append(replace(entry, version_num=version_num))
            updated = True
        else:
            new_entries.append(entry)

    if not updated:
        new_entries.append(GlobalVersionEntry(
            key=key,
            platform=platform.strip().lower(),
            version_num=version_num,
            game_server_ip=game_server_ip,
        ))

    return new_entries


def load_global_version_info(
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[GlobalVersionEntry]:
    """Read the remote global_version.info and return parsed entries.

    Missing files default to an empty list so first-time uploads can publish one platform at a time.
    """
    remote_path = build_global_version_remote_path()
    request_path = build_download_request_path(settings.base_url, remote_path)
    connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
    try:
        connection.request("GET", request_path, headers=build_authorization_headers(settings))
        response = connection.getresponse()
        response_body = response.read()
    finally:
        connection.close()

    if response.status == 404:
        return []

    if response.status != 200:
        detail = response_body.decode("utf-8", errors="replace")
        raise ClientResourceVersionManifestError(
            f"Load global_version.info failed. status={response.status}, detail={detail}"
        )

    try:
        return parse_global_version_entries(response_body.decode("utf-8"))
    except UnicodeDecodeError as exc:
        raise ClientResourceVersionManifestError(
            "global_version.info is not valid UTF-8."
        ) from exc


def save_global_version_info(
    entries: list[GlobalVersionEntry],
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> None:
    """Upload the global_version.info back to the file server."""
    remote_path = build_global_version_remote_path()
    content = serialize_global_version_entries(entries)

    with tempfile.TemporaryDirectory(prefix="clientres_global_version_") as temp_dir:
        temp_file = Path(temp_dir) / GLOBAL_VERSION_FILENAME
        temp_file.write_text(content, encoding="utf-8")
        upload_single_file(
            temp_file,
            remote_path=remote_path,
            settings=settings,
            overwrite=True,
            timeout_seconds=timeout_seconds,
        )


def load_client_resource_version_manifest(
    platform: str,
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> ClientResourceVersionManifest:
    """Read the remote global_version.info and extract the version for the given platform.

    Missing files or missing platform entries default to ``0.0.0``.
    """
    entries = load_global_version_info(settings=settings, timeout_seconds=timeout_seconds)
    entry = find_entry_by_platform(entries, platform)
    if entry is None:
        return ClientResourceVersionManifest()

    return parse_client_resource_version_manifest(entry.version_num)


def save_client_resource_version_manifest(
    platform: str,
    manifest: ClientResourceVersionManifest,
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> None:
    """Upload the updated manifest for the given platform back to the global_version.info on the file server."""
    entries = load_global_version_info(settings=settings, timeout_seconds=timeout_seconds)
    updated_entries = upsert_global_version_entry(
        entries,
        platform,
        version_num=manifest.to_text(),
    )
    save_global_version_info(updated_entries, settings=settings, timeout_seconds=timeout_seconds)


def build_updated_global_version_entries(
    entries: list[GlobalVersionEntry],
    *,
    platform: str,
    component_kind: str,
    build_label: str,
) -> tuple[ClientResourceVersionManifest, list[GlobalVersionEntry]]:
    """基于当前远端 entries 只替换指定组件的版本段，并返回新的 manifest 与 entries。"""
    current_entry = find_entry_by_platform(entries, platform)
    current_manifest = (
        parse_client_resource_version_manifest(current_entry.version_num)
        if current_entry is not None
        else ClientResourceVersionManifest()
    )
    updated_manifest = current_manifest.with_component(component_kind, build_label)
    updated_entries = upsert_global_version_entry(
        entries,
        platform,
        version_num=updated_manifest.to_text(),
    )
    return updated_manifest, updated_entries


def should_retry_global_version_publish(exc: Exception) -> bool:
    """判断共享版控回写失败是否属于可通过重新加载远端后重试恢复的并发覆盖。"""
    if not isinstance(exc, ArtifactUploadError):
        return False

    error_text = str(exc)
    return (
        GLOBAL_VERSION_FILENAME in error_text
        and ("metadata sha256_mismatch" in error_text or "download sha256_mismatch" in error_text)
    )


def publish_client_resource_version_manifest(
    platform: str,
    *,
    component_kind: str,
    build_label: str,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> ClientResourceVersionManifest:
    """把单个组件的新构建号合并进共享版控，并在并发覆盖时重新加载远端后重试。"""
    normalized_build_label = normalize_manifest_version_token(build_label)
    last_error: Exception | None = None

    for attempt_index in range(GLOBAL_VERSION_PUBLISH_MAX_ATTEMPTS):
        entries = load_global_version_info(settings=settings, timeout_seconds=timeout_seconds)
        updated_manifest, updated_entries = build_updated_global_version_entries(
            entries,
            platform=platform,
            component_kind=component_kind,
            build_label=normalized_build_label,
        )

        try:
            save_global_version_info(
                updated_entries,
                settings=settings,
                timeout_seconds=timeout_seconds,
            )
            return updated_manifest
        except Exception as exc:
            last_error = exc
            if attempt_index >= GLOBAL_VERSION_PUBLISH_MAX_ATTEMPTS - 1 or not should_retry_global_version_publish(exc):
                raise ClientResourceVersionManifestError(
                    f"Publish global_version.info failed. platform={platform}, componentKind={component_kind}, "
                    f"attempt={attempt_index + 1}, error={exc}"
                ) from exc

    raise ClientResourceVersionManifestError(
        f"Publish global_version.info failed after {GLOBAL_VERSION_PUBLISH_MAX_ATTEMPTS} attempts. "
        f"platform={platform}, componentKind={component_kind}, error={last_error}"
    ) from last_error


def publish_table_version_manifests(
    *,
    build_label: str,
    settings: FileServerClientSettings,
    platforms: tuple[str, ...] = KNOWN_CLIENT_RES_PLATFORMS,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> dict[str, ClientResourceVersionManifest]:
    """Publish the table build number into every runtime platform manifest.

    Table resources are cross-platform, but the runtime still resolves them through
    ``global_version.info``.
    """
    updated_manifests: dict[str, ClientResourceVersionManifest] = {}
    for platform in platforms:
        updated_manifests[platform] = publish_client_resource_version_manifest(
            platform,
            component_kind="table",
            build_label=build_label,
            settings=settings,
            timeout_seconds=timeout_seconds,
        )
    return updated_manifests


__all__ = [
    "ClientResourceVersionManifest",
    "ClientResourceVersionManifestError",
    "DEFAULT_GAME_SERVER_IP",
    "DEFAULT_VERSION_KEY",
    "GLOBAL_VERSION_FILENAME",
    "GlobalVersionEntry",
    "KNOWN_CLIENT_RES_PLATFORMS",
    "build_global_version_remote_path",
    "find_entry_by_platform",
    "load_client_resource_version_manifest",
    "load_global_version_info",
    "normalize_manifest_version_token",
    "parse_client_resource_version_manifest",
    "parse_global_version_entries",
    "publish_client_resource_version_manifest",
    "publish_table_version_manifests",
    "resolve_manifest_build_label",
    "save_client_resource_version_manifest",
    "save_global_version_info",
    "serialize_global_version_entries",
    "upsert_global_version_entry",
]
