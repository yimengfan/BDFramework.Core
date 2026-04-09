from __future__ import annotations

"""BuildTools 公共文件服务器上传模块。

设计原则：
1. 上传模块使用 BuildTools 自己的全局配置，不复用其他目录下的配置文件。
2. 统一封装四类制品的远端目录规则，避免调用方各自手写路径。
3. 同时支持“单文件上传”和“目录递归上传”，便于 CI 在产物落地后直接复用。
"""

import argparse
import hashlib
import http.client
import json
import mimetypes
import os
from dataclasses import dataclass
from enum import Enum
from pathlib import Path, PurePosixPath
from typing import Any, Callable, Iterator
from urllib.parse import quote, urlencode, urlparse

try:
	import tomllib
except ModuleNotFoundError:  # pragma: no cover - Python < 3.11 fallback
	try:
		import tomli as tomllib
	except ModuleNotFoundError:  # pragma: no cover - TeamCity old Python fallback
		tomllib = None


DEFAULT_TIMEOUT_SECONDS = 600
DEFAULT_UPLOAD_CHUNK_SIZE_BYTES = 1024 * 1024
DEFAULT_HASH_CHUNK_SIZE_BYTES = 1024 * 1024
SKIPPED_LOCAL_FILENAMES = {".DS_Store"}
BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG_PATH = BUILD_TOOLS_ROOT / "buildtools.toml"
DEFAULT_CONFIG_EXAMPLE_PATH = BUILD_TOOLS_ROOT / "buildtools.toml.example"


class ArtifactUploadError(RuntimeError):
	"""文件服务器上传相关错误。"""


class ArtifactType(str, Enum):
	"""支持的制品分类。"""

	CLIENT_PACKAGE = "client-package"
	CODE = "code"
	ASSET_BUNDLE = "asset-bundle"
	TABLE = "table"


@dataclass(frozen=True)
class FileServerClientSettings:
	"""文件服务器客户端配置。

	注意：
	1. base_url 是 BuildTools 真正发起 HTTP 请求时使用的访问地址。
	2. 它不一定等于服务端监听地址；服务端可能监听 0.0.0.0，但客户端需要访问某个内网 IP。
	"""

	base_url: str
	token: str | None
	config_path: Path | None
	upload_chunk_size_bytes: int = DEFAULT_UPLOAD_CHUNK_SIZE_BYTES
	hash_chunk_size_bytes: int = DEFAULT_HASH_CHUNK_SIZE_BYTES


@dataclass(frozen=True)
class UploadedArtifact:
	"""单个上传文件的结果。"""

	local_path: Path
	remote_path: str
	size: int
	sha256: str
	status_code: int
	integrity_status: str | None = None


UploadStartCallback = Callable[[int, int, Path, str], None]
UploadCompleteCallback = Callable[[int, int, UploadedArtifact], None]


def parse_token_values(value: Any) -> tuple[str, ...]:
	"""把 token 字段标准化为去重后的字符串元组。"""
	if value is None:
		return ()

	if isinstance(value, str):
		items = [part.strip() for part in value.replace(";", ",").split(",") if part.strip()]
	elif isinstance(value, (list, tuple)):
		items = [str(part).strip() for part in value if str(part).strip()]
	else:
		raise ArtifactUploadError(f"Unsupported token value: {value!r}")

	unique_items: list[str] = []
	for item in items:
		if item not in unique_items:
			unique_items.append(item)
	return tuple(unique_items)


def split_toml_value_and_comment(line: str) -> str:
	"""移除 TOML 行内注释，忽略字符串内部的 #。"""
	in_single_quote = False
	in_double_quote = False

	for index, char in enumerate(line):
		if char == '"' and not in_single_quote:
			is_escaped = index > 0 and line[index - 1] == "\\"
			if not is_escaped:
				in_double_quote = not in_double_quote
		elif char == "'" and not in_double_quote:
			in_single_quote = not in_single_quote
		elif char == "#" and not in_single_quote and not in_double_quote:
			return line[:index].rstrip()

	return line.rstrip()


def find_toml_delimiter(value: str, delimiter: str) -> int:
	"""在忽略字符串和数组内部内容时查找分隔符。"""
	in_single_quote = False
	in_double_quote = False
	array_depth = 0

	for index, char in enumerate(value):
		if char == '"' and not in_single_quote:
			is_escaped = index > 0 and value[index - 1] == "\\"
			if not is_escaped:
				in_double_quote = not in_double_quote
		elif char == "'" and not in_double_quote:
			in_single_quote = not in_single_quote
		elif not in_single_quote and not in_double_quote:
			if char == "[":
				array_depth += 1
			elif char == "]":
				array_depth = max(0, array_depth - 1)
			elif char == delimiter and array_depth == 0:
				return index

	return -1


