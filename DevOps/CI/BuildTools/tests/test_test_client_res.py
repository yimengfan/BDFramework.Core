"""TeamCity TestClientRes 编排辅助函数测试。

测试覆盖范围：
1. 构建复用：验证优先复用同分支、同修订、同参数的成功构建。
2. 运行中构建复用：验证无可复用成功构建时回退到正在运行的构建。
3. resolve-builds 命令：验证复用已有构建或排队缺失的上游任务，并导出 TeamCity 参数。
4. wait-builds 命令：验证等待每个上游构建完成并导出版本信息。
5. queue-verify-build 命令：验证当前父构建内会直接执行平台校验脚本，并在失败时返回错误。
"""

from __future__ import annotations

from pathlib import Path
import sys
from types import SimpleNamespace

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from VerifyClientRes import test_client_res  # noqa: E402


def make_build(
    *,
    build_id: int,
    build_type_id: str,
    number: str,
    state: str,
    status: str,
    branch_name: str,
    revision: str,
    client_version: str,
    build_extra_args: str = "",
) -> dict[str, object]:
    """构造一个模拟 TeamCity 构建负载的字典，用于 TestClientRes 单元测试。

    参数：
        build_id: 构建唯一 ID。
        build_type_id: 构建类型标识（如 BDFrameworkCore_BuildCodeAndroid）。
        number: 构建编号。
        state: 构建状态（finished / running / queued）。
        status: 构建结果（SUCCESS / FAILURE）。
        branch_name: VCS 分支名。
        revision: VCS 修订哈希。
        client_version: 客户端版本号。
        build_extra_args: 额外构建参数（默认为空）。
    """
    return {
        "id": build_id,
        "buildTypeId": build_type_id,
        "number": number,
        "state": state,
        "status": status,
        "statusText": status,
        "branchName": branch_name,
        "webUrl": f"http://ci/build/{build_id}",
        "revisions": {
            "revision": [
                {
                    "version": revision,
                    "vcsBranchName": f"refs/heads/{branch_name}",
                }
            ]
        },
        "properties": {
            "property": [
                {"name": "build.client.version", "value": client_version},
                {"name": "build.extra.args", "value": build_extra_args},
            ]
        },
    }


def test_find_reusable_build_prefers_finished_success_for_same_revision_and_properties() -> None:
    """验证 TestClientRes 优先复用同分支、同修订、同构建参数的成功构建。"""
    builds = [
        make_build(
            build_id=201,
            build_type_id="BDFrameworkCore_BuildCodeAndroid",
            number="31",
            state="finished",
            status="SUCCESS",
            branch_name="v4/v-4.0.0",
            revision="abc123",
            client_version="0.1",
        ),
        make_build(
            build_id=202,
            build_type_id="BDFrameworkCore_BuildCodeAndroid",
            number="32",
            state="finished",
            status="FAILURE",
            branch_name="v4/v-4.0.0",
            revision="abc123",
            client_version="0.1",
        ),
    ]

    reusable = test_client_res.find_reusable_build(
        builds,
        branch_name="v4/v-4.0.0",
        vcs_revision="abc123",
        client_version="0.1",
        build_extra_args="",
    )

    assert reusable is not None
    assert reusable.build_id == 201
    assert reusable.number == "31"


def test_find_reusable_build_falls_back_to_running_build_when_no_success_exists() -> None:
    """验证在没有可复用的成功构建时，TestClientRes 可以复用同分支和修订的进行中构建。"""
    builds = [
        make_build(
            build_id=301,
            build_type_id="BDFrameworkCore_BuildAssetbundleAndroid",
            number="",
            state="running",
            status="SUCCESS",
            branch_name="v4/v-4.0.0",
            revision="def456",
            client_version="0.1",
            build_extra_args="--dry-run",
        )
    ]

    reusable = test_client_res.find_reusable_build(
        builds,
        branch_name="refs/heads/v4/v-4.0.0",
        vcs_revision="def456",
        client_version="0.1",
        build_extra_args="--dry-run",
    )

    assert reusable is not None
    assert reusable.build_id == 301
    assert reusable.is_inflight is True


