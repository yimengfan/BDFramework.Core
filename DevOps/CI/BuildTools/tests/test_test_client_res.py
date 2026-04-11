"""Tests for the TeamCity TestClientRes orchestration helpers."""

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
    """Build one TeamCity-like build payload for TestClientRes unit tests."""
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
    """Verify TestClientRes reuses a successful build for the same branch, revision, and build parameters."""
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
    """Verify TestClientRes can reuse an inflight build for the same branch and revision instead of queueing another one."""
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


def test_command_resolve_builds_reuses_existing_builds_and_emits_teamcity_params(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify resolve-builds reuses matching upstream TeamCity builds and exports their ids/numbers."""
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
    """Verify resolve-builds queues missing upstream tasks with the exact revision and forwarded build properties."""
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
    """Verify wait-builds waits each upstream build and exports both individual numbers and the merged version.info value."""
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


def test_command_queue_verify_build_queues_platform_specific_local_check(
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """Verify queue-verify-build targets the platform-specific VerifyClientRes buildType with the resolved asset versions."""
    args = SimpleNamespace(
        platform="android",
        client_version="0.1",
        branch="v4/v-4.0.0",
        vcs_revision="abc123",
        config=None,
        source_build_id="903",
        timeout_seconds=120,
        poll_interval_seconds=10,
        expected_code_version="11",
        expected_assetbundle_version="22",
        expected_table_version="33",
        verify_build_extra_args="--server-url http://192.168.0.240:20001",
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
            build_id=901,
            build_type_id=build_type_id,
            number="12",
            state="queued",
            status="SUCCESS",
            status_text="queued",
            branch_name=branch_name,
            web_url="http://ci/build/901",
            revision=vcs_revision,
            client_version="0.1",
            build_extra_args="--server-url http://192.168.0.240:20001",
        )

    monkeypatch.setattr(test_client_res, "queue_build", fake_queue_build)
    monkeypatch.setattr(
        test_client_res,
        "wait_for_build_success",
        lambda config, *, build_id, timeout_seconds, poll_interval_seconds, log_prefix: test_client_res.BuildHandle(
            build_id=build_id,
            build_type_id="BDFrameworkCore_VerifyClientResAndroid",
            number="12",
            state="finished",
            status="SUCCESS",
            status_text="success",
            branch_name="v4/v-4.0.0",
            web_url="http://ci/build/901",
            revision="abc123",
            client_version="0.1",
            build_extra_args="--server-url http://192.168.0.240:20001",
        ),
    )

    assert test_client_res.command_queue_verify_build(args) == 0

    output = capsys.readouterr().out
    assert queued_calls == [
        {
            "build_type_id": "BDFrameworkCore_VerifyClientResAndroid",
            "branch_name": "v4/v-4.0.0",
            "vcs_revision": "abc123",
            "properties": [
                {"name": "build.client.version", "value": "0.1"},
                {"name": "test.clientres.expected.code.version", "value": "11"},
                {"name": "test.clientres.expected.assetbundle.version", "value": "22"},
                {"name": "test.clientres.expected.table.version", "value": "33"},
                {"name": "build.extra.args", "value": "--server-url http://192.168.0.240:20001"},
            ],
            "comment": "Triggered by TestClientRes | scope=verify-local-check | platform=Android | sourceBuildId=903",
        }
    ]
    assert "##teamcity[setParameter name='test.clientres.verify.build.id' value='901']" in output
    assert "##teamcity[setParameter name='test.clientres.verify.build.number' value='12']" in output