def split_toml_array_items(raw_value: str) -> list[str]:
	"""按最小 TOML 规则拆分数组元素。"""
	items: list[str] = []
	current: list[str] = []
	in_single_quote = False
	in_double_quote = False
	array_depth = 0

	for index, char in enumerate(raw_value):
		if char == '"' and not in_single_quote:
			is_escaped = index > 0 and raw_value[index - 1] == "\\"
			if not is_escaped:
				in_double_quote = not in_double_quote
			current.append(char)
			continue

		if char == "'" and not in_double_quote:
			in_single_quote = not in_single_quote
			current.append(char)
			continue

		if not in_single_quote and not in_double_quote:
			if char == "[":
				array_depth += 1
			elif char == "]":
				array_depth = max(0, array_depth - 1)
			elif char == "," and array_depth == 0:
				item = "".join(current).strip()
				if not item:
					raise ArtifactUploadError("Minimal TOML parser does not support empty array items.")
				items.append(item)
				current = []
				continue

		current.append(char)

	last_item = "".join(current).strip()
	if last_item:
		items.append(last_item)
	return items


def parse_minimal_toml_value(raw_value: str) -> Any:
	"""解析 BuildTools 需要的最小 TOML 值类型。"""
	value = raw_value.strip()
	if not value:
		raise ArtifactUploadError("Minimal TOML parser received an empty value.")

	if value.startswith("["):
		if not value.endswith("]"):
			raise ArtifactUploadError(f"Minimal TOML parser found an unterminated array: {value!r}")
		inner = value[1:-1].strip()
		if not inner:
			return []
		return [parse_minimal_toml_value(item) for item in split_toml_array_items(inner)]

	if value.startswith('"'):
		try:
			return json.loads(value)
		except json.JSONDecodeError as exc:
			raise ArtifactUploadError(f"Minimal TOML parser failed to decode string: {value!r}") from exc

	if value.startswith("'") and value.endswith("'") and len(value) >= 2:
		return value[1:-1]

	if value in {"true", "false"}:
		return value == "true"

	numeric_value = value[1:] if value.startswith(("+", "-")) else value
	if numeric_value.isdigit():
		return int(value, 10)

	raise ArtifactUploadError(
		"Minimal TOML parser only supports strings, integers, booleans, and arrays. "
		f"Unsupported value: {value!r}"
	)


def ensure_toml_table(root: dict[str, Any], section_name: str) -> dict[str, Any]:
	"""按 section 路径创建并返回 TOML 表。"""
	section_parts = [part.strip() for part in section_name.split(".")]
	if not section_parts or any(not part for part in section_parts):
		raise ArtifactUploadError(f"Minimal TOML parser found an invalid table name: {section_name!r}")

	current: dict[str, Any] = root
	for part in section_parts:
		next_value = current.get(part)
		if next_value is None:
			next_table: dict[str, Any] = {}
			current[part] = next_table
			current = next_table
			continue
		if not isinstance(next_value, dict):
			raise ArtifactUploadError(
				"Minimal TOML parser cannot redefine a value as a table. "
				f"section={section_name!r}"
			)
		current = next_value
	return current


def load_minimal_toml(content: str) -> dict[str, Any]:
	"""解析 BuildTools 当前配置形状所需的最小 TOML 子集。"""
	result: dict[str, Any] = {}
	current_table = result

	for line_number, raw_line in enumerate(content.splitlines(), start=1):
		line = split_toml_value_and_comment(raw_line).strip()
		if not line:
			continue

		if line.startswith("["):
			if not line.endswith("]"):
				raise ArtifactUploadError(
					f"Minimal TOML parser found an unterminated table header at line {line_number}: {raw_line!r}"
				)
			section_name = line[1:-1].strip()
			current_table = ensure_toml_table(result, section_name)
			continue

		delimiter_index = find_toml_delimiter(line, "=")
		if delimiter_index < 0:
			raise ArtifactUploadError(
				f"Minimal TOML parser expected key/value assignment at line {line_number}: {raw_line!r}"
			)

		key = line[:delimiter_index].strip()
		if not key:
			raise ArtifactUploadError(f"Minimal TOML parser found an empty key at line {line_number}.")

		value = line[delimiter_index + 1 :].strip()
		current_table[key] = parse_minimal_toml_value(value)

	return result


def load_toml_file(path: Path) -> dict[str, Any]:
	"""读取 TOML 配置文件。"""
	if tomllib is not None:
		with path.open("rb") as handle:
			data = tomllib.load(handle)
	else:
		data = load_minimal_toml(path.read_text(encoding="utf-8"))
	if not isinstance(data, dict):
		raise ArtifactUploadError(f"Config root must be a TOML table: {path}")
	return data


def resolve_optional_config_path(
	config_path: str | os.PathLike[str] | None,
) -> Path | None:
	"""解析客户端要读取的 BuildTools 全局配置路径。"""
	raw_candidate = (
		str(config_path).strip()
		if config_path is not None
		else os.environ.get("ARTIFACT_FILE_SERVER_CONFIG", "").strip()
		or os.environ.get("ARTIFACT_SERVER_CONFIG", "").strip()
	)
	if raw_candidate:
		candidate = Path(raw_candidate).expanduser().resolve()
		if not candidate.exists():
			raise ArtifactUploadError(f"File server config does not exist: {candidate}")
		return candidate

	if DEFAULT_CONFIG_PATH.exists():
		return DEFAULT_CONFIG_PATH
	if DEFAULT_CONFIG_EXAMPLE_PATH.exists():
		return DEFAULT_CONFIG_EXAMPLE_PATH
	return None


