"""BuildTools 公共上传模块测试。

测试重点：
1. 远端目录规则验证：确认各类产物（母包、代码、AssetBundle、表格）的远端路径布局符合约定。
2. BuildTools 全局配置解析：验证 TOML 配置中的文件服务器地址、Token、分块大小能被正确读取。
3. 环境变量优先级：验证环境变量可以覆盖配置文件中的默认值。
4. 模拟上传验证：使用本地 HTTP 服务器模拟上传流程，验证上传结果正确返回提交元数据。
5. 错误恢复验证：模拟上传失败后的远端核验恢复流程。
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
	"""记录成功上传请求的 HTTP 测试服务器。用于断言上传请求的元数据。"""
	def __init__(self, server_address: tuple[str, int]) -> None:
		"""初始化服务器并绑定 RecordingUploadHandler。"""
		super().__init__(server_address, RecordingUploadHandler)
		self.requests: list[dict[str, str]] = []


class RecordingUploadHandler(BaseHTTPRequestHandler):
	"""记录成功上传请求元数据的请求处理器。存储路径、认证头、SHA256 和覆盖标志。"""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""处理 PUT 请求：记录上传元数据并返回 201 和已验证文件信息。"""
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
	"""始终返回指定错误状态码和错误负载的 HTTP 测试服务器。用于测试上传失败场景。"""
	def __init__(self, server_address: tuple[str, int], status_code: int, payload: dict[str, str]) -> None:
		"""初始化服务器并绑定 ErroringUploadHandler。

		参数：
			server_address: 服务器监听地址和端口。
			status_code: 始终返回的 HTTP 状态码。
			payload: 始终返回的错误响应体字典。
		"""
		super().__init__(server_address, ErroringUploadHandler)
		self.status_code = status_code
		self.payload = payload


class ErroringUploadHandler(BaseHTTPRequestHandler):
	"""模拟确定性上传失败的请求处理器。始终返回配置的错误状态码和负载。"""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""消费上传请求体并返回配置的错误响应。"""
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
	"""模拟上传失败但支持后续恢复验证的 HTTP 测试服务器。

	上传接口返回 500 错误，但同时暴露元数据和下载接口，
	用于测试上传失败后的远端核验恢复流程。

	参数：
		metadata_available: 是否启用元数据查询接口。
		metadata_returns_sha256: 元数据响应中是否包含 SHA256 字段。
		download_available: 是否启用文件下载接口。
		integrity_status: 返回的完整性状态值。
	"""
	def __init__(
		self,
		server_address: tuple[str, int],
		*,
		metadata_available: bool,
		metadata_returns_sha256: bool,
		download_available: bool,
		integrity_status: str,
	) -> None:
		"""初始化服务器并绑定 RecoveringErrorUploadHandler。"""
		super().__init__(server_address, RecoveringErrorUploadHandler)
		self.metadata_available = metadata_available
		self.metadata_returns_sha256 = metadata_returns_sha256
		self.download_available = download_available
		self.integrity_status = integrity_status
		self.uploaded_files: dict[str, dict[str, str | bytes]] = {}


class RecoveringErrorUploadHandler(BaseHTTPRequestHandler):
	"""模拟部分上传成功并支持恢复探测的请求处理器。PUT 记录文件但返回 500，GET 支持元数据和下载查询。"""
	protocol_version = "HTTP/1.1"

	def do_PUT(self) -> None:  # noqa: N802 - stdlib handler signature
		"""记录上传字节数据但故意返回 500 响应，模拟服务端内部错误。"""
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
		"""提供元数据或文件下载接口，使恢复检查可以验证部分上传成功的情况。"""
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
	"""启动一个记录上传请求的本地 HTTP 服务器，返回服务器实例、基础 URL 和线程。"""
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
	"""启动一个始终返回错误响应的上传服务器。

	参数：
		status_code: 始终返回的 HTTP 状态码。
		payload: 始终返回的错误响应体。
	"""
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
	"""启动一个支持上传失败后远端验证检查的 HTTP 服务器。

	参数：
		metadata_available: 是否启用元数据查询接口。
		metadata_returns_sha256: 元数据是否包含 SHA256 字段。
		download_available: 是否启用文件下载接口。
		integrity_status: 返回的完整性状态值。
	"""
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
	"""验证各类产物的远端路径遵循约定的布局格式：母包、代码、AssetBundle、表格。"""
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
	"""验证文件服务器配置能正确解析 IP、端口、Token 和分块大小。"""
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
	"""验证最小化 TOML 解析器支持 BuildTools 配置中的文件服务器、CI 服务器和远程测试配置表。"""
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
	"""验证显式传入的服务器 URL 和 Token 会覆盖 BuildTools 配置文件中的值。"""
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
	"""验证环境变量优先级高于 BuildTools 配置文件中的默认值。"""
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
	"""验证 BuildTools 全局配置文件存在，用于默认的产物上传配置。"""
	config_path = BUILD_TOOLS_ROOT / "buildtools.toml"
	assert config_path.exists()
	assert resolve_file_server_settings().config_path == config_path


def test_build_artifact_remote_root_rejects_invalid_segments() -> None:
	"""验证远端根路径构造会拒绝非法的平台名和构建号（如包含路径分隔符或特殊目录名）。"""
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