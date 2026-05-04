from __future__ import annotations

"""验证 TeamCity 主脚本的调度与重试行为。 Verify TeamCity helper dispatch and retry behavior."""

import argparse
from io import BytesIO
from pathlib import Path
import subprocess
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
    merge_queue_properties_with_ci_credentials,
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


def test_load_env_file_preserves_shell_value_and_reports_override(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证 .env 只补缺，不覆盖现有 shell 变量，并打印覆盖提示。
    Verify that .env only fills missing variables, keeps existing shell values, and reports preserved overrides.
    """

    env_file = tmp_path / "teamcity.env"
    env_file.write_text(
        "TEAMCITY_TOKEN=file-token\nTEAMCITY_BASE_URL=http://from-file\n",
        encoding="utf-8",
    )
    monkeypatch.setenv("TEAMCITY_TOKEN", "shell-token")
    monkeypatch.delenv("TEAMCITY_BASE_URL", raising=False)

    ups.load_env_file(env_file)

    output = capsys.readouterr().out
    assert "keep existing shell env TEAMCITY_TOKEN" in output
    assert ups.os.environ["TEAMCITY_TOKEN"] == "shell-token"
    assert ups.os.environ["TEAMCITY_BASE_URL"] == "http://from-file"


def test_load_env_file_rejects_invalid_line(tmp_path: Path) -> None:
    """验证非法 .env 行会立即抛错，而不是静默忽略。
    Verify that an invalid .env line fails fast instead of being silently ignored.
    """

    env_file = tmp_path / "teamcity.env"
    env_file.write_text("TEAMCITY_TOKEN=file-token\nINVALID_LINE\n", encoding="utf-8")

    with pytest.raises(TeamCityApiError, match="Invalid \\.env line"):
        ups.load_env_file(env_file)


def test_build_config_resolves_output_dir_from_env_file(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证 TEAMCITY_OUTPUT_DIR 会相对 skill 目录解析，而不是相对当前工作目录。
    Verify that TEAMCITY_OUTPUT_DIR resolves relative to the skill directory instead of the current working directory.
    """

    env_file = tmp_path / "teamcity.env"
    env_file.write_text(
        "TEAMCITY_BASE_URL=http://svn.funtoo.games\n"
        "TEAMCITY_PROJECT_ID=BDFrameworkCore\n"
        "TEAMCITY_TOKEN=file-token\n"
        "TEAMCITY_OUTPUT_DIR=tc_output\n",
        encoding="utf-8",
    )
    for key in (
        "TEAMCITY_BASE_URL",
        "TEAMCITY_PROJECT_ID",
        "TEAMCITY_TOKEN",
        "TEAMCITY_USERNAME",
        "TEAMCITY_PASSWORD",
        "TEAMCITY_OUTPUT_DIR",
    ):
        monkeypatch.delenv(key, raising=False)

    config = ups.build_config(
        argparse.Namespace(
            env_file=str(env_file),
            base_url=None,
            project_id=None,
        )
    )

    assert config.output_dir == (ups.SKILL_DIR / "tc_output").resolve()


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
            "windows, manual-check",
            "manual-check",
            "android",
        ]
    ) == ["windows", "manual-check", "android"]


def test_build_queue_comment_always_includes_target_and_branch() -> None:
    assert build_queue_comment(
        build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        branch="v4/v-4.0.0",
        comment="回归验证",
    ) == "回归验证 | 测试目标: BDFrameworkCore_BuildClientPackageAndroid | 分支: v4/v-4.0.0"


def test_build_queue_tags_keep_only_user_supplied_tags() -> None:
    """验证 build_queue_tags 只保留用户显式传入的 tag，不再自动注入默认来源标记。
    Verify that build_queue_tags keeps only the explicitly supplied tags and no longer injects a default source tag.
    """
    assert build_queue_tags(
        build_type_id="BDFrameworkCore_BuildClientPackageAndroid",
        tags=["win64", "manual-check"],
    ) == [
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
                {"name": "manual-check"},
            ]
        },
    }


def test_merge_queue_properties_with_ci_credentials_injects_token_when_missing() -> None:
    """验证 run-build 默认会把当前 TEAMCITY_TOKEN 透传给远端构建。 Verify run-build forwards the current TEAMCITY_TOKEN to queued builds by default."""

    merged_properties = merge_queue_properties_with_ci_credentials(
        make_config(),
        [{"name": "build.client.version", "value": "0.1"}],
    )

    assert merged_properties == [
        {"name": "build.client.version", "value": "0.1"},
        {"name": "env.TEAMCITY_TOKEN", "value": "token"},
    ]


