from __future__ import annotations

"""验证已存在构建轮询脚本的关键退出行为。 Verify the key exit behavior of the existing-build polling helper."""

from pathlib import Path
import sys


TEAMCITY_SKILL_DIR = Path(__file__).resolve().parents[1]
TEAMCITY_SCRIPT_DIR = TEAMCITY_SKILL_DIR / "scripts"
if str(TEAMCITY_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(TEAMCITY_SCRIPT_DIR))

import tc_poll_existing_build as poller
from update_project_settings import TeamCityConfig


def make_config() -> TeamCityConfig:
    """构造轮询测试用配置，隔离真实 TeamCity 凭据与输出目录。
    Build a polling-test configuration isolated from real TeamCity credentials and output paths.
    """

    return TeamCityConfig(
        base_url="http://svn.funtoo.games",
        project_id="BDFrameworkCore",
        token="token",
        username=None,
        password=None,
        output_dir=Path("/tmp/teamcityskill-tests"),
    )


def test_poll_build_returns_zero_when_existing_build_finishes_success(
    monkeypatch,
) -> None:
    """验证已存在构建成功结束时轮询脚本返回 0。
    Verify that polling an existing build returns 0 when the build finishes successfully.
    """

    builds = [
        {
            "id": 501,
            "state": "running",
            "status": "SUCCESS",
            "statusText": "Running",
            "running-info": {"currentStageText": "stage-a", "percentageComplete": 10},
        },
        {
            "id": 501,
            "state": "finished",
            "status": "SUCCESS",
            "statusText": "Success",
            "running-info": {},
        },
    ]
    monotonic_values = iter([0.0, 0.0, 0.0, 1.0])

    monkeypatch.setattr(poller, "get_build", lambda config, build_id: builds.pop(0))
    monkeypatch.setattr(poller, "print_build_summary", lambda config, build: None)
    monkeypatch.setattr(poller.time, "monotonic", lambda: next(monotonic_values))
    monkeypatch.setattr(poller.time, "sleep", lambda seconds: None)

    result = poller.poll_build(
        make_config(),
        build_id=501,
        timeout_seconds=30,
        poll_interval_seconds=1,
        log_tail_lines=20,
    )

    assert result == 0


def test_poll_build_returns_one_and_prints_log_tail_when_existing_build_fails(
    monkeypatch,
    capsys,
) -> None:
    """验证已存在构建失败时轮询脚本返回 1 并打印日志尾部。
    Verify that polling an existing failed build returns 1 and prints the log tail.
    """

    monotonic_values = iter([0.0, 0.0, 0.0])

    monkeypatch.setattr(
        poller,
        "get_build",
        lambda config, build_id: {
            "id": 502,
            "state": "finished",
            "status": "FAILURE",
            "statusText": "Failed",
            "running-info": {},
        },
    )
    monkeypatch.setattr(poller, "print_build_summary", lambda config, build: None)
    monkeypatch.setattr(poller, "get_build_log_tail", lambda config, build_id, lines: "tail output")
    monkeypatch.setattr(poller.time, "monotonic", lambda: next(monotonic_values))
    monkeypatch.setattr(poller.time, "sleep", lambda seconds: None)

    result = poller.poll_build(
        make_config(),
        build_id=502,
        timeout_seconds=30,
        poll_interval_seconds=1,
        log_tail_lines=20,
    )

    output = capsys.readouterr().out
    assert result == 1
    assert "tail output" in output