"""Windows 热更 Assetbundle 构建入口。"""

from __future__ import annotations

from pathlib import Path
import sys


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.client_resource_flow import (  # noqa: E402
    ArtifactUploadError,
    ClientResourceArtifactsError,
    UnityBatchModeError,
    run_platform_resource_build,
)


LOG_PREFIX = "[BuildAssetbundle][Windows]"


def main() -> int:
    """执行 Windows 热更 Assetbundle 的 Unity 构建与上传主流程。"""
    return run_platform_resource_build(
        platform_key="windows",
        log_prefix=LOG_PREFIX,
        description="Build Windows hotfix assetbundle via Unity batchmode.",
        execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.BuildAssetbundleWindows",
        build_kind="clientres_assetbundle",
        artifact_kind="assetbundle",
    )


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (UnityBatchModeError, ClientResourceArtifactsError, ArtifactUploadError) as exc:
        print(f"{LOG_PREFIX}[ERROR] {exc}")
        raise SystemExit(2)