def test_merge_queue_properties_with_ci_credentials_keeps_explicit_token_override() -> None:
    """验证显式传入的 env.TEAMCITY_TOKEN 不会被 helper 自动覆盖。 Verify an explicit env.TEAMCITY_TOKEN property is never overwritten by the helper."""

    merged_properties = merge_queue_properties_with_ci_credentials(
        make_config(),
        [{"name": "env.TEAMCITY_TOKEN", "value": "manual-token"}],
    )

    assert merged_properties == [
        {"name": "env.TEAMCITY_TOKEN", "value": "manual-token"}
    ]


def test_merge_queue_properties_with_ci_credentials_falls_back_to_basic_auth() -> None:
    """验证没有 token 时 helper 会退回透传账号密码变量。 Verify the helper falls back to username and password properties when no token is available."""

    config = TeamCityConfig(
        base_url="http://svn.funtoo.games",
        project_id="BDFrameworkCore",
        token=None,
        username="user",
        password="pass",
        output_dir=Path("/tmp/teamcityskill-tests"),
    )

    merged_properties = merge_queue_properties_with_ci_credentials(config, [])

    assert merged_properties == [
        {"name": "env.TEAMCITY_USERNAME", "value": "user"},
        {"name": "env.TEAMCITY_PASSWORD", "value": "pass"},
    ]


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


def test_normalize_public_teamcity_url_rebases_internal_web_url() -> None:
    """验证 helper 会把 TeamCity 返回的内网 webUrl 重写到当前配置的公开 base_url。
    Verify that the helper rebases an intranet TeamCity webUrl onto the currently configured public base URL.
    """
    config = TeamCityConfig(
        base_url="https://ci.example.com/teamcity",
        project_id="BDFrameworkCore",
        token="token",
        username=None,
        password=None,
        output_dir=Path("/tmp/teamcityskill-tests"),
    )

    normalized_url = ups.normalize_public_teamcity_url(
        config,
        "http://192.168.0.240:20000/buildConfiguration/BDFrameworkCore_TalosAIStep01BaseFlowTest/1001?mode=builds",
    )

    assert normalized_url == (
        "https://ci.example.com/teamcity/buildConfiguration/"
        "BDFrameworkCore_TalosAIStep01BaseFlowTest/1001?mode=builds"
    )


