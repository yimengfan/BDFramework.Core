"""TeamCity skill 环境路径回归测试。 TeamCity skill environment path regression tests.

这些测试用于锁定 TeamCity 辅助脚本的共享环境配置位置，
避免默认加载路径回退到已废弃的 teamcityskill 目录。
These tests lock the shared environment locations used by the TeamCity helper
so the defaults cannot drift back to the retired teamcityskill directory.
"""

from __future__ import annotations

from pathlib import Path
import sys


TEAMCITY_SKILL_DIR = Path(__file__).resolve().parents[1]
TEAMCITY_SCRIPT_DIR = TEAMCITY_SKILL_DIR / "scripts"
if str(TEAMCITY_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(TEAMCITY_SCRIPT_DIR))

import update_project_settings as ups


def test_default_env_file_points_to_shared_teamcity_directory() -> None:
    """验证默认环境文件路径指向共享的 .test-DevOps/.teamcity/.env。
    Verify that the default environment file points to the shared .test-DevOps/.teamcity/.env path.
    """
    assert ups.DEFAULT_ENV_FILE == TEAMCITY_SKILL_DIR.parents[2] / ".test-DevOps" / ".teamcity" / ".env"


def test_default_env_file_no_longer_points_to_teamcityskill_directory() -> None:
    """验证默认环境文件路径不再落在旧的 teamcityskill/.env。
    Verify that the default environment file no longer resolves to the legacy teamcityskill/.env path.
    """
    assert ups.DEFAULT_ENV_FILE != TEAMCITY_SKILL_DIR / ".env"


def test_shared_teamcity_env_example_exists() -> None:
    """验证共享 TeamCity 环境模板已迁入 .test-DevOps/.teamcity/.env.example。
    Verify that the shared TeamCity environment template exists at .test-DevOps/.teamcity/.env.example.
    """
    env_example_path = TEAMCITY_SKILL_DIR.parents[2] / ".test-DevOps" / ".teamcity" / ".env.example"

    assert env_example_path.exists()