def get_config_section(config_data: dict[str, Any], section_name: str) -> dict[str, Any]:
	"""安全读取 TOML 二级配置表。"""
	section = config_data.get(section_name, {}) if isinstance(config_data, dict) else {}
	if section in (None, ""):
		return {}
	if not isinstance(section, dict):
		raise ArtifactUploadError(f"Config section must be a TOML table: {section_name}")
	return section


def get_file_server_section(config_data: dict[str, Any]) -> dict[str, Any]:
	"""读取 BuildTools 全局配置中的文件服务器表。"""
	return get_config_section(config_data, "artifact_file_server")


def normalize_url_host(host: str) -> str:
	"""把服务端绑定地址转换成客户端可访问地址。"""
	normalized = host.strip()
	if not normalized or normalized in {"0.0.0.0", "::", "[::]"}:
		return "127.0.0.1"
	if ":" in normalized and not normalized.startswith("["):
		return f"[{normalized}]"
	return normalized


def normalize_base_url(base_url: str) -> str:
	"""规范化服务 URL。"""
	normalized = base_url.strip().rstrip("/")
	parsed = urlparse(normalized)
	if parsed.scheme not in {"http", "https"} or not parsed.netloc:
		raise ArtifactUploadError(
			"File server base URL must include scheme and host, "
			f"for example http://127.0.0.1:18080. got={base_url!r}"
		)
	return normalized


def resolve_base_url(
	file_server_section: dict[str, Any],
	*,
	server_url: str | None,
) -> str:
	"""解析文件服务器基地址。

	优先级：
	1. 调用方显式传入的完整 URL
	2. 环境变量里的完整 URL
	3. BuildTools 全局配置 `artifact_file_server.base_url`
	4. BuildTools 全局配置 / 环境变量里的客户端 IP + 端口
	5. 回退到本地默认地址 127.0.0.1:20001

	这里始终解析“客户端实际访问地址”，不再复用服务端目录下的 bind 配置。
	"""
	if server_url and server_url.strip():
		return normalize_base_url(server_url)

	env_server_url = os.environ.get("ARTIFACT_FILE_SERVER_URL", "").strip()
	if env_server_url:
		return normalize_base_url(env_server_url)

	configured_base_url = str(file_server_section.get("base_url") or "").strip()
	if configured_base_url:
		return normalize_base_url(configured_base_url)

	host = (
		os.environ.get("ARTIFACT_FILE_SERVER_IP", "").strip()
		or os.environ.get("ARTIFACT_FILE_SERVER_HOST", "").strip()
		or str(file_server_section.get("ip") or file_server_section.get("host") or "").strip()
		or "127.0.0.1"
	)
	port = (
		os.environ.get("ARTIFACT_FILE_SERVER_PORT", "").strip()
		or str(file_server_section.get("port") or "").strip()
		or "20001"
	)
	scheme = (
		os.environ.get("ARTIFACT_FILE_SERVER_SCHEME", "").strip()
		or str(file_server_section.get("scheme") or "").strip()
		or "http"
	)

	if not port:
		raise ArtifactUploadError("File server port is empty.")

	return normalize_base_url(f"{scheme}://{normalize_url_host(host)}:{port}")


def resolve_token(
	file_server_section: dict[str, Any],
	*,
	token: str | None,
) -> str | None:
	"""解析可选 Bearer Token。"""
	if token is not None:
		normalized = token.strip()
		return normalized or None

	for env_name in (
		"ARTIFACT_FILE_SERVER_TOKEN",
		"ARTIFACT_SERVER_TOKEN",
		"ARTIFACT_FILE_SERVER_TOKENS",
		"ARTIFACT_SERVER_TOKENS",
	):
		env_value = os.environ.get(env_name, "").strip()
		parsed_tokens = parse_token_values(env_value)
		if parsed_tokens:
			return parsed_tokens[0]

	configured_token = str(file_server_section.get("token") or "").strip()
	if configured_token:
		return configured_token

	parsed_tokens = parse_token_values(file_server_section.get("tokens"))
	if parsed_tokens:
		return parsed_tokens[0]
	return None


def resolve_chunk_size_bytes(
	file_server_section: dict[str, Any],
	*,
	env_name: str,
	config_key: str,
	default_value: int,
) -> int:
	"""读取块大小配置。"""
	raw_value = os.environ.get(env_name, "").strip()
	if raw_value:
		return int(raw_value) * 1024

	configured = file_server_section.get(config_key)
	if configured is not None:
		return int(configured) * 1024

	return default_value


