"""TeamCity skill 环境路径回归测试。

这些测试用于锁定 TeamCity 辅助脚本的默认环境配置位置，
避免默认加载路径回退到已废弃的 teamcityskill/.env。
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
    """验证默认环境文件路径指向 .test-DevOps/.teamcity/.env。"""
    assert ups.DEFAULT_ENV_FILE == TEAMCITY_SKILL_DIR.parents[2] / ".test-DevOps" / ".teamcity" / ".env"


def test_default_env_file_no_longer_points_to_teamcityskill_directory() -> None:
    """验证默认环境文件路径不再落在 teamcityskill/.env。"""
    assert ups.DEFAULT_ENV_FILE != TEAMCITY_SKILL_DIR / ".env"