def test_find_reusable_build_can_require_additional_properties() -> None:
    """验证构建复用逻辑支持额外属性匹配，避免错误复用不同参数的旧构建。"""
    mismatched_build = make_build(
        build_id=401,
        build_type_id="BDFrameworkCore_VerifyClientResAndroid",
        number="17",
        state="finished",
        status="SUCCESS",
        branch_name="v4/v-4.0.0",
        revision="verify123",
        client_version="0.1",
        build_extra_args="--server-url http://192.168.0.240:20001",
    )
    mismatched_build["properties"]["property"].extend(
        [
            {"name": "test.clientres.expected.code.version", "value": "31"},
            {"name": "test.clientres.expected.assetbundle.version", "value": "33"},
            {"name": "test.clientres.expected.table.version", "value": "27"},
        ]
    )

    matching_build = make_build(
        build_id=402,
        build_type_id="BDFrameworkCore_VerifyClientResAndroid",
        number="18",
        state="running",
        status="SUCCESS",
        branch_name="v4/v-4.0.0",
        revision="verify123",
        client_version="0.1",
        build_extra_args="--server-url http://192.168.0.240:20001",
    )
    matching_build["properties"]["property"].extend(
        [
            {"name": "test.clientres.expected.code.version", "value": "32"},
            {"name": "test.clientres.expected.assetbundle.version", "value": "34"},
            {"name": "test.clientres.expected.table.version", "value": "29"},
        ]
    )

    reusable = test_client_res.find_reusable_build(
        [mismatched_build, matching_build],
        branch_name="v4/v-4.0.0",
        vcs_revision="verify123",
        client_version="0.1",
        build_extra_args="--server-url http://192.168.0.240:20001",
        required_properties={
            "test.clientres.expected.code.version": "32",
            "test.clientres.expected.assetbundle.version": "34",
            "test.clientres.expected.table.version": "29",
        },
    )

    assert reusable is not None
    assert reusable.build_id == 402
    assert reusable.is_inflight is True


