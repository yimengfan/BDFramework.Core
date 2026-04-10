"""BuildTools 公共上传模块测试。

这些测试重点覆盖两类事情：
1. 远端目录规则和 BuildTools 全局配置解析是否符合预期。
2. 构造一组接近真实构建产物的文件后，上传结果是否能正确返回提交元数据。
"""

from __future__ import annotations

import hashlib
import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, unquote, urlparse

import pytest

from Common.artifact_uploader import (
	ArtifactType,
	ArtifactUploadError,
	build_artifact_remote_path,
	build_artifact_remote_root,
	load_minimal_toml,
	normalize_relative_remote_path,
	resolve_file_server_settings,
	upload_client_package,
	upload_code,
	upload_single_file,
	upload_table,
)


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]


class RecordingHTTPServer(ThreadingHTTPServer):
	"""HTTP server that records successful upload requests for assertions."""
	def __init__(self, server_address: tuple[str, int]) -> None:
		"""HTTP server that records successful upload requests for assertions."""
		super().__init__(server_address, RecordingUploadHandler)
		self.requests: list[dict[str, str]] = []


class RecordingUploadHandler(BaseHTTPRequestHandler):
	"""Request handler that stores successful upload request metadata."""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""Store successful upload request metadata and respond with verified file info."""
		content_length = int(self.headers.get("Content-Length", "0"))
		body = self.rfile.read(content_length)
		parsed = urlparse(self.path)
		remote_path = unquote(parsed.path.split("/api/files/", 1)[1])
		query = parse_qs(parsed.query)

		self.server.requests.append(  # type: ignore[attr-defined]
			{
				"remote_path": remote_path,
				"authorization": self.headers.get("Authorization", ""),
				"sha256": self.headers.get("X-Checksum-Sha256", ""),
				"overwrite": query.get("overwrite", [""])[0],
				"size": str(len(body)),
			}
		)

		payload = json.dumps(
			{
				"path": remote_path,
				"size": len(body),
				"sha256": self.headers.get("X-Checksum-Sha256", ""),
				"integrity_status": "verified",
			}
		).encode("utf-8")
		self.send_response(201)
		self.send_header("Content-Type", "application/json")
		self.send_header("Content-Length", str(len(payload)))
		self.end_headers()
		self.wfile.write(payload)

	def log_message(self, format: str, *args: object) -> None:
		return


class ErroringHTTPServer(ThreadingHTTPServer):
	"""HTTP server that always returns a configured upload error payload."""
	def __init__(self, server_address: tuple[str, int], status_code: int, payload: dict[str, str]) -> None:
		"""HTTP server that always returns a configured upload error payload."""
		super().__init__(server_address, ErroringUploadHandler)
		self.status_code = status_code
		self.payload = payload


class ErroringUploadHandler(BaseHTTPRequestHandler):
	"""Request handler that simulates deterministic upload failures."""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""Consume the upload body and return the configured error response."""
		content_length = int(self.headers.get("Content-Length", "0"))
		self.rfile.read(content_length)
		payload = json.dumps(self.server.payload).encode("utf-8")  # type: ignore[attr-defined]
		self.send_response(self.server.status_code)  # type: ignore[attr-defined]
		self.send_header("Content-Type", "application/json")
		self.send_header("Content-Length", str(len(payload)))
		self.end_headers()
		self.wfile.write(payload)

	def log_message(self, format: str, *args: object) -> None:
		return


class RecoveringErrorHTTPServer(ThreadingHTTPServer):
	"""HTTP server that returns upload errors while exposing follow-up verification endpoints."""
	def __init__(
		self,
		server_address: tuple[str, int],
		*,
		metadata_available: bool,
		metadata_returns_sha256: bool,
		download_available: bool,
		integrity_status: str,
	) -> None:
		"""HTTP server that returns upload errors while exposing follow-up verification endpoints."""
		super().__init__(server_address, RecoveringErrorUploadHandler)
		self.metadata_available = metadata_available
		self.metadata_returns_sha256 = metadata_returns_sha256
		self.download_available = download_available
		self.integrity_status = integrity_status
		self.uploaded_files: dict[str, dict[str, str | bytes]] = {}


