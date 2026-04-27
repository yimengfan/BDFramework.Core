"""BuildTools 公共上传与资源辅助接口导出。

该包给各个 CI 入口脚本提供稳定的公共 API，避免入口文件直接依赖过多内部模块。
"""

from .buildtools_config import (
    BuildToolsCiServerConfig,
    BuildToolsExternalConfig,
    BuildToolsFileServerConfig,
    BuildToolsIosXcodeSigningConfig,
    BuildToolsRemoteArtifactTestConfig,
    iter_ios_xcode_shell_pairs,
    load_buildtools_external_config,
)
from .artifact_uploader import (
    ArtifactType,
    ArtifactUploadError,
    FileServerClientSettings,
    UploadedArtifact,
    build_artifact_remote_path,
    build_artifact_remote_root,
    resolve_file_server_settings,
    upload_artifact,
    upload_asset_bundle,
    upload_client_package,
    upload_code,
    upload_table,
)


__all__ = [
    "ArtifactType",
    "ArtifactUploadError",
    "BuildToolsCiServerConfig",
    "BuildToolsExternalConfig",
    "BuildToolsFileServerConfig",
    "BuildToolsIosXcodeSigningConfig",
    "BuildToolsRemoteArtifactTestConfig",
    "FileServerClientSettings",
    "UploadedArtifact",
    "build_artifact_remote_path",
    "build_artifact_remote_root",
    "iter_ios_xcode_shell_pairs",
    "load_buildtools_external_config",
    "resolve_file_server_settings",
    "upload_artifact",
    "upload_asset_bundle",
    "upload_client_package",
    "upload_code",
    "upload_table",
]