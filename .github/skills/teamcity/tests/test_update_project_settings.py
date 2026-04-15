from __future__ import annotations

"""验证 TeamCity 主脚本的调度与重试行为。"""

from io import BytesIO
from pathlib import Path
import sys
import urllib.error

import pytest


TEAMCITY_SKILL_DIR = Path(__file__).resolve().parents[1]
TEAMCITY_SCRIPT_DIR = TEAMCITY_SKILL_DIR / "scripts"
if str(TEAMCITY_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(TEAMCITY_SCRIPT_DIR))

import update_project_settings as ups
from update_project_settings import (
    AgentHandle,
    TeamCityApiError,
    TeamCityConfig,
    api_request,
    api_request_text,
    build_queue_comment,
    build_queue_payload,
    build_queue_tags,
    build_dispatch_plan,
    parse_build_tags,
    parse_build_type_ids,
)


class FakeResponse:
    """模拟 urllib 返回对象，避免测试依赖真实网络。"""

    def __init__(self, status: int, payload: bytes) -> None:
        self.status = status
        self._payload = payload

    def read(self) -> bytes:
        return self._payload

    def __enter__(self) -> "FakeResponse":
        return self

    def __exit__(self, exc_type, exc, tb) -> bool:
        return False


def make_config() -> TeamCityConfig:
    """构造测试用配置，隔离真实 TeamCity 凭据与输出目录。"""

    return TeamCityConfig(
        base_url="http://svn.funtoo.games",
        project_id="BDFrameworkCore",
        token="token",
        username=None,
        password=None,
        output_dir=Path("/tmp/teamcityskill-tests"),
    )


def test_parse_build_type_ids_supports_repeat_and_comma_separated_values() -> None:
    assert parse_build_type_ids(
        [
            "BDFrameworkCore_BuildClientPackageIos, BDFrameworkCore_BuildClientPackageWindows",
            "BDFrameworkCore_BuildClientPackageAndroid",
        ]
    ) == [
        "BDFrameworkCore_BuildClientPackageIos",
        "BDFrameworkCore_BuildClientPackageWindows",
        "BDFrameworkCore_BuildClientPackageAndroid",
    ]


def test_parse_build_tags_supports_repeat_and_comma_separated_values() -> None:
    assert parse_build_tags(
        [
            "teamcityskill, manual-check",
            "manual-check",
            "android",
        ]
    ) == ["teamcityskill", "manual-check", "android"]


def test_build_queue_comment_always_includes_target_and_branch() -> None:
    assert build_queue_comment(
        build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        branch="v4/v-4.0.0",
        comment="回归验证",
    ) == "回归验证 | 测试目标: BDFrameworkCore_BuildClientPackageAndroid | 分支: v4/v-4.0.0"


def test_build_queue_tags_include_defaults_and_user_tags() -> None:
    """验证 build_queue_tags 只包含 teamcityskill 默认 tag 和用户自定义 tag，不自动注入 buildTypeId。"""
    assert build_queue_tags(
        build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        tags=["win64", "manual-check"],
    ) == [
        "teamcityskill",
        "win64",
        "manual-check",
    ]


def test_build_queue_payload_includes_comment_and_tags() -> None:
    payload = build_queue_payload(
        build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        branch="v4/v-4.0.0",
        properties=[{"name": "build.client.version", "value": "0.1"}],
        comment="回归验证",
        tags=["manual-check"],
    )

    assert payload == {
        "buildType": {"id": "BDFrameworkCore_BuildClientPackageAndroid"},
        "branchName": "v4/v-4.0.0",
        "properties": {
            "property": [{"name": "build.client.version", "value": "0.1"}]
        },
        "comment": {
            "text": "回归验证 | 测试目标: BDFrameworkCore_BuildClientPackageAndroid | 分支: v4/v-4.0.0"
        },
        "tags": {
            "tag": [
                {"name": "teamcityskill"},
                {"name": "manual-check"},
            ]
        },
    }


def test_build_dispatch_plan_prefers_parallel_when_idle_agents_cover_all_builds() -> None:
    ready_agents = {
        1: AgentHandle(1, "TCMac", True, True, True, 0),
        2: AgentHandle(2, "TCWindows", True, True, True, 0),
    }
    plan = build_dispatch_plan(
        [
            "BDFrameworkCore_BuildClientPackageIos",
            "BDFrameworkCore_BuildClientPackageWindows",
        ],
        compatible_agent_ids_by_build_type={
            "BDFrameworkCore_BuildClientPackageIos": (1,),
            "BDFrameworkCore_BuildClientPackageWindows": (2,),
        },
        ready_agents=ready_agents,
        dispatch_mode="auto",
    )

    assert plan.mode == "parallel"
    assert plan.parallel_assignment == {
        "BDFrameworkCore_BuildClientPackageIos": 1,
        "BDFrameworkCore_BuildClientPackageWindows": 2,
    }