class RecoveringErrorUploadHandler(BaseHTTPRequestHandler):
	"""Request handler that simulates partial upload success with recovery probes."""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""Record uploaded bytes but intentionally answer with a 500 response."""
		content_length = int(self.headers.get("Content-Length", "0"))
		body = self.rfile.read(content_length)
		parsed = urlparse(self.path)
		remote_path = unquote(parsed.path.split("/api/files/", 1)[1])
		self.server.uploaded_files[remote_path] = {  # type: ignore[attr-defined]
			"body": body,
			"sha256": self.headers.get("X-Checksum-Sha256", ""),
		}

		payload = json.dumps({"detail": "Internal Server Error"}).encode("utf-8")
		self.send_response(500)
		self.send_header("Content-Type", "application/json")
		self.send_header("Content-Length", str(len(payload)))
		self.end_headers()
		self.wfile.write(payload)

	def do_GET(self) -> None:  # noqa: N802 - stdlib handler signature
		"""Serve metadata or file downloads so recovery checks can verify partial success."""
		parsed = urlparse(self.path)
		if parsed.path.startswith("/api/files/"):
			remote_path = unquote(parsed.path.split("/api/files/", 1)[1])
			record = self.server.uploaded_files.get(remote_path)  # type: ignore[attr-defined]
			if not self.server.metadata_available or record is None:  # type: ignore[attr-defined]
				payload = json.dumps({"detail": "Not Found"}).encode("utf-8")
				self.send_response(404)
				self.send_header("Content-Type", "application/json")
				self.send_header("Content-Length", str(len(payload)))
				self.end_headers()
				self.wfile.write(payload)
				return

			payload_dict = {
				"path": remote_path,
				"size": len(record["body"]),
				"integrity_status": self.server.integrity_status,  # type: ignore[attr-defined]
			}
			if self.server.metadata_returns_sha256:  # type: ignore[attr-defined]
				payload_dict["sha256"] = record["sha256"]
			payload = json.dumps(payload_dict).encode("utf-8")
			self.send_response(200)
			self.send_header("Content-Type", "application/json")
			self.send_header("Content-Length", str(len(payload)))
			self.end_headers()
			self.wfile.write(payload)
			return

		if parsed.path.startswith("/files/"):
			remote_path = unquote(parsed.path.split("/files/", 1)[1])
			record = self.server.uploaded_files.get(remote_path)  # type: ignore[attr-defined]
			if not self.server.download_available or record is None:  # type: ignore[attr-defined]
				self.send_response(404)
				self.send_header("Content-Length", "0")
				self.end_headers()
				return

			body = record["body"]
			self.send_response(200)
			self.send_header("Content-Type", "application/octet-stream")
			self.send_header("Content-Length", str(len(body)))
			self.end_headers()
			self.wfile.write(body)
			return

		self.send_response(404)
		self.send_header("Content-Length", "0")
		self.end_headers()

	def log_message(self, format: str, *args: object) -> None:
		return


def make_upload_server() -> tuple[RecordingHTTPServer, str, threading.Thread]:
	"""Start a recording upload server and return the server, base URL, and thread."""
	server = RecordingHTTPServer(("127.0.0.1", 0))
	thread = threading.Thread(target=server.serve_forever, daemon=True)
	thread.start()
	base_url = f"http://127.0.0.1:{server.server_port}"
	return server, base_url, thread


def make_error_upload_server(
	*,
	status_code: int,
	payload: dict[str, str],
) -> tuple[ErroringHTTPServer, str, threading.Thread]:
	"""Start an upload server that always responds with the configured error payload."""
	server = ErroringHTTPServer(("127.0.0.1", 0), status_code, payload)
	thread = threading.Thread(target=server.serve_forever, daemon=True)
	thread.start()
	base_url = f"http://127.0.0.1:{server.server_port}"
	return server, base_url, thread


def make_recovering_error_upload_server(
	*,
	metadata_available: bool,
	metadata_returns_sha256: bool,
	download_available: bool,
	integrity_status: str,
) -> tuple[RecoveringErrorHTTPServer, str, threading.Thread]:
	"""Start an upload server that supports post-failure remote verification checks."""
	server = RecoveringErrorHTTPServer(
		("127.0.0.1", 0),
		metadata_available=metadata_available,
		metadata_returns_sha256=metadata_returns_sha256,
		download_available=download_available,
		integrity_status=integrity_status,
	)
	thread = threading.Thread(target=server.serve_forever, daemon=True)
	thread.start()
	base_url = f"http://127.0.0.1:{server.server_port}"
	return server, base_url, thread


def create_mock_client_build_output(root_dir: Path) -> Path:
	"""构造一组接近真实母包输出的测试文件。"""
	package_dir = root_dir / "BuildClientPackage_windows"
	(package_dir / "Game_Data").mkdir(parents=True)
	(package_dir / "BuildReport").mkdir(parents=True)
	(package_dir / "Launcher.exe").write_bytes(b"launcher payload")
	(package_dir / "Game_Data" / "globalgamemanagers").write_bytes(b"game data payload")
	(package_dir / "BuildReport" / "summary.json").write_text(
		'{"result": "success", "platform": "windows"}',
		encoding="utf-8",
	)
	return package_dir


def test_build_artifact_remote_paths_match_expected_layout() -> None:
	"""Verify artifact remote paths follow the expected client package, code, assetbundle, and table layout."""
	assert (
		build_artifact_remote_root(
			ArtifactType.CLIENT_PACKAGE,
			platform="android",
			build_number="239",
		)
		== "ClientPackage_android/239"
	)
	assert (
		build_artifact_remote_root(
			ArtifactType.CODE,
			platform="windows",
			build_number="238",
		)
		== "ClientRes_Code_windows/238"
	)
	assert (
		build_artifact_remote_path(
			ArtifactType.ASSET_BUNDLE,
			platform="ios",
			build_number="240",
			remote_relative_path="catalogs/main.bytes",
		)
		== "ClientRes_Assetbundle_ios/240/catalogs/main.bytes"
	)
	assert (
		build_artifact_remote_path(
			ArtifactType.TABLE,
			build_number="777",
			remote_relative_path="global/config.bytes",
		)
		== "ClientRes_Table/777/global/config.bytes"
	)


def test_resolve_file_server_settings_reads_client_ip_and_chunk_values(tmp_path: Path) -> None:
	"""Verify file server settings resolve client IP, port, token, and chunk sizes from TOML."""
	config_path = tmp_path / "buildtools.toml"
	config_path.write_text(
		"""
