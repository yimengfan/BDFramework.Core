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


LOG_PREFIX = "[BuildCode][Android]"


def main() -> int:
    return run_platform_resource_build(
        platform_key="android",
        log_prefix=LOG_PREFIX,
        description="Build Android hotfix code via Unity batchmode.",
        execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid",
        build_kind="clientres_code",
        artifact_kind="code",
    )


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (UnityBatchModeError, ClientResourceArtifactsError, ArtifactUploadError) as exc:
        print(f"{LOG_PREFIX}[ERROR] {exc}")
        raise SystemExit(2)