def test_print_build_summary_rewrites_web_url_and_shows_running_info(
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证构建摘要会同时输出公开 webUrl 和 running-info 阶段字段。
    Verify that the build summary prints both the public webUrl and the running-info stage fields.
    """
    build = {
        "id": 1001,
        "buildTypeId": "BDFrameworkCore_TalosAIStep01BaseFlowTest",
        "state": "running",
        "status": "SUCCESS",
        "statusText": "Step 1/2",
        "webUrl": "http://192.168.0.240:20000/buildConfiguration/BDFrameworkCore_TalosAIStep01BaseFlowTest/1001",
        "running-info": {
            "percentageComplete": 38,
            "probablyHanging": False,
            "currentStageText": "Step 1/2: waiting upstream build 1002",
        },
    }

    ups.print_build_summary(make_config(), build)

    output = capsys.readouterr().out
    assert "webUrl='http://svn.funtoo.games/buildConfiguration/BDFrameworkCore_TalosAIStep01BaseFlowTest/1001'" in output
    assert "serverWebUrl='http://192.168.0.240:20000/buildConfiguration/BDFrameworkCore_TalosAIStep01BaseFlowTest/1001'" in output
    assert "progress=38" in output
    assert "hanging=False" in output
    assert "stage='Step 1/2: waiting upstream build 1002'" in output


def test_wait_for_build_completion_reprints_same_state_after_heartbeat(
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证等待循环在状态不变但超过心跳窗口时仍会再次输出构建摘要。
    Verify that the wait loop still prints another build summary after the heartbeat window even when the state tuple stays unchanged.
    """
    builds = [
        {
            "id": 277,
            "buildTypeId": "BDFrameworkCore_TalosAIStep01BaseFlowTest",
            "state": "running",
            "status": "SUCCESS",
            "statusText": "Step 1/2",
            "webUrl": "http://192.168.0.240:20000/build/277",
            "running-info": {
                "percentageComplete": 10,
                "probablyHanging": False,
                "currentStageText": "stage-a",
            },
        },
        {
            "id": 277,
            "buildTypeId": "BDFrameworkCore_TalosAIStep01BaseFlowTest",
            "state": "running",
            "status": "SUCCESS",
            "statusText": "Step 1/2",
            "webUrl": "http://192.168.0.240:20000/build/277",
            "running-info": {
                "percentageComplete": 10,
                "probablyHanging": False,
                "currentStageText": "stage-a",
            },
        },
        {
            "id": 277,
            "buildTypeId": "BDFrameworkCore_TalosAIStep01BaseFlowTest",
            "state": "finished",
            "status": "SUCCESS",
            "statusText": "Success",
            "webUrl": "http://192.168.0.240:20000/build/277",
            "running-info": {},
        },
    ]
    monotonic_values = iter([0.0, 0.0, 61.0, 62.0])

    # The wait loop now does a fast-path state-only check first; mock it to return running
    # so the test continues into the full polling loop as originally designed.
    monkeypatch.setattr(
        ups,
        "_fetch_build_state_only",
        lambda config, build_id: {"id": build_id, "state": "running", "status": "SUCCESS"},
    )
    monkeypatch.setattr(ups, "get_build", lambda config, build_id: builds.pop(0))
    monkeypatch.setattr(ups.time, "monotonic", lambda: next(monotonic_values))
    monkeypatch.setattr(ups.time, "sleep", lambda seconds: None)

    result = ups.wait_for_build_completion(
        make_config(),
        build_id=277,
        timeout_seconds=120,
        poll_interval_seconds=5,
        log_tail_lines=20,
    )

    output = capsys.readouterr().out
    assert result == 0
    assert output.count("[TeamCitySkill] build summary") == 3


def test_wait_for_build_completion_fast_path_returns_immediately_when_already_finished(
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证快速检查发现构建已结束时，wait 立即返回且不进入轮询循环。
    Verify that the fast-path state-only check returns immediately when the build is already finished.
    """
    finished_build = {
        "id": 42,
        "buildTypeId": "BDFrameworkCore_TalosAIStep01BaseFlowTest",
        "state": "finished",
        "status": "SUCCESS",
        "statusText": "Success",
        "webUrl": "http://192.168.0.240:20000/build/42",
        "running-info": {},
    }

    # The fast-path check should see state=finished and skip the full polling loop.
    monkeypatch.setattr(
        ups,
        "_fetch_build_state_only",
        lambda config, build_id: {"id": build_id, "state": "finished", "status": "SUCCESS", "finishDate": "20260421T100555+0800"},
    )
    # get_build is still called once for the final summary, but the polling loop must never run.
    monkeypatch.setattr(ups, "get_build", lambda config, build_id: finished_build)

    result = ups.wait_for_build_completion(
        make_config(),
        build_id=42,
        timeout_seconds=120,
        poll_interval_seconds=5,
        log_tail_lines=20,
    )

    output = capsys.readouterr().out
    assert result == 0
    assert "state='finished'" in output
    assert output.count("[TeamCitySkill] build summary") == 1


def test_run_local_talos_batchmode_preflight_forwards_test_file_in_tcp_mode(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证本地 Talos gate 在 TCP 模式下会把显式 test-file 传给 batchmode 脚本。
    Verify that the local Talos gate forwards the explicit test-file to the batchmode script in TCP mode.
    """

    captured: dict[str, object] = {}

    monkeypatch.setattr(ups, "TALOS_BATCHMODE_SCRIPT", Path("/tmp/test-batchmode.sh"))
    monkeypatch.setattr(Path, "is_file", lambda self: True)
    monkeypatch.setattr(ups, "resolve_bash_executable", lambda: "/bin/bash")

    def fake_run(command, cwd, env, check):
        captured["command"] = command
        captured["cwd"] = cwd
        captured["env"] = env
        captured["check"] = check
        return subprocess.CompletedProcess(command, 0)

    monkeypatch.setattr(ups.subprocess, "run", fake_run)

    result = ups.run_local_talos_batchmode_preflight(
        unity_path="/Applications/Unity/Unity",
        project_path=Path("/tmp/project"),
        test_file="tests/testBaseFlow-e2e.spec.ts",
        local_batchmode_mode="tcp",
    )

    assert result == 0
    assert captured["command"] == [
        "/bin/bash",
        "/tmp/test-batchmode.sh",
        "--test-file",
        "tests/testBaseFlow-e2e.spec.ts",
    ]
    assert captured["cwd"] == str(ups.PLAYWRIGHT_DIR)
    assert captured["env"]["UNITY_PATH"] == "/Applications/Unity/Unity"
    assert captured["env"]["PROJECT_PATH"] == str(Path("/tmp/project").resolve())
    assert "TALOS_MODE" not in captured["env"]


def test_command_run_talos_baseflow_chain_reuses_package_build_for_remote_baseflow(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证 Talos BaseFlow 链路会先通过本地 gate，再把成功的 package build id 传给远端 BaseFlow。
    Verify that the Talos BaseFlow chain passes the local gate first and then reuses the successful package build id for the remote BaseFlow step.
    """

    ordered_calls: list[tuple[str, object]] = []

    monkeypatch.setattr(
        ups,
        "run_local_talos_batchmode_preflight",
        lambda **kwargs: ordered_calls.append(("local", kwargs)) or 0,
    )

    def fake_queue_and_wait_build(config, **kwargs):
        ordered_calls.append(("remote", kwargs))
        build_id = 1038 if kwargs["build_type_id"] == "BDFrameworkCore_BuildClientPackageAndroid" else 1041
        return 0, ups.QueuedBuildHandle(build_id=build_id, href=None, web_url=f"http://ci.example.com/build/{build_id}")

    monkeypatch.setattr(ups, "queue_and_wait_build", fake_queue_and_wait_build)

    result = ups.command_run_talos_baseflow_chain(
        make_config(),
        platform="android",
        unity_path="/Applications/Unity/Unity",
        project_path=Path("/tmp/project"),
        local_batchmode_mode="tcp",
        allow_local_sync_fallback=False,
        test_file="tests/testBaseFlow-e2e.spec.ts",
        client_version="0.1",
        build_mode="Debug",
        branch="v4/v-4.0.0",
        comment="Android BaseFlow sqlite probe validation",
        tags=["android", "baseflow-sqlite"],
        adb_serial="127.0.0.1:62001",
        adb_connect_targets="127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555",
        emulator_type="nox",
        baseflow_build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
        package_build_type_id=None,
        extra_properties=[],
        dry_run=False,
        timeout_seconds=7200,
        poll_interval_seconds=10,
        log_tail_lines=200,
    )

    assert result == 0
    assert ordered_calls[0][0] == "local"
    assert ordered_calls[1][1]["build_type_id"] == "BDFrameworkCore_BuildClientPackageAndroid"
    assert ordered_calls[2][1]["build_type_id"] == "BDFrameworkCore_TalosAIStep01BaseFlowTest"
    assert ordered_calls[2][1]["properties"] == [
        {"name": "build.client.version", "value": "0.1"},
        {"name": "build.build.mode", "value": "Debug"},
        {"name": "talos.e2e.platform", "value": "android"},
        {"name": "talos.e2e.package.build.id", "value": "1038"},
        {"name": "talos.e2e.package.build.type.id", "value": "BDFrameworkCore_BuildClientPackageAndroid"},
        {"name": "talos.e2e.test.file", "value": "tests/testBaseFlow-e2e.spec.ts"},
        {"name": "talos.e2e.adb.serial", "value": "127.0.0.1:62001"},
        {"name": "talos.e2e.adb.connect.targets", "value": "127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555"},
        {"name": "talos.e2e.emulator.type", "value": "nox"},
    ]


def test_command_run_talos_baseflow_chain_requires_explicit_sync_fallback_opt_in() -> None:
    """验证本地 sync 回退必须显式 opt-in，避免把较弱一致性的 gate 默认为标准链路。
    Verify that the local sync fallback requires an explicit opt-in so the less-consistent gate cannot silently become the default chain.
    """

    with pytest.raises(TeamCityApiError, match="allow-local-sync-fallback"):
        ups.command_run_talos_baseflow_chain(
            make_config(),
            platform="android",
            unity_path="/Applications/Unity/Unity",
            project_path=Path("/tmp/project"),
            local_batchmode_mode="sync",
            allow_local_sync_fallback=False,
            test_file="tests/testBaseFlow-e2e.spec.ts",
            client_version="0.1",
            build_mode="Debug",
            branch="v4/v-4.0.0",
            comment="Android BaseFlow sqlite probe validation",
            tags=["android"],
            adb_serial=None,
            adb_connect_targets=None,
            emulator_type=None,
            baseflow_build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
            package_build_type_id=None,
            extra_properties=[],
            dry_run=False,
            timeout_seconds=7200,
            poll_interval_seconds=10,
            log_tail_lines=200,
        )