[artifact_file_server]
ip = "192.168.10.24"
port = 28080
scheme = "http"
upload_chunk_size_kb = 256
hash_chunk_size_kb = 128
tokens = ["token-a", "token-b"]
""".strip(),
		encoding="utf-8",
	)

	settings = resolve_file_server_settings(config_path=config_path)

	assert settings.base_url == "http://192.168.10.24:28080"
	assert settings.token == "token-a"
	assert settings.upload_chunk_size_bytes == 256 * 1024
	assert settings.hash_chunk_size_bytes == 128 * 1024


def test_load_minimal_toml_supports_buildtools_config_shape() -> None:
	"""Verify the minimal TOML loader supports file-server and remote-test BuildTools config tables."""
	parsed = load_minimal_toml(
		"""
[artifact_file_server]
ip = "192.168.10.24"
port = 28080
scheme = "http"
tokens = ["token-a", "token-b"]
upload_chunk_size_kb = 256
hash_chunk_size_kb = 128

[ci_server]
provider = "teamcity"
base_url = "https://ci.example.com"

[tests.remote_artifact]
enabled = true
build_number = "remote-smoke-tests"
filename = "artifact_uploader_remote_test.txt"
""".strip()
	)

	assert parsed == {
		"artifact_file_server": {
			"ip": "192.168.10.24",
			"port": 28080,
			"scheme": "http",
			"tokens": ["token-a", "token-b"],
			"upload_chunk_size_kb": 256,
			"hash_chunk_size_kb": 128,
		},
		"ci_server": {
			"provider": "teamcity",
			"base_url": "https://ci.example.com",
		},
		"tests": {
			"remote_artifact": {
				"enabled": True,
				"build_number": "remote-smoke-tests",
				"filename": "artifact_uploader_remote_test.txt",
			},
		}
	}


def test_resolve_file_server_settings_prefers_explicit_inputs_over_buildtools_config(tmp_path: Path) -> None:
	"""Verify explicit server URL and token inputs override BuildTools config values."""
	config_path = tmp_path / "buildtools.toml"
	config_path.write_text(
		"""
