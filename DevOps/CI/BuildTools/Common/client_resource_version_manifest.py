from __future__ import annotations

"""ClientRes 共享版控指针 helper。

作用：
1. 维护文件服务器上的 ``clientRes_{platform}/version.info``。
2. 文件内容固定为 ``code.assetbundle.table`` 三段构建号。
3. 运行时 ``AssetsVersionController.DevOps.cs`` 会读取这份指针文件，再分别下载 Code / Assetbundle / Table。

Example:
    ``clientRes_ios/version.info`` -> ``101.202.303``
"""

from dataclasses import dataclass
from pathlib import Path
import tempfile

from Common.artifact_uploader import (
    DEFAULT_TIMEOUT_SECONDS,
    FileServerClientSettings,
    build_authorization_headers,
    build_download_request_path,
    create_http_connection,
    upload_single_file,
)


VERSION_MANIFEST_FILENAME = "version.info"
VERSION_MANIFEST_ROOT_TEMPLATE = "clientRes_{platform}"
KNOWN_CLIENT_RES_PLATFORMS = ("android", "ios", "windows")


class ClientResourceVersionManifestError(RuntimeError):
    """Raised when the shared ClientRes version manifest cannot be parsed or published."""


@dataclass(frozen=True)
class ClientResourceVersionManifest:
    """Represents the shared file-server version pointer for ClientRes.

    The server-side format is fixed to `code.assetbundle.table`, so each segment must not contain dots.
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


def build_client_resource_version_remote_path(platform: str) -> str:
    """Build the remote manifest path under the shared lower-case clientRes root."""
    normalized_platform = (platform or "").strip().lower()
    if not normalized_platform:
        raise ClientResourceVersionManifestError("ClientRes platform is empty.")
    if any(ch in normalized_platform for ch in ("/", "\\", ".")):
        raise ClientResourceVersionManifestError(f"ClientRes platform is invalid: {platform!r}")

    return f"{VERSION_MANIFEST_ROOT_TEMPLATE.format(platform=normalized_platform)}/{VERSION_MANIFEST_FILENAME}"


def load_client_resource_version_manifest(
    platform: str,
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> ClientResourceVersionManifest:
    """Read the remote shared manifest.

    Missing files default to ``0.0.0`` so first-time uploads can publish one component at a time.
    """
    remote_path = build_client_resource_version_remote_path(platform)
    request_path = build_download_request_path(settings.base_url, remote_path)
    connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
    try:
        connection.request("GET", request_path, headers=build_authorization_headers(settings))
        response = connection.getresponse()
        response_body = response.read()
    finally:
        connection.close()

    if response.status == 404:
        return ClientResourceVersionManifest()

    if response.status != 200:
        detail = response_body.decode("utf-8", errors="replace")
        raise ClientResourceVersionManifestError(
            f"Load ClientRes version manifest failed. platform={platform}, status={response.status}, detail={detail}"
        )

    try:
        return parse_client_resource_version_manifest(response_body.decode("utf-8"))
    except UnicodeDecodeError as exc:
        raise ClientResourceVersionManifestError(
            f"ClientRes version manifest is not valid UTF-8. platform={platform}"
        ) from exc


def save_client_resource_version_manifest(
    platform: str,
    manifest: ClientResourceVersionManifest,
    *,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> None:
    """Upload the shared manifest back to the file server.

    The helper writes through a temporary file so callers can keep passing pure strings instead of managing temp paths.
    """
    remote_path = build_client_resource_version_remote_path(platform)
    manifest_text = manifest.to_text()

    with tempfile.TemporaryDirectory(prefix=f"clientres_manifest_{platform}_") as temp_dir:
        temp_file = Path(temp_dir) / VERSION_MANIFEST_FILENAME
        temp_file.write_text(manifest_text, encoding="utf-8")
        upload_single_file(
            temp_file,
            remote_path=remote_path,
            settings=settings,
            overwrite=True,
            timeout_seconds=timeout_seconds,
        )


def publish_client_resource_version_manifest(
    platform: str,
    *,
    component_kind: str,
    build_label: str,
    settings: FileServerClientSettings,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> ClientResourceVersionManifest:
    """Update one component version in the shared manifest and upload it back.

    This is called by ``client_resource_artifacts.py`` after Code or Assetbundle uploads succeed.
    """
    normalized_build_label = normalize_manifest_version_token(build_label)
    current_manifest = load_client_resource_version_manifest(
        platform,
        settings=settings,
        timeout_seconds=timeout_seconds,
    )
    updated_manifest = current_manifest.with_component(component_kind, normalized_build_label)
    save_client_resource_version_manifest(
        platform,
        updated_manifest,
        settings=settings,
        timeout_seconds=timeout_seconds,
    )
    return updated_manifest


def publish_table_version_manifests(
    *,
    build_label: str,
    settings: FileServerClientSettings,
    platforms: tuple[str, ...] = KNOWN_CLIENT_RES_PLATFORMS,
    timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> dict[str, ClientResourceVersionManifest]:
    """Publish the table build number into every runtime platform manifest.

    Table resources are cross-platform, but the runtime still resolves them through
    ``clientRes_{platform}/version.info``.
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
    "KNOWN_CLIENT_RES_PLATFORMS",
    "VERSION_MANIFEST_FILENAME",
    "build_client_resource_version_remote_path",
    "load_client_resource_version_manifest",
    "normalize_manifest_version_token",
    "parse_client_resource_version_manifest",
    "publish_client_resource_version_manifest",
    "publish_table_version_manifests",
    "resolve_manifest_build_label",
    "save_client_resource_version_manifest",
]