def resolve_file_server_settings(
	*,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
) -> FileServerClientSettings:
	"""解析上传模块要使用的文件服务器配置。

	流程：
	1. 先定位要读取的 TOML 文件。
	2. 再从 BuildTools 全局配置里把“客户端访问地址”“鉴权 token”“上传块大小”统一解出来。
	3. 最终返回一个可复用的 settings，供多个上传动作共享。
	"""
	resolved_config_path = resolve_optional_config_path(config_path)
	config_data = load_toml_file(resolved_config_path) if resolved_config_path else {}
	file_server_section = get_file_server_section(config_data)

	base_url = resolve_base_url(file_server_section, server_url=server_url)
	resolved_token = resolve_token(file_server_section, token=token)
	upload_chunk_size_bytes = resolve_chunk_size_bytes(
		file_server_section,
		env_name="ARTIFACT_FILE_SERVER_CHUNK_SIZE_KB",
		config_key="upload_chunk_size_kb",
		default_value=DEFAULT_UPLOAD_CHUNK_SIZE_BYTES,
	)
	hash_chunk_size_bytes = resolve_chunk_size_bytes(
		file_server_section,
		env_name="ARTIFACT_FILE_SERVER_HASH_CHUNK_SIZE_KB",
		config_key="hash_chunk_size_kb",
		default_value=DEFAULT_HASH_CHUNK_SIZE_BYTES,
	)

	return FileServerClientSettings(
		base_url=base_url,
		token=resolved_token,
		config_path=resolved_config_path,
		upload_chunk_size_bytes=upload_chunk_size_bytes,
		hash_chunk_size_bytes=hash_chunk_size_bytes,
	)


def validate_path_segment(value: str, field_name: str) -> str:
	"""校验单个路径段。"""
	normalized = value.strip()
	if not normalized:
		raise ArtifactUploadError(f"{field_name} is empty.")
	if normalized in {".", ".."}:
		raise ArtifactUploadError(f"{field_name} cannot be '.' or '..': {value!r}")
	if any(char in normalized for char in "/\\"):
		raise ArtifactUploadError(f"{field_name} cannot contain path separators: {value!r}")
	if any(ord(char) < 32 for char in normalized):
		raise ArtifactUploadError(f"{field_name} contains control characters: {value!r}")
	return normalized


def normalize_relative_remote_path(
	relative_path: str | os.PathLike[str] | None,
) -> PurePosixPath | None:
	"""规范化可选远端相对路径。"""
	if relative_path is None:
		return None

	raw_text = str(relative_path).strip().replace("\\", "/")
	if not raw_text or raw_text == ".":
		return None

	path = PurePosixPath(raw_text)
	if path.is_absolute():
		raise ArtifactUploadError(
			f"Remote relative path must be relative, got absolute path: {relative_path!r}"
		)

	normalized_parts: list[str] = []
	for part in path.parts:
		if part in {"", "."}:
			continue
		if part == "..":
			raise ArtifactUploadError(
				f"Remote relative path cannot contain parent segments: {relative_path!r}"
			)
		normalized_parts.append(validate_path_segment(part, "remote_relative_path"))

	if not normalized_parts:
		return None
	return PurePosixPath(*normalized_parts)


def normalize_required_remote_path(remote_path: str | os.PathLike[str]) -> PurePosixPath:
	"""规范化必填远端路径。"""
	resolved_path = normalize_relative_remote_path(remote_path)
	if resolved_path is None:
		raise ArtifactUploadError(f"Remote path is empty: {remote_path!r}")
	return resolved_path


def coerce_artifact_type(artifact_type: ArtifactType | str) -> ArtifactType:
	"""把字符串转换成制品类型枚举。"""
	if isinstance(artifact_type, ArtifactType):
		return artifact_type

	normalized = str(artifact_type).strip().lower().replace("_", "-")
	try:
		return ArtifactType(normalized)
	except ValueError as exc:
		raise ArtifactUploadError(f"Unsupported artifact type: {artifact_type!r}") from exc


def build_artifact_remote_root(
	artifact_type: ArtifactType | str,
	*,
	build_number: str | int,
	platform: str | None = None,
) -> str:
	"""生成四类制品的远端根目录。"""
	resolved_type = coerce_artifact_type(artifact_type)
	build_label = validate_path_segment(str(build_number), "build_number")

	if resolved_type is ArtifactType.TABLE:
		return f"ClientRes_Table/{build_label}"

	platform_label = validate_path_segment(platform or "", "platform")
	if resolved_type is ArtifactType.CLIENT_PACKAGE:
		return f"ClientPackage_{platform_label}/{build_label}"
	if resolved_type is ArtifactType.CODE:
		return f"ClientRes_Code_{platform_label}/{build_label}"
	if resolved_type is ArtifactType.ASSET_BUNDLE:
		return f"ClientRes_Assetbundle_{platform_label}/{build_label}"

	raise ArtifactUploadError(f"Unsupported artifact type: {artifact_type!r}")


