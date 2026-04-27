"""iOS 热更文件服务器验证入口。"""

from __future__ import annotations

from pathlib import Path
import sys


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.client_resource_flow import (  # noqa: E402
    ArtifactUploadError,
    UnityBatchModeError,
    run_platform_resource_verify,
)


LOG_PREFIX = "[VerifyClientRes][iOS]"


def main() -> int:
    """执行 iOS 热更文件服务器下载验证主流程。"""
    return run_platform_resource_verify(
        platform_key="ios",
        log_prefix=LOG_PREFIX,
        description="Verify iOS ClientRes file-server download via Unity batchmode.",
        execute_method="BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResIOS",
    )


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (UnityBatchModeError, ArtifactUploadError) as exc:
        print(f"{LOG_PREFIX}[ERROR] {exc}")
        raise SystemExit(2)