def test_build_dispatch_plan_falls_back_to_sequential_when_only_one_idle_agent_exists() -> None:
    ready_agents = {
        1: AgentHandle(1, "TCMainAgent", True, True, True, 0),
    }
    plan = build_dispatch_plan(
        [
            "BDFrameworkCore_BuildClientPackageIos",
            "BDFrameworkCore_BuildClientPackageWindows",
        ],
        compatible_agent_ids_by_build_type={
            "BDFrameworkCore_BuildClientPackageIos": (1,),
            "BDFrameworkCore_BuildClientPackageWindows": (1,),
        },
        ready_agents=ready_agents,
        dispatch_mode="auto",
    )

    assert plan.mode == "sequential"
    assert plan.parallel_assignment in (
        {"BDFrameworkCore_BuildClientPackageIos": 1},
        {"BDFrameworkCore_BuildClientPackageWindows": 1},
    )


def test_build_dispatch_plan_raises_when_parallel_is_forced_without_capacity() -> None:
    ready_agents = {
        1: AgentHandle(1, "TCMainAgent", True, True, True, 0),
    }

    try:
        build_dispatch_plan(
            [
                "BDFrameworkCore_BuildClientPackageIos",
                "BDFrameworkCore_BuildClientPackageWindows",
            ],
            compatible_agent_ids_by_build_type={
                "BDFrameworkCore_BuildClientPackageIos": (1,),
                "BDFrameworkCore_BuildClientPackageWindows": (1,),
            },
            ready_agents=ready_agents,
            dispatch_mode="parallel",
        )
    except Exception as exc:  # pragma: no branch - explicit assertion keeps dependency-free
        assert "dispatch-mode=parallel" in str(exc)
    else:  # pragma: no cover
        raise AssertionError("Expected build_dispatch_plan to reject impossible parallel mode")


def test_api_request_retries_get_502_then_succeeds(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    attempts: list[str] = []

    def fake_urlopen(request):
        attempts.append(request.full_url)
        if len(attempts) == 1:
            raise urllib.error.HTTPError(
                request.full_url,
                502,
                "Bad Gateway",
                hdrs=None,
                fp=BytesIO(b"temporary gateway error"),
            )
        return FakeResponse(200, b'{"id": 277}')

    sleep_calls: list[int] = []
    monkeypatch.setattr(ups.urllib.request, "urlopen", fake_urlopen)
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: sleep_calls.append(seconds))

    response = api_request(make_config(), "GET", "/app/rest/builds/id:277")

    assert response.status_code == 200
    assert response.data == {"id": 277}
    assert len(attempts) == 2
    assert sleep_calls == [1]


def test_api_request_text_retries_get_502_then_succeeds(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    attempts = 0

    def fake_urlopen(request):
        nonlocal attempts
        attempts += 1
        if attempts == 1:
            raise urllib.error.HTTPError(
                request.full_url,
                502,
                "Bad Gateway",
                hdrs=None,
                fp=BytesIO(b"temporary gateway error"),
            )
        return FakeResponse(200, b"build log")

    sleep_calls: list[int] = []
    monkeypatch.setattr(ups.urllib.request, "urlopen", fake_urlopen)
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: sleep_calls.append(seconds))

    payload = api_request_text(make_config(), "GET", "/downloadBuildLog.html?buildId=277")

    assert payload == "build log"
    assert attempts == 2
    assert sleep_calls == [1]


def test_api_request_retries_get_cpolar_404_then_succeeds(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    attempts = 0

    def fake_urlopen(request):
        nonlocal attempts
        attempts += 1
        if attempts == 1:
            raise urllib.error.HTTPError(
                request.full_url,
                404,
                "Not Found",
                hdrs=None,
                fp=BytesIO(
                    b"<!DOCTYPE html><html><title>(404) The page you were looking for domain doesn't exist.</title><body>Powered by cpolar.com</body></html>"
                ),
            )
        return FakeResponse(200, b'{"id": 280}')

    sleep_calls: list[int] = []
    monkeypatch.setattr(ups.urllib.request, "urlopen", fake_urlopen)
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: sleep_calls.append(seconds))

    response = api_request(make_config(), "GET", "/app/rest/builds/id:280")

    assert response.status_code == 200
    assert response.data == {"id": 280}
    assert attempts == 2
    assert sleep_calls == [1]


def test_api_request_does_not_retry_plain_404(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    attempts = 0

    def fake_urlopen(request):
        nonlocal attempts
        attempts += 1
        raise urllib.error.HTTPError(
            request.full_url,
            404,
            "Not Found",
            hdrs=None,
            fp=BytesIO(b'{"error":"build not found"}'),
        )

    sleep_calls: list[int] = []
    monkeypatch.setattr(ups.urllib.request, "urlopen", fake_urlopen)
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: sleep_calls.append(seconds))

    with pytest.raises(TeamCityApiError, match="status=404"):
        api_request(make_config(), "GET", "/app/rest/builds/id:999999")

    assert attempts == 1
    assert sleep_calls == []


def test_api_request_does_not_retry_post_502(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    attempts = 0

    def fake_urlopen(request):
        nonlocal attempts
        attempts += 1
        raise urllib.error.HTTPError(
            request.full_url,
            502,
            "Bad Gateway",
            hdrs=None,
            fp=BytesIO(b"temporary gateway error"),
        )

    sleep_calls: list[int] = []
    monkeypatch.setattr(ups.urllib.request, "urlopen", fake_urlopen)
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: sleep_calls.append(seconds))

    with pytest.raises(TeamCityApiError, match="status=502"):
        api_request(make_config(), "POST", "/app/rest/buildQueue", payload={"buildType": {"id": "foo"}})

    assert attempts == 1
    assert sleep_calls == []