def build_artifact_remote_path(
	artifact_type: ArtifactType | str,
	*,
	build_number: str | int,
	platform: str | None = None,
	remote_relative_path: str | os.PathLike[str] | None = None,
) -> str:
	"""生成制品的完整远端路径。"""
	remote_root = PurePosixPath(
		build_artifact_remote_root(
			artifact_type,
			build_number=build_number,
			platform=platform,
		)
	)
	relative_path = normalize_relative_remote_path(remote_relative_path)
	return str(remote_root if relative_path is None else remote_root / relative_path)


def compute_file_sha256(file_path: Path, *, chunk_size_bytes: int) -> str:
	"""计算本地文件 SHA256。"""
	hash_object = hashlib.sha256()
	with file_path.open("rb") as handle:
		while True:
			chunk = handle.read(chunk_size_bytes)
			if not chunk:
				break
			hash_object.update(chunk)
	return hash_object.hexdigest()


def iter_source_files(source_path: Path) -> Iterator[tuple[Path, PurePosixPath]]:
	"""按稳定顺序遍历待上传文件。"""
	if source_path.is_file():
		yield source_path, PurePosixPath(source_path.name)
		return

	for file_path in sorted(source_path.rglob("*")):
		if not file_path.is_file() or file_path.name in SKIPPED_LOCAL_FILENAMES:
			continue
		relative_path = PurePosixPath(file_path.relative_to(source_path).as_posix())
		yield file_path, relative_path


def ensure_source_path(source_path: str | os.PathLike[str]) -> Path:
	"""解析并校验本地源路径。"""
	resolved = Path(source_path).expanduser().resolve()
	if not resolved.exists():
		raise ArtifactUploadError(f"Source path does not exist: {resolved}")
	if not resolved.is_file() and not resolved.is_dir():
		raise ArtifactUploadError(f"Source path must be a file or directory: {resolved}")
	return resolved


def build_request_path(base_url: str, remote_path: str, overwrite: bool | None) -> str:
	"""生成 API 请求路径。"""
	parsed = urlparse(base_url)
	base_path = parsed.path.rstrip("/")
	request_path = f"{base_path}/api/files/{quote(remote_path, safe='/')}"
	if overwrite is not None:
		request_path = f"{request_path}?{urlencode({'overwrite': 'true' if overwrite else 'false'})}"
	return request_path


def build_download_request_path(base_url: str, remote_path: str) -> str:
	"""生成文件下载请求路径。"""
	parsed = urlparse(base_url)
	base_path = parsed.path.rstrip("/")
	return f"{base_path}/files/{quote(remote_path, safe='/')}"


def create_http_connection(base_url: str, *, timeout_seconds: int) -> http.client.HTTPConnection:
	"""为目标服务创建 HTTP 连接。"""
	parsed = urlparse(base_url)
	if parsed.scheme == "https":
		return http.client.HTTPSConnection(parsed.hostname, parsed.port, timeout=timeout_seconds)
	if parsed.scheme == "http":
		return http.client.HTTPConnection(parsed.hostname, parsed.port, timeout=timeout_seconds)
	raise ArtifactUploadError(f"Unsupported file server URL scheme: {parsed.scheme!r}")


def parse_upload_response_body(response_body: bytes) -> dict[str, Any]:
	"""尽量把响应体解析成 JSON。"""
	if not response_body:
		return {}
	try:
		payload = json.loads(response_body.decode("utf-8"))
	except (UnicodeDecodeError, json.JSONDecodeError):
		return {}
	return payload if isinstance(payload, dict) else {}


def build_authorization_headers(settings: FileServerClientSettings) -> dict[str, str]:
	"""构建共享鉴权请求头。"""
	if not settings.token:
		return {}
	return {"Authorization": f"Bearer {settings.token}"}


def fetch_remote_metadata(
	*,
	remote_path: str,
	settings: FileServerClientSettings,
	timeout_seconds: int,
) -> tuple[int, dict[str, Any], bytes]:
	"""读取远端文件元数据。"""
	request_path = build_request_path(settings.base_url, remote_path, overwrite=None)
	connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
	try:
		connection.request("GET", request_path, headers=build_authorization_headers(settings))
		response = connection.getresponse()
		response_body = response.read()
	finally:
		connection.close()

	return response.status, parse_upload_response_body(response_body), response_body


def fetch_remote_download_sha256(
	*,
	remote_path: str,
	settings: FileServerClientSettings,
	timeout_seconds: int,
) -> tuple[int, str | None, int | None]:
	"""下载远端文件并计算 SHA256，用于上传失败后的恢复确认。"""
	request_path = build_download_request_path(settings.base_url, remote_path)
	connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
	try:
		connection.request("GET", request_path, headers=build_authorization_headers(settings))
		response = connection.getresponse()
		if response.status != 200:
			response.read()
			return response.status, None, None

		hasher = hashlib.sha256()
		total_size = 0
		while True:
			chunk = response.read(settings.hash_chunk_size_bytes)
			if not chunk:
				break
			total_size += len(chunk)
			hasher.update(chunk)
	finally:
		connection.close()

	return 200, hasher.hexdigest(), total_size


