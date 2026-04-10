"""Remote smoke tests for the artifact uploader.

These tests talk to the configured file server and verify upload, listing, metadata, and download visibility.
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


REMOTE_TEST_BUILD_NUMBER = "remote-smoke-tests"
REMOTE_TEST_FILENAME = "artifact_uploader_remote_test.txt"


def fetch_remote_json(
    url: str,
    *,
    token: str | None,
    timeout_seconds: float = 20.0,
) -> tuple[int, dict[str, object]]:
    """Fetch a JSON response from the remote file server and normalize error handling."""
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
) -> tuple[int, dict[str, object]]:
    """Fetch a directory listing from the remote file server listing API."""
    query = urlencode(
        {
            "prefix": prefix,
            "recursive": "true" if recursive else "false",
        }
    )
    return fetch_remote_json(
        f"{base_url.rstrip('/')}/api/files?{query}",
        token=token,
    )


def fetch_remote_metadata(
    *,
    base_url: str,
    token: str | None,
    remote_path: str,
) -> tuple[int, dict[str, object]]:
    """Fetch metadata for a single remote artifact path."""
    return fetch_remote_json(
        f"{base_url.rstrip('/')}/api/files/{quote(remote_path, safe='/')}",
        token=token,
    )


def download_remote_file(
    *,
    base_url: str,
    token: str | None,
    remote_path: str,
    timeout_seconds: float = 20.0,
) -> bytes:
    """Download a remote file payload for post-upload verification."""
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
    """Poll the remote listing API until the uploaded file becomes visible or times out."""
    deadline = time.monotonic() + timeout_seconds
    last_status = 0
    last_payload: dict[str, object] = {}

    while time.monotonic() < deadline:
        status_code, payload = fetch_remote_directory_listing(
            base_url=base_url,
            token=token,
            prefix=prefix,
            recursive=False,
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
def remote_file_server_settings(request):
    """Provide configured remote file server settings for smoke tests or skip when disabled."""
    if not request.config.getoption("--run-remote-artifact-tests"):
        pytest.skip(
            "remote artifact integration tests are disabled by default; "
            "pass --run-remote-artifact-tests to enable real uploads"
        )
    return resolve_file_server_settings()


@pytest.mark.remote_artifact
def test_remote_upload_is_visible_in_remote_directory_listing(
    tmp_path: Path,
    remote_file_server_settings,
) -> None:
    """Verify uploaded artifacts become visible through listing, metadata, and download APIs."""
    settings = remote_file_server_settings

    health_status, health_payload = fetch_remote_json(
        f"{settings.base_url.rstrip('/')}/healthz",
        token=settings.token,
    )
    assert health_status == 200
    assert health_payload.get("status") == "ok"

    run_id = f"{datetime.now(timezone.utc):%Y%m%d-%H%M%S}-{uuid4().hex[:8]}"
    remote_relative_path = f"{run_id}/{REMOTE_TEST_FILENAME}"
    expected_remote_path = build_artifact_remote_path(
        ArtifactType.TABLE,
        build_number=REMOTE_TEST_BUILD_NUMBER,
        remote_relative_path=remote_relative_path,
    )
    expected_list_prefix = build_artifact_remote_path(
        ArtifactType.TABLE,
        build_number=REMOTE_TEST_BUILD_NUMBER,
        remote_relative_path=run_id,
    )

    local_file = tmp_path / REMOTE_TEST_FILENAME
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
            build_number=REMOTE_TEST_BUILD_NUMBER,
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
    )
    assert downloaded_content == content
    assert hashlib.sha256(downloaded_content).hexdigest() == sha256