def test_command_resolve_builds_reuses_existing_builds_and_emits_teamcity_params(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 resolve-builds 命令复用匹配的上游 TeamCity 构建并导出其 ID 和编号。"""
    args = SimpleNamespace(
        platform="android",
        client_version="0.1",
        branch="v4/v-4.0.0",
        vcs_revision="abc123",
        config="/tmp/buildtools.toml",
        source_build_id="901",
        upstream_build_extra_args="",
        search_count=20,
    )

    monkeypatch.setattr(
        test_client_res,
        "resolve_teamcity_runtime_config",
        lambda config_path: test_client_res.TeamCityRuntimeConfig(
            base_url="http://ci",
            token="token",
            config_path=Path("/tmp/buildtools.toml"),
        ),
    )
    monkeypatch.setattr(
        test_client_res,
        "list_recent_builds",
        lambda config, *, build_type_id, search_count: [
            make_build(
                build_id={
                    "BDFrameworkCore_BuildCodeAndroid": 101,
                    "BDFrameworkCore_BuildAssetbundleAndroid": 102,
                    "BDFrameworkCore_BuildTable": 103,
                }[build_type_id],
                build_type_id=build_type_id,
                number={
                    "BDFrameworkCore_BuildCodeAndroid": "11",
                    "BDFrameworkCore_BuildAssetbundleAndroid": "22",
                    "BDFrameworkCore_BuildTable": "33",
                }[build_type_id],
                state="finished",
                status="SUCCESS",
                branch_name="v4/v-4.0.0",
                revision="abc123",
                client_version="0.1",
            )
        ],
    )
    monkeypatch.setattr(test_client_res, "queue_build", lambda *args, **kwargs: (_ for _ in ()).throw(AssertionError("queue_build should not be called")))

    assert test_client_res.command_resolve_builds(args) == 0

    output = capsys.readouterr().out
    assert "reuse code buildTypeId=BDFrameworkCore_BuildCodeAndroid buildId=101" in output
    assert "##teamcity[setParameter name='test.clientres.code.build.id' value='101']" in output
    assert "##teamcity[setParameter name='test.clientres.assetbundle.build.number' value='22']" in output
    assert "##teamcity[setParameter name='test.clientres.table.build.number' value='33']" in output


def test_command_resolve_builds_queues_missing_builds_with_revision_and_properties(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 resolve-builds 命令为缺失的上游任务排队，并传递精确的修订版本和构建属性。"""
    args = SimpleNamespace(
        platform="windows",
        client_version="0.2",
        branch="v4/v-4.0.0",
        vcs_revision="fedcba",
        config=None,
        source_build_id="902",
        upstream_build_extra_args="--dry-run",
        search_count=5,
    )
    queued_calls: list[dict[str, object]] = []

    monkeypatch.setattr(
        test_client_res,
        "resolve_teamcity_runtime_config",
        lambda config_path: test_client_res.TeamCityRuntimeConfig(
            base_url="http://ci",
            token="token",
            config_path=None,
        ),
    )
    monkeypatch.setattr(test_client_res, "list_recent_builds", lambda *args, **kwargs: [])

    def fake_queue_build(config, *, build_type_id, branch_name, vcs_revision, properties, comment):
        queued_calls.append(
            {
                "build_type_id": build_type_id,
                "branch_name": branch_name,
                "vcs_revision": vcs_revision,
                "properties": properties,
                "comment": comment,
            }
        )
        return test_client_res.BuildHandle(
            build_id=700 + len(queued_calls),
            build_type_id=build_type_id,
            number="",
            state="queued",
            status="SUCCESS",
            status_text="queued",
            branch_name=branch_name,
            web_url=f"http://ci/build/{700 + len(queued_calls)}",
            revision=vcs_revision,
            client_version="0.2",
            build_extra_args="--dry-run",
        )

    monkeypatch.setattr(test_client_res, "queue_build", fake_queue_build)

    assert test_client_res.command_resolve_builds(args) == 0

    output = capsys.readouterr().out
    assert len(queued_calls) == 3
    assert queued_calls[0]["build_type_id"] == "BDFrameworkCore_BuildCodeWindows"
    assert queued_calls[0]["vcs_revision"] == "fedcba"
    assert queued_calls[0]["properties"] == [
        {"name": "build.client.version", "value": "0.2"},
        {"name": "build.extra.args", "value": "--dry-run"},
    ]
    assert "##teamcity[setParameter name='test.clientres.code.build.id' value='701']" in output
    assert "queued table buildTypeId=BDFrameworkCore_BuildTable buildId=703" in output


def test_command_wait_builds_exports_resolved_numbers(monkeypatch: pytest.MonkeyPatch, capsys) -> None:
    """验证 wait-builds 命令等待每个上游构建完成并导出各组件编号和合并后的版本信息。"""
    args = SimpleNamespace(
        platform="ios",
        config="/tmp/buildtools.toml",
        timeout_seconds=60,
        poll_interval_seconds=5,
        code_build_id="401",
        assetbundle_build_id="402",
        table_build_id="403",
    )

    monkeypatch.setattr(
        test_client_res,
        "resolve_teamcity_runtime_config",
        lambda config_path: test_client_res.TeamCityRuntimeConfig(
            base_url="http://ci",
            token="token",
            config_path=Path("/tmp/buildtools.toml"),
        ),
    )

    def fake_wait_for_build_success(config, *, build_id, timeout_seconds, poll_interval_seconds, log_prefix):
        return test_client_res.BuildHandle(
            build_id=build_id,
            build_type_id="fake",
            number={401: "111", 402: "222", 403: "333"}[build_id],
            state="finished",
            status="SUCCESS",
            status_text="success",
            branch_name="v4/v-4.0.0",
            web_url=None,
            revision="abc",
            client_version="0.1",
            build_extra_args="",
        )

    monkeypatch.setattr(test_client_res, "wait_for_build_success", fake_wait_for_build_success)

    assert test_client_res.command_wait_builds(args) == 0

    output = capsys.readouterr().out
    assert "##teamcity[setParameter name='test.clientres.code.build.number' value='111']" in output
    assert "##teamcity[setParameter name='test.clientres.assetbundle.build.number' value='222']" in output
    assert "##teamcity[setParameter name='test.clientres.table.build.number' value='333']" in output
    assert "##teamcity[setParameter name='test.clientres.expected.version.info' value='111.222.333']" in output


def test_queue_build_includes_vcs_root_instance_for_revision(monkeypatch: pytest.MonkeyPatch) -> None:
    """Verify queue_build includes the TeamCity VCS root instance context required by the buildQueue revisions payload."""
    config = test_client_res.TeamCityRuntimeConfig(base_url="http://ci", token="token", config_path=None)
    captured_post_payload: dict[str, object] = {}

    def fake_api_request_json(config_value, method, path, *, payload=None):
        assert config_value == config
        if method == "GET":
            assert path.startswith("/app/rest/buildTypes/id:BDFrameworkCore_BuildCodeAndroid/vcsRootInstances")
            return {
                "vcs-root-instance": [
                    {
                        "id": "12",
                        "default": True,
                        "properties": {
                            "property": [
                                {"name": "branch", "value": "refs/heads/v4/v-4.0.0"},
                            ]
                        },
                    }
                ]
            }

        assert method == "POST"
        assert path == "/app/rest/buildQueue"
        assert payload is not None
        captured_post_payload.update(payload)
        return {
            "id": 901,
            "buildType": {"id": "BDFrameworkCore_BuildCodeAndroid"},
            "number": "77",
            "state": "queued",
            "status": "SUCCESS",
            "statusText": "queued",
            "branchName": "v4/v-4.0.0",
            "webUrl": "http://ci/build/901",
            "revisions": {
                "revision": [
                    {
                        "version": "abc123",
                        "vcsBranchName": "refs/heads/v4/v-4.0.0",
                    }
                ]
            },
            "properties": {
                "property": [
                    {"name": "build.client.version", "value": "0.1"},
                    {"name": "build.extra.args", "value": "--dry-run"},
                ]
            },
        }

    monkeypatch.setattr(test_client_res, "api_request_json", fake_api_request_json)

    handle = test_client_res.queue_build(
        config,
        build_type_id="BDFrameworkCore_BuildCodeAndroid",
        branch_name="v4/v-4.0.0",
        vcs_revision="abc123",
        properties=[
            {"name": "build.client.version", "value": "0.1"},
            {"name": "build.extra.args", "value": "--dry-run"},
        ],
        comment="Triggered by TestClientRes",
    )

    assert handle.build_id == 901
    assert captured_post_payload["branchName"] == "v4/v-4.0.0"
    assert captured_post_payload["revisions"] == {
        "failOnMissingRevisions": True,
        "revision": [
            {
                "version": "abc123",
                "vcs-root-instance": {"id": "12"},
                "vcsBranchName": "refs/heads/v4/v-4.0.0",
            }
        ],
    }


def test_command_queue_verify_build_executes_platform_specific_local_check_in_current_build(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 queue-verify-build 会在当前父构建内直接执行平台校验脚本，并回写父构建参数。"""
    args = SimpleNamespace(
        platform="android",
        client_version="0.1",
        branch="v4/v-4.0.0",
        vcs_revision="abc123",
        config="/tmp/buildtools.toml",
        source_build_id="903",
        timeout_seconds=120,
        poll_interval_seconds=10,
        expected_code_version="11",
        expected_assetbundle_version="22",
        expected_table_version="33",
        verify_build_extra_args="--server-url http://192.168.0.240:20001",
    )
    executed_commands: list[dict[str, object]] = []
    expected_script_path = Path(test_client_res.__file__).resolve().with_name("verify_android.py")

    monkeypatch.setattr(test_client_res.sys, "executable", "/tmp/python3")
    monkeypatch.setenv("TEAMCITY_BUILDCONF_NAME", "TestClientRes")
    monkeypatch.setenv("BUILD_NUMBER", "18")

    def fake_subprocess_run(command, *, cwd, check):
        executed_commands.append(
            {
                "command": command,
                "cwd": cwd,
                "check": check,
            }
        )
        return SimpleNamespace(returncode=0)

    monkeypatch.setattr(test_client_res.subprocess, "run", fake_subprocess_run)

    assert test_client_res.command_queue_verify_build(args) == 0

    output = capsys.readouterr().out
    assert executed_commands == [
        {
            "command": [
                "/tmp/python3",
                str(expected_script_path),
                "--client-version",
                "0.1",
                "--expected-code-version",
                "11",
                "--expected-assetbundle-version",
                "22",
                "--expected-table-version",
                "33",
                "--config",
                "/tmp/buildtools.toml",
                "--build-name",
                "TestClientRes",
                "--build-number",
                "18",
                "--server-url",
                "http://192.168.0.240:20001",
            ],
            "cwd": str(test_client_res.WORKSPACE_ROOT),
            "check": False,
        }
    ]
    assert "##teamcity[setParameter name='test.clientres.verify.build.id' value='903']" in output
    assert "##teamcity[setParameter name='test.clientres.verify.build.number' value='18']" in output
    assert "local verify finished successfully in current build" in output


def test_command_queue_verify_build_raises_when_local_verify_script_fails(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证 queue-verify-build 如果本地校验脚本返回非零退出码，会立刻抛出错误。"""
    args = SimpleNamespace(
        platform="android",
        client_version="0.1",
        branch="v4/v-4.0.0",
        vcs_revision="abc123",
        config=None,
        source_build_id="904",
        timeout_seconds=120,
        poll_interval_seconds=10,
        expected_code_version="32",
        expected_assetbundle_version="34",
        expected_table_version="29",
        verify_build_extra_args="--server-url http://192.168.0.240:20001",
    )

    monkeypatch.setattr(test_client_res.subprocess, "run", lambda *args, **kwargs: SimpleNamespace(returncode=2))

    with pytest.raises(
        test_client_res.TestClientResError,
        match="Local verify script failed with exit code 2",
    ):
        test_client_res.command_queue_verify_build(args)


def test_wait_for_build_success_emits_heartbeat_when_state_does_not_change(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证等待中的 TeamCity 构建如果长时间无状态变化，会持续输出心跳日志。"""
    config = test_client_res.TeamCityRuntimeConfig(base_url="http://ci", token="token", config_path=None)
    handles = iter(
        [
            test_client_res.BuildHandle(
                build_id=906,
                build_type_id="BDFrameworkCore_VerifyClientResAndroid",
                number="18",
                state="running",
                status="SUCCESS",
                status_text="running",
                branch_name="v4/v-4.0.0",
                web_url="http://ci/build/906",
                revision="abc123",
                client_version="0.1",
                build_extra_args="",
            ),
            test_client_res.BuildHandle(
                build_id=906,
                build_type_id="BDFrameworkCore_VerifyClientResAndroid",
                number="18",
                state="running",
                status="SUCCESS",
                status_text="running",
                branch_name="v4/v-4.0.0",
                web_url="http://ci/build/906",
                revision="abc123",
                client_version="0.1",
                build_extra_args="",
            ),
            test_client_res.BuildHandle(
                build_id=906,
                build_type_id="BDFrameworkCore_VerifyClientResAndroid",
                number="18",
                state="finished",
                status="SUCCESS",
                status_text="success",
                branch_name="v4/v-4.0.0",
                web_url="http://ci/build/906",
                revision="abc123",
                client_version="0.1",
                build_extra_args="",
            ),
        ]
    )
    monotonic_values = iter([0.0, 0.0, 61.0, 62.0])

    monkeypatch.setattr(test_client_res, "get_build", lambda *args, **kwargs: next(handles))
    monkeypatch.setattr(test_client_res, "read_build_log_tail", lambda *args, **kwargs: "")
    monkeypatch.setattr(test_client_res.time, "sleep", lambda *_args, **_kwargs: None)
    monkeypatch.setattr(test_client_res.time, "monotonic", lambda: next(monotonic_values))
    monkeypatch.setattr(test_client_res, "DEFAULT_WAIT_HEARTBEAT_SECONDS", 60)

    handle = test_client_res.wait_for_build_success(
        config,
        build_id=906,
        timeout_seconds=300,
        poll_interval_seconds=1,
        log_prefix="[TestClientRes][Android]",
    )

    assert handle.build_id == 906
    output = capsys.readouterr().out
    assert "stillWaiting buildId=906 buildTypeId=BDFrameworkCore_VerifyClientResAndroid" in output