def try_recover_failed_upload(
	*,
	remote_path: str,
	local_path: Path,
	file_size: int,
	sha256: str,
	response_status: int,
	settings: FileServerClientSettings,
	timeout_seconds: int,
) -> tuple[UploadedArtifact | None, str]:
	"""当服务端返回 5xx 时，尝试确认文件是否其实已经成功落盘。"""
	metadata_status, metadata_payload, _ = fetch_remote_metadata(
		remote_path=remote_path,
		settings=settings,
		timeout_seconds=timeout_seconds,
	)
	if metadata_status != 200:
		return None, f"metadata status={metadata_status}"

	remote_size = int(metadata_payload.get("size") or file_size)
	resolved_remote_path = str(metadata_payload.get("path") or remote_path)
	remote_sha256 = str(metadata_payload.get("sha256") or "").strip()
	integrity_status = (
		str(metadata_payload.get("integrity_status"))
		if metadata_payload.get("integrity_status") is not None
		else None
	)

	if remote_size != file_size:
		return None, f"metadata size_mismatch expected={file_size} actual={remote_size}"

	if remote_sha256:
		if remote_sha256 != sha256:
			return None, f"metadata sha256_mismatch expected={sha256} actual={remote_sha256}"
		return UploadedArtifact(
			local_path=local_path,
			remote_path=resolved_remote_path,
			size=remote_size,
			sha256=remote_sha256,
			status_code=response_status,
			integrity_status=integrity_status,
		), "metadata verified"

	download_status, downloaded_sha256, downloaded_size = fetch_remote_download_sha256(
		remote_path=remote_path,
		settings=settings,
		timeout_seconds=timeout_seconds,
	)
	if download_status != 200:
		return None, f"download status={download_status}"
	if downloaded_size != file_size:
		return None, f"download size_mismatch expected={file_size} actual={downloaded_size}"
	if downloaded_sha256 != sha256:
		return None, f"download sha256_mismatch expected={sha256} actual={downloaded_sha256}"

	return UploadedArtifact(
		local_path=local_path,
		remote_path=resolved_remote_path,
		size=remote_size,
		sha256=sha256,
		status_code=response_status,
		integrity_status=integrity_status,
	), "download verified"


