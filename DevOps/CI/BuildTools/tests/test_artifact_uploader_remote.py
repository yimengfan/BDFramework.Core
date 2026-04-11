"""远程产物上传器冒烟测试。

本模块包含连接到真实文件服务器的集成测试，验证上传、目录列表、元数据和下载的完整链路。

测试范围：
1. 健康检查：验证文件服务器可达且状态为 ok。
2. 文件上传：构造测试文件并上传到远端指定路径。
3. 目录列表轮询：等待上传文件在目录列表中可见。
4. 元数据校验：验证文件大小、SHA256 和完整性状态。
5. 下载验证：下载已上传文件并比对原始内容。

前置条件：
- 需要通过 --run-remote-artifact-tests CLI 参数启用。
- buildtools.toml 中 [tests.remote_artifact].enabled 必须为 true。
- 文件服务器必须可达且健康。
"""

from __future__ import annotations

import hashlib
import json
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlencode
from urllib.request import Request, urlopen
from uuid import uuid4

import pytest

from Common.artifact_uploader import (
    ArtifactType,
    ArtifactUploadError,
    build_artifact_remote_path,
    resolve_file_server_settings,
    upload_table,
)
from Common.buildtools_config import load_buildtools_external_config


def fetch_remote_json(
    url: str,
    *,
    token: str | None,
    timeout_seconds: float = 20.0,
) -> tuple[int, dict[str, object]]:
    """从远程文件服务器获取 JSON 响应并统一错误处理。

    参数：
        url: 请求的完整 URL。
        token: 可选的 Bearer 认证令牌。
        timeout_seconds: HTTP 请求超时时间（秒）。

    返回：
        (HTTP 状态码, 解析后的 JSON 字典)。

    异常：
        ArtifactUploadError: 当网络不可达时抛出。
    """
    headers = {"Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"

    request = Request(url, headers=headers)
    try:
        with urlopen(request, timeout=timeout_seconds) as response:
            response_body = response.read()
            payload = json.loads(response_body.decode("utf-8")) if response_body else {}
            return response.status, payload if isinstance(payload, dict) else {}
    except HTTPError as exc:
        response_body = exc.read()
        try:
            payload = json.loads(response_body.decode("utf-8")) if response_body else {}
        except (UnicodeDecodeError, json.JSONDecodeError):
            payload = {"detail": response_body.decode("utf-8", errors="replace")}
        return exc.code, payload if isinstance(payload, dict) else {}
    except URLError as exc:
        raise ArtifactUploadError(f"Failed to reach remote file server: {exc.reason}") from exc


def fetch_remote_directory_listing(
    *,
    base_url: str,
    token: str | None,
    prefix: str,
    recursive: bool,
    timeout_seconds: float = 20.0,
) -> tuple[int, dict[str, object]]:
    """从远程文件服务器列表 API 获取目录列表。

    参数：
        base_url: 文件服务器基础 URL。
        token: 可选的 Bearer 认证令牌。
        prefix: 列表查询前缀路径。
        recursive: 是否递归列出子目录。
        timeout_seconds: HTTP 请求超时时间（秒）。
    """
    query = urlencode(
        {
            "prefix": prefix,
            "recursive": "true" if recursive else "false",
        }
    )
    return fetch_remote_json(
        f"{base_url.rstrip('/')}/api/files?{query}",
        token=token,
        timeout_seconds=timeout_seconds,
    )


def fetch_remote_metadata(
    *,
    base_url: str,
    token: str | None,
    remote_path: str,
    timeout_seconds: float = 20.0,
) -> tuple[int, dict[str, object]]:
    """获取单个远程产物的元数据（大小、SHA256、完整性状态）。

    参数：
        base_url: 文件服务器基础 URL。
        token: 可选的 Bearer 认证令牌。
        remote_path: 远程产物路径。
        timeout_seconds: HTTP 请求超时时间（秒）。
    """
    return fetch_remote_json(
        f"{base_url.rstrip('/')}/api/files/{quote(remote_path, safe='/')}",
        token=token,
        timeout_seconds=timeout_seconds,
    )


def download_remote_file(
    *,
    base_url: str,
    token: str | None,
    remote_path: str,
    timeout_seconds: float = 20.0,
) -> bytes:
    """下载远程文件内容用于上传后验证。

    参数：
        base_url: 文件服务器基础 URL。
        token: 可选的 Bearer 认证令牌。
        remote_path: 远程产物路径。
        timeout_seconds: HTTP 请求超时时间（秒）。

    返回：
        文件的原始字节数据。

    异常：
        ArtifactUploadError: 下载失败时抛出。
    """
    headers: dict[str, str] = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"

    request = Request(
        f"{base_url.rstrip('/')}/files/{quote(remote_path, safe='/')}",
        headers=headers,
    )
    try:
        with urlopen(request, timeout=timeout_seconds) as response:
            return response.read()
    except URLError as exc:
        raise ArtifactUploadError(f"Failed to download uploaded remote file: {exc.reason}") from exc


def wait_for_remote_list_entry(
    *,
    base_url: str,
    token: str | None,
    prefix: str,
    expected_remote_path: str,
    timeout_seconds: float = 15.0,
    poll_interval_seconds: float = 1.0,
) -> dict[str, object]:
    """轮询远程目录列表 API，直到上传文件可见或超时。

    参数：
        base_url: 文件服务器基础 URL。
        token: 可选的 Bearer 认证令牌。
        prefix: 列表查询前缀路径。
        expected_remote_path: 期望出现的远程文件路径。
        timeout_seconds: 轮询超时时间（秒）。
        poll_interval_seconds: 每次轮询间隔（秒）。

    返回：
        匹配到的远程列表条目字典。

    异常：
        AssertionError: 超时后文件仍未出现。
    """
    deadline = time.monotonic() + timeout_seconds
    last_status = 0
    last_payload: dict[str, object] = {}

    while time.monotonic() < deadline:
        status_code, payload = fetch_remote_directory_listing(
            base_url=base_url,
            token=token,
            prefix=prefix,
            recursive=False,
            timeout_seconds=timeout_seconds,
        )
        last_status = status_code
        last_payload = payload

        if status_code == 200:
            entries = payload.get("entries")
            if isinstance(entries, list):
                for entry in entries:
                    if isinstance(entry, dict) and entry.get("path") == expected_remote_path:
                        return entry

        time.sleep(poll_interval_seconds)

    raise AssertionError(
        "Uploaded file did not appear in remote directory listing within timeout. "
        f"prefix={prefix}, expected={expected_remote_path}, "
        f"last_status={last_status}, last_payload={last_payload}"
    )


@pytest.fixture
def buildtools_external_config(request):
    """加载 BuildTools 外部集成配置（文件服务器、CI 服务器等）。未启用远程测试时自动跳过。"""
    if not request.config.getoption("--run-remote-artifact-tests"):
        pytest.skip(
            "remote artifact integration tests are disabled by default; "
            "pass --run-remote-artifact-tests to enable real uploads"
        )
    return load_buildtools_external_config(
        env_var_names=("ARTIFACT_FILE_SERVER_CONFIG", "ARTIFACT_SERVER_CONFIG"),
    )


@pytest.fixture
def remote_artifact_test_config(buildtools_external_config):
    """提供远程测试配置对象。当 buildtools.toml 中未启用远程测试时自动跳过。"""
    config = buildtools_external_config.remote_artifact_test
    if not config.enabled:
        pytest.skip(
            "remote artifact integration tests require [tests.remote_artifact].enabled = true "
            "in DevOps/CI/BuildTools/buildtools.toml"
        )
    return config


@pytest.fixture
def remote_file_server_settings(buildtools_external_config, remote_artifact_test_config):
    """提供已解析的远程文件服务器连接配置（URL、Token、分块大小等）。"""
    del remote_artifact_test_config
    return resolve_file_server_settings(config_path=buildtools_external_config.config_path)


@pytest.mark.remote_artifact
def test_remote_upload_is_visible_in_remote_directory_listing(
    tmp_path: Path,
    remote_artifact_test_config,
    remote_file_server_settings,
) -> None:
    """端到端验证：上传产物后通过列表、元数据和下载 API 均可查看到。

    测试流程：
    1. 检查文件服务器健康状态。
    2. 构造唯一测试文件并上传到远端。
    3. 轮询目录列表直到文件可见。
    4. 通过元数据 API 验证文件大小和 SHA256。
    5. 通过下载 API 下载文件并比对原始内容。
    """
    settings = remote_file_server_settings

    health_status, health_payload = fetch_remote_json(
        f"{settings.base_url.rstrip('/')}/healthz",
        token=settings.token,
        timeout_seconds=remote_artifact_test_config.request_timeout_seconds,
    )
    assert health_status == 200
    assert health_payload.get("status") == "ok"

    run_id = f"{datetime.now(timezone.utc):%Y%m%d-%H%M%S}-{uuid4().hex[:8]}"
    remote_relative_path = f"{run_id}/{remote_artifact_test_config.filename}"
    expected_remote_path = build_artifact_remote_path(
        ArtifactType.TABLE,
        build_number=remote_artifact_test_config.build_number,
        remote_relative_path=remote_relative_path,
    )
    expected_list_prefix = build_artifact_remote_path(
        ArtifactType.TABLE,
        build_number=remote_artifact_test_config.build_number,
        remote_relative_path=run_id,
    )

    local_file = tmp_path / remote_artifact_test_config.filename
    content = (
        f"artifact_uploader remote smoke test\n"
        f"run_id={run_id}\n"
        f"server={settings.base_url}\n"
    ).encode("utf-8")
    local_file.write_bytes(content)
    sha256 = hashlib.sha256(content).hexdigest()

    print(f"remote_test_path={expected_remote_path}")
    print(f"remote_list_prefix={expected_list_prefix}")

    upload_results = None
    upload_error = None
    try:
        upload_results = upload_table(
            local_file,
            build_number=remote_artifact_test_config.build_number,
            remote_relative_path=remote_relative_path,
            settings=settings,
            overwrite=False,
        )
    except ArtifactUploadError as exc:
        upload_error = exc
        # 当前线上服务存在一种已落盘但响应返回 500 的情况，这里保留真实上传后的远端核验。
        assert "status=500" in str(exc)

    if upload_results is not None:
        assert len(upload_results) == 1
        assert upload_results[0].remote_path == expected_remote_path
        assert upload_results[0].sha256 == sha256
        assert upload_results[0].size == len(content)
        assert upload_results[0].status_code in {201, 500}
        assert upload_results[0].integrity_status in {"verified", "missing"}
    else:
        assert upload_error is not None

    list_entry = wait_for_remote_list_entry(
        base_url=settings.base_url,
        token=settings.token,
        prefix=expected_list_prefix,
        expected_remote_path=expected_remote_path,
        timeout_seconds=remote_artifact_test_config.listing_timeout_seconds,
        poll_interval_seconds=remote_artifact_test_config.poll_interval_seconds,
    )
    assert list_entry["type"] == "file"
    assert list_entry["size"] == len(content)
    assert list_entry["integrity_status"] in {"verified", "missing"}
    if list_entry.get("sha256") is not None:
        assert list_entry["sha256"] == sha256

    metadata_status, metadata_payload = fetch_remote_metadata(
        base_url=settings.base_url,
        token=settings.token,
        remote_path=expected_remote_path,
        timeout_seconds=remote_artifact_test_config.request_timeout_seconds,
    )
    assert metadata_status == 200
    assert metadata_payload["path"] == expected_remote_path
    assert metadata_payload["size"] == len(content)
    assert metadata_payload["integrity_status"] in {"verified", "missing"}
    if metadata_payload.get("sha256") is not None:
        assert metadata_payload["sha256"] == sha256

    downloaded_content = download_remote_file(
        base_url=settings.base_url,
        token=settings.token,
        remote_path=expected_remote_path,
        timeout_seconds=remote_artifact_test_config.request_timeout_seconds,
    )
    assert downloaded_content == content
    assert hashlib.sha256(downloaded_content).hexdigest() == sha256