[artifact_file_server]
base_url = "http://192.168.0.240:20001"
token = "config-token"
upload_chunk_size_kb = 512
hash_chunk_size_kb = 256
""".strip(),
		encoding="utf-8",
	)

	settings = resolve_file_server_settings(
		config_path=config_path,
		server_url="https://files.example.com:8443/artifacts",
		token="explicit-token",
	)

	assert settings.base_url == "https://files.example.com:8443/artifacts"
	assert settings.token == "explicit-token"
	assert settings.upload_chunk_size_bytes == 512 * 1024
	assert settings.hash_chunk_size_bytes == 256 * 1024


def test_resolve_file_server_settings_prefers_env_over_buildtools_config(
	tmp_path: Path,
	monkeypatch: pytest.MonkeyPatch,
) -> None:
	"""Verify environment overrides take precedence over BuildTools config defaults."""
	config_path = tmp_path / "buildtools.toml"
	config_path.write_text(
		"""
[artifact_file_server]
ip = "192.168.10.24"
port = 28080
scheme = "http"
token = "config-token"
upload_chunk_size_kb = 512
hash_chunk_size_kb = 256
""".strip(),
		encoding="utf-8",
	)
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_IP", "10.0.0.8")
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_PORT", "29090")
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_SCHEME", "https")
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_TOKEN", "env-token")
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_CHUNK_SIZE_KB", "64")
	monkeypatch.setenv("ARTIFACT_FILE_SERVER_HASH_CHUNK_SIZE_KB", "32")

	settings = resolve_file_server_settings(config_path=config_path)

	assert settings.base_url == "https://10.0.0.8:29090"
	assert settings.token == "env-token"
	assert settings.upload_chunk_size_bytes == 64 * 1024
	assert settings.hash_chunk_size_bytes == 32 * 1024


def test_buildtools_global_config_exists_for_shared_defaults() -> None:
	"""Verify the shared BuildTools config file exists for default artifact upload settings."""
	config_path = BUILD_TOOLS_ROOT / "buildtools.toml"
	assert config_path.exists()
	assert resolve_file_server_settings().config_path == config_path


def test_build_artifact_remote_root_rejects_invalid_segments() -> None:
	"""Verify remote root construction rejects invalid platform and build number segments."""
	with pytest.raises(ArtifactUploadError, match="platform cannot contain path separators"):
		build_artifact_remote_root(
			ArtifactType.CLIENT_PACKAGE,
			platform="android/release",
			build_number="239",
		)

	with pytest.raises(ArtifactUploadError, match="build_number cannot be '.' or '..'"):
		build_artifact_remote_root(
			ArtifactType.TABLE,
			build_number="..",
		)


def test_normalize_relative_remote_path_rejects_invalid_paths() -> None:
	"""Verify remote relative path normalization rejects absolute paths and parent segments."""
	with pytest.raises(ArtifactUploadError, match="absolute path"):
		normalize_relative_remote_path("/absolute/output.zip")

	with pytest.raises(ArtifactUploadError, match="parent segments"):
		normalize_relative_remote_path("../outside/output.zip")


def test_upload_client_package_directory_preserves_relative_layout(tmp_path: Path) -> None:
	"""Verify client package directory uploads preserve the original relative file layout on the remote server."""
	server, base_url, thread = make_upload_server()
	try:
		package_dir = create_mock_client_build_output(tmp_path)

		settings = resolve_file_server_settings(server_url=base_url, token="secret-token")
		results = upload_client_package(
			package_dir,
			platform="windows",
			build_number="238",
			settings=settings,
			overwrite=False,
		)

		assert [result.remote_path for result in results] == [
			"ClientPackage_windows/238/BuildReport/summary.json",
			"ClientPackage_windows/238/Game_Data/globalgamemanagers",
			"ClientPackage_windows/238/Launcher.exe",
		]
		assert [request["remote_path"] for request in server.requests] == [
			"ClientPackage_windows/238/BuildReport/summary.json",
			"ClientPackage_windows/238/Game_Data/globalgamemanagers",
			"ClientPackage_windows/238/Launcher.exe",
		]
		assert {request["authorization"] for request in server.requests} == {"Bearer secret-token"}
		assert {request["overwrite"] for request in server.requests} == {"false"}
		assert server.requests[0]["sha256"] == hashlib.sha256(
			b'{"result": "success", "platform": "windows"}'
		).hexdigest()
		assert server.requests[1]["sha256"] == hashlib.sha256(b"game data payload").hexdigest()
		assert server.requests[2]["sha256"] == hashlib.sha256(b"launcher payload").hexdigest()
		assert [result.status_code for result in results] == [201, 201, 201]
		assert {result.integrity_status for result in results} == {"verified"}
		assert [result.size for result in results] == [
			len('{"result": "success", "platform": "windows"}'.encode("utf-8")),
			len(b"game data payload"),
			len(b"launcher payload"),
		]
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_code_single_build_file_returns_submit_result(tmp_path: Path) -> None:
	"""Verify single code-file uploads return the submitted remote path and integrity metadata."""
	server, base_url, thread = make_upload_server()
	try:
		code_file = tmp_path / "hotfix.dll"
		code_file.write_bytes(b"compiled hotfix")

		results = upload_code(
			code_file,
			platform="android",
			build_number="239",
			remote_relative_path="hotfix/Assembly-CSharp.dll",
			settings=resolve_file_server_settings(server_url=base_url),
		)

		assert len(results) == 1
		assert results[0].remote_path == "ClientRes_Code_android/239/hotfix/Assembly-CSharp.dll"
		assert results[0].sha256 == hashlib.sha256(b"compiled hotfix").hexdigest()
		assert results[0].status_code == 201
		assert results[0].integrity_status == "verified"
		assert [request["remote_path"] for request in server.requests] == [
			"ClientRes_Code_android/239/hotfix/Assembly-CSharp.dll"
		]
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_single_file_surfaces_server_error_detail(tmp_path: Path) -> None:
	"""Verify upload errors surface the server status code and detail payload."""
	server, base_url, thread = make_error_upload_server(
		status_code=409,
		payload={"detail": "Artifact already exists."},
	)
	try:
		build_file = tmp_path / "Launcher.apk"
		build_file.write_bytes(b"android package")

		with pytest.raises(ArtifactUploadError) as exc_info:
			upload_single_file(
				build_file,
				remote_path="ClientPackage_android/239/Launcher.apk",
				settings=resolve_file_server_settings(server_url=base_url),
			)

		assert "status=409" in str(exc_info.value)
		assert "Artifact already exists." in str(exc_info.value)
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_single_file_recovers_when_server_returns_500_but_file_is_remotely_available(
	tmp_path: Path,
) -> None:
	"""Verify upload recovery succeeds when the file is remotely available after a 500 response."""
	server, base_url, thread = make_recovering_error_upload_server(
		metadata_available=True,
		metadata_returns_sha256=False,
		download_available=True,
		integrity_status="missing",
	)
	try:
		build_file = tmp_path / "Launcher.apk"
		build_file.write_bytes(b"android package")

		result = upload_single_file(
			build_file,
			remote_path="ClientPackage_android/239/Launcher.apk",
			settings=resolve_file_server_settings(server_url=base_url),
		)

		assert result.remote_path == "ClientPackage_android/239/Launcher.apk"
		assert result.sha256 == hashlib.sha256(b"android package").hexdigest()
		assert result.size == len(b"android package")
		assert result.status_code == 500
		assert result.integrity_status == "missing"
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_single_file_raises_when_server_returns_500_and_remote_verification_fails(
	tmp_path: Path,
) -> None:
	"""Verify upload recovery raises when a 500 response cannot be confirmed through remote checks."""
	server, base_url, thread = make_recovering_error_upload_server(
		metadata_available=False,
		metadata_returns_sha256=False,
		download_available=False,
		integrity_status="missing",
	)
	try:
		build_file = tmp_path / "Launcher.apk"
		build_file.write_bytes(b"android package")

		with pytest.raises(ArtifactUploadError) as exc_info:
			upload_single_file(
				build_file,
				remote_path="ClientPackage_android/239/Launcher.apk",
				settings=resolve_file_server_settings(server_url=base_url),
				timeout_seconds=2,
			)

		assert "status=500" in str(exc_info.value)
		assert "recovery_check=metadata status=404" in str(exc_info.value)
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_table_single_file_supports_nested_remote_name(tmp_path: Path) -> None:
	"""Verify table uploads preserve nested remote relative paths."""
	server, base_url, thread = make_upload_server()
	try:
		table_file = tmp_path / "all.bytes"
		table_file.write_bytes(b"table payload")

		results = upload_table(
			table_file,
			build_number="501",
			remote_relative_path="mobile/all.bytes",
			settings=resolve_file_server_settings(server_url=base_url),
		)

		assert [result.remote_path for result in results] == ["ClientRes_Table/501/mobile/all.bytes"]
		assert [request["remote_path"] for request in server.requests] == ["ClientRes_Table/501/mobile/all.bytes"]
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_client_package_reports_progress_callbacks(tmp_path: Path) -> None:
	"""Verify client package uploads report progress and completion callbacks for every file."""
	server, base_url, thread = make_upload_server()
	progress_events: list[tuple[int, int, str, str]] = []
	completed_events: list[tuple[int, int, str]] = []
	try:
		package_dir = create_mock_client_build_output(tmp_path)

		results = upload_client_package(
			package_dir,
			platform="windows",
			build_number="240",
			settings=resolve_file_server_settings(server_url=base_url),
			on_uploading=lambda index, total, local_path, remote_path: progress_events.append(
				(index, total, local_path.name, remote_path)
			),
			on_uploaded=lambda index, total, result: completed_events.append(
				(index, total, result.remote_path)
			),
		)

		assert len(results) == 3
		assert progress_events == [
			(1, 3, "summary.json", "ClientPackage_windows/240/BuildReport/summary.json"),
			(2, 3, "globalgamemanagers", "ClientPackage_windows/240/Game_Data/globalgamemanagers"),
			(3, 3, "Launcher.exe", "ClientPackage_windows/240/Launcher.exe"),
		]
		assert completed_events == [
			(1, 3, "ClientPackage_windows/240/BuildReport/summary.json"),
			(2, 3, "ClientPackage_windows/240/Game_Data/globalgamemanagers"),
			(3, 3, "ClientPackage_windows/240/Launcher.exe"),
		]
	finally:
		server.shutdown()
		thread.join(timeout=5)
		server.server_close()


def test_upload_client_package_rejects_empty_directory(tmp_path: Path) -> None:
	"""Verify client package uploads reject empty source directories."""
	empty_output_dir = tmp_path / "BuildClientPackage_android"
	empty_output_dir.mkdir()

	with pytest.raises(ArtifactUploadError, match="No files found to upload"):
		upload_client_package(
			empty_output_dir,
			platform="android",
			build_number="239",
			settings=resolve_file_server_settings(server_url="http://127.0.0.1:20001"),
		)