def upload_single_file(
	local_path: str | os.PathLike[str],
	*,
	remote_path: str,
	settings: FileServerClientSettings,
	overwrite: bool | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> UploadedArtifact:
	"""上传单个文件到文件服务器。

	流程：
	1. 校验本地文件和远端路径是否合法。
	2. 先在本地计算 SHA256，后续会同时用于请求头和结果校验。
	3. 使用 PUT 流式上传，避免把大文件整包读入内存。
	4. 读取服务端返回的元数据，统一转换成 UploadedArtifact。
	"""
	resolved_local_path = ensure_source_path(local_path)
	if not resolved_local_path.is_file():
		raise ArtifactUploadError(f"Single-file upload requires a file path: {resolved_local_path}")

	resolved_remote_path = str(normalize_required_remote_path(remote_path))

	# 先做本地摘要，确保请求头和最终返回结果都基于同一份文件内容。
	sha256 = compute_file_sha256(
		resolved_local_path,
		chunk_size_bytes=settings.hash_chunk_size_bytes,
	)
	request_path = build_request_path(settings.base_url, resolved_remote_path, overwrite)
	content_type = mimetypes.guess_type(resolved_local_path.name)[0] or "application/octet-stream"
	file_size = resolved_local_path.stat().st_size

	headers = {
		"Content-Length": str(file_size),
		"Content-Type": content_type,
		"X-Checksum-Sha256": sha256,
	}
	headers.update(build_authorization_headers(settings))

	connection = create_http_connection(settings.base_url, timeout_seconds=timeout_seconds)
	try:
		# 这里故意走原生 PUT + chunk send，保证大文件上传时内存占用可控。
		connection.putrequest("PUT", request_path)
		for key, value in headers.items():
			connection.putheader(key, value)
		connection.endheaders()

		with resolved_local_path.open("rb") as handle:
			while True:
				chunk = handle.read(settings.upload_chunk_size_bytes)
				if not chunk:
					break
				connection.send(chunk)

		response = connection.getresponse()
		response_body = response.read()
	finally:
		connection.close()

	payload = parse_upload_response_body(response_body)
	if response.status != 201:
		message = payload.get("detail") or response_body.decode("utf-8", errors="replace")
		if response.status >= 500:
			recovered_artifact, recovery_check = try_recover_failed_upload(
				remote_path=resolved_remote_path,
				local_path=resolved_local_path,
				file_size=file_size,
				sha256=sha256,
				response_status=response.status,
				settings=settings,
				timeout_seconds=timeout_seconds,
			)
			if recovered_artifact is not None:
				return recovered_artifact
			message = f"{message}, recovery_check={recovery_check}"
		raise ArtifactUploadError(
			"File upload failed. "
			f"local={resolved_local_path}, remote={resolved_remote_path}, "
			f"status={response.status}, detail={message}"
		)

	return UploadedArtifact(
		local_path=resolved_local_path,
		remote_path=str(payload.get("path") or resolved_remote_path),
		size=int(payload.get("size") or file_size),
		sha256=str(payload.get("sha256") or sha256),
		status_code=response.status,
		integrity_status=str(payload.get("integrity_status")) if payload.get("integrity_status") is not None else None,
	)


def upload_to_remote_root(
	source_path: str | os.PathLike[str],
	*,
	remote_root: str,
	settings: FileServerClientSettings,
	remote_relative_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""把本地文件或目录上传到指定远端根目录。

	流程：
	1. 先确定 source 是单文件还是目录。
	2. 单文件时直接映射到目标根目录下。
	3. 目录时保留原有相对结构，整体平移到远端根目录。
	4. 每个文件最终都复用 upload_single_file，避免两套上传逻辑分叉。
	"""
	resolved_source_path = ensure_source_path(source_path)
	resolved_remote_root = normalize_relative_remote_path(remote_root)
	if resolved_remote_root is None:
		raise ArtifactUploadError("remote_root is empty.")
	resolved_relative_remote_path = normalize_relative_remote_path(remote_relative_path)

	results: list[UploadedArtifact] = []
	if resolved_source_path.is_file():
		# 单文件允许调用方额外指定远端文件名；未指定时保留本地文件名。
		file_relative_path = resolved_relative_remote_path or PurePosixPath(resolved_source_path.name)
		remote_path = str(resolved_remote_root / file_relative_path)
		if on_uploading is not None:
			on_uploading(1, 1, resolved_source_path, remote_path)
		uploaded = upload_single_file(
			resolved_source_path,
			remote_path=remote_path,
			settings=settings,
			overwrite=overwrite,
			timeout_seconds=timeout_seconds,
		)
		results.append(uploaded)
		if on_uploaded is not None:
			on_uploaded(1, 1, uploaded)
		return results

	upload_plan = list(iter_source_files(resolved_source_path))
	if not upload_plan:
		raise ArtifactUploadError(f"No files found to upload: {resolved_source_path}")

	# 目录上传时保留相对目录结构，只是在最前面拼接统一的 artifact root。
	total_files = len(upload_plan)
	for index, (local_file_path, local_relative_path) in enumerate(upload_plan, start=1):
		remote_file_path = resolved_remote_root
		if resolved_relative_remote_path is not None:
			remote_file_path /= resolved_relative_remote_path
		remote_file_path /= local_relative_path
		resolved_remote_file_path = str(remote_file_path)
		if on_uploading is not None:
			on_uploading(index, total_files, local_file_path, resolved_remote_file_path)
		uploaded = upload_single_file(
			local_file_path,
			remote_path=resolved_remote_file_path,
			settings=settings,
			overwrite=overwrite,
			timeout_seconds=timeout_seconds,
		)
		results.append(uploaded)
		if on_uploaded is not None:
			on_uploaded(index, total_files, uploaded)

	return results


def upload_artifact(
	source_path: str | os.PathLike[str],
	*,
	artifact_type: ArtifactType | str,
	build_number: str | int,
	platform: str | None = None,
	remote_relative_path: str | os.PathLike[str] | None = None,
	settings: FileServerClientSettings | None = None,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""通用制品上传入口。

	这个函数只负责两件事：
	1. 解析本次上传要使用的共享 settings。
	2. 根据制品类型生成远端根目录，再交给 upload_to_remote_root 执行。
	"""
	resolved_settings = settings or resolve_file_server_settings(
		server_url=server_url,
		token=token,
		config_path=config_path,
	)
	remote_root = build_artifact_remote_root(
		artifact_type,
		build_number=build_number,
		platform=platform,
	)
	return upload_to_remote_root(
		source_path,
		remote_root=remote_root,
		remote_relative_path=remote_relative_path,
		settings=resolved_settings,
		overwrite=overwrite,
		on_uploading=on_uploading,
		on_uploaded=on_uploaded,
		timeout_seconds=timeout_seconds,
	)


def upload_client_package(
	source_path: str | os.PathLike[str],
	*,
	platform: str,
	build_number: str | int,
	remote_relative_path: str | os.PathLike[str] | None = None,
	settings: FileServerClientSettings | None = None,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""上传母包制品。"""
	return upload_artifact(
		source_path,
		artifact_type=ArtifactType.CLIENT_PACKAGE,
		platform=platform,
		build_number=build_number,
		remote_relative_path=remote_relative_path,
		settings=settings,
		server_url=server_url,
		token=token,
		config_path=config_path,
		overwrite=overwrite,
		on_uploading=on_uploading,
		on_uploaded=on_uploaded,
		timeout_seconds=timeout_seconds,
	)


def upload_code(
	source_path: str | os.PathLike[str],
	*,
	platform: str,
	build_number: str | int,
	remote_relative_path: str | os.PathLike[str] | None = None,
	settings: FileServerClientSettings | None = None,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""上传代码制品。"""
	return upload_artifact(
		source_path,
		artifact_type=ArtifactType.CODE,
		platform=platform,
		build_number=build_number,
		remote_relative_path=remote_relative_path,
		settings=settings,
		server_url=server_url,
		token=token,
		config_path=config_path,
		overwrite=overwrite,
		on_uploading=on_uploading,
		on_uploaded=on_uploaded,
		timeout_seconds=timeout_seconds,
	)


def upload_asset_bundle(
	source_path: str | os.PathLike[str],
	*,
	platform: str,
	build_number: str | int,
	remote_relative_path: str | os.PathLike[str] | None = None,
	settings: FileServerClientSettings | None = None,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""上传 AssetBundle 制品。"""
	return upload_artifact(
		source_path,
		artifact_type=ArtifactType.ASSET_BUNDLE,
		platform=platform,
		build_number=build_number,
		remote_relative_path=remote_relative_path,
		settings=settings,
		server_url=server_url,
		token=token,
		config_path=config_path,
		overwrite=overwrite,
		on_uploading=on_uploading,
		on_uploaded=on_uploaded,
		timeout_seconds=timeout_seconds,
	)


def upload_table(
	source_path: str | os.PathLike[str],
	*,
	build_number: str | int,
	remote_relative_path: str | os.PathLike[str] | None = None,
	settings: FileServerClientSettings | None = None,
	server_url: str | None = None,
	token: str | None = None,
	config_path: str | os.PathLike[str] | None = None,
	overwrite: bool | None = None,
	on_uploading: UploadStartCallback | None = None,
	on_uploaded: UploadCompleteCallback | None = None,
	timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS,
) -> list[UploadedArtifact]:
	"""上传表格制品。"""
	return upload_artifact(
		source_path,
		artifact_type=ArtifactType.TABLE,
		build_number=build_number,
		remote_relative_path=remote_relative_path,
		settings=settings,
		server_url=server_url,
		token=token,
		config_path=config_path,
		overwrite=overwrite,
		on_uploading=on_uploading,
		on_uploaded=on_uploaded,
		timeout_seconds=timeout_seconds,
	)


def parse_cli_args() -> argparse.Namespace:
	"""解析命令行参数。"""
	parser = argparse.ArgumentParser(
		description="Upload build artifacts to .test-DevOps/GameFileServer.",
	)
	parser.add_argument("source", help="Local file or directory to upload.")
	parser.add_argument(
		"--artifact-type",
		required=True,
		choices=[artifact_type.value for artifact_type in ArtifactType],
		help="Artifact classification for remote path generation.",
	)
	parser.add_argument(
		"--platform",
		default=None,
		help="Required for client-package / code / asset-bundle uploads.",
	)
	parser.add_argument(
		"--build-number",
		required=True,
		help="Build number or version directory segment.",
	)
	parser.add_argument(
		"--remote-relative-path",
		default=None,
		help="Optional extra path under the artifact root. For a single file it can also rename the uploaded file.",
	)
	parser.add_argument(
		"--server-url",
		default=None,
		help="Optional file server base URL. Defaults to local fileserver.toml or ARTIFACT_FILE_SERVER_URL.",
	)
	parser.add_argument(
		"--token",
		default=None,
		help="Optional bearer token override.",
	)
	parser.add_argument(
		"--config",
		default=None,
		help="Optional path to BuildTools global config TOML.",
	)
	parser.add_argument(
		"--overwrite",
		choices=["true", "false"],
		default=None,
		help="Optional overwrite policy. If omitted, use the server default.",
	)
	parser.add_argument(
		"--timeout-seconds",
		type=int,
		default=DEFAULT_TIMEOUT_SECONDS,
		help="HTTP timeout per file upload.",
	)
	return parser.parse_args()


def main() -> int:
	"""命令行入口。

	CLI 本身不承载上传细节，真正的流程都复用 upload_artifact，
	这样脚本调用和模块调用看到的是同一套行为。
	"""
	args = parse_cli_args()
	overwrite = None if args.overwrite is None else args.overwrite == "true"
	results = upload_artifact(
		args.source,
		artifact_type=args.artifact_type,
		platform=args.platform,
		build_number=args.build_number,
		remote_relative_path=args.remote_relative_path,
		server_url=args.server_url,
		token=args.token,
		config_path=args.config,
		overwrite=overwrite,
		timeout_seconds=args.timeout_seconds,
	)
	print(f"uploaded_files={len(results)}")
	for result in results:
		print(
			f"- {result.local_path} -> {result.remote_path} "
			f"size={result.size} sha256={result.sha256} integrity={result.integrity_status or 'unknown'}"
		)
	return 0


if __name__ == "__main__":
	try:
		raise SystemExit(main())
	except ArtifactUploadError as exc:
		print(f"[ArtifactUploader][ERROR] {exc}")
